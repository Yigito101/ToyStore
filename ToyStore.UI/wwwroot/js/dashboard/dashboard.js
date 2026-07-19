// --- YAŞAM DÖNGÜSÜ ---
document.addEventListener("DOMContentLoaded", async () => {
    const currentRole = window.getUserRole();

    if (currentRole === window.AppRoles.ADMIN || currentRole === window.AppRoles.MANAGER) {
        await loadAdminDashboard();
    } else {
        await loadUserDashboard();
    }
});

// --- ADMIN / MANAGER VERİ AKIŞI ---
async function loadAdminDashboard() {
    try {
        // Tamamen bağımsız yerel metotlardan verileri çekiyoruz (404 İhtimali Sıfırlandı)
        const products = await DashboardApi.fetchRawProducts();
        const categories = await DashboardApi.fetchRawCategories();
        // Dashboard.js içindeki loadAdminDashboard metodunda kullanıcıları çeken satır:
        const users = await UserApi.getAll(); // Tertemiz, standart ve dinamik döngü!

        // 1. Ürün Hesaplamaları
        const totalProducts = products.length;
        const activeProducts = products.filter(p => p.isActive === true || p.IsActive === true).length;
        const passiveProducts = totalProducts - activeProducts;

        // 2. Kategori Hesaplamaları
        const totalCategories = categories.length;
        const activeCategories = categories.filter(c => c.isActive === true || c.IsActive === true).length;
        const passiveCategories = totalCategories - activeCategories;

        // 3. Kullanıcı Hesaplamaları
        const totalUsers = users.length;
        const activeUsers = users.filter(u => u.isActive === true || u.IsActive === true).length;
        const passiveUsers = totalUsers - activeUsers;

        // 4. Favori Hesaplamaları
        const favProductsList = products.filter(p => p.isFavorite === true || p.IsFavorite === true);
        const totalFavoriteProducts = favProductsList.length;
        const activeFavoriteProducts = favProductsList.filter(p => p.isActive === true || p.IsActive === true).length;
        const passiveFavoriteProducts = totalFavoriteProducts - activeFavoriteProducts;

        const favCategoriesList = categories.filter(c => c.isFavorite === true || c.IsFavorite === true);
        const totalFavoriteCategories = favCategoriesList.length;
        const activeFavoriteCategories = favCategoriesList.filter(c => c.isActive === true || c.IsActive === true).length;
        const passiveFavoriteCategories = totalFavoriteCategories - activeFavoriteCategories;

        // 5. Kritik Stok Ürünleri
        const criticalStockProducts = products.filter(p =>
            (p.isActive === true || p.IsActive === true) &&
            (p.stock !== undefined ? p.stock : p.Stock || 0) <= 10
        );

        const statsData = {
            totalProducts, activeProducts, passiveProducts,
            totalCategories, activeCategories, passiveCategories,
            totalUsers, activeUsers, passiveUsers,
            totalFavoriteProducts, activeFavoriteProducts, passiveFavoriteProducts,
            totalFavoriteCategories, activeFavoriteCategories, passiveFavoriteCategories,
            criticalStockProducts
        };

        DashboardUI.renderAdminDashboard(statsData);

    } catch (error) {
        console.error("Admin Dashboard canlı hesaplama hatası:", error);
    }
}

// --- STANDART KULLANICI VERİ AKIŞI ---
async function loadUserDashboard() {
    try {
        const products = await DashboardApi.fetchRawProducts();
        const categories = await DashboardApi.fetchRawCategories();

        const favProductsList = products.filter(p => p.isFavorite === true || p.IsFavorite === true);
        const totalFavoriteProducts = favProductsList.length;
        const activeFavoriteProducts = favProductsList.filter(p => p.isActive === true || p.IsActive === true).length;
        const passiveFavoriteProducts = totalFavoriteProducts - activeFavoriteProducts;

        const favCategoriesList = categories.filter(c => c.isFavorite === true || c.IsFavorite === true);
        const totalFavoriteCategories = favCategoriesList.length;
        const activeFavoriteCategories = favCategoriesList.filter(c => c.isActive === true || c.IsActive === true).length;
        const passiveFavoriteCategories = totalFavoriteCategories - activeFavoriteCategories;

        const userData = {
            totalFavoriteProducts, activeFavoriteProducts, passiveFavoriteProducts,
            totalFavoriteCategories, activeFavoriteCategories, passiveFavoriteCategories
        };

        DashboardUI.renderUserDashboard(userData);
    } catch (error) {
        console.error("User Dashboard canlı hesaplama hatası:", error);
    }
}

// --- KÜRESEL DASHBOARD YENİLEME KISAYOLU ---
window.refreshDashboardData = async () => {
    const container = document.getElementById("dashboardContainer");
    if (container) {
        container.innerHTML = `
            <div class="text-center text-gray-500 py-10">
                <span class="animate-pulse flex items-center justify-center gap-2">🔄 Veriler güncelleniyor...</span>
            </div>
        `;
    }
    await loadAdminDashboard();
};

// --- SENİN EFSANE FİKRİN: HIZLI STOK GÜNCELLEME MOTORU ---
// --- SENİN EFSANE FİKRİN: STANDART PUT ENDPOINT UYUMLU STOK GÜNCELLEME MOTORU ---
window.quickStockUpdate = async (productId, productName, currentStock) => {
    // 1. Kullanıcıdan yeni stok miktarını alıyoruz
    const newStockStr = prompt(`[${productName}] için yeni fiziksel stok miktarını giriniz:`, currentStock);

    if (newStockStr === null || newStockStr.trim() === "") return;

    const newStock = parseInt(newStockStr, 10);

    if (isNaN(newStock) || newStock < 0) {
        if (typeof NotificationUI === "object") {
            NotificationUI.show("Lütfen geçerli ve pozitif bir stok adedi giriniz!", "error");
        } else {
            alert("Hata: Geçerli bir stok adedi girmelisiniz!");
        }
        return;
    }

    try {
        // 2. Adım: Ürünün mevcut tüm bilgilerini backend'den GET ile çekiyoruz (PUT şeması kırılmasın diye)
        const getResponse = await window.apiFetch(`/Products/${productId}`);
        if (!getResponse || !getResponse.ok) {
            if (typeof NotificationUI === "object") {
                NotificationUI.show("Ürün bilgileri backend'den doğrulanamadı!", "error");
            }
            return;
        }

        const result = await getResponse.json();
        // Backend DTO yapısına göre ham datayı ayıklıyoruz
        const productDto = result.data || result.Data || result;

        // 3. Adım: Nesne içerisindeki stok alanını kullanıcının girdiği yeni değerle güncelliyoruz
        if (productDto.hasOwnProperty("stock")) productDto.stock = newStock;
        if (productDto.hasOwnProperty("Stock")) productDto.Stock = newStock;

        // 4. Adım: Backend'deki orijinal "PUT /api/Products/{id}" endpoint'ine güncel paketi basıyoruz
        const putResponse = await window.apiFetch(`/Products/${productId}`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(productDto)
        });

        if (putResponse && putResponse.ok) {
            if (typeof NotificationUI === "object") {
                NotificationUI.show("Stok başarıyla güncellendi ve envanter eşitlendi!", "success");
            }

            // Dashboard ekranını ve ilerleme çubuklarını anında canlı olarak tazeleyelim
            await window.refreshDashboardData();
        } else {
            if (typeof NotificationUI === "object") {
                NotificationUI.show("Stok güncellenirken backend reddetti (Validasyon Hatası)!", "error");
            }
        }
    } catch (error) {
        console.error("Hızlı stok güncellenirken mimari hatası:", error);
    }
};
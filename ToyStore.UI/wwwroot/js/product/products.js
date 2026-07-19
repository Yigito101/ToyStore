// --- GLOBAL STATE (Orkestratörün Hafızası) ---
let allProductsData = [];
let currentSortColumn = '';
let isAscending = true;
let currentStatusFilter = window.AppStatuses.ALL;
window.currentUserRole = typeof window.getUserRole === 'function' ? window.getUserRole() : window.AppRoles.USER;

// --- YAŞAM DÖNGÜSÜ ---
document.addEventListener("DOMContentLoaded", async () => {
    const currentRole = window.getUserRole();

    if (typeof ProductUI !== 'undefined' && ProductUI.applyRoleBasedUI) {
        ProductUI.applyRoleBasedUI(currentRole);
    }

    await loadCategoriesDropdown();
    await loadProducts();
});

// --- VERİ YÖNETİMİ (API Köprüsü) ---
async function loadProducts() {
    try {
        allProductsData = await ProductApi.getAll(currentStatusFilter);
        applyFiltersAndRender();
    } catch (error) {
        console.error("Ürünler çekilemedi:", error);
    }
}

async function loadCategoriesDropdown() {
    try {
        const response = await ProductApi.getCategoriesDropdown();
        if (response.ok) {
            const categories = await response.json();
            ProductUI.populateCategoryDropdown(categories);
        }
    } catch (error) {
        console.error("Kategoriler dropdown için çekilemedi:", error);
    }
}

// --- FİLTRELEME & SIRALAMA MOTORU ---
window.applyFiltersAndRender = function () {
    const searchInput = document.getElementById("searchInput");
    const searchQuery = searchInput ? searchInput.value.toLowerCase().trim() : "";

    let filteredProducts = allProductsData.filter(product => {
        const isProductActive = product.isActive === true || product.IsActive === true;

        if (currentStatusFilter === window.AppStatuses.ACTIVE && !isProductActive) return false;
        if (currentStatusFilter === window.AppStatuses.INACTIVE && isProductActive) return false;

        const idMatch = product.id.toString().includes(searchQuery);
        const nameMatch = (product.name || "").toLowerCase().includes(searchQuery);
        const catMatch = (product.categoryName || "").toLowerCase().includes(searchQuery);

        return idMatch || nameMatch || catMatch;
    });

    if (currentSortColumn !== '') {
        filteredProducts.sort((a, b) => {
            let valA = a[currentSortColumn];
            let valB = b[currentSortColumn];

            if (currentSortColumn === 'isActive') {
                valA = (a.isActive === true || a.IsActive === true) ? 1 : 0;
                valB = (b.isActive === true || b.IsActive === true) ? 1 : 0;
            } else if (typeof valA === 'string') {
                valA = valA.toLowerCase();
                valB = valB.toLowerCase();
            }

            if (valA < valB) return isAscending ? -1 : 1;
            if (valA > valB) return isAscending ? 1 : -1;
            return 0;
        });
    }

    ProductUI.updateSortIcons(currentSortColumn, isAscending);
    PaginationHelper.init(filteredProducts, (paginated) => {
        ProductUI.renderTable(paginated, window.currentUserRole);
    });
};

// --- OLAY YÖNETİCİLERİ (Event Handlers) ---
window.filterByStatus = (status) => { currentStatusFilter = status; applyFiltersAndRender(); };
window.searchProducts = () => applyFiltersAndRender();
window.sortBy = (columnName) => {
    if (currentSortColumn === columnName) isAscending = !isAscending;
    else { currentSortColumn = columnName; isAscending = true; }
    applyFiltersAndRender();
};
window.resetSorting = () => {
    currentSortColumn = '';
    isAscending = true;
    currentStatusFilter = window.AppStatuses.ALL;
    const searchInput = document.getElementById("searchInput");
    if (searchInput) searchInput.value = "";
    applyFiltersAndRender();
};

window.handleToggleFavorite = async (id) => {
    try {
        const response = await ProductApi.toggleFavorite(id);
        if (response.ok) {
            await loadProducts();
        }
    } catch (error) {
        console.error("Favori işlemi hatası:", error);
    }
};

window.handleToggleStatus = async (id, isActive) => {
    const actionText = isActive ? 'pasif duruma getirmek (silmek)' : 'tekrar aktif etmek';

    // Hibrit Yapı Çözüldü: Eski confirm yerine modern zırh entegre edildi
    window.NotificationUI.confirm(`Bu ürünü ${actionText} istediğinize emin misiniz?`, async () => {
        try {
            let response = isActive ? await ProductApi.delete(id) : await ProductApi.restore(id);
            if (response.ok) {
                await loadProducts();
            }
        } catch (error) {
            console.error("Beklenmedik hata:", error);
        }
    });
};

window.handleEditProduct = (product) => {
    document.getElementById("originalProductName").value = product.name;
    document.getElementById("originalProductCategoryId").value = product.categoryId;
    document.getElementById("originalProductPrice").value = product.price;
    document.getElementById("originalProductStock").value = product.stock;

    const isProdActive = product.isActive === true || product.IsActive === true;
    document.getElementById("originalProductIsActive").value = isProdActive ? "true" : "false";

    ProductUI.openModal(product);
};

window.saveProduct = async (e) => {
    e.preventDefault();

    const id = parseInt(document.getElementById("productId").value) || 0;
    const name = document.getElementById("productName").value.trim();
    const price = parseFloat(document.getElementById("productPrice").value);
    const stock = parseInt(document.getElementById("productStock").value, 10);
    const categoryId = parseInt(document.getElementById("productCategory").value, 10);
    const rowVersion = document.getElementById("productRowVersion").value;

    if (id > 0) {
        const origName = document.getElementById("originalProductName").value;
        const origCategoryId = parseInt(document.getElementById("originalProductCategoryId").value, 10);
        const origPrice = parseFloat(document.getElementById("originalProductPrice").value);
        const origStock = parseInt(document.getElementById("originalProductStock").value, 10);

        if (name === origName && categoryId === origCategoryId && price === origPrice && stock === origStock) {
            window.NotificationUI.show("Hiçbir değişiklik yapmadınız.", "warning");
            return;
        }
    }

    const data = {
        name: name,
        price: price,
        stock: stock,
        categoryId: categoryId,
        rowVersion: rowVersion
    };

    try {
        const response = await ProductApi.save(data, id);
        if (response.ok) {
            ProductUI.closeModal();
            await loadProducts();
        }
    } catch (error) {
        console.error("Ürün kayıt hatası:", error);
    }
};

window.openNewProductModal = () => {
    ProductUI.openModal(null);
};
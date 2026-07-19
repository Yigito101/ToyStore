// --- GLOBAL STATE ---
let allCategoriesData = [];
let currentFilter = window.AppStatuses.ALL;
let currentSort = { column: null, desc: false };

// --- YAŞAM DÖNGÜSÜ (Başlangıç Ayarları) ---
document.addEventListener("DOMContentLoaded", async () => {
    const currentRole = window.getUserRole();
    if (typeof CategoryUI !== 'undefined' && CategoryUI.applyRoleBasedUI) {
        CategoryUI.applyRoleBasedUI(currentRole);
    }
    await loadCategories();
});

// --- FİLTRELERİ SIFIRLAMA ---
window.resetSorting = function () {
    currentSort = { column: null, desc: false };
    const searchInput = document.getElementById('searchInput');
    if (searchInput) searchInput.value = '';
    currentFilter = window.AppStatuses.ALL;
    applyFiltersAndRender();
};

// --- API VERİ YÜKLEME KÖPRÜSÜ ---
async function loadCategories() {
    try {
        CategoryUI.showLoading(true);
        const response = await CategoryApi.getAll(currentFilter);
        const data = await response.json();
        allCategoriesData = data.data || data;
        applyFiltersAndRender();
    } catch (error) {
        console.error("Veri yükleme hatası:", error);
    } finally {
        CategoryUI.showLoading(false);
    }
}

// --- FİLTRELEME & SIRALAMA MOTORU ---
window.applyFiltersAndRender = function () {
    let filtered = allCategoriesData.filter(cat => {
        const isCatActive = cat.isActive === true || cat.IsActive === true;
        const matchesStatus = currentFilter === window.AppStatuses.ALL ||
            (currentFilter === window.AppStatuses.ACTIVE && isCatActive) ||
            (currentFilter === window.AppStatuses.INACTIVE && !isCatActive);

        const query = document.getElementById('searchInput')?.value.toLowerCase().trim() || "";
        const matchesSearch = cat.name.toLowerCase().includes(query) || cat.id.toString().includes(query);

        return matchesStatus && matchesSearch;
    });

    filtered.sort((a, b) => {
        if (!currentSort.column) return 0;
        let valA = a[currentSort.column], valB = b[currentSort.column];
        if (typeof valA === 'string') { valA = valA.toLowerCase(); valB = valB.toLowerCase(); }
        return (valA < valB) ? (currentSort.desc ? 1 : -1) : (currentSort.desc ? -1 : 1);
    });

    const currentRole = window.getUserRole();
    const actionsHeader = document.getElementById("actionsColumnHeader");
    if (actionsHeader) {
        if (currentRole !== window.AppRoles.ADMIN) {
            actionsHeader.classList.add('hidden');
        } else {
            actionsHeader.classList.remove('hidden');
        }
    }

    CategoryUI.updateSortIcons(currentSort);
    PaginationHelper.init(filtered, (paginated) => {
        CategoryUI.renderTable(paginated, currentRole);
    });
};

// --- BUTON VE OLAY YÖNETİCİLERİ (Event Handlers) ---
window.filterByStatus = (status) => { currentFilter = status; applyFiltersAndRender(); };
window.searchCategories = () => applyFiltersAndRender();
window.sortBy = (column) => {
    currentSort.desc = (currentSort.column === column) ? !currentSort.desc : false;
    currentSort.column = column;
    applyFiltersAndRender();
};

// 1. Favori Tetikleyici (Kalıntı alert'ler temizlendi)
window.handleToggleFavorite = async (categoryId) => {
    try {
        const response = await CategoryApi.toggleFavorite(categoryId);
        if (response.ok) {
            await loadCategories();
        }
    } catch (error) {
        console.error("Favori işlemi hatası:", error);
    }
};

// 2. Durum Değiştirme (Kalıntı confirm temizlendi, modern zırh eklendi)
window.handleToggleStatus = async (id, isCurrentlyActive) => {
    const actionText = isCurrentlyActive ? "pasif etmek" : "aktif etmek";

    window.NotificationUI.confirm(`Bu kategoriyi ${actionText} istediğinizden emin misiniz?`, async () => {
        try {
            let response = isCurrentlyActive ? await CategoryApi.delete(id) : await CategoryApi.restore(id);
            if (response.ok) {
                await loadCategories();
            }
        } catch (error) {
            console.error("Beklenmedik hata:", error);
        }
    });
};

// 3. Düzenleme Modu Başlatıcı
window.handleEditCategory = (category) => {
    document.getElementById("originalCategoryName").value = category.name;
    const isCatActive = category.isActive === true || category.IsActive === true;
    document.getElementById("originalCategoryIsActive").value = isCatActive ? "true" : "false";
    CategoryUI.openModal(category);
};

// 4. Kategori Kaydetme (Sıfır Ürün / Otomatik Pasif Kuralı Entegre Edildi)
window.saveCategory = async function (e) {
    e.preventDefault();

    const id = parseInt(document.getElementById('categoryId').value) || 0;
    const rowVersion = document.getElementById('categoryRowVersion').value;
    const name = document.getElementById('categoryName').value.trim();
    const isEdit = id > 0;

    if (isEdit) {
        const origName = document.getElementById("originalCategoryName").value;
        if (name === origName) {
            window.NotificationUI.show("Hiçbir değişiklik yapmadınız.", "warning");
            return;
        }
    }

    // YENİ MİMARİ KURAL: Sıfır ürünle açılan yeni kategori otomatik PASİF (isActive: false) gönderilir[cite: 1].
    // Düzenleme modunda ise eski statüsü korunur.
    let targetActiveStatus = false;
    if (isEdit) {
        targetActiveStatus = document.getElementById("originalCategoryIsActive").value === "true";
    }

    const data = {
        id: id,
        name: name,
        isActive: targetActiveStatus, // Backend DTO ile tam uyum[cite: 1]
        rowVersion: rowVersion
    };

    try {
        const response = await CategoryApi.save(data, isEdit);
        if (response.ok) {
            CategoryUI.closeModal();
            await loadCategories();
        }
    } catch (error) {
        console.error("Kategori kayıt hatası:", error);
    }
};
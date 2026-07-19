const ProductApi = {
    // SIFIR HARDCODED: 'All' yerine merkezi enum yapısı varsayılan değer olarak atandı
    getAll: async (status = window.AppStatuses.ALL) => {
        // Merkezi pagination konfigürasyonu korunuyor
        const pageSize = window.AppConfig?.Pagination?.MaxSize || 50;
        let currentPage = 1;
        let allProducts = [];

        let response = await window.apiFetch(API_ROUTES.PRODUCTS.LIST(status, currentPage, pageSize));
        if (!response.ok) throw new Error("Ürünler getirilemedi.");

        let result = await response.json();
        allProducts = result.data || [];

        if (result.totalPages > 1) {
            for (currentPage = 2; currentPage <= result.totalPages; currentPage++) {
                let nextResponse = await window.apiFetch(API_ROUTES.PRODUCTS.LIST(status, currentPage, pageSize));
                if (nextResponse.ok) {
                    let nextResult = await nextResponse.json();
                    allProducts = allProducts.concat(nextResult.data || []);
                }
            }
        }
        return allProducts;
    },

    getCategoriesDropdown: () => window.apiFetch(API_ROUTES.CATEGORIES.DROPDOWN),

    toggleFavorite: (id) => window.apiFetch(API_ROUTES.FAVORITES.TOGGLE_PRODUCT(id), { method: 'POST' }),

    delete: (id) => window.apiFetch(API_ROUTES.PRODUCTS.DELETE(id), { method: 'DELETE' }),

    restore: (id) => window.apiFetch(API_ROUTES.PRODUCTS.RESTORE(id), { method: 'PUT' }),

    save: (data, id) => {
        const endpoint = id > 0 ? API_ROUTES.PRODUCTS.UPDATE(id) : API_ROUTES.PRODUCTS.CREATE;
        const method = id > 0 ? 'PUT' : 'POST';
        return window.apiFetch(endpoint, { method, body: JSON.stringify(data) });
    }
};
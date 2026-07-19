const CategoryApi = {
    // SIFIR HARDCODED: 'All' yerine merkezi enum yapısı varsayılan değer olarak atandı
    getAll: (status = window.AppStatuses.ALL) => window.apiFetch(API_ROUTES.CATEGORIES.LIST(status)),

    toggleFavorite: (id) => window.apiFetch(API_ROUTES.FAVORITES.TOGGLE_CATEGORY(id), { method: 'POST' }),
    delete: (id) => window.apiFetch(API_ROUTES.CATEGORIES.DELETE(id), { method: 'DELETE' }),
    restore: (id) => window.apiFetch(API_ROUTES.CATEGORIES.RESTORE(id), { method: 'PUT' }),
    save: (data, isEdit) => {
        const endpoint = isEdit ? API_ROUTES.CATEGORIES.UPDATE(data.id) : API_ROUTES.CATEGORIES.CREATE;
        const method = isEdit ? 'PUT' : 'POST';
        return window.apiFetch(endpoint, { method, body: JSON.stringify(data) });
    },
    getDropdown: () => window.apiFetch(API_ROUTES.CATEGORIES.DROPDOWN)
};
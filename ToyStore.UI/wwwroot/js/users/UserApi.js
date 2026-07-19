const UserApi = {
    // Tüm Kullanıcıları Getirme (Pagination Döngülü)
    // Tüm Kullanıcıları Getirme (Pagination Döngülü - Backend Harf Standartlı Uyum)
    getAll: async () => {
        const limit = window.AppConfig?.Pagination?.MaxSize || 50;
        let currentPage = 1;
        let allUsers = [];

        // ÇÖZÜM: Parametre isimleri küçük harfe (pageNumber & pageSize) çekildi
        let response = await window.apiFetch(`${API_ROUTES.USERS.LIST}?pageSize=${limit}&pageNumber=${currentPage}`);
        if (!response || !response.ok) {
            return [];
        }

        let result = await response.json();
        // Hibrit veri koruması (data veya Data)
        allUsers = result.data || result.Data || [];
        const totalPages = result.totalPages || result.TotalPages || 1;

        if (totalPages > 1) {
            for (currentPage = 2; currentPage <= totalPages; currentPage++) {
                // ÇÖZÜM: Döngü içi parametre isimleri de küçük harfe çekildi
                let nextResponse = await window.apiFetch(`${API_ROUTES.USERS.LIST}?pageSize=${limit}&pageNumber=${currentPage}`);
                if (nextResponse && nextResponse.ok) {
                    let nextResult = await nextResponse.json();
                    let pageData = nextResult.data || nextResult.Data || [];
                    allUsers = allUsers.concat(pageData);
                }
            }
        }
        return allUsers;
    },

    // Yeni Kullanıcı Kaydı (POST)
    register: (userData) => {
        return window.apiFetch(API_ROUTES.AUTH.REGISTER, {
            method: "POST",
            body: JSON.stringify(userData)
        });
    },

    // Şifre Güncelleme (PUT)
    resetPassword: (id, passwordData) => {
        return window.apiFetch(API_ROUTES.USERS.RESET_PASSWORD(id), {
            method: "PUT",
            body: JSON.stringify(passwordData)
        });
    },

    // Rol Atama (PUT) - ENUM SÜZGEÇLİ GÜVENLİK KALKANI
    assignRole: (id, newRole) => {
        const isValidRole = Object.values(window.AppRoles).includes(newRole);
        if (!isValidRole) {
            // Arayüze çirkin hata fırlatmak yerine güvenli bildirim motorumuzu tetikliyoruz
            window.NotificationUI.show(`Geçersiz kullanıcı rolü atama isteği engellendi: ${newRole}`, "error");
            return Promise.reject("Invalid role assignment request");
        }

        return window.apiFetch(`${API_ROUTES.USERS.ASSIGN_ROLE(id)}?newRole=${newRole}`, {
            method: "PUT"
        });
    },

    // Kullanıcıyı Pasif Etme (PUT)
    deactivate: (id) => {
        return window.apiFetch(API_ROUTES.USERS.DEACTIVATE(id), { method: "PUT" });
    },

    // Kullanıcıyı Aktif Etme (PUT)
    activate: (id) => {
        return window.apiFetch(API_ROUTES.USERS.ACTIVATE(id), { method: "PUT" });
    }
};
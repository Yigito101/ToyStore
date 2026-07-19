// ==========================================
// 1. KÜRESEL ENUMLAR VE ROTA YÖNETİMİ
// ==========================================
window.AppRoles = {
    ADMIN: "Admin",
    MANAGER: "Manager", // YENİ: Backend'deki Manager rolü tam uyum için eklendi!
    USER: "User"
};

window.AppStatuses = {
    ALL: "All",
    ACTIVE: "Active",
    INACTIVE: "Inactive"
};

// ÇÖZÜM: Merkezi API taban adresi doğru şekilde set ediliyor
window.API_BASE_URL = `${window.AppConfig.ApiUrl}/api`;

window.API_ROUTES = {
    AUTH: {
        VALIDATE: "/Auth/validate-session",
        LOGIN: "/Auth/login",
        REGISTER: "/Auth/register",
        CHANGE_PASSWORD: "/Auth/change-password",
        LOGOUT: "/Auth/logout",
        ME: "/Auth/me"
    },
    CATEGORIES: {
        LIST: (status = window.AppStatuses.ALL, pageNumber = 1, pageSize = (window.AppConfig && window.AppConfig.Pagination ? window.AppConfig.Pagination.MaxSize : 50)) => `/Categories?status=${status}&PageNumber=${pageNumber}&PageSize=${pageSize}`,
        CREATE: "/Categories",
        UPDATE: (id) => `/Categories/${id}`,
        DELETE: (id) => `/Categories/${id}`,
        RESTORE: (id) => `/Categories/${id}/restore`,
        DROPDOWN: "/Categories/dropdown"
    },
    PRODUCTS: {
        LIST: (status = window.AppStatuses.ALL, pageNumber = 1, pageSize = (window.AppConfig && window.AppConfig.Pagination ? window.AppConfig.Pagination.MaxSize : 50)) => `/Products?status=${status}&pageNumber=${pageNumber}&pageSize=${pageSize}`,
        CREATE: "/Products",
        UPDATE: (id) => `/Products/${id}`,
        DELETE: (id) => `/Products/${id}`,
        RESTORE: (id) => `/Products/${id}/restore`
    },
    USERS: {
        // Eski orijinal haline geri çekiyoruz, böylece projenin diğer sayfaları kurtuluyor
        LIST: "/Users",
        DEACTIVATE: (id) => `/Users/deactivate/${id}`,
        ACTIVATE: (id) => `/Users/activate/${id}`,
        RESET_PASSWORD: (id) => `/Users/admin-reset-password/${id}`,
        ASSIGN_ROLE: (id) => `/Users/assign-role/${id}`
    },
    FAVORITES: {
        TOGGLE_CATEGORY: (id) => `/Favorites/category/${id}`,
        TOGGLE_PRODUCT: (id) => `/Favorites/product/${id}`
    }
};

// ==========================================
// 2. MERKEZİ İSTEK MOTORU VE SUNUCU KALKANI
// ==========================================
window.apiFetch = async function (endpoint, options = {}) {
    const token = localStorage.getItem("token");

    options.headers = {
        "Content-Type": "application/json",
        ...options.headers
    };

    if (token) {
        options.headers["Authorization"] = `Bearer ${token}`;
    }

    const url = `${window.API_BASE_URL}${endpoint}`;

    try {
        const response = await fetch(url, options);

        // 🌟 TAZELEME KALKANI: Backend'in ürettiği Sliding Expiration token'ını yakalıyoruz
        const newContinuationToken = response.headers.get("New-Token");
        if (newContinuationToken) {
            // Yeni uzatılmış token'ı ortak havuzumuza yazarak tüm sekmeleri güncelliyoruz
            localStorage.setItem("token", newContinuationToken);
        }

        // GLOBAL HATA YAKALAYICI
        if (!response.ok) {
            let errorMessage = "İşlem sırasında bir hata oluştu.";
            try {
                const errorData = await response.clone().json();
                errorMessage = errorData.Message || errorData.message || errorMessage;
            } catch (e) {
                try { errorMessage = await response.clone().text(); } catch (t) { }
            }

            if (response.status === 401) {
                localStorage.removeItem("token");
                window.location.replace("/Login");
                return response;
            }

            window.NotificationUI.show(errorMessage, 'error');
            return response;
        }

        // GLOBAL BAŞARI YAKALAYICI
        const method = (options.method || "GET").toUpperCase();
        if (["POST", "PUT", "DELETE"].includes(method)) {
            let successMessage = "İşlem başarıyla gerçekleştirildi.";
            try {
                const successData = await response.clone().json();
                if (successData && (successData.Message || successData.message)) {
                    successMessage = successData.Message || successData.message;
                }
            } catch (e) { }
            window.NotificationUI.show(successMessage, 'success');
        }

        return response;

    } catch (error) {
        console.error("Network / Sunucu Hatası:", error);
        window.NotificationUI.show("Sunucu ile bağlantı kurulamadı. Lütfen API servislerini kontrol edin.", 'error');
        throw error;
    }
};

// ==========================================
// 3. YARDIMCI GÜVENLİK FONKSİYONLARI
// ==========================================
window.logout = function () {
    window.NotificationUI.confirm("Oturumunuz kapatılacaktır. Emin misiniz?", () => {
        localStorage.removeItem("token");
        window.location.replace("/Login");
    });
};

// api.js içerisindeki Auth yardımcı fonksiyonlarının güncel hali:

window.checkAuthStatus = function () {
    const token = localStorage.getItem("token");

    // DÜZELTME: Bu fonksiyon genel sayfalarda (Layout kullanan) koruma sağlar.
    // Eğer token yoksa, kullanıcının bu sayfalarda işi yoktur; direkt Login'e fırlat.
    if (!token) {
        window.location.replace("/Login");
    }
};

window.getUserRole = function () {
    const token = localStorage.getItem("token");
    if (!token) return window.AppRoles.USER;

    try {
        let base64Url = token.split('.')[1];
        let base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        let jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        const payload = JSON.parse(jsonPayload);
        return payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || window.AppRoles.USER;
    } catch (e) {
        return window.AppRoles.USER;
    }
};
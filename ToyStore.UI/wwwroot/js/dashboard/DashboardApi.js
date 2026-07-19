const DashboardApi = {
    // 1. Ürünleri Sayfa Sayfa Çeken Zırhlı Metot
    fetchRawProducts: async () => {
        let allProducts = [];
        let currentPage = 1;
        let hasMoreData = true;

        const pageSize = window.AppConfig?.Pagination?.MaxSize || 50;

        try {
            while (hasMoreData) {
                // ÇÖZÜM: API_ROUTES fonksiyonunu parametreleriyle tetikleyerek temiz URL üretiyoruz
                const url = API_ROUTES.PRODUCTS.LIST(window.AppStatuses.ALL, currentPage, pageSize);
                const response = await window.apiFetch(url);

                if (!response || !response.ok) {
                    hasMoreData = false;
                    break;
                }

                const result = await response.json();
                const pageData = result.data || result.Data || [];

                if (pageData.length > 0) {
                    allProducts = allProducts.concat(pageData);
                    currentPage++;
                } else {
                    hasMoreData = false;
                }

                if (pageData.length < pageSize) {
                    hasMoreData = false;
                }
            }
        } catch (error) {
            console.error("Ürün listesi çekilirken rotasyon hatası:", error);
        }

        return allProducts;
    },

    // 2. Kategorileri Sayfa Sayfa Çeken Zırhlı Metot
    fetchRawCategories: async () => {
        let allCategories = [];
        let currentPage = 1;
        let hasMoreData = true;

        const pageSize = window.AppConfig?.Pagination?.MaxSize || 50;

        try {
            while (hasMoreData) {
                // ÇÖZÜM: API_ROUTES fonksiyonunu parametreleriyle tetikleyerek temiz URL üretiyoruz
                const url = API_ROUTES.CATEGORIES.LIST(window.AppStatuses.ALL, currentPage, pageSize);
                const response = await window.apiFetch(url);

                if (!response || !response.ok) {
                    hasMoreData = false;
                    break;
                }

                const result = await response.json();
                const pageData = result.data || result.Data || [];

                if (pageData.length > 0) {
                    allCategories = allCategories.concat(pageData);
                    currentPage++;
                } else {
                    hasMoreData = false;
                }

                if (pageData.length < pageSize) {
                    hasMoreData = false;
                }
            }
        } catch (error) {
            console.error("Kategori listesi çekilirken rotasyon hatası:", error);
        }

        return allCategories;
    },

    // 3. Tüm Kullanıcıları Sayfa Sayfa Çeken Ham Fetch Metodu (apiFetch Bypass Edildi)
    fetchRawUsers: async () => {
        let allUsers = [];
        let currentPage = 1;
        let hasMoreData = true;

        // Konfigürasyondan dinamik port ve pagination bilgilerini güvenle alıyoruz
        const baseUrl = window.AppConfig?.ApiUrl || "https://localhost:7223";
        const pageSize = window.AppConfig?.Pagination?.MaxSize || 50;
        const token = localStorage.getItem("token");

        try {
            while (hasMoreData) {
                // ÇÖZÜM: window.apiFetch kullanmıyoruz! Tam adresi doğrudan biz inşa ediyoruz.
                const url = `${baseUrl}/api/Users?pageNumber=${currentPage}&pageSize=${pageSize}`;

                const response = await fetch(url, {
                    method: "GET",
                    headers: {
                        "Content-Type": "application/json",
                        // Yetkilendirme kontrolü (204) başarılı geçsin diye token'ı ekliyoruz
                        "Authorization": token ? `Bearer ${token}` : ""
                    }
                });

                if (!response || !response.ok) {
                    hasMoreData = false;
                    break;
                }

                const result = await response.json();

                // Backend DTO yapısına göre (Array, .data veya .Data) veri ayıklama zırhı
                let pageData = [];
                if (Array.isArray(result)) {
                    pageData = result;
                } else if (result && Array.isArray(result.data)) {
                    pageData = result.data;
                } else if (result && Array.isArray(result.Data)) {
                    pageData = result.Data;
                }

                if (pageData.length > 0) {
                    allUsers = allUsers.concat(pageData);
                    currentPage++;
                } else {
                    hasMoreData = false;
                }

                if (pageData.length < pageSize) {
                    hasMoreData = false;
                }
            }
        } catch (error) {
            console.error("Kullanıcı listesi ham fetch ile çekilirken hata:", error);
        }

        return allUsers;
    }
};
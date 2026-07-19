// site.js - Ortak Fonksiyonlar (Cross-Tab ve Tanımsız Sayfa Korumalı)
window.checkUserRole = function () {
    const token = localStorage.getItem("token");

    // Varsayılan rol ataması
    window.currentUserRole = window.AppRoles.USER;

    if (token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            window.currentUserRole = payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || window.AppRoles.USER;
        } catch (e) {
            console.error("Token çözümlenemedi:", e);
        }
    }

    const path = window.location.pathname.toLowerCase();

    // Sayfa Mesajları Merkezi Sözlük Yapısı
    const pageMessages = {
        "/dashboard": {
            admin: "Sistem genel özetini ve analiz grafiklerini buradan takip edebilirsiniz.",
            user: "Hoş geldiniz! Sistem özet bilgilerini buradan inceleyebilirsiniz."
        },
        "/products": {
            admin: "Sistemdeki tüm oyuncakları ve stoklarını buradan yönetebilirsiniz.",
            user: "Sistemdeki tüm oyuncakları ve stok durumlarını buradan inceleyebilirsiniz."
        },
        "/categories": {
            admin: "Sistemdeki tüm oyuncak kategorilerini buradan yönetebilirsiniz.",
            user: "Sistemde kayıtlı olan tüm oyuncak kategorilerini buradan inceleyebilirsiniz."
        },
        "/users": {
            admin: "Sistem kullanıcılarını, yetkilerini ve hesap durumlarını buradan yönetebilirsiniz.",
            user: "Kullanıcı yönetimi sadece yöneticilere özeldir."
        }
    };

    // ZIRH: Eğer eşleşen sayfa bulunamazsa dashboard mesajlarını yedek olarak alıyoruz
    const currentPageKey = Object.keys(pageMessages).find(key => path.includes(key)) || "/dashboard";
    const messages = pageMessages[currentPageKey];

    // 2. ADIM: Arayüz Metinlerini Güvenle Güncelle
    const desc = document.getElementById("pageDescription");
    if (desc && messages) {
        desc.textContent = window.currentUserRole === window.AppRoles.ADMIN ? messages.admin : messages.user;
    }

    // Buton ve Başlık Kontrolleri (Dinamik Yetki Kalkanı)
    const btnAddProduct = document.getElementById("btnAddNewProduct");
    const btnAddCategory = document.getElementById("btnAddNewCategory");
    const actionHeader = document.getElementById("actionsColumnHeader");

    if (window.currentUserRole !== window.AppRoles.ADMIN) {
        if (btnAddProduct) btnAddProduct.classList.add("hidden");
        if (btnAddCategory) btnAddCategory.classList.add("hidden");
        if (actionHeader) actionHeader.classList.add("hidden");
    } else {
        // Admin geri geldiğinde butonların gizliliğini kaldırma garantisi
        if (btnAddProduct) btnAddProduct.classList.remove("hidden");
        if (btnAddCategory) btnAddCategory.classList.remove("hidden");
        if (actionHeader) actionHeader.classList.remove("hidden");
    }
};
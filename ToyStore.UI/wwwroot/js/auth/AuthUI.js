const AuthUI = {
    /**
     * ŞİFRE DEĞİŞTİRME PENCERESİNİ AÇMA VE DURUM SIFIRLAMA
     * UX DOĞRULUK: Modal her açıldığında eski girdiler ve hata kutuları temizlenerek kararlı arayüz sağlanır.
     */
    openChangePasswordModal: () => {
        const form = document.getElementById("changePwForm");
        if (form) form.reset(); // Form içeriğini tamamen sıfırlar.

        const errBox = document.getElementById("changePwError");
        if (errBox) errBox.classList.add("hidden"); // Eski hata mesajlarını gizler.

        const modal = document.getElementById("changePasswordModal");
        if (modal) modal.classList.remove("hidden"); // Modalı görünür kılar.
    },

    /**
     * ŞİFRE DEĞİŞTİRME PENCERESİNİ GÜVENLİ KAPATMA
     */
    closeChangePasswordModal: () => {
        const modal = document.getElementById("changePasswordModal");
        if (modal) modal.classList.add("hidden");
    },

    /**
     * MODAL İÇİ KULLANICI HATA BİLGİLENDİRMESİ
     */
    showError: (message) => {
        const errBox = document.getElementById("changePwError");
        if (errBox) {
            errBox.textContent = message;
            errBox.classList.remove("hidden");
        }
    },

    /**
     * ROL TABANLI DINAMİK MENÜ VE ARAYÜZ YAPILANDIRMASI (SIFIR HARDCODED)
     * MİMARİ STRATEJİ: Giriş yapan aktörün yetkisine göre hassas yönetim menülerini (Örn: Kullanıcı Yönetimi) 
     * DOM düzeyinde gizler veya gösterir. Rol kontrolleri merkezi enum üzerinden (window.AppRoles) doğrulanır.
     */
    applyLayoutRoleUI: (role) => {
        const userMenu = document.getElementById('menu-users');
        const greeting = document.getElementById('user-greeting');
        const sidebarBrand = document.getElementById('sidebar-brand');

        if (role === window.AppRoles.ADMIN) {
            // Aktör Yöneticisi ise İdari Menüleri Görünür Kıl
            if (userMenu) userMenu.classList.remove('hidden');
            if (greeting) greeting.textContent = `Hoş Geldiniz, ${window.AppRoles.ADMIN}`;
            if (sidebarBrand) sidebarBrand.textContent = "ToyStore Admin";

            // SEO ve Tarayıcı Başlığı Optimizasyonu
            if (!document.title.includes("Admin")) {
                document.title = document.title + " Admin";
            }
        } else {
            // Aktör Standart Kullanıcı veya Manager ise İdari Menüleri DOM'da Maskele
            if (userMenu) userMenu.classList.add('hidden');
            // Gelen rolden dinamik beslenen kurumsal karşılama metni
            if (greeting) greeting.textContent = `Hoş Geldiniz, ${role || window.AppRoles.USER}`;
            if (sidebarBrand) sidebarBrand.textContent = "ToyStore Paneli";
        }
    }
};
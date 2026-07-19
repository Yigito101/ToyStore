const LoginUI = {
    /**
     * GİRİŞ HATALARINI EKRANA YANSITMA
     */
    showError: (message) => {
        const errorBox = document.getElementById('loginError');
        if (errorBox) {
            errorBox.textContent = message;
            errorBox.classList.remove('hidden');
        }
    },

    /**
     * HATA KUTUSUNU GÜVENLİ MASKELEME
     */
    hideError: () => {
        const errorBox = document.getElementById('loginError');
        if (errorBox) errorBox.classList.add('hidden');
    },

    /**
     * ASENKRON İSTEK ESNASINDA FORM KİLİTLEME DÜZENEĞİ (UI LOCK SHIELD)
     * UX & PERFORMANS: Kullanıcı butona bastığı an buton kilitlenir (disabled). Bu sayede, 
     * asenkron istek arka planda dönerken mükerrer tıklamalar (Double-Submit) ve sunucuya 
     * gereksiz mükerrer login sorgusu atılması mimari seviyede engellenmiş olur.
     */
    setLoading: (isLoading) => {
        const btn = document.getElementById('loginBtn');
        if (btn) {
            btn.disabled = isLoading;
            btn.textContent = isLoading ? "Giriş yapılıyor..." : "Giriş Yap";
        }
    }
};
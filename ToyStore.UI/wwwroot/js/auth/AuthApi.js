const AuthApi = {
    /**
     * KULLANICI ŞİFRE GÜNCELLEME İSTEĞİ
     * GÜVENLİK NOTU: Şifre değiştirme verileri body içerisinde JSON formatında şifreli (HTTPS) 
     * boru hattı üzerinden güvenli bir şekilde backend servis katmanına iletilir.
     */
    changePassword: (passwordData) => {
        return window.apiFetch(API_ROUTES.AUTH.CHANGE_PASSWORD, {
            method: "PUT",
            body: JSON.stringify(passwordData)
        });
    }
};
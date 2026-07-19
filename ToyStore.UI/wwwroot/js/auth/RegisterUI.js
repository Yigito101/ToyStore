const RegisterUI = {
    /**
     * DİNAMİK BİLGİLENDİRME VE DURUM MESAJI MOTORU (ALERTER)
     * MİMARİ NOT: Tailwind CSS durum sınıfları (Hata/Başarı) DOM ağacında dinamik olarak yönetilir.
     */
    showMessage: (text, type) => {
        const msgBox = document.getElementById('messageBox');
        if (!msgBox) return;

        msgBox.textContent = text;
        // Eski durum sınıflarını güvenli bir şekilde temizle
        msgBox.classList.remove('hidden', 'bg-red-100', 'border-red-500', 'text-red-700', 'bg-green-100', 'border-green-500', 'text-green-700');

        if (type === "error") {
            msgBox.classList.add('bg-red-100', 'border-red-500', 'text-red-700');
        } else {
            msgBox.classList.add('bg-green-100', 'border-green-500', 'text-green-700');
        }
    },

    /**
     * KAYIT ESNASINDA ÇİFT TIKLAMA ENGELLEYİCİ KİLİT SİSTEMİ
     */
    setLoading: (isLoading) => {
        const btn = document.getElementById('registerBtn');
        if (btn) {
            btn.disabled = isLoading;
            btn.textContent = isLoading ? "Kaydediliyor..." : "Kayıt Ol";
        }
    }
};
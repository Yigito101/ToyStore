window.NotificationUI = {
    // 1. XSS Korumalı Sağ Üst Köşe Toast Bildirim Fırlatıcı
    show: function (message, type = 'success', duration = 3000) {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.className = 'fixed top-5 right-5 z-[9999] flex flex-col gap-3';
            document.body.appendChild(container);
        }

        const styles = {
            success: 'bg-green-600 border-green-700 text-white',
            error: 'bg-red-600 border-red-700 text-white',
            warning: 'bg-amber-500 border-amber-600 text-white',
            info: 'bg-blue-600 border-blue-700 text-white'
        };

        const toast = document.createElement('div');
        toast.className = `${styles[type] || styles.info} px-5 py-3 rounded-lg shadow-xl border flex items-center justify-between min-w-[280px] max-w-[400px] transform transition-all duration-300 translate-x-full opacity-0`;

        // GÜVENLİK KALKANI: Mesaj alanını ayrı bir element olarak yaratıp textContent ile besliyoruz
        const textSpan = document.createElement('span');
        textSpan.className = 'text-sm font-medium pr-4';
        textSpan.textContent = message; // <-- Tarayıcı script kodlarını asla çalıştırmaz, düz yazı olarak basar!

        // Kapatma butonu
        const closeBtn = document.createElement('button');
        closeBtn.className = 'text-white hover:text-gray-200 font-bold text-xs select-none';
        closeBtn.innerHTML = '✕'; // Statik güvenli karakter
        closeBtn.onclick = function () { toast.remove(); };

        // Elementleri birleştiriyoruz
        toast.appendChild(textSpan);
        toast.appendChild(closeBtn);
        container.appendChild(toast);

        setTimeout(() => {
            toast.classList.remove('translate-x-full', 'opacity-0');
            toast.classList.add('translate-x-0', 'opacity-100');
        }, 10);

        setTimeout(() => {
            toast.classList.remove('translate-x-0', 'opacity-100');
            toast.classList.add('translate-x-full', 'opacity-0');
            setTimeout(() => toast.remove(), 300);
        }, duration);
    },

    // 2. XSS Korumalı Modern Onay Penceresi (Confirm Modal)
    confirm: function (message, onConfirm) {
        const oldModal = document.getElementById('global-confirm-modal');
        if (oldModal) oldModal.remove();

        const modal = document.createElement('div');
        modal.id = 'global-confirm-modal';
        modal.className = 'fixed inset-0 bg-gray-900 bg-opacity-60 flex items-center justify-center z-[9999] p-4';

        // Şablon yapısını kuruyoruz
        modal.innerHTML = `
            <div class="bg-white rounded-xl shadow-2xl max-w-sm w-full p-6 transform transition-all scale-95 opacity-0 duration-200">
                <h3 class="text-lg font-semibold text-gray-900">İşlem Onayı</h3>
                <p id="global-confirm-text" class="text-sm text-gray-500 mt-2"></p>
                <div class="mt-6 flex justify-end gap-3">
                    <button id="confirm-cancel-btn" class="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 text-sm font-medium rounded-lg transition-colors">Vazgeç</button>
                    <button id="confirm-ok-btn" class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg shadow-md transition-colors">Onayla</button>
                </div>
            </div>
        `;

        document.body.appendChild(modal);

        // GÜVENLİK KALKANI: Dinamik gelen mesajı textContent ile güvenli alana yazıyoruz
        modal.querySelector('#global-confirm-text').textContent = message;

        const box = modal.querySelector('div');
        setTimeout(() => { box.classList.remove('scale-95', 'opacity-0'); box.classList.add('scale-100', 'opacity-100'); }, 10);

        const closeModal = () => {
            box.classList.remove('scale-100', 'opacity-100'); box.classList.add('scale-95', 'opacity-0');
            setTimeout(() => modal.remove(), 200);
        };

        modal.querySelector('#confirm-cancel-btn').addEventListener('click', closeModal);
        modal.querySelector('#confirm-ok-btn').addEventListener('click', () => {
            closeModal();
            if (typeof onConfirm === 'function') onConfirm();
        });
    }
};
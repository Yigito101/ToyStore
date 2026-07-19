const PaginationHelper = {
    currentPage: 1,
    // SIFIR HARDCODED: Önce Layout'taki konfigürasyona bakar, bulamazsa emniyet kemeri olarak 10'a düşer.
    pageSize: window.AppConfig?.Pagination?.DefaultSize || 10,
    data: [],
    renderCallback: null,

    // Modülü başlatan ana fonksiyon
    init: function (dataArray, renderFunction) {
        // Dinamik kontrolü garantiye alıyoruz (Filtrelemelerde tetiklendiğinde güncel değeri korur)
        this.pageSize = window.AppConfig?.Pagination?.DefaultSize || 10;

        this.data = dataArray;
        this.renderCallback = renderFunction;

        this.updateTotalPages();
        this.renderPage(1);
    },

    updateTotalPages: function () {
        this.totalPages = Math.ceil(this.data.length / this.pageSize) || 1;
    },

    // Belirtilen sayfanın verilerini kesip UI'a gönderir
    // Butonlardan gelen İleri/Geri isteklerini yönetir
    changePage: function (direction) {

        // DİKKAT: JS'de metin + sayı çakışmasını önlemek için kesinlikle Integer'a çeviriyoruz
        const nextTarget = parseInt(this.currentPage) + parseInt(direction);


        this.renderPage(nextTarget);
    },

    // Belirtilen sayfanın verilerini kesip UI'a gönderir
    renderPage: function (pageIndex) {

        pageIndex = parseInt(pageIndex); // Güvenlik için tekrar parseInt

        if (pageIndex < 1) {
            pageIndex = 1;
        }
        if (pageIndex > this.totalPages) {
            pageIndex = this.totalPages;
        }


        this.currentPage = pageIndex;

        const startIndex = (this.currentPage - 1) * this.pageSize;
        const endIndex = startIndex + this.pageSize;
        const slicedData = this.data.slice(startIndex, endIndex);

        if (this.renderCallback) {
            this.renderCallback(slicedData);
        }

        this.updateUI(startIndex, endIndex);

    },

    // Alt kısımdaki HTML çubuğunu günceller
    updateUI: function (startIndex, endIndex) {
        const container = document.getElementById('paginationContainer');
        if (!container) return;

        const totalItems = this.data.length;

        // AKILLI UI: Toplam kayıt, sayfa sınırından küçük/eşitse çubuğu tamamen gizle
        if (totalItems <= this.pageSize) {
            container.classList.add('hidden');
        } else {
            container.classList.remove('hidden');
        }

        document.getElementById('totalItemCount').innerText = totalItems;
        document.getElementById('startItemIndex').innerText = totalItems > 0 ? startIndex + 1 : 0;
        document.getElementById('endItemIndex').innerText = Math.min(endIndex, totalItems);
        document.getElementById('pageInfoText').innerText = `${this.currentPage} / ${this.totalPages}`;

        document.getElementById('btnPrevPage').disabled = this.currentPage === 1;
        document.getElementById('btnNextPage').disabled = this.currentPage === this.totalPages;
    }
};

// HTML'deki onclick eventlerinin modüle erişebilmesi için Global (window) tanım
window.changeGlobalPage = function (direction) {
    PaginationHelper.changePage(direction);
};
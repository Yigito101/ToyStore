using System.ComponentModel;

namespace ToyStore.API.DTOs
{
    /// <summary>
    /// SERVER-SIDE PAGINATION AND INBOUND SORTING FILTER
    /// MİMARİ DÖNÜŞÜM: Max ve Default sayfa boyutları artık 'const' değil, merkezi Program.cs 
    /// üzerinden 'appsettings.json' verisiyle beslenebilecek şekilde dinamik hale getirilmiştir.
    /// Kötü niyetli büyük sayfa boyutu isteklerine (DoS saldırıları) karşı API katmanında emniyet kemeri görevi görür.
    /// </summary>
    public class PaginationFilter
    {
        // MERKEZİ DİNAMİK PARAMETRELER (FALLBACK DESTEKLİ)
        public static int MaxPageSize { get; set; } = 50;
        public static int DefaultPageSize { get; set; } = 10;

        private int _pageNumber = 1;
        private int _pageSize;

        public PaginationFilter()
        {
            // Emniyet Kemeri: Nesne belleğe çıktığı an güncel varsayılan sayfa boyutu otomatik atanır.
            _pageSize = DefaultPageSize;
        }

        [DefaultValue(1)]
        public int PageNumber
        {
            get => _pageNumber;
            // Pitfall Engelleyici: Negatif veya 0 sayfa numarası girişleri filtre seviyesinde 1'e fikslenir.
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            // APPSCEN KALKANI: İstemci MaxPageSize sınırından büyük veri isterse sunucu otomatik olarak 
            // tavan değere kilitler. Bellek taşması (Out Of Memory) ve DoS riskleri engellenir.
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value < 1 ? DefaultPageSize : value);
        }

        // --- DİNAMİK SIRALAMA ALTYAPISI (DYNAMIC SORTING) ---
        public string? SortColumn { get; set; } // Örn: "Price", "IsFavorite" gibi dinamik kolon ismi kabul eder.
        public bool IsAscending { get; set; } = true; // Sıralama yönü: true (Artan/A-Z), false (Azalan/Z-A)
    }
}
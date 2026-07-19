using System;

namespace ToyStore.Api.DTOs
{
    /// <summary>
    /// CATEGORY OUTBOUND DATA RESPONSE DTO
    /// MİMARİ STRATEJİ: Veritabanındaki ham kategori entitesini doğrudan dış dünyaya açmamak (Data Leakage Prevention) 
    /// amacıyla tasarlanmıştır. Arayüzün ihtiyaç duyduğu tüm metrikleri ve favori durumlarını bir arada sunar.
    /// </summary>
    public class CategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// CONCURRENCY TOKEN (EŞZAMANLI KONTROL KILIDI)
        /// VERİ GÜVENLİĞİ: Veritabanı katmanındaki RowVersion damgasını arayüze taşır. Aynı anda iki adminin
        /// kategoriyi düzenlerken birbirinin verisini ezmesini (Lost Update) engellemek için kritik bir köprüdür.
        /// </summary>
        public byte[]? RowVersion { get; set; }

        // MİMARİ AGREGASYON: İlişkisel tabloları (Product) Join etmeden, servis katmanında hesaplanmış
        // istatistiksel verileri arayüz dashboard panellerine doğrudan aktarır.
        public int ProductCount { get; set; }
        public int TotalStock { get; set; }

        // ARAYÜZ ENTEGRASYONU: Aktif oturum sahibinin bu kategoriyi favoriye ekleyip eklemediğini gösterir.
        public bool IsFavorite { get; set; }
        public int FavoritedProductCount { get; set; }
    }
}
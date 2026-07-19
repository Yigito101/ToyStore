using System;

namespace ToyStore.Api.DTOs
{
    /// <summary>
    /// PRODUCT OUTBOUND RESPONSE DATA DTO
    /// MİMARİ AÇIKLAMA: Oyuncak detay ve vitrin verilerini arayüze taşırken, veritabanı şemasını 
    /// bütünüyle gizleyen ve performans odaklı ilişkisel metadataları (CategoryName) düzleştiren modeldir.
    /// </summary>
    public class ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }

        // DÜZLEŞTİRME OPTİMİZASYONU (FLATTENING): Frontend tarafında kompleks iç içe nesne taraması 
        // yapılmasına gerek kalmadan ilişkili kategori ismini doğrudan string olarak taşır.
        public string CategoryName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        // EŞZAMANLI DOĞRULAMA VERİSİ: Veri mutasyonlarında (Güncelleme) çakışma analizinde tüketilir.
        public byte[]? RowVersion { get; set; }

        // MÜŞTERİ BAZLI KİŞİSELLEŞTİRME: Aktif giriş yapan kullanıcının bu ürünü favorilediğini arayüze iletir.
        public bool IsFavorite { get; set; }
    }
}
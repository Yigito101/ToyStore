namespace ToyStore.Api.Models
{
    /// <summary>
    /// ÜRÜN (OYUNCAK) ETKİLEŞİM ENTİTESİ
    /// </summary>
    public class Product : BaseEntity
    {
        public required string Name { get; set; }

        // Finansal veri hassasiyeti için decimal tipi tercih edilmiştir[cite: 7].
        public decimal Price { get; set; }

        public int Stock { get; set; }

        // İLİŞKİSEL BAĞ (FOREIGN KEY)[cite: 7]: Veritabanı düzeyinde referans bütünlüğünü kuran anahtar alan.
        public int CategoryId { get; set; }

        /// <summary>
        /// KATEGORİ NAVIGATİON NESNESİ (ONE-TO-MANY BACK-REFERENCE)
        /// MİMARİ STANDART[cite: 7]: 'null!' (Null-Forgiving Operatörü) derleyiciye bu alanın ilk aşamada 
        /// null atanabileceğini, ancak EF Core (Include/Lazy) tarafından çalışma zamanında doldurulacağını garanti eder.
        /// Kodun C# 11 uyarısı vermesini ve kirli uyarı logları üretmesini engeller.
        /// </summary>
        public Category Category { get; set; } = null!;

        public int? CreatedById { get; set; }
        public int? UpdatedById { get; set; }

        public byte[]? RowVersion { get; set; }

        public ICollection<UserFavoriteProduct> FavoritedByUsers { get; set; } = new List<UserFavoriteProduct>();
    }
}
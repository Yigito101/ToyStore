using System.Collections.Generic;

namespace ToyStore.Api.Models
{
    /// <summary>
    /// KATEGORİ ETKİLEŞİM ENTİTESİ
    /// </summary>
    public class Category : BaseEntity
    {
        public required string Name { get; set; }

        /// <summary>
        /// ÜRÜN İLİŞKİSEL KOLEKSİYONU (ONE-TO-MANY NAVIGATION PROPERTY)
        /// MİMARİ ZIRH[cite: 7]: Koleksiyonun doğrudan 'new List<Product>()' ile türetilmesi (Eager Initialization),
        /// alt katmanlarda ürün eklenmeye veya sayılmaya çalışıldığında patlayacak NullReferenceException riskini kökten yok eder.
        /// </summary>
        public ICollection<Product> Products { get; set; } = new List<Product>();

        // AUDIT TRAIL: Veri manipülasyonunun izlenebilirliği adına işlemi tetikleyen aktörlerin ID takibi.
        public int? CreatedById { get; set; }
        public int? UpdatedById { get; set; }

        // OPTIMISTIC CONCURRENCY SHIELD[cite: 7]: Veritabanı katmanında (DbContext) Concurrency Token 
        // olarak işaretlenen ve veri çakışmalarını izole eden byte dizisi.
        public byte[]? RowVersion { get; set; }

        // MANY-TO-MANY LINK[cite: 7]: Kategoriyi favorileyen kullanıcılar arasındaki çoka çok köprü bağı.
        public ICollection<UserFavoriteCategory> FavoritedByUsers { get; set; } = new List<UserFavoriteCategory>();
    }
}
namespace ToyStore.Api.Models
{
    /// <summary>
    /// KATEGORİ FAVORİ İLİŞKİSEL KÖPRÜ ENTİTESİ (M2M JOIN TABLE)
    /// MİMARİ AÇIKLAMA[cite: 7]: Veritabanı normalizasyon kuralları gereği, User ve Category 
    /// tabloları arasındaki çoka çok (Many-to-Many) ilişkiyi fiziksel olarak yöneten ara köprüdür.
    /// </summary>
    public class UserFavoriteCategory
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}
namespace ToyStore.Api.Service
{
    /// <summary>
    /// M2M RELATION FAVORITES SERVICE INTERFACE
    /// MİMARİ AÇIKLAMA[cite: 11]: Kullanıcıların ürün ve kategori bazlı favori etkileşimlerini 
    /// yöneten, çoka çok (Many-to-Many) kaskad iş mantığı kurallarının kontratıdır.
    /// </summary>
    public interface IFavoriteService
    {
        // Tekil ürün favori durum değiştirici tetikleyici imzası[cite: 11].
        Task<string> ToggleProductFavoriteAsync(int userId, int productId);

        // Kategori bazlı toplu (Cascade) favori durum değiştirici tetikleyici imzası[cite: 11].
        Task<string> ToggleCategoryFavoriteAsync(int userId, int categoryId);
    }
}
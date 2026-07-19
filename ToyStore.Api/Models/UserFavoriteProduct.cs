namespace ToyStore.Api.Models
{
    /// <summary>
    /// ÜRÜN FAVORİ İLİŞKİSEL KÖPRÜ ENTİTESİ (M2M JOIN TABLE)
    /// </summary>
    public class UserFavoriteProduct
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
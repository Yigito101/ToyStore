namespace ToyStore.Api.DTOs
{
    /// <summary>
    /// PRODUCT INBOUND CREATION REQUEST DTO
    /// MİMARİ REFACTOR: Nesne dönüştürücü (Mapster) kütüphanesinin veritabanı entite kolonlarıyla
    /// tam uyumlu (zero-configuration) eşleşebilmesi için 'CategoryId' olarak standartlaştırılmıştır.
    /// </summary>
    public class ProductCreateDTO
    {
        public required string Name { get; set; }
        public required decimal Price { get; set; }
        public required int Stock { get; set; }
        public required int CategoryId { get; set; }
    }
}
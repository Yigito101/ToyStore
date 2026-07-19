using System.Text.Json.Serialization;

namespace ToyStore.Api.DTOs
{
    /// <summary>
    /// PRODUCT INBOUND MUTATION UPDATE DTO
    /// MİMARİ KORUMA: Ürün güncellemelerinde bütünlüğü sağlayan, manipülasyon önleyici istek modelidir.
    /// </summary>
    public class ProductUpdateDTO
    {
        /// <summary>
        /// API KORUMA KALKANI (JSON IGNORE): İstek gövdesinden ID gelmesi engellenir. 
        /// Hatalı ID mutasyonları ve olası BadRequest (400) durumları mimari olarak pasifize edilmiştir.
        /// </summary>
        [JsonIgnore]
        public int? Id { get; set; }

        public required string Name { get; set; }
        public required decimal Price { get; set; }
        public required int Stock { get; set; }
        public required int CategoryId { get; set; }

        public byte[]? RowVersion { get; set; }
    }
}
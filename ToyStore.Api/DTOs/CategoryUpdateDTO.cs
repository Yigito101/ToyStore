using System.Text.Json.Serialization;

namespace ToyStore.Api.DTOs
{
    /// <summary>
    /// CATEGORY INBOUND MUTATION UPDATE DTO
    /// MİMARİ KORUMA: Kategori güncelleme operasyonlarında veri bütünlüğünü sağlayan istek modelidir.
    /// </summary>
    public class CategoryUpdateDTO
    {
        /// <summary>
        /// API KORUMA KALKANI (JSON IGNORE): URL'den gelen ID ile JSON body içinden gelen ID'nin
        /// çakışmasını engellemek amacıyla Swagger ve API trafiğinde bu alan tamamen gizlenir.
        /// İstek işlenirken Id parametresi tamamen güvenli olan URL rotasından beslenir.
        /// </summary>
        [JsonIgnore]
        public int? Id { get; set; }

        public required string Name { get; set; }

        /// <summary>
        /// OPTIMISTIC CONCURRENCY ENTEGRASYONU: Güncelleme esnasında arayüzden gelen RowVersion damgası
        /// DbContext katmanına iletilerek veri tutarlılığı kontrolü fiziksel olarak işletilir.
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
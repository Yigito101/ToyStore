namespace ToyStore.Api.DTOs
{
    /// <summary>
    /// CATEGORY INBOUND INITIATION DTO
    /// MİMARİ AÇIKLAMA: Yeni kategori oluşturma isteklerinde istemciden (UI) alınan,
    /// ağ trafiğinde minimum veri kaplayan ve sistem entitelerini maskeleyen hafif giriş modelidir.
    /// </summary>
    public class CategoryCreateDTO
    {
        // C# 11 required anahtar kelimesiyle boş veri gönderimi dil seviyesinde engellenmiştir.
        public required string Name { get; set; }
    }
}
namespace ToyStore.Api.Models.Enums
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// KULLANICI ROL ENUMERATION YAPISI
    /// MİMARİ NOT: [JsonConverter] özniteliği sayesinde API çıktılarında 1, 2, 3 gibi büyülü 
    /// sayısal değerler (Magic Numbers) yerine "Admin", "User" şeklinde string metinler basılır. 
    /// Bu kurgu, frontend tarafında okunabilirliği ve kod kalitesini artırır.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))] // API ağ trafiğinde semantik string eşleşmesi sağlar.
    public enum UserRole
    {
        Admin = 1,
        Manager = 2,
        User = 3
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserStatus
    {
        All = 0,      // Performans Optimizasyonu: Hem aktif hem pasif kayıtları getirmek için kullanılır.
        Active = 1,   // Canlı durumdaki kullanıcılar
        Inactive = 2  // Soft-delete veya deaktif edilmiş hesaplar
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ItemStatus
    {
        All = 0,
        Active = 1,
        Inactive = 2
    }
}
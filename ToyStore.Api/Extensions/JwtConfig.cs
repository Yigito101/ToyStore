namespace ToyStore.API.Extensions
{
    /// <summary>
    /// GLOBAL JWT CONFIGURATION MEMORY POOL
    /// MİMARİ AÇIKLAMA: Uygulama genelinde JWT token ömürlerini ve Sliding Expiration (Kira) kurallarını 
    /// yöneten, çalışma zamanında (Runtime) dinamik olarak güncellenebilen statik yapılandırma merkezidir[cite: 6].
    /// </summary>
    public static class JwtConfig
    {
        /// <summary>
        /// TOKEN GEÇERLİLİK SÜRESİ (DAKİKA BAZINDA)
        /// APPSCEN KALKANI (FALLBACK): 'appsettings.json' dosyasından veri okunamaması veya JSON formatının 
        /// bozulması durumunda, sistemin kilitlenip 0 saniyelik geçersiz token üretmesini engellemek adına 
        /// 30 dakikalık kurumsal bir emniyet kemeri varsayılan olarak rezerve edilmiştir[cite: 6].
        /// </summary>
        public static int ExpiryMinutes { get; set; } = 30;

        /// <summary>
        /// SLIDING EXPIRATION AKTİFLİK BAYRAĞI
        /// MİMARİ STRATEJİ: Sürekli işlem yapan aktif kullanıcıların oturumunun yarı sürede otomatik uzatılmasını 
        /// sağlayan altyapısal anahtardır[cite: 6]. İhtiyaç anında merkezi olarak kapatılabilir.
        /// </summary>
        public static bool EnableSlidingExpiration { get; set; } = true;
    }
}
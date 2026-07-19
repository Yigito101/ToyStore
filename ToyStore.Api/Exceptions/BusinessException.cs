using System;

namespace ToyStore.API.Exceptions
{
    /// <summary>
    /// BUSINESS RULES VALİDASYON HATASI
    /// MİMARİ AÇIKLAMA: Veritabanı veya sistem çökmelerinden ziyade, tamamen iş kuralları 
    /// ihlal edildiğinde (Örn: İçinde ürün olan kategorinin silinmeye çalışılması) fırlatılan özel sınıftır.
    /// Global Exception Middleware tarafından yakalanarak istemciye HTTP 400 Bad Request döner.
    /// </summary>
    public class BusinessException : Exception
    {
        /// <summary>
        /// STANDARTLAŞTIRILMIŞ TEKNİK HATA KODU
        /// ARAYÜZ ENTEGRASYONU: Hata mesajlarının yanı sıra "ERR_STOCK_001", "ERR_AUTH_005" gibi 
        /// benzersiz kodlar taşır. Frontend katmanının çoklu dil (i18n) veya özel hata senaryolarını 
        /// yönetebilmesi için kurumsal bir altyapı sunar.
        /// </summary>
        public string ErrorCode { get; }

        public BusinessException(string message, string errorCode = "ERR_DEFAULT") : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
using System.Text.Json;

namespace ToyStore.API.Models
{
    /// <summary>
    /// STANDART HATA ÇIKTI ŞABLONU (API ERROR WRAPPER)
    /// MİMARİ STANDART[cite: 7]: ExceptionMiddleware veya filtreler tarafından yakalanan hataları 
    /// istemciye (Frontend) gönderirken kullanılan, kurumsal standarttaki nesne modelidir.
    /// </summary>
    public class ErrorDetails
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// STRATETİK JSON DÖNÜŞTÜRÜCÜ (SERIALIZATION OVERRIDE)
        /// PERFORMANS[cite: 7]: Nesne metinsel olarak çağrıldığı anda (Örn: Console veya Response loglama),
        /// System.Text.Json kullanarak nesneyi otomatik olarak JSON string formatına serialize eder.
        /// </summary>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using ToyStore.API.Exceptions;

namespace ToyStore.API.Middlewares
{
    /// <summary>
    /// GLOBAL HTTP EXCEPTION & ERROR ISOLATION MIDDLEWARE
    /// MİMARİ AÇIKLAMA: HTTP istek hattına (Request Pipeline) en üst sırada entegre edilen, 
    /// alt katmanlarda (Service/Data) fırlatılan tüm istisnaları (Exceptions) merkezi olarak yakalayıp 
    /// HTTP durum kodlarına dönüştüren kurumsal hata yönetim kalkanıdır.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// PIPELINE TRAMVAYI VE YAKALAMA SÜRECİ
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // İstek sorunsuz çalışıyorsa, zinciri bozmadan bir sonraki middleware katmanına aktar.
                await _next(context);
            }
            catch (Exception ex)
            {
                // AŞAĞI KATMANLARDA PATLAYAN BOMBAYI YAKALAMA:
                // Uygulamanın çalışma zamanında çökmesini (Runtime Crash) önler ve hatayı merkezi analize yönlendirir.
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// TEKNİK İSTİSNAYI ANLAMSAL HTTP YANITINA DÖNÜŞTÜRÜCÜ (SEMANTIC TRANSLATOR)
        /// </summary>
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // API Standartizasyonu: İstemcinin (Frontend) gelen hatayı parse edebilmesi için içerik tipi JSON olarak kilitlenir.
            context.Response.ContentType = "application/json";

            // --- 1. AŞAĞI SIZAMAYAN EN DIŞ DEFANS: BİLİNMEYEN SİSTEM ÇÖKMELERİ ---
            // Kod tabanında öngöremediğimiz (NullReference, DB Bağlantı Kopması vb.) kritik hatalar için 
            // dış dünyaya sunucu detaylarını sızdırmamak adına HTTP 500 ve maskelenmiş hata mesajı atanır.
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var response = new { ErrorCode = "ERR_SYSTEM", Message = "Sunucu tarafında beklenmeyen bir hata oluştu." };

            // --- 2. BİLİNÇLİ FIRLATILAN İŞ MANTIKLARININ HTTP KODLARINA TERCÜMESİ ---
            switch (exception)
            {
                // İş Kuralları İhlali Kalkanı:
                case BusinessException businessEx:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    // Servisten gelen kurumsal ErrorCode ve özel dinamik mesaj doğrudan paketlenir.
                    response = new { ErrorCode = businessEx.ErrorCode, Message = businessEx.Message };
                    break;

                // Kayıt Bulunamadı Kalkanı:
                case NotFoundException notFoundEx:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    response = new { ErrorCode = "ERR_NOT_FOUND", Message = notFoundEx.Message };
                    break;
            }

            // Oluşturulan standart JSON objesini HTTP yanıt gövdesine asenkron olarak yazar.
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
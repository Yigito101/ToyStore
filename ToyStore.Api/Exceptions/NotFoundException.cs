using System;

namespace ToyStore.API.Exceptions
{
    /// <summary>
    /// KAYIT BULUNAMADI (NOT FOUND) İSTİSNASI
    /// MİMARİ AÇIKLAMA: Sorgulanan kaynağın veritabanında karşılığı olmadığında fırlatılır.
    /// Denetleyici (Controller) katmanında mükerrer if-null kontrolleri yazılmasını önler (DRY Prensibi).
    /// Global Exception Middleware bu hatayı yakaladığında istemciye anlamsal olarak HTTP 404 NotFound basar.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }
}
namespace ToyStore.Api.DTOs.Auth
{
    /// <summary>
    /// USER INBOUND REGISTER DTO
    /// MİMARİ STANDARTLAŞTIRMA: Projenin diğer katmanlarındaki isimlendirme standartlarıyla tam senkron 
    /// çalışması adına hatalı 'ConfirmedPassword' alanı 'ConfirmPassword' olarak refactor edilmiştir.
    /// </summary>
    public class UserRegisterDTO
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}
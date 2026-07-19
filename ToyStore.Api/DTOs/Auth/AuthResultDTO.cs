namespace ToyStore.Api.DTOs.Auth
{
    /// <summary>
    /// AUTHENTICATION RESPONSE DTO
    /// MİMARİ AÇIKLAMA: Başarılı bir giriş (Login) veya tazelemeli oturum (Sliding Expiration) sonrasında,
    /// API katmanından Frontend arayüzüne (Client) gönderilen şifrelenmiş kimlik belgesi ve meta veridir.
    /// </summary>
    public class AuthResultDTO
    {
        // GÜVENLİK: İstemci tarafında (localStorage) saklanacak olan kriptografik olarak imzalanmış JWT string değeri.
        public string Token { get; set; } = string.Empty;

        // SENKRONİZASYON: Token'ın ömrünün dolacağı zaman damgası. Client-side anti-flicker ve geçerlilik kontrollerinde tüketilir.
        public DateTime Expiration { get; set; }
    }
}
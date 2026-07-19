using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using ToyStore.Api.DTOs.Auth;
using ToyStore.Api.Models.Enums;
using ToyStore.Api.Service;
using ToyStore.API.DTOs;
using ToyStore.API.Exceptions; // Merkezi hata yönetim mekanizması entegrasyonu

namespace ToyStore.API.Controllers
{
    /// <summary>
    /// AUTHENTICATION & AUTHORIZATION CONTROLLER
    /// MİMARİ AÇIKLAMA: Sistem genelindeki kullanıcı kayıt, giriş, profil sorgulama ve 
    /// şifre operasyonlarını yöneten, JWT (JSON Web Token) tabanlı stateless kimlik doğrulama merkezidir.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController] // DRY Prensibi: Model state validasyonlarını otomatik gerçekleştirir.
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        public AuthController(IAuthService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        /// <summary>
        /// CLIENT-SIDE OTURUM VE KİRA (SLIDING EXPIRATION) DOĞRULAMA UÇ NOKTASI
        /// MİMARİ KORUMA: Arayüz (Frontend) her yüklendiğinde veya yenilendiğinde anti-flicker mekanizmasıyla tetiklenir.
        /// İstek buraya ulaşabiliyorsa, JwtBearer Middleware validasyonundan başarıyla geçmiş demektir.
        /// Bu esnada JwtBearerEvents altındaki 'Sliding Expiration' kuralımız tetiklenerek response header'a 'New-Token' basılır.
        /// </summary>
        [Authorize]
        [HttpGet("validate-session")]
        public IActionResult ValidateSession()
        {
            return Ok(new { message = "Oturum aktif ve geçerli." });
        }

        /// <summary>
        /// YENİ KULLANICI KAYIT ENDPOINT'İ
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO dto)
        {
            // PITFALL ENGELLEYİCİ: Hatalı formatta veri (örn: geçersiz email) gönderilirse,
            // [ApiController] özniteliği servis katmanına inmeden isteği 400 Bad Request ile keser.
            await _authService.RegisterAsync(dto);
            return Ok(new { message = "Kullanıcı başarıyla kaydedildi." });
        }

        /// <summary>
        /// KULLANICI GİRİŞ ENDPOINT'İ (JWT ÜRETİM MERKEZİ)
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO dto)
        {
            var authResult = await _authService.LoginAsync(dto);
            return Ok(authResult); // Token ve kullanıcı temel rollerini içeren DTO döner.
        }

        /// <summary>
        /// OTURUM KAPATMA (LOGOUT) ENDPOINT'İ
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return Ok(new { message = "Başarıyla çıkış yapıldı ve mevcut oturum sonlandırıldı." });
        }

        /// <summary>
        /// AKTİF OTURUM SAHİBİNİN PROFİL BİLGİLERİNİ GETİRME
        /// SENKRONİZASYON: ClaimsPrincipal (HttpContext.User) havuzundan şifrelenmiş NameIdentifier
        /// claim'ini çözer. Arayüzün profil bilgilerini manipüle edilemez güvenli bir kaynaktan çekmesini sağlar.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            // JWT içerisindeki NameIdentifier claim'ini okuyoruz
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Bağımlılıkların Azaltılması (Decoupling): Profil okuma işlemi UserService katmanına devredilmiştir.
            var user = await _userService.GetUserByIdAsync(userId);

            return user != null ? Ok(user) : NotFound(new { message = "Kullanıcı bulunamadı." });
        }

        /// <summary>
        /// KULLANICININ KENDİ ŞİFRESİNİ DEĞİŞTİRMESİ
        /// GÜVENLİK KALKANI: Dışarıdan manipüle edilebilir bir UserId parametresi almaz! Güvenliği en üst 
        /// düzeye çıkarmak adına, istek atan kişinin Id'si doğrudan imzalı JWT içerisinden (Claims) okunur.
        /// </summary>
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] UserChangePasswordDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            await _authService.ChangePasswordAsync(userId, dto);

            // GÜVENLİK SENKRONİZASYONU: Şifre değiştiği an veritabanındaki SecurityStamp patlatılır.
            // Arayüz bu yanıtı aldığında localStorage'ı temizlemeli ve tüm sekmelerdeki oturumu düşürmelidir.
            return Ok(new { message = "Şifreniz başarıyla güncellendi. Lütfen yeni şifrenizle tekrar giriş yapınız." });
        }
    }
}
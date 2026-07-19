using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using ToyStore.Api.Data;
using ToyStore.Api.DTOs.Auth;
using ToyStore.Api.Models;
using ToyStore.Api.Models.Enums;
using ToyStore.API.Exceptions;
using ToyStore.API.DTOs;
using Microsoft.Extensions.Configuration;

namespace ToyStore.Api.Service
{
    /// <summary>
    /// AUTHENTICATION & SECURITY BUSINESS SERVICE
    /// MİMARİ AÇIKLAMA: Kullanıcı kayıt, giriş, şifre mutasyonları ve JWT token 
    /// yaşam döngüsü operasyonlarını üstlenen, sistemin kimlik ve yetkilendirme (Identity) motorudur.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ToyStoreDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService;

        public AuthService(ToyStoreDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IUserService userService)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
        }

        /// <summary>
        /// YENİ KULLANICI KAYIT OPERASYONU
        /// </summary>
        public async Task<bool> RegisterAsync(UserRegisterDTO dto)
        {
            // 1. GÜVENLİK ZIRHI: Aynı e-posta adresiyle mükerrer hesap açılması iş kuralıyla engellenir[cite: 8].
            var userExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (userExists)
                throw new BusinessException("Bu e-posta adresi zaten kullanılıyor.");

            // DRY Prensibi & Performans: Mapster kütüphanesiyle DTO nesnesi doğrudan User modeline adapte edilir[cite: 8].
            var user = dto.Adapt<User>();

            // APPSCEN KALKANI: Kullanıcı şifresi ham (plain text) saklanmaz. Kriptografik olarak güçlü 
            // Blowfish tabanlı BCrypt hashing algoritmasıyla tuzlanarak şifrelenir[cite: 8].
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.Role = UserRole.User.ToString(); // Varsayılan rol ataması
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// GÜVENLİ GİRİŞ VE TOKEN ÜRETİM SÜRECİ
        /// </summary>
        public async Task<AuthResultDTO> LoginAsync(UserLoginDTO dto)
        {
            // Soft-Delete Entegrasyonu: Kullanıcı var olsa bile hesabı pasifse (IsActive=0) içeri alınmaz[cite: 8].
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !user.IsActive)
                throw new BusinessException("Kullanıcı bulunamadı veya hesap pasif.");

            // BCrypt Şifre Doğrulama: Hashlenmiş veri çözülmeden kriptografik olarak kıyaslanır[cite: 8].
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
                throw new BusinessException("Hatalı şifre girdiniz.");

            // MİMARİ DOĞRULUK: appsettings.json dosyasından Program.cs vasıtasıyla okunan ve JwtConfig 
            // içine yazılan merkezi statik parametre (can simidi fallback destekli) doğrudan tüketilir[cite: 8].
            int expiryMinutes = ToyStore.API.Extensions.JwtConfig.ExpiryMinutes;

            var token = GenerateJwtToken(user, expiryMinutes);

            return new AuthResultDTO
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };
        }

        /// <summary>
        /// STATELESS OTURUM KAPATMA (TOKEN REVOCATION)
        /// </summary>
        public async Task<bool> LogoutAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                throw new BusinessException("Aktif oturum bulunamadı.");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                throw new NotFoundException("Kullanıcı bulunamadı.");

            // SİBER GÜVENLİK KALKANI: Stateless (durumsuz) olan JWT token'ı vaktinden önce iptal etmek için 
            // veritabanındaki SecurityStamp kolonu tamamen yenilenir. Böylece bu token'a sahip tüm sekmeler 
            // bir sonraki istekte boru hattından (OnTokenValidated) geçemeyerek anında kapı dışarı edilir.
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedById = userId.Value;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// KULLANICININ KENDİ ŞİFRESİNİ GÜVENLİ DEĞİŞTİRMESİ
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, UserChangePasswordDTO dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                throw new NotFoundException("Kullanıcı bulunamadı.");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
            if (!isPasswordValid)
                throw new BusinessException("Mevcut şifrenizi hatalı girdiniz.");

            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new BusinessException("Yeni şifreler uyuşmuyor.");

            // PITFALL ENGELEYİCİ: Kullanıcının güvenlik bilincini artırmak adına, 
            // yeni şifrenin eski şifreyle birebir aynı olması iş kuralıyla engellenmiştir[cite: 8].
            bool isSameAsOld = BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash);
            if (isSameAsOld)
                throw new BusinessException("Yeni şifreniz, mevcut şifrenizle aynı olamaz.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            // Çapraz Sekme İptali: Şifre değiştiği an diğer sekmelerdeki açık oturumlar da anında düşürülür[cite: 7].
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedById = GetCurrentUserId();

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// GÜVENLİ CONTEXT ÜZERİNDEN KULLANICI ID OKUYUCU
        /// </summary>
        private int? GetCurrentUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// DİNAMİK OTURUM SÜRE UZATMA (SLIDING REISSUE INTERFACE implementation)
        /// MİMARİ ENTEGRASYON: JwtBearer pipeline'ı içerisinden çağrılan, 
        /// aktif sekmelerin ömrünü tazeleyen yeni JWT token üretim köprüsüdür[cite: 8].
        /// </summary>
        public string ReissueToken(User user, int expiryMinutes)
        {
            return GenerateJwtToken(user, expiryMinutes);
        }

        /// <summary>
        /// JWT KRİPTOGRAFİK TOKEN ÜRETİCİ METOT (CORE ENGINE)
        /// </summary>
        private string GenerateJwtToken(User user, int expiryMinutes)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // HIGH SECURITY: Kod tabanından bağımsız, bellekte çalışma zamanında (Runtime) 
            // üretilmiş olan 512-bit'lik süper korunaklı kriptografik anahtar yüklenir.
            var key = ToyStore.API.Extensions.ServerSessionSecret.Key;

            // Claims Havuzu: Yetkilendirme ve senkronizasyon için kritik claims token içine mühürlenir[cite: 8].
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("SecurityStamp", user.SecurityStamp ?? "")
                }),
                Issuer = _configuration.GetSection("Jwt:Issuer").Value,
                Audience = _configuration.GetSection("Jwt:Audience").Value,
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes), // Dakika standardı kilitlendi[cite: 8].
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
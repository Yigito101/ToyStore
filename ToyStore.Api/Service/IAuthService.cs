using ToyStore.Api.DTOs.Auth;
using ToyStore.Api.Models;
using ToyStore.Api.Models.Enums;
using ToyStore.API.DTOs;

namespace ToyStore.Api.Service
{
    /// <summary>
    /// AUTHENTICATION CONTRACT INTERFACE
    /// MİMARİ AÇIKLAMA: Kimlik doğrulama, token ve şifre yönetim operasyonlarının 
    /// iş mantığı katmanındaki sınırlarını belirleyen güvenlik kontratıdır.
    /// </summary>
    public interface IAuthService
    {
        Task<bool> RegisterAsync(UserRegisterDTO dto);
        Task<AuthResultDTO> LoginAsync(UserLoginDTO dto);
        Task<bool> LogoutAsync();
        Task<bool> ChangePasswordAsync(int userId, UserChangePasswordDTO dto);

        /// <summary>
        /// SLIDING EXPIRATION TOKEN YENİLEME İMZASI
        /// MİMARİ ENTEGRASYON: JwtBearer pipeline'ından çağrılan ve aktif kullanıcıların 
        /// oturum sürelerini tazeleyen dinamik token üretim sözleşmesidir.
        /// </summary>
        string ReissueToken(User user, int expiryMinutes);
    }
}

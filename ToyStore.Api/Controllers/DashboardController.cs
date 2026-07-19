using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToyStore.Api.Service;

namespace ToyStore.Api.Controllers
{
    /// <summary>
    /// DASHBOARD METRICS CONTROLLER
    /// MİMARİ AÇIKLAMA: Rol tabanlı (RBAC) analiz, grafik ve metrik verilerini toplayan uç noktadır.
    /// Sistemdeki Admin ve normal kullanıcı panellerinin (Dashboard) ihtiyaç duyduğu istatistiksel 
    /// özetleri merkezi servis katmanı üzerinden güvenli bir şekilde sunar.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// YÖNETİCİ PANELİ İSTATİSTİKLERİ
        /// YETKİLENDİRME (RBAC): Sadece 'Admin' rolüne sahip kullanıcılar bu uç noktaya erişebilir.
        /// Toplam kullanıcı sayısı, ciro, stok durumları gibi hassas iş analitiği verilerini döner.
        /// </summary>
        [HttpGet("admin-stats")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAdminStats()
        {
            var data = _dashboardService.GetAdminStats();
            return Ok(data);
        }

        /// <summary>
        /// MÜŞTERİ / STANDART KULLANICI PANELİ İSTATİSTİKLERİ
        /// GÜVENLİK KALKANI: Dışarıdan manipulatif bir kullanıcı ID'si parametre olarak kabul edilmez!
        /// Kullanıcı verilerinin güvenliği için ID bilgisi doğrudan şifrelenmiş JWT token claim'leri içerisinden çözümlenir.
        /// </summary>
        [HttpGet("user-stats")]
        [Authorize] // Herhangi bir rol kısıtlaması yok, giriş yapmış tüm kullanıcılar erişebilir.
        public IActionResult GetUserStats()
        {
            // ClaimsPrincipal havuzundan şifrelenmiş NameIdentifier claim'ini çekiyoruz
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // PITFALL ENGELLEYİCİ: Token içindeki claim bir sebeple boş veya bozuk gelirse,
            // alt katmandaki servisi kirletmemek adına operasyon anında HTTP 401 Unauthorized ile kesilir.
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Oturum bilgileriniz doğrulanamadı.");
            }

            var data = _dashboardService.GetUserStats(userId);
            return Ok(data);
        }
    }
}
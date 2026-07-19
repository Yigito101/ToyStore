using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using ToyStore.Api.Service;

namespace ToyStore.Api.Controllers
{
    /// <summary>
    /// FAVORITES CONTROLLER (M2M RELATION MANAGEMENT)
    /// MİMARİ AÇIKLAMA: Kullanıcıların ürün ve kategorileri favori listelerine ekleme veya listeden çıkarma 
    /// (Toggle) operasyonlarını üstlenen, bütünüyle kimlik doğrulamasına bağımlı (Stateless) bir yönetim merkezidir.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // GÜVENLİK KALKANI: Bu controller'daki tüm uç noktalara yalnızca geçerli JWT sahipleri erişebilir.
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        /// <summary>
        /// MERKEZİ TOKEN OKUYUCU GÜVENLİK YARDIMCISI
        /// GÜVENLİK: İstek atan kullanıcının HTTP Context üzerindeki ClaimsPrincipal havuzunu doğrular.
        /// Kod tekrarını önler (DRY Prensibi) ve dışarıdan parametreyle gelebilecek ID manipülasyonlarını (Id Spoofing) engeller.
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            // SAVUNMACI PROGRAMLAMA: Token geçerli olsa bile içindeki ID claim'inin bozuk olma ihtimaline karşı
            // operasyon anında kesilerek HTTP 401 düzeyinde ek emniyet kemeri görevi görür.
            if (userIdClaim == null) throw new UnauthorizedAccessException("Kullanıcı kimliği doğrulanamadı.");

            return int.Parse(userIdClaim.Value);
        }

        /// <summary>
        /// ÜRÜN FAVORİ DURUMUNU DEĞİŞTİRME (TOGGLE MECHANISM)
        /// ARAYÜZ ENTEGRASYONU: Tek bir endpoint üzerinden çalışır. Veri tabanında kayıt varsa siler (Remove), 
        /// yoksa ekler (Add). Böylece Frontend tek bir buton tetiklemesiyle durum senkronizasyonunu kolayca sağlar.
        /// </summary>
        [HttpPost("product/{productId}")]
        public async Task<IActionResult> ToggleProductFavorite(int productId)
        {
            int userId = GetCurrentUserId(); // Güvenli JWT claim'inden okunan ID

            var message = await _favoriteService.ToggleProductFavoriteAsync(userId, productId);
            return Ok(new { message });
        }

        /// <summary>
        /// KATEGORİ FAVORİ DURUMUNU DEĞİŞTİRME (TOGGLE MECHANISM)
        /// </summary>
        [HttpPost("category/{categoryId}")]
        public async Task<IActionResult> ToggleCategoryFavorite(int categoryId)
        {
            int userId = GetCurrentUserId(); // Güvenli JWT claim'inden okunan ID

            var message = await _favoriteService.ToggleCategoryFavoriteAsync(userId, categoryId);
            return Ok(new { message });
        }
    }
}
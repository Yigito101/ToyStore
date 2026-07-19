using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using ToyStore.Api.DTOs.Auth;
using ToyStore.Api.Models.Enums;
using ToyStore.Api.Service;
using ToyStore.API.DTOs;
using ToyStore.API.Exceptions;

namespace ToyStore.Api.Controllers
{
    /// <summary>
    /// IDENTITY & USERS MANAGEMENT CONTROLLER
    /// MİMARİ AÇIKLAMA: Tamamen Admin korumalı (Roles = "Admin") çalışan, sistemdeki kullanıcı hesaplarının
    /// durumlarını (aktif/pasif), rol atamalarını ve acil şifre sıfırlama süreçlerini kontrol eden yönetim merkezidir.
    /// Rol tabanlı güvenlik (RBAC) bu katmanda en katı kurallarla işletilir.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // ROBUST SECURITY: Sadece 'Admin' rolüne sahip olan yetkililer bu kapıdan içeri girebilir.
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// KULLANICI LİSTELEME (PAGINATED & FILTERED)
        /// SENKRONİZASYON: Frontend yönetim panelindeki filtreleme ve arama mimarisini besler.
        /// Sunucu performansını korumak için server-side sayfalama (PaginationFilter) parametrelerini kabul eder.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers(
           [FromQuery] PaginationFilter filter,
           [FromQuery] UserStatus status = UserStatus.All,
           [FromQuery] UserRole? role = null)
        {
            return Ok(await _userService.GetUsersAsync(filter, status, role));
        }

        /// <summary>
        /// KULLANICI HESABI PASİFE ÇEKME (DEACTIVATE)
        /// GÜVENLİK ZIRHI (CRITICAL LOCKOUT PROTECTION): İstek atan adminin token'ındaki Id'si ile pasif edilmek istenen 
        /// hedef kullanıcının Id'si karşılaştırılır. Sisteme yön veren tek adminin yanlışlıkla veya manipülasyonla 
        /// kendi kendini kilitlemesi (Self-Lockout / Kendi Yetkini İptal Etme) mimari düzeyde kesin olarak engellenir.
        /// </summary>
        [HttpPut("deactivate/{id}")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            // Sisteme giriş yapmış olan adminin gerçek ID'sini şifreli token'dan okuyoruz.
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (currentUserIdClaim != null && int.Parse(currentUserIdClaim) == id)
            {
                // Güvenlik İlkesi İhlali: Kendi bindiği dalı kesmeye çalışan admine geçit verilmez.
                return BadRequest(new { message = "Kendi hesabınızı pasif edemezsiniz." });
            }

            await _userService.DeactivateUserAsync(id);
            return Ok(new { message = "Kullanıcı başarıyla pasife çekildi." });
        }

        /// <summary>
        /// KULLANICI HESABINI AKTİF ETME (REACTIVATE)
        /// </summary>
        [HttpPut("activate/{id}")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            await _userService.ActivateUserAsync(id);
            return Ok(new { message = "Kullanıcı başarıyla aktif edildi." });
        }

        /// <summary>
        /// KULLANICIYA ROL ATAMA (RBAC MUTATION)
        /// VERİ YÖNETİMİ: Kullanıcıların rollerini dinamik olarak günceller. Arayüzün yetkilendirme 
        /// mekanizmaları (site.js üzerindeki checkUserRole) bu adımdan sonra üretilecek yeni token'a göre şekillenir.
        /// </summary>
        [HttpPut("assign-role/{id}")]
        public async Task<IActionResult> AssignRole(int id, [FromQuery] UserRole newRole)
        {
            await _userService.AssignRoleAsync(id, newRole);
            return Ok(new { message = $"Rol başarıyla '{newRole}' olarak güncellendi." });
        }

        /// <summary>
        /// YÖNETİCİ TABANLI ŞİFRE SIFIRLAMA (ADMIN FORCE RESET)
        /// VERİ SENKRONİZASYON GÜVENLİĞİ: Bir kullanıcının şifresi admin tarafından zorla sıfırlandığında, 
        /// servis katmanında o kullanıcının veritabanındaki SecurityStamp değeri anında patlatılır (güncellenir). 
        /// Bu sayede o kullanıcının diğer tüm cihaz ve sekmelerdeki aktif JWT token'ları (Program.cs / OnTokenValidated) 
        /// anında geçersiz (invalid) kalır ve sonraki ilk API isteğinde sistem dışına (401) itilir.
        /// </summary>
        [HttpPut("admin-reset-password/{targetUserId}")]
        public async Task<IActionResult> AdminResetPassword(int targetUserId, [FromBody] AdminResetPasswordDTO dto)
        {
            // Hata İzolasyonu: Gelen iki şifrenin eşleşme kontrolü iş mantığı kuralları gereği BusinessException üretir.
            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new BusinessException("Şifreler uyuşmuyor.");

            await _userService.AdminResetPasswordAsync(targetUserId, dto.NewPassword);
            return Ok(new { message = "Kullanıcının şifresi başarıyla sıfırlandı ve mevcut oturumları sonlandırıldı." });
        }
    }
}
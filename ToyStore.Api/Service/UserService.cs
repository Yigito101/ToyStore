using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ToyStore.Api.Data;
using ToyStore.Api.DTOs.Auth;
using ToyStore.Api.Models.Enums;
using ToyStore.API.DTOs;
using ToyStore.API.Exceptions;

namespace ToyStore.Api.Service
{
    /// <summary>
    /// IDENTITY & USER MANAGEMENT BUSINESS SERVICE
    /// MİMARİ AÇIKLAMA: Sistemdeki kullanıcı hesaplarının listelenmesi, rol mutasyonları, 
    /// aktiflik durumları (Soft-Delete/Askıya Alma) ve idari şifre operasyonlarının iş mantığı kurallarını işleten merkezdir.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ToyStoreDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(ToyStoreDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// KULLANICI HESAPLARINI SAYFALAMALI VE FİLTRELİ LİSTELEME
        /// OPTİMİZASYON: Büyük organizasyonlarda veritabanını yormamak adına Server-Side Pagination
        /// altyapısı işletilir. Mapster ProjectToType projeksiyonu sayesinde ham 'User' entitesindeki hassas alanlar 
        /// (PasswordHash, SecurityStamp) daha veritabanından çekilme aşamasında (Select) maskelenerek DTO'ya dönüştürülür.
        /// </summary>
        public async Task<PagedResponse<UserListDTO>> GetUsersAsync(PaginationFilter filter, UserStatus status, UserRole? role)
        {
            var query = _context.Users.AsQueryable();

            // Durum (Aktif/Pasif) Kırılım Filtresi
            query = status switch
            {
                UserStatus.Active => query.Where(u => u.IsActive),
                UserStatus.Inactive => query.Where(u => !u.IsActive),
                _ => query
            };

            // Rol Bazlı Arama/Listeleme Filtresi
            if (role.HasValue)
            {
                var roleString = role.Value.ToString();
                query = query.Where(u => u.Role == roleString);
            }

            var totalRecords = await query.CountAsync();

            // Sayfalama ve Data Projection Hattı[cite: 12]
            var users = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ProjectToType<UserListDTO>() // Bellek ve Veri Sızıntısı Kalkanı (Data Leakage Prevention)
                .ToListAsync();

            return new PagedResponse<UserListDTO>(users, filter.PageNumber, filter.PageSize, totalRecords);
        }

        /// <summary>
        /// KULLANICI HESABINI DEAKTİF ETME (SOFT DELETE / HESAP ASKIYA ALMA)
        /// </summary>
        public async Task DeactivateUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                throw new NotFoundException("Kullanıcı bulunamadı.");
            if (!user.IsActive)
                throw new BusinessException("Kullanıcı zaten pasif durumda.");

            // MİMARİ NOT: Fiziksel veri silinmesi engellenmiş, Audit Trail amacıyla işlemi tetikleyen 
            // adminin ID'si ve işlem zamanı mühürlenmiştir[cite: 12].
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedById = GetCurrentUserId();

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// PASİF HESABI YENİDEN AKTİFLEŞTİRME (REACTIVATE)
        /// </summary>
        public async Task ActivateUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                throw new NotFoundException("Kullanıcı bulunamadı.");
            if (user.IsActive)
                throw new BusinessException("Kullanıcı zaten aktif durumda.");

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedById = GetCurrentUserId();

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// KULLANICIYA YENİ ROL ATAMA (RBAC MUTATION)
        /// </summary>
        public async Task AssignRoleAsync(int id, UserRole role)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                throw new NotFoundException("Kullanıcı bulunamadı.");

            // Mükerrer Rol Atama Blokajı[cite: 12]: Sistem yükünü azaltmak amacıyla aynı rol için DB mutasyonu engellenir.
            if (user.Role == role.ToString())
                throw new BusinessException($"Kullanıcı zaten '{role}' rolüne sahip. Herhangi bir değişiklik yapılmadı.");

            user.Role = role.ToString();
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedById = GetCurrentUserId();

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// YÖNETİCİ TABANLI ZORUNLU ŞİFRE SIFIRLAMA (ADMIN FORCE RESET CORNERSTONE)
        /// </summary>
        public async Task<bool> AdminResetPasswordAsync(int targetUserId, string newPassword)
        {
            var user = await _context.Users.FindAsync(targetUserId);
            if (user == null)
                throw new NotFoundException("Kullanıcı bulunamadı.");

            // APPSCEN KALKANI (DEFANSİF ŞİFRE POLİTİKASI)[cite: 12]: Kriptografik doğrulama mekanizmasıyla, 
            // adminin el ile atadığı yeni şifrenin kullanıcının eski şifresiyle birebir aynı olması iş mantığıyla engellenir.
            bool isSameAsOld = BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash);
            if (isSameAsOld)
                throw new BusinessException("Belirlediğiniz yeni şifre, kullanıcının mevcut şifresiyle tamamen aynı. Herhangi bir değişiklik yapılmadı.");

            // Yeni şifrenin güvenli bir şekilde BCrypt ile tuzlanarak hashlenmesi[cite: 8, 12].
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // CANLI OTURUM DÜŞÜRME BOMBASI[cite: 6, 12]: Şifre sıfırlandığı an hedef kullanıcının SecurityStamp alanı 
            // tamamen yenilenir. Bu sayede o kullanıcının elinde tuttuğu mevcut tüm JWT token'lar havada geçersiz kalır, 
            // tarayıcısındaki tüm açık sekmeler sonraki ilk API çağrısında anında 401 Unauthorized ile oturumdan düşer.
            user.SecurityStamp = Guid.NewGuid().ToString();

            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedById = GetCurrentUserId();

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// TEKİL KULLANICI DETAYI SORGULAMA (DECOUPLING ARCHITECTURE)
        /// </summary>
        public async Task<UserListDTO?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Where(u => u.Id == id)
                .ProjectToType<UserListDTO>()
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// HTTP CONTEXT ÜZERİNDEN OPERASYONU TETİKLEYEN AKTÖRÜN GÜVENLİ ID TAKİBİ
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
    }
}
using ToyStore.Api.DTOs.Auth;
using ToyStore.Api.Models.Enums;
using ToyStore.API.DTOs;

namespace ToyStore.Api.Service
{
    /// <summary>
    /// IDENTITY & USER MANAGEMENT INTERFACE
    /// MİMARİ AÇIKLAMA[cite: 11]: Sistemdeki kullanıcı hesaplarının idari yönetim paneli 
    /// üzerindeki yetki atama, deaktif etme ve zorunlu şifre sıfırlama sınırlarını belirler.
    /// </summary>
    public interface IUserService
    {
        Task<PagedResponse<UserListDTO>> GetUsersAsync(PaginationFilter filter, UserStatus status, UserRole? role);

        // Decoupling: Profil detay sorguları yetki ayrımı prensibi gereği UserService kontratına taşınmıştır[cite: 11].
        Task<UserListDTO?> GetUserByIdAsync(int id);

        Task DeactivateUserAsync(int id);
        Task ActivateUserAsync(int id);
        Task AssignRoleAsync(int id, UserRole role);
        Task<bool> AdminResetPasswordAsync(int targetUserId, string newPassword);
    }
}

namespace ToyStore.Api.DTOs.Auth
{
    /// <summary>
    /// ADMIN FORCE RESET PASSWORD REQUEST DTO
    /// MİMARİ AÇIKLAMA: Yöneticinin (Admin), bir kullanıcının şifresini güvenli ve zorunlu olarak 
    /// değiştirmek için kullandığı, ağ trafiğinde minimum veri tüketen hafif istek modelidir.
    /// </summary>
    public class AdminResetPasswordDTO
    {
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
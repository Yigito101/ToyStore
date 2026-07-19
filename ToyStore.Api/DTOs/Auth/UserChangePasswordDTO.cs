namespace ToyStore.Api.DTOs.Auth
{
    /// <summary>
    /// USER SELF PASSWORD UPDATE REQUEST DTO
    /// GÜVENLİK KALKANI: Kullanıcının kendi profil panelinden şifresini güncellemesini sağlar.
    /// C# 'required' anahtar kelimesi kullanılarak, eksik parametreli isteklerin API kapısında elenmesi dil seviyesinde kilitlenmiştir.
    /// </summary>
    public class UserChangePasswordDTO
    {
        // AppSec İlkesi: Brute force veya yetkisiz şifre değişimlerini engellemek için mevcut şifrenin doğrulanması zorunludur.
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
        public required string ConfirmNewPassword { get; set; }
    }
}
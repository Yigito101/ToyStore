namespace ToyStore.Api.DTOs.Auth
{
    /// <summary>
    /// USER RESPONSE DATA DTO (DATA LEAKAGE PREVENTION)
    /// MİMARİ STRATEJİ: Admin panelinde kullanıcıları listelerken veritabanındaki ham 'User' entitesini 
    /// doğrudan dışarı sızdırmamak (Data Leakage) amacıyla tasarlanmış, maskelenmiş güvenlik modelidir.
    /// </summary>
    public class UserListDTO
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // SOFT-DELETE VE AKTİFLİK DURUMU: Yönetim panelindeki pasife çekme/aktif etme butonlarını besleyen durum bayrağı.
        public bool IsActive { get; set; }
    }
}
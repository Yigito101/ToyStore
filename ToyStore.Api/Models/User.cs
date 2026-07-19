using System;
using System.Collections.Generic;

namespace ToyStore.Api.Models
{
    /// <summary>
    /// SİSTEM KULLANICI ENTİTESİ (IDENTITY ROOT)
    /// </summary>
    public class User : BaseEntity
    {
        public required string Email { get; set; }

        // APPSCEN KALKANI[cite: 7]: Kullanıcı şifreleri veritabanında asla düz metin (plain text) 
        // olarak saklanmaz, kriptografik tuzlanmış hash algoritmasıyla (PasswordHash) tutulur.
        public required string PasswordHash { get; set; }

        public string Role { get; set; } = "User";

        public int? UpdatedById { get; set; }

        /// <summary>
        /// KRİPTOGRAFİK GÜVENLİK DAMGASI (SECURITY STAMP)
        /// MİMARİ KALKAN[cite: 7]: Kullanıcının şifresi değiştiğinde veya hesabı askıya alındığında bu 
        /// Guid değeri yeniden üretilir. Program.cs (OnTokenValidated) boru hattı her istekte bu damgayı kontrol 
        /// ederek, eski çalınmış veya açık kalmış JWT token'ları anında sistem dışı bırakır (İptal havuzu).
        /// </summary>
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

        // MANY-TO-MANY COLLECTIONS[cite: 7]: Çapraz sekmeli senkronizasyonu besleyen favori listesi koleksiyonları.
        public ICollection<UserFavoriteProduct> FavoriteProducts { get; set; } = new List<UserFavoriteProduct>();
        public ICollection<UserFavoriteCategory> FavoriteCategories { get; set; } = new List<UserFavoriteCategory>();
    }
}
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ToyStore.Api.Data;

namespace ToyStore.Api.Service
{
    /// <summary>
    /// DASHBOARD METRICS BUSINESS SERVICE (1.0 BAŞYAPIT VERSİYONU)
    /// MİMARİ AÇIKLAMA: Arayüz gösterge panelinin ihtiyaç duyduğu tüm aktif, pasif, 
    /// toplam ve oran metriklerini veritabanı düzeyinde tek adımda hesaplayan analitik servistir.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ToyStoreDbContext _context;

        public DashboardService(ToyStoreDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// YÖNETİCİ PANELİ DETAYLI METRİK İSTATİSTİKLERİ
        /// </summary>
        public object GetAdminStats()
        {
            // Tek bir DB turunda verileri çekebilmek için hızlı count operasyonları
            var totalProducts = _context.Products.Count();
            var activeProducts = _context.Products.Count(p => p.IsActive);
            var inactiveProducts = totalProducts - activeProducts;

            var totalCategories = _context.Categories.Count();
            var activeCategories = _context.Categories.Count(c => c.IsActive);
            var inactiveCategories = totalCategories - activeCategories;

            var totalUsers = _context.Users.Count();
            var activeUsers = _context.Users.Count(u => u.IsActive);
            var inactiveUsers = totalUsers - activeUsers;

            // Arayüze tam kırılımlı kurumsal veri modeli besleniyor
            return new
            {
                totalProducts = totalProducts,
                activeProducts = activeProducts,
                inactiveProducts = inactiveProducts,
                productActivePercentage = totalProducts > 0 ? (int)((double)activeProducts / totalProducts * 100) : 0,

                totalCategories = totalCategories,
                activeCategories = activeCategories,
                inactiveCategories = inactiveCategories,
                categoryActivePercentage = totalCategories > 0 ? (int)((double)activeCategories / totalCategories * 100) : 0,

                totalUsers = totalUsers,
                activeUsers = activeUsers,
                inactiveUsers = inactiveUsers,
                userActivePercentage = totalUsers > 0 ? (int)((double)activeUsers / totalUsers * 100) : 0
            };
        }

        /// <summary>
        /// KULLANICI BAZLI KİŞİSELLEŞTİRİLMİŞ DETAYLI PANEL İSTATİSTİKLERİ
        /// </summary>
        public object GetUserStats(string userId)
        {
            if (!int.TryParse(userId, out int userIdInt))
            {
                return new
                {
                    favoriteProducts = 0,
                    activeFavoriteProducts = 0,
                    inactiveFavoriteProducts = 0,
                    favoriteCategories = 0,
                    activeFavoriteCategories = 0,
                    inactiveFavoriteCategories = 0
                };
            }

            // 1. Ürün Favori Metrikleri Kırılımı
            var totalFavProducts = _context.UserFavoriteProducts.Count(f => f.UserId == userIdInt);
            var activeFavProducts = _context.UserFavoriteProducts.Count(f => f.UserId == userIdInt && f.Product.IsActive);
            var inactiveFavProducts = totalFavProducts - activeFavProducts;

            // 2. Kategori Favori Metrikleri Kırılımı (Çakışmayı çözen kritik nokta)
            var totalFavCategories = _context.UserFavoriteCategories.Count(f => f.UserId == userIdInt);
            var activeFavCategories = _context.UserFavoriteCategories.Count(f => f.UserId == userIdInt && f.Category.IsActive);
            var inactiveFavCategories = totalFavCategories - activeFavCategories;

            return new
            {
                favoriteProducts = totalFavProducts,
                activeFavoriteProducts = activeFavProducts,
                inactiveFavoriteProducts = inactiveFavProducts,

                favoriteCategories = totalFavCategories, // Artık arayüz gibi '3' dönecek!
                activeFavoriteCategories = activeFavCategories, // '2'
                inactiveFavoriteCategories = inactiveFavCategories // '1'
            };
        }
    }
}
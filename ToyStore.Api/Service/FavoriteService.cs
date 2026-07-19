using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToyStore.Api.Data;
using ToyStore.Api.Models;

namespace ToyStore.Api.Service
{
    /// <summary>
    /// FAVORITES INTERACTIONS BUSINESS SERVICE
    /// MİMARİ AÇIKLAMA[cite: 10]: Çoka çok (Many-to-Many) ilişkisel favori modellerinin 
    /// iş kurallarını, veri tutarlılık senaryolarını ve kaskad tetikleyicilerini yöneten operasyonel servistir.
    /// </summary>
    public class FavoriteService : IFavoriteService
    {
        private readonly ToyStoreDbContext _context;

        public FavoriteService(ToyStoreDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// TEKİL ÜRÜN FAVORİ DURUMU DEĞİŞTİRME (TOGGLE)
        /// </summary>
        public async Task<string> ToggleProductFavoriteAsync(int userId, int productId)
        {
            var existingFavorite = await _context.UserFavoriteProducts
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (existingFavorite != null)
            {
                // Durum 1[cite: 10]: Kayıt mevcutsa, kullanıcı butona tekrar bastığında sistemden temizlenir (Remove).
                _context.UserFavoriteProducts.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return "Ürün favorilerden çıkarıldı.";
            }
            else
            {
                // Durum 2[cite: 10]: Kayıt yoksa yeni bir çoka çok ilişkisel köprü nesnesi eklenir (Add).
                _context.UserFavoriteProducts.Add(new UserFavoriteProduct { UserId = userId, ProductId = productId });
                await _context.SaveChangesAsync();
                return "Ürün favorilere eklendi.";
            }
        }

        /// <summary>
        /// KOMPLE KATEGORİ VE ALT ÜRÜNLERİ FAVORİ DÖNGÜSÜ (CASCADE TOGGLE MİMARİSİ)
        /// </summary>
        public async Task<string> ToggleCategoryFavoriteAsync(int userId, int categoryId)
        {
            var existingCategoryFav = await _context.UserFavoriteCategories
                .FirstOrDefaultAsync(f => f.UserId == userId && f.CategoryId == categoryId);

            if (existingCategoryFav != null)
            {
                // ==========================================
                // SENARYO A[cite: 10]: KATEGORİYİ FAVORİDEN ÇIKARMA (CASCADE REMOVE)
                // ==========================================

                // 1. Ana kategori favori kaydını siler[cite: 10].
                _context.UserFavoriteCategories.Remove(existingCategoryFav);

                // 2. Performans Odaklı Alt Sorgu[cite: 10]: İlgili kategori altındaki tüm ürün ID'lerini çeker.
                var productIdsInCat = await _context.Products
                    .Where(p => p.CategoryId == categoryId)
                    .Select(p => p.Id)
                    .ToListAsync();

                // 3. Kullanıcının bu kategoriye ait ürünlerdeki favorilerini listeler[cite: 10].
                var productFavsToRemove = await _context.UserFavoriteProducts
                    .Where(f => f.UserId == userId && productIdsInCat.Contains(f.ProductId))
                    .ToListAsync();

                // TOPLU SİLME OPTİMİZASYONU (PERFORMANCE ZAFERİ)[cite: 10]: Döngüyle tek tek silmek yerine 
                // RemoveRange ile tek bir SQL DELETE ifadesi üreterek veri tabanını yormadan temizler.
                _context.UserFavoriteProducts.RemoveRange(productFavsToRemove);
                await _context.SaveChangesAsync();

                return "Kategori ve altındaki tüm ürünler favorilerden çıkarıldı.";
            }
            else
            {
                // ==========================================
                // SENARYO B[cite: 10]: KATEGORİYİ FAVORİYE EKLEME (CASCADE ADD)
                // ==========================================

                // 1. Ana kategori favori kaydını oluşturur[cite: 10].
                _context.UserFavoriteCategories.Add(new UserFavoriteCategory { UserId = userId, CategoryId = categoryId });

                // 2. İş Kuralı Zırhı[cite: 10]: Kategoriye bağlı sadece AKTİF (silinmemiş) ürünlerin ID'lerini çeker.
                var activeProductIdsInCat = await _context.Products
                    .Where(p => p.CategoryId == categoryId && p.IsActive)
                    .Select(p => p.Id)
                    .ToListAsync();

                // 3. Mükerrer Kayıt Önleyici[cite: 10]: Kullanıcının o kategoride önceden tekil olarak favorilediği ürünleri ayıklar.
                var alreadyFavoritedProductIds = await _context.UserFavoriteProducts
                    .Where(f => f.UserId == userId && activeProductIdsInCat.Contains(f.ProductId))
                    .Select(f => f.ProductId)
                    .ToListAsync();

                // DİNAMİK KÜME FARKI (EXCEPT LOGIC)[cite: 10]: Henüz favorilenmemiş olanları bulup toplu nesne haritası üretir.
                var productsToFavorite = activeProductIdsInCat
                    .Except(alreadyFavoritedProductIds)
                    .Select(pid => new UserFavoriteProduct { UserId = userId, ProductId = pid })
                    .ToList();

                // TOPLU EKLEME OPTİMİZASYONU[cite: 10]: Tek SQL INSERT toplu paketiyle veri tabanına mühürlenir.
                _context.UserFavoriteProducts.AddRange(productsToFavorite);
                await _context.SaveChangesAsync();

                return "Kategori ve altındaki ürünler favorilere eklendi.";
            }
        }
    }
}
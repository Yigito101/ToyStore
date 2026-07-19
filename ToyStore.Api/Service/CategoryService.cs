using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ToyStore.Api.Data;
using ToyStore.Api.DTOs;
using ToyStore.Api.Models;
using ToyStore.Api.Models.Enums;
using ToyStore.API.DTOs;
using ToyStore.API.Exceptions;

namespace ToyStore.API.Services
{
    /// <summary>
    /// CATEGORY MANAGEMENT BUSINESS SERVICE
    /// MİMARİ AÇIKLAMA: Ürün kategorilerinin listelenmesi, filtrelenmesi, soft-delete ve 
    /// restore süreçlerini yöneten, veri bütünlüğü ve performans optimizasyon odaklı iş mantığı katmanıdır.
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly ToyStoreDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoryService(ToyStoreDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// SAYFALAMALI VE FİLTRELİ KATEGORİ LİSTELEME
        /// PERFORMANS ZAFERİ: Alt ürünlerin count ve sum metriklerini tek bir SQL Select sorgusu içine 
        /// gömerek N+1 sorgu problemini engeller. Arayüze doğrudan ihtiyacı olan veriyi taşır.
        /// </summary>
        public async Task<PagedResponse<CategoryDTO>> GetAllCategoriesAsync(PaginationFilter filter, ItemStatus status)
        {
            var currentUserId = GetCurrentUserId() ?? 0;
            var query = _context.Categories.AsQueryable();

            // 1. Durum Filtresi Orkestrasyonu
            query = status switch
            {
                ItemStatus.Active => query.Where(c => c.IsActive),
                ItemStatus.Inactive => query.Where(c => !c.IsActive),
                _ => query
            };

            // =========================================================================
            // SIFIR HARDCODED: DİNAMİK EXPRESSION TABANLI SIRALAMA MOTORU (TIP GÜVENLİ)
            // MİMARİ AÇIKLAMA: Derleyicinin tip çıkarsama (CS8917) hatasını engellemek için
            // Lambda ifadeleri önce açık delege türlerine atanmış, ardından polimorfizm ile bağlanmıştır.
            // =========================================================================
            if (!string.IsNullOrEmpty(filter.SortColumn))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(typeof(Category), "c");
                System.Linq.Expressions.LambdaExpression? orderByExpression = null;

                // 1. Durum: Çoka Çok İlişkisel Favori Sıralama Expression Ağacı
                if (filter.SortColumn.Equals("IsFavorite", StringComparison.OrdinalIgnoreCase))
                {
                    System.Linq.Expressions.Expression<Func<Category, bool>> favoriteExpr = c => c.FavoritedByUsers.Any(f => f.UserId == currentUserId);
                    orderByExpression = favoriteExpr;
                }
                // 2. Durum: Dinamik Alt Tablo Ürün Adedi (Count) - AÇIK TİPLENDİRİLDİ (CS8917 Çözümü)
                else if (filter.SortColumn.Equals("ProductCount", StringComparison.OrdinalIgnoreCase))
                {
                    System.Linq.Expressions.Expression<Func<Category, int>> productCountExpr = c => c.Products.Count(p => p.IsActive);
                    orderByExpression = productCountExpr;
                }
                // 3. Durum: Dinamik Alt Tablo Stok Toplamı (Sum) - AÇIK TİPLENDİRİLDİ (CS8917 Çözümü)
                else if (filter.SortColumn.Equals("TotalStock", StringComparison.OrdinalIgnoreCase))
                {
                    System.Linq.Expressions.Expression<Func<Category, int>> totalStockExpr = c => c.Products.Where(p => p.IsActive).Sum(p => (int?)p.Stock) ?? 0;
                    orderByExpression = totalStockExpr;
                }
                // 4. Durum: Standart Fiziksel Kolonlar İçin Otomatik Yansıtma (Reflection)
                else
                {
                    var propertyInfo = typeof(Category).GetProperties()
                        .FirstOrDefault(prop => prop.Name.Equals(filter.SortColumn, StringComparison.OrdinalIgnoreCase));

                    if (propertyInfo != null)
                    {
                        var propertyAccess = System.Linq.Expressions.Expression.MakeMemberAccess(parameter, propertyInfo);
                        orderByExpression = System.Linq.Expressions.Expression.Lambda(propertyAccess, parameter);
                    }
                }

                // Üretilen dinamik Expression ağacını IQueryable boru hattına bağlama operasyonu
                if (orderByExpression != null)
                {
                    string methodName = filter.IsAscending ? "OrderBy" : "OrderByDescending";
                    var resultExpression = System.Linq.Expressions.Expression.Call(
                        typeof(Queryable),
                        methodName,
                        new Type[] { typeof(Category), orderByExpression.Body.Type },
                        query.Expression,
                        System.Linq.Expressions.Expression.Quote(orderByExpression));

                    query = query.Provider.CreateQuery<Category>(resultExpression);
                }
                else
                {
                    query = query.OrderBy(c => c.Id); // Güvenli Fallback
                }
            }
            else
            {
                query = query.OrderBy(c => c.Id);
            }

            var totalRecords = await query.CountAsync();

            // 2. Sayfalama (Server-Side Pagination) ve DTO Düzleştirme (Flattening) Boru Hattı
            var categories = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new CategoryDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    IsActive = c.IsActive,
                    RowVersion = c.RowVersion,
                    ProductCount = c.Products.Count(p => p.IsActive),
                    TotalStock = c.Products.Where(p => p.IsActive).Sum(p => (int?)p.Stock) ?? 0,
                    IsFavorite = currentUserId > 0 && c.FavoritedByUsers.Any(f => f.UserId == currentUserId),
                    FavoritedProductCount = c.Products.Count(p => p.IsActive && p.FavoritedByUsers.Any(f => f.UserId == currentUserId))
                })
                .ToListAsync();

            return new PagedResponse<CategoryDTO>(categories, filter.PageNumber, filter.PageSize, totalRecords);
        }

        /// <summary>
        /// TEKİL KATEGORİ SORGULAMA
        /// OPTİMİZASYON[cite: 9]: Mapster ProjectToType ile tüm Category nesnesini belleğe doldurmadan, 
        /// SELECT aşamasında sadece DTO'ya denk gelen SQL kolonlarını çeker (Data Projection).
        /// </summary>
        public async Task<CategoryDTO?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories
                .Where(c => c.Id == id && c.IsActive)
                .ProjectToType<CategoryDTO>()
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// YENİ KATEGORİ OLUŞTURMA İŞ MANTIĞI
        /// </summary>
        public async Task<CategoryDTO> CreateCategoryAsync(CategoryCreateDTO dto)
        {
            // İŞ KURALI KALKANI[cite: 9]: Büyük-küçük harf duyarsız benzersizlik denetimi.
            var isNameExist = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower());

            if (isNameExist)
                throw new BusinessException("Bu isimde bir kategori zaten mevcut. Kategori isimleri benzersiz olmalıdır.");

            var category = dto.Adapt<Category>();
            category.CreatedAt = DateTime.UtcNow;
            category.IsActive = true;
            category.CreatedById = GetCurrentUserId(); // Audit Trail (İşlemi Yapanın İz Takibi)[cite: 9]

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return category.Adapt<CategoryDTO>();
        }

        /// <summary>
        /// KATEGORİ GÜNCELLEME VE CONCURRENCY MANAGEMENT
        /// </summary>
        public async Task UpdateCategoryAsync(int id, CategoryUpdateDTO dto)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                throw new NotFoundException("Kategori bulunamadı.");
            if (!category.IsActive)
                throw new BusinessException("Kategori pasif durumda, bu işlem yapılamaz.");

            // Mükerrer İsim Blokajı[cite: 9]
            var isNameExist = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.Id != id);

            if (isNameExist)
                throw new BusinessException("Bu isim başka bir kategori tarafından kullanılmaktadır.");

            category.Name = dto.Name;

            // OPTIMISTIC CONCURRENCY TRIGGER[cite: 9]: Arayüzden (UI) gelen RowVersion orijinal değer olarak 
            // EF Core takip mekanizmasına (Change Tracker) mühürlenir. SQL düzeyinde sürüm doğrulaması tetiklenir.
            if (dto.RowVersion != null)
            {
                _context.Entry(category).Property(c => c.RowVersion).OriginalValue = dto.RowVersion;
            }

            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedById = GetCurrentUserId();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // ÇAKIŞMA ZIRHI[cite: 9]: Aynı anda başka bir admin veriyi değiştirdiyse 'Lost Update' 
                // hatasını önlemek adına kurumsal uyarı fırlatılır.
                throw new BusinessException("Bu kayıt siz işlem yaparken başka bir kullanıcı tarafından güncellenmiş. Lütfen sayfayı yenileyerek güncel veriler üzerinden tekrar işlem yapınız.");
            }
        }

        /// <summary>
        /// FORM DROPDOWN SELECT MENÜ BESLEYİCİ
        /// ARAYÜZ SİHİRBAZI[cite: 9]: Form ekranlarındaki select box'ların dolması için hafif liste döner. 
        /// Yönetimsel esneklik adına silinmiş (pasif) olanları da görsel etiket ekleyerek listede korur.
        /// </summary>
        public async Task<IEnumerable<CategoryDTO>> GetActiveCategoriesForDropdownAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDTO
                {
                    Id = c.Id,
                    Name = c.IsActive ? c.Name : $"{c.Name} (Pasif)", // UI Bilgilendirme Kalkanı[cite: 9]
                    IsActive = c.IsActive
                })
                .ToListAsync();
        }

        /// <summary>
        /// SOFT-DELETE İPTALİ VE VERİ KURTARMA (RESTORE) İŞ KURALLARI
        /// </summary>
        public async Task<CategoryDTO> RestoreCategoryAsync(int id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                throw new NotFoundException("Kategori bulunamadı.");
            if (category.IsActive)
                throw new BusinessException("Kategori zaten aktif durumda.");

            // İsim Çakışma Güvencesi[cite: 9]
            var isNameExist = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.IsActive && c.Id != id);

            if (isNameExist)
                throw new BusinessException($"'{category.Name}' isminde aktif bir kategori zaten mevcut. Sistem çakışmasını önlemek için eski kategoriyi kurtaramazsınız.");

            // KATEGORİ SİGORTASI (CRITICAL BUSINESS RULE)[cite: 9]: Altında stoğu olan hiçbir ürün yoksa, 
            // boş ve atıl bir kategorinin vitrine çıkmasını engellemek adına restore işlemi durdurulur.
            var hasActiveProducts = await _context.Products.AnyAsync(p => p.CategoryId == id && p.Stock > 0);
            if (!hasActiveProducts)
                throw new BusinessException("Bu kategoriye ait stoğu bulunan hiçbir ürün yoktur! Bir kategorinin aktif olabilmesi için içinde en az 1 adet stoklu ürün bulunmalıdır.");

            category.IsActive = true;
            category.DeletedAt = null;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedById = GetCurrentUserId();

            await _context.SaveChangesAsync();
            return category.Adapt<CategoryDTO>();
        }

        /// <summary>
        /// KATEGORİ SİLME (SOFT DELETE) İŞ KURALLARI
        /// </summary>
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                throw new NotFoundException("Kategori bulunamadı.");
            if (!category.IsActive)
                throw new BusinessException("Kategori zaten silinmiş durumda.");

            // REFERANS BÜTÜNLÜĞÜ KORUMASI[cite: 9]: Kategoriye bağlı aktif oyuncaklar varsa 
            // öksüz (orphan) kayıt oluşmaması için silme operasyonu iş mantığı düzeyinde bloke edilir.
            bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id && p.IsActive);
            if (hasProducts)
                throw new BusinessException("Bu kategoriye ait aktif ürünler bulunmaktadır. Lütfen önce ürünleri siliniz.");

            category.IsActive = false; // Mantıksal Silme[cite: 9]
            category.DeletedAt = DateTime.UtcNow;
            category.UpdatedById = GetCurrentUserId();

            await _context.SaveChangesAsync();
            return true;
        }

        private int? GetCurrentUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId)) return userId;
            return null;
        }
    }
}
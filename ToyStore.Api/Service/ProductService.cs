using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
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
    /// PRODUCT MANAGEMENT BUSINESS SERVICE
    /// MİMARİ AÇIKLAMA: Oyuncak (Product) kayıtlarının CRUD operasyonlarını, dinamik stok 
    /// durum otomasyonlarını ve kategori reaktif durum tetikleyicilerini yöneten çekirdek iş mantığı katmanıdır.
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly ToyStoreDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductService(ToyStoreDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// DİNAMİK FİLTRELİ VE SAYFALAMALI ÜRÜN VİTRİNİ GETİRME
        /// OPTİMİZASYON: Mapster projeksiyonu yerine manuel Select kullanılmıştır. Bu sayede, 
        /// aktif oturum sahibinin (currentUserId) verileri doğrudan SQL pipeline'ına enjekte edilerek 
        /// tek sorguda 'IsFavorite' durumu hesaplanır, mükerrer döngü maliyetleri (N+1) engellenir.
        /// </summary>
        public async Task<PagedResponse<ProductDTO>> GetAllProductsAsync(PaginationFilter filter, ItemStatus status, int? categoryId = null)
        {
            var currentUserId = GetCurrentUserId() ?? 0;
            var query = _context.Products.AsQueryable();

            // 1. Aktiflik/Pasiflik Kırılım Yönetimi
            query = status switch
            {
                ItemStatus.Active => query.Where(p => p.IsActive),
                ItemStatus.Inactive => query.Where(p => !p.IsActive),
                _ => query
            };

            // 2. Arayüz Vitrin Filtre Entegrasyonu
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // =========================================================================
            // SIFIR HARDCODED: DİNAMİK EXPRESSION TABANLI SIRALAMA MOTORU
            // Swagger'dan veya UI'dan gelen SortColumn değerini çalışma zamanında (Runtime) 
            // Reflection ile analiz ederek doğrudan ilgili entity özelliğine eşler.
            // =========================================================================
            if (!string.IsNullOrEmpty(filter.SortColumn))
            {
                // Özel Durum Yönetimi: M2M ilişkisel tablo kontrolü
                if (filter.SortColumn.Equals("IsFavorite", StringComparison.OrdinalIgnoreCase))
                {
                    query = filter.IsAscending
                        ? query.OrderBy(p => p.FavoritedByUsers.Any(f => f.UserId == currentUserId))
                        : query.OrderByDescending(p => p.FavoritedByUsers.Any(f => f.UserId == currentUserId));
                }
                else
                {
                    // Yansıtma (Reflection) ve Expression Ağacı ile dinamik property eşlemesi
                    var parameter = System.Linq.Expressions.Expression.Parameter(typeof(Product), "p");

                    // Harf duyarlılığını ortadan kaldırmak için entity üzerindeki gerçek mülk adını buluyoruz
                    var propertyInfo = typeof(Product).GetProperties()
                        .FirstOrDefault(prop => prop.Name.Equals(filter.SortColumn, StringComparison.OrdinalIgnoreCase));

                    if (propertyInfo != null)
                    {
                        var propertyAccess = System.Linq.Expressions.Expression.MakeMemberAccess(parameter, propertyInfo);
                        var orderByExpression = System.Linq.Expressions.Expression.Lambda(propertyAccess, parameter);

                        // EF Core Queryable üzerinde dinamik metot çağrısı (OrderBy / OrderByDescending)
                        string methodName = filter.IsAscending ? "OrderBy" : "OrderByDescending";
                        var resultExpression = System.Linq.Expressions.Expression.Call(
                            typeof(Queryable),
                            methodName,
                            new Type[] { typeof(Product), propertyInfo.PropertyType },
                            query.Expression,
                            System.Linq.Expressions.Expression.Quote(orderByExpression));

                        query = query.Provider.CreateQuery<Product>(resultExpression);
                    }
                    else
                    {
                        // Mülk bulunamazsa güvenli fallback olarak Id'ye göre sırala
                        query = query.OrderBy(p => p.Id);
                    }
                }
            }
            else
            {
                // Varsayılan sıralama düzeni (Sayfalama kararlılığı için zorunludur)
                query = query.OrderBy(p => p.Id);
            }

            var totalRecords = await query.CountAsync();

            // 3. Server-Side Sayfalama ve Data Flattening (Düzleştirme) Döngüsü
            var products = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new ProductDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Stock = p.Stock,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsActive = p.IsActive,
                    RowVersion = p.RowVersion,
                    IsFavorite = currentUserId > 0 && p.FavoritedByUsers.Any(f => f.UserId == currentUserId)
                })
                .ToListAsync();

            return new PagedResponse<ProductDTO>(products, filter.PageNumber, filter.PageSize, totalRecords);
        }

        /// <summary>
        /// TEKİL ÜRÜN DETAYI SORGULAMA
        /// </summary>
        public async Task<ProductDTO?> GetProductByIdAsync(int id)
        {
            // Interface kontratına tam uyumlu nullable (?) dönüşü destekler[cite: 11, 12].
            return await _context.Products
                .Where(p => p.Id == id && p.IsActive)
                .ProjectToType<ProductDTO>() // Bellek dostu projeksiyon
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// YENİ ÜRÜN EKLEME VE KATEGORİ REAKTİF UYANDIRMA
        /// </summary>
        public async Task<ProductDTO> CreateProductAsync(ProductCreateDTO dto)
        {
            // REFERANS BÜTÜNLÜĞÜ (FK SHIELD)[cite: 12]: Olmayan bir kategoriye ürün eklenmesi engellenir.
            // Kritik Karar: İş mantığı esnekliği açısından pasif kategorilere de stok eklenebilmesi sağlanmıştır.
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                throw new BusinessException("Belirtilen kategori bulunamadı.");

            var product = dto.Adapt<Product>();
            product.CreatedAt = DateTime.UtcNow;

            // STOK OTOMASYON KURALI 1[cite: 12]: Ürünün stoğu 0 ise otomatik Pasif, büyükse Aktif başlar.
            // Bu sayede vitrinde hayalet/stoksuz ürünlerin listelenmesi mimari seviyede engellenir.
            product.IsActive = dto.Stock > 0;
            product.CreatedById = GetCurrentUserId(); // Audit Trail

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // REAKTİF TETİKLEYİCİ[cite: 12]: Eklenen bu ürün sayesinde ilgili kategori 
            // otomatik olarak uyandırılabilir (IsActive = true olur).
            await EvaluateCategoryStatusAsync(product.CategoryId);

            return product.Adapt<ProductDTO>();
        }

        /// <summary>
        /// ÜRÜN GÜNCELLEME VE KATEGORİ DURUM SİHRİ
        /// REFACTOR: Arayüz kontratındaki isim değişikliği somut sınıfa yansıtılarak standartlara uyum sağlandı.
        /// </summary>
        public async Task UpdateProductAsync(int id, ProductUpdateDTO dto)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                throw new NotFoundException("Ürün bulunamadı.");

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                throw new BusinessException("Belirtilen kategori bulunamadı.");

            int oldCategoryId = product.CategoryId;

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;

            product.IsActive = dto.Stock > 0;

            if (dto.RowVersion != null)
            {
                _context.Entry(product).Property(p => p.RowVersion).OriginalValue = dto.RowVersion;
            }

            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedById = GetCurrentUserId();

            try
            {
                await _context.SaveChangesAsync();

                await EvaluateCategoryStatusAsync(product.CategoryId);

                if (oldCategoryId != product.CategoryId)
                {
                    await EvaluateCategoryStatusAsync(oldCategoryId);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new BusinessException("Bu kayıt siz işlem yaparken başka bir kullanıcı tarafından güncellenmiş. Lütfen sayfayı yenileyerek güncel veriler üzerinden tekrar işlem yapınız.");
            }
        }

        /// <summary>
        /// DİNAMİK KATEGORİ DURUM OTOMASYONU (REACTIVE STATE ENGINE)
        /// MİMARİ AÇIKLAMA[cite: 12]: Stok-Aktiflik zincirini yöneten gizli motordur. Bir kategoriye bağlı 
        /// stoğu olan canlı tek bir ürün bile kalmadıysa kategoriyi otomatik uyutur (IsActive=false); 
        /// aksine en az 1 stoklu ürün girdiğinde kategoriyi otomatik canlandırır. Vitrin tutarlılığı %100 korunur.
        /// </summary>
        private async Task EvaluateCategoryStatusAsync(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null) return;

            // KURAL 2[cite: 12]: Bu kategoriye ait en az 1 tane stoğu olan (aktif olabilecek) ürün var mı?
            bool hasActiveProducts = await _context.Products.AnyAsync(p => p.CategoryId == categoryId && p.Stock > 0);

            // Durum Değişikliği Tespiti (Gereksiz DB Update isteklerini önler)[cite: 12]
            if (category.IsActive != hasActiveProducts)
            {
                category.IsActive = hasActiveProducts;
                category.UpdatedAt = DateTime.UtcNow;
                category.UpdatedById = GetCurrentUserId();

                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// ÜRÜN MANTIKSAL SİLME (SOFT DELETE)
        /// GÜVENLİK VE AUDIT TRAIL[cite: 12]: Veri kaybını önlemek adına fiziksel silme (Hard Delete) 
        /// yerine durum bayrağı pasife çekilir (IsActive = false) ve silinme zamanı mühürlenir.
        /// </summary>
        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                // Hata İzolasyonu: Middleware katmanına iletilmek üzere kurumsal hata fırlatılır[cite: 12].
                throw new NotFoundException("Silinmek istenen ürün bulunamadı.");
            }

            product.IsActive = false;
            product.DeletedAt = DateTime.UtcNow; // O anki sunucu saati
            product.UpdatedById = GetCurrentUserId();

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// SİLİNMİŞ ÜRÜNÜ STOK KONTROLLÜ GERİ GETİRME (RESTORE)
        /// </summary>
        public async Task<ProductDTO> RestoreProductAsync(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                throw new NotFoundException("Ürün bulunamadı.");
            if (product.IsActive)
                throw new BusinessException("Ürün zaten aktif durumda.");

            // ÜRÜN SİGORTASI (CRITICAL BUSINESS RULE)[cite: 12]: Stok değeri 0 veya negatif olan 
            // bir ürünün diriltilmesi iş mantığı gereği engellenir. Önce stok güncellenmelidir.
            if (product.Stock <= 0)
                throw new BusinessException("Bu ürünün stoğu bulunmamaktadır! Aktif edebilmek için lütfen düzenleme ekranından stok ekleyin.");

            product.IsActive = true;
            product.DeletedAt = null; // Soft-delete iptali[cite: 12]
            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedById = GetCurrentUserId();

            await _context.SaveChangesAsync();

            // Ürün dirilince bağlı olduğu kategoriyi de reaktif olarak tetikliyoruz[cite: 12].
            await EvaluateCategoryStatusAsync(product.CategoryId);

            return product.Adapt<ProductDTO>();
        }

        /// <summary>
        /// HTTP CONTEXT ÜZERİNDEN GÜVENLİ AKTÖR ID OKUYUCU
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
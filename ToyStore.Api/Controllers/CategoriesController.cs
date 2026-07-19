using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ToyStore.Api.DTOs;
using ToyStore.Api.Models.Enums;
using ToyStore.API.DTOs;
using ToyStore.API.Services;

namespace ToyStore.API.Controllers
{
    /// <summary>
    /// CATEGORIES MANAGEMENT CONTROLLER
    /// MİMARİ AÇIKLAMA: Oyuncak kategorilerinin CRUD (Oluşturma, Okuma, Güncelleme, Silme) 
    /// operasyonlarını RESTful standartlara tam uyumlu şekilde yöneten servis katmanıdır.
    /// Global Exception Middleware entegrasyonu sayesinde controller katmanında try-catch blokları barındırmaz.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// KATEGORİ LİSTELEME (PAGINATED & FILTERED)
        /// PERFORMANS VE ÖLÇEKLENEBİLİRLİK: Büyük veri setlerinde sunucuyu ve veri tabanını yormamak adına 
        /// PaginationFilter ile sayfalama (Server-Side Pagination) mimarisine uygun çalışır.
        /// ItemStatus filtresi sayesinde aktif veya soft-delete edilmiş kategoriler kırılımlı olarak listelenebilir.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategories(
            [FromQuery] PaginationFilter filter,
            [FromQuery] ItemStatus status = ItemStatus.Active) // Varsayılan: Sadece Aktifler
        {
            var categories = await _categoryService.GetAllCategoriesAsync(filter, status);
            return Ok(categories);
        }

        /// <summary>
        /// TEKİL KATEGORİ DETAYI GETİRME
        /// GÜVENLİK KALKANI: Rota kısıtlaması (Route Constraint) kullanılarak id parametresinin 
        /// sadece pozitif tam sayı (min(1)) olması garanti edilmiş, geçersiz istekler doğrudan kapıda elenmiştir.
        /// </summary>
        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            // CLEAN CODE & DRY PRINCIPLE: Null kontrolü mimari gereği servis katmanına ve Global Exception handler'a 
            // devredilmiştir. Kategori bulunamazsa NotFoundException fırlatılır ve istemciye otomatik 404 dönülür.
            var category = await _categoryService.GetCategoryByIdAsync(id);
            return Ok(category);
        }

        /// <summary>
        /// YENİ KATEGORİ OLUŞTURMA
        /// YETKİLENDİRME (RBAC): Sadece 'Admin' rolüne sahip kullanıcılar yeni kategori ekleyebilir.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDTO dto)
        {
            var result = await _categoryService.CreateCategoryAsync(dto);

            // REST STANDARDI: Kaynak başarıyla üretildiğinde HTTP 201 Created durum koduyla birlikte
            // yeni kaynağın detay uç noktası (GetCategory) ve üretilen nesne istemciye döndürülür.
            return CreatedAtAction(nameof(GetCategory), new { id = result.Id }, result);
        }

        /// <summary>
        /// KATEGORİ GÜNCELLEME
        /// MİMARİ SADELEŞTİRME: URL'deki Id ile Body'deki Id uyuşmazlığı kontrolleri DTO bazında JsonIgnore
        /// veya servis katmanı mimarisine devredilerek controller'daki kod kirliliği ve mükerrer logic engellenmiştir.
        /// </summary>
        [HttpPut("{id:int:min(1)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDTO dto)
        {
            await _categoryService.UpdateCategoryAsync(id, dto);

            // REST STANDARDI: Başarılı bir güncelleme (mutasyon) işlemi sonucunda, 
            // istemciye gövdesiz HTTP 204 NoContent durum kodu dönülerek bant genişliği optimize edilir.
            return NoContent();
        }

        /// <summary>
        /// KATEGORİ SİLME (SOFT DELETE MİMARİSİ)
        /// GÜVENLİK VE VERİ TUTARLILIĞI: İlişkili ürünü olan kategorilerin silinmesi servis katmanında incelenir.
        /// Eğer kategoriye bağlı oyuncak varsa 'BusinessException' fırlatılır, altyapı bunu 400 Bad Request olarak arayüze iletir.
        /// Veri tabanından doğrudan veri silmek yerine IsActive=false (Soft Delete) kurgusu uygulanır.
        /// </summary>
        [HttpDelete("{id:int:min(1)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// DROPDOWN SELECT MENÜLERİ İÇİN HAFİFLETİLMİŞ LİSTELEME
        /// PERFORMANS OPTİMİZASYONU: Sayfalama (Pagination) maliyetine takılmaksızın UI select elementlerinin hızlıca dolması için
        /// sadece Aktif kategorilerin Id ve Name bilgilerini içeren, gereksiz ilişkisel yüklerden arındırılmış hafif DTO listesi döner.
        /// </summary>
        [HttpGet("dropdown")]
        public async Task<IActionResult> GetDropdownCategories()
        {
            var categories = await _categoryService.GetActiveCategoriesForDropdownAsync();
            return Ok(categories);
        }

        /// <summary>
        /// SOFT-DELETE İPTALİ (RESTORE)
        /// VERİ YÖNETİMİ: Yanlışlıkla silinen veya tekrar aktif edilmek istenen kategorileri veri tabanına 
        /// dokunmadan durum flag'ini güncelleyerek (IsActive = true) tek hamlede sisteme geri kazandırır.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/restore")]
        public async Task<IActionResult> RestoreCategory(int id)
        {
            await _categoryService.RestoreCategoryAsync(id);
            return Ok(new { message = "Kategori başarıyla geri getirildi (aktif edildi)." });
        }
    }
}
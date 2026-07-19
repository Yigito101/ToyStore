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
    /// PRODUCTS MANAGEMENT CONTROLLER
    /// MİMARİ AÇIKLAMA: Sistemdeki ana veri entitesi olan Oyuncak (Product) kayıtlarının CRUD 
    /// operasyonlarını ve gelişmiş arayüz filtreleme entegrasyonlarını yöneten RESTful servis katmanıdır.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// DİNAMİK ÜRÜN FİLTRELEME VE LİSTELEME
        /// SENKRONİZASYON: Sunucu ve veri tabanı performansını korumak için server-side sayfalama (PaginationFilter)
        /// altyapısı kullanır. Aktiflik durumlarının yanı sıra, opsiyonel olarak gönderilen 'categoryId' parametresi 
        /// sayesinde arayüzdeki dinamik kategori filtreleme mekanizmasını doğrudan besler.
        /// </summary>
        [Authorize] // Güvenlik: Kayıtlı tüm roller ürün listesini inceleyebilir.
        [HttpGet]
        public async Task<IActionResult> GetAllProducts(
            [FromQuery] PaginationFilter filter,
            [FromQuery] ItemStatus status = ItemStatus.Active,
            [FromQuery] int? categoryId = null)
        {
            var result = await _productService.GetAllProductsAsync(filter, status, categoryId);
            return Ok(result);
        }

        /// <summary>
        /// TEKİL ÜRÜN DETAYI GETİRME
        /// GÜVENLİK KALKANI: Rota kısıtlaması (Route Constraint) sayesinde 'id' parametresinin sadece 
        /// pozitif tam sayı (:int:min(1)) olması garanti edilerek hatalı istekler api kapısında elenir.
        /// </summary>
        [Authorize]
        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            // MİMARİ STRATEJİ: GET (okuma) sorgularında veri bulunamazsa servis katmanı null dönebilir. 
            // Controller katmanı bu null durumunu 404 NotFound yanıtına çevirerek HTTP anlamsal (semantik) kontrol yeteneğini artırır.
            if (product == null)
                return NotFound(new { message = "Ürün bulunamadı." });

            return Ok(product);
        }

        /// <summary>
        /// YENİ ÜRÜN OLUŞTURMA
        /// YETKİLENDİRME (RBAC): Yalnızca 'Admin' rolüne sahip kullanıcılar yeni oyuncak kaydı ekleyebilir.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDTO dto)
        {
            var result = await _productService.CreateProductAsync(dto);
            // REST Standartları gereği 201 Created ve kaynağa erişim adresi dönülür.
            return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
        }

        /// <summary>
        /// ÜRÜN BİLGİLERİNİ GÜNCELLEME
        /// MİMARİ REFACTOR: URL'deki ID ile Body içerisindeki ID uyuşmazlığı gibi gereksiz kod kalabalığı yaratan 
        /// doğrulamalar, DTO bazında JsonIgnore özniteliği kullanılarak veya servis katmanına devredilerek temizlenmiştir.
        /// </summary>
        [HttpPut("{id:int:min(1)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDTO dto)
        {
            await _productService.UpdateProductAsync(id, dto);
            // REST Standardı: Başarılı güncelleme mutasyonları sonucunda gövdesiz HTTP 204 NoContent döndürülür.
            return NoContent();
        }

        /// <summary>
        /// ÜRÜN SİLME (SOFT DELETE)
        /// VERİ KORUMA: Veri tabanından fiziksel veri silinmesi engellenmiş, bunun yerine durum flag'i 
        /// pasife (IsActive = false) çekilerek veri kaybı (data loss) riski ortadan kaldırılmıştır.
        /// </summary>
        [HttpDelete("{id:int:min(1)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// SILINEN ÜRÜNÜ GERİ GETİRME (RESTORE MECHANISM)
        /// ARAYÜZ ENTEGRASYONU: Admin yönetim panelinde kazara silinen veya tekrar stoğa giren pasif oyuncakları 
        /// veri tabanına dokunmadan, tek hamlede sisteme ve arayüze (IsActive = true) geri kazandırır.
        /// </summary>
        [HttpPut("{id:int:min(1)}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RestoreProduct(int id)
        {
            var result = await _productService.RestoreProductAsync(id);
            return Ok(new { message = "Ürün başarıyla geri getirildi.", data = result });
        }
    }
}
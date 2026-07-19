using System.Collections.Generic;
using System.Threading.Tasks;
using ToyStore.Api.DTOs;
using ToyStore.Api.Models.Enums;
using ToyStore.API.DTOs;


namespace ToyStore.API.Services
{
    /// <summary>
    /// PRODUCT CORE CONTRACT INTERFACE
    /// MİMARİ AÇIKLAMA[cite: 11]: En yoğun veri trafiğine sahip Oyuncak (Product) CRUD operasyonlarının 
    /// ve kategoriye göre dinamik arayüz filtreleme imzalarının merkez üssüdür.
    /// </summary>
    public interface IProductService
    {
        // Dinamik Kategori Filtreleme: İsteğe bağlı (int? categoryId = null) parametresi ile arayüz vitrinini besler[cite: 11].
        Task<PagedResponse<ProductDTO>> GetAllProductsAsync(PaginationFilter filter, ItemStatus status, int? categoryId = null);
        Task<ProductDTO?> GetProductByIdAsync(int id);
        Task<ProductDTO> CreateProductAsync(ProductCreateDTO dto);

        // REST Standardı Senkronizasyonu: Geriye HTTP 204 NoContent döndürülmesi amacıyla asenkron Task/void bırakılmıştır[cite: 11].
        Task UpdateProductAsync(int id, ProductUpdateDTO dto);

        Task<bool> DeleteProductAsync(int id);
        Task<ProductDTO> RestoreProductAsync(int id);
    }
}

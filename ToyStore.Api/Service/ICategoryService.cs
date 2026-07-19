using System.Collections.Generic;
using System.Threading.Tasks;
using ToyStore.Api.DTOs;
using ToyStore.Api.Models.Enums;
using ToyStore.API.DTOs;


namespace ToyStore.API.Services
{
    /// <summary>
    /// CATEGORY CORE CONTRACT INTERFACE
    /// MİMARİ AÇIKLAMA: Kategori CRUD, soft-delete ve UI dropdown veri akışlarının 
    /// hizmet sınırlarını tanımlayan kontrattır.
    /// </summary>
    public interface ICategoryService
    {
        // Sayfalamalı ve Filtreli Listeleme Standardı
        Task<PagedResponse<CategoryDTO>> GetAllCategoriesAsync(PaginationFilter filter, ItemStatus status);

        // Nullable Dönüş Desteği: Kayıt bulunamadığında NotFoundException yönetimini esnekleştirmek için Nullable (?) yapılmıştır[cite: 9, 11].
        Task<CategoryDTO?> GetCategoryByIdAsync(int id);

        Task<CategoryDTO> CreateCategoryAsync(CategoryCreateDTO dto);
        Task UpdateCategoryAsync(int id, CategoryUpdateDTO dto);
        Task<bool> DeleteCategoryAsync(int id);
        Task<CategoryDTO> RestoreCategoryAsync(int id);

        /// <summary>
        /// HAFİF DROPDOWN SELECT VERİ BESLEYİCİ İMZASI
        /// PERFORMANS OPTİMİZASYONU: UI tarafındaki select elementlerini doldururken 
        /// sayfalama (pagination) maliyetine takılmadan veriyi hafifletilmiş DTO listesi olarak çeker.
        /// </summary>
        Task<IEnumerable<CategoryDTO>> GetActiveCategoriesForDropdownAsync();
    }
}

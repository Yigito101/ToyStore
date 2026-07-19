using System;
using System.Collections.Generic;

namespace ToyStore.API.DTOs
{
    /// <summary>
    /// PAGED RESPONSE WRAPPER MODEL (API STANDARD)
    /// MİMARİ AÇIKLAMA: API çıktılarının kurumsal standartlara (Standart Response Format) kavuşmasını sağlayan,
    /// veriyi sayfalama meta datalarıyla (toplam kayıt, sayfa sayısı vb.) sarmalayan jenerik sınıftır.
    /// </summary>
    public class PagedResponse<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        public PagedResponse(IEnumerable<T> data, int pageNumber, int pageSize, int totalRecords)
        {
            Data = data;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalRecords = totalRecords;
            // Matematiksel tavan değer hesaplamasıyla toplam sayfa sayısı dinamik üretilir.
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        }
    }
}
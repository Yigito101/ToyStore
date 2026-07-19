using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyStore.Api.DTOs
{
    /// <summary>
    /// PAGED RESULT DTO FOR SERVICE LAYER
    /// MİMARİ AÇIKLAMA: Servis katmanından (Business Layer) Controller katmanına 
    /// sayfalanmış veri kümelerini taşırken kullanılan hafif jenerik veri transfer nesnesidir.
    /// </summary>
    public class PagedResultDTO<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
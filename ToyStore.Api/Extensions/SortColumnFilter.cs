using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyStore.API.Extensions
{
    /// <summary>
    /// SWAGGER SORT COLUMN DROPDOWN FILTER (IOperationFilter Versiyonu)
    /// MİMARİ AÇIKLAMA: Endpoint (Operation) düzeyinde çalışan bu filtre, ilgili API rotasını 
    /// analiz eder ve eğer 'SortColumn' adında bir query parametresi varsa, o endpoint'e 
    /// özel geçerli sütunları Swagger UI üzerinde şık bir dropdown listesine dönüştürür.
    /// </summary>
    public class SortColumnFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // 1. İlgili istek rotasını (Route) güvenli bir şekilde alıyoruz
            var actionRoute = context.ApiDescription?.RelativePath?.ToLower() ?? "";

            // 2. Bu endpoint içerisindeki 'SortColumn' parametresini buluyoruz
            var sortColumnParameter = operation.Parameters?
                .FirstOrDefault(p => p.Name.Equals("SortColumn", StringComparison.OrdinalIgnoreCase));

            // Eğer parametre bu endpoint'te mevcutsa dropdown dönüşümünü başlat
            if (sortColumnParameter != null)
            {
                var allowedColumns = new List<IOpenApiAny>();

                // 3. Kategori Listeleme Endpoint'i İçin Geçerli Sütunlar
                if (actionRoute.Contains("categories"))
                {
                    allowedColumns.Add(new OpenApiString("Id"));
                    allowedColumns.Add(new OpenApiString("Name"));
                    allowedColumns.Add(new OpenApiString("CreatedAt"));
                    allowedColumns.Add(new OpenApiString("IsFavorite"));
                    allowedColumns.Add(new OpenApiString("ProductCount"));
                }
                // 4. Ürün Listeleme Endpoint'i İçin Geçerli Sütunlar
                else if (actionRoute.Contains("products"))
                {
                    allowedColumns.Add(new OpenApiString("Id"));
                    allowedColumns.Add(new OpenApiString("Name"));
                    allowedColumns.Add(new OpenApiString("Price"));
                    allowedColumns.Add(new OpenApiString("Stock"));
                    allowedColumns.Add(new OpenApiString("IsFavorite"));
                    allowedColumns.Add(new OpenApiString("CreatedAt"));
                }
                // 5. Kullanıcı Listeleme Endpoint'i İçin Geçerli Sütunlar
                else if (actionRoute.Contains("users"))
                {
                    allowedColumns.Add(new OpenApiString("Id"));
                    allowedColumns.Add(new OpenApiString("Email"));
                    allowedColumns.Add(new OpenApiString("Role"));
                    allowedColumns.Add(new OpenApiString("CreatedAt"));
                }

                // Eğer kurallarımız eşleştiyse parametre şemasına enjekte et
                if (allowedColumns.Count > 0)
                {
                    sortColumnParameter.Schema.Enum = allowedColumns;
                    sortColumnParameter.Description = "";
                }
            }
        }
    }
}
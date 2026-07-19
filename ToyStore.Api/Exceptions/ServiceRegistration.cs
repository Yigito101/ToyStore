using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using ToyStore.Api.Data;
using ToyStore.Api.Service;
using ToyStore.API.Services;

namespace ToyStore.API.Extensions
{
    /// <summary>
    /// KRİPTOGRAFİK GÜVENLİ ANAHTAR ÜRETİCİSİ
    /// APPSCEN KALKANI (HIGH SECURITY): Sabit bir şifre (hardcoded secret string) kullanmak yerine,
    /// sunucu her ayağa kalktığında runtime'da bellekte 64 byte'lık (512-bit) benzersiz, rastgele bir dizi üretir.
    /// Bu sayede kaynak kod sızsa dahi JWT imza anahtarının ele geçirilmesi fiziksel olarak imkansız hale gelir.
    /// </summary>
    public static class ServerSessionSecret
    {
        public static readonly byte[] Key;

        static ServerSessionSecret()
        {
            Key = new byte[64];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(Key); // Kriptografik güvenli rastgele veri üretimi
            }
        }
    }

    /// <summary>
    /// MERKEZİ BAĞIMLILIK VE GÜVENLİK YÖNETİM UZANTISI
    /// MİMARİ AÇIKLAMA: Program.cs üzerindeki kod kirliliğini önlemek amacıyla tüm servislerin,
    /// JWT koruma kalkanlarının ve Swagger konfigürasyonlarının modüler olarak kaydedildiği alandır.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// UYGULAMA SERVİSLERİ VE RATE LIMITER ENTEGRASYONU
        /// </summary>
        public static void AddApplicationServices(this IServiceCollection services)
        {
            // Tüm iş mantığı servisleri merkezi olarak tek bir çatı altında toplandı
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductService, ProductService>();

            // REFACTOR: Program.cs altından buraya taşınarak bağımlılık yönetimi kapsüllendi.
            services.AddScoped<IFavoriteService, FavoriteService>();
            services.AddScoped<IDashboardService, DashboardService>();

            services.AddHttpContextAccessor();

            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            QueueLimit = 2,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
        }

        /// <summary>
        /// JWT KIMLIK DOGRULAMA VE DİNAMİK OTURUM (SLIDING) KALKANI
        /// </summary>
        public static void AddJwtSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(ServerSessionSecret.Key),
                    ClockSkew = TimeSpan.Zero // MİMARİ DETAY: Tolerans süresi sıfırlanarak tam zamanında expiration kontrolü sağlanır.
                };

                // --- DİNAMİK OTURUM SÜRECİ VE GÜVENLİK ZIRHI (PIPELINE) ---
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var dbContext = context.HttpContext!.RequestServices.GetRequiredService<ToyStoreDbContext>();

                        // Defansif Programlama: Farklı isimlendirme claim varyasyonları taranır.
                        var userIdString = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                           ?? context.Principal?.FindFirst("Id")?.Value
                                           ?? context.Principal?.FindFirst("id")?.Value;

                        var tokenStamp = context.Principal?.FindFirst("SecurityStamp")?.Value;

                        if (int.TryParse(userIdString, out int userId))
                        {
                            var user = await dbContext.Users.FindAsync(userId);

                            // ANLIK OTURUM DÜŞÜRME KALKANI: 
                            // Kullanıcı pasife çekilmişse veya admin/kullanıcı şifre sıfırladığı için veritabanındaki 
                            // SecurityStamp değişmişse, token süresi dolmamış olsa dahi oturum havada imha edilir.
                            if (user == null || !user.IsActive || user.SecurityStamp != tokenStamp)
                            {
                                context.Fail("Oturumunuz geçersiz kılınmıştır. Lütfen tekrar giriş yapınız.");
                                return;
                            }

                            // --- DİNAMİK SÜRE UZATMA (SLIDING EXPIRATION) MİMARİSİ ---
                            bool isSlidingEnabled = JwtConfig.EnableSlidingExpiration;

                            if (isSlidingEnabled)
                            {
                                var securityToken = context.SecurityToken as JwtSecurityToken;
                                if (securityToken != null)
                                {
                                    var remainingTime = securityToken.ValidTo - DateTime.UtcNow;

                                    int expiryMinutes = JwtConfig.ExpiryMinutes;
                                    var totalDuration = TimeSpan.FromMinutes(expiryMinutes);

                                    // KURAL: Eğer mevcut token'ın ömrünün yarısından fazlası tükenmişse otomatik yenileme tetiklenir.
                                    if (remainingTime < (totalDuration / 2))
                                    {
                                        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                                        var newToken = authService.ReissueToken(user, expiryMinutes);

                                        // Arayüzün (Frontend) token'ı yakalayabilmesi için hem header eklenir 
                                        // hem de CORS politikalarına takılmaması için Expose edilir.
                                        context.HttpContext.Response.Headers.Append("New-Token", newToken);
                                        context.HttpContext.Response.Headers.Append("Access-Control-Expose-Headers", "New-Token");
                                    }
                                }
                            }
                        }
                        else
                        {
                            context.Fail("Geçersiz Token: Kullanıcı kimliği okunamadı.");
                        }
                    }
                };
            });
        }

        /// <summary>
        /// SWAGGER OLUŞTURUCU VE DOKÜMANTASYON AYARLARI
        /// </summary>
        public static void AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                // REFACTOR: ParameterFilter yerine OperationFilter olarak boru hattına kaydedildi
                options.OperationFilter<SortColumnFilter>();

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Aldığınız JWT Token'ı buraya yapıştırın."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
            });
        }
    }
}
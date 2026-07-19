using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using ToyStore.Api.Data;
using ToyStore.API.Extensions;
using ToyStore.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı Bağlantısı
builder.Services.AddDbContext<ToyStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Dependency Injection (Mükerrer kayıtlardan tamamen arındırıldı!)
builder.Services.AddApplicationServices();

// 3. Controller ve JSON Ayarları
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "https://localhost:7080";
    options.AddPolicy("AllowUI", policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

// 4. FluentValidation Ayarları
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 5. JWT Kimlik Doğrulama Ayarları
builder.Services.AddJwtSecurity(builder.Configuration);

// 6. Swagger ve Güvenlik Ayarları
builder.Services.AddSwaggerConfiguration();

// 7. appsettings.json Parametre Bağlamaları (Statik Fallback Destekli)
if (!string.IsNullOrEmpty(builder.Configuration["Jwt:ExpiryMinutes"]))
    ToyStore.API.Extensions.JwtConfig.ExpiryMinutes = int.Parse(builder.Configuration["Jwt:ExpiryMinutes"]!);

if (!string.IsNullOrEmpty(builder.Configuration["Jwt:EnableSlidingExpiration"]))
    ToyStore.API.Extensions.JwtConfig.EnableSlidingExpiration = bool.Parse(builder.Configuration["Jwt:EnableSlidingExpiration"]!);

if (!string.IsNullOrEmpty(builder.Configuration["Pagination:DefaultPageSize"]))
    ToyStore.API.DTOs.PaginationFilter.DefaultPageSize = int.Parse(builder.Configuration["Pagination:DefaultPageSize"]!);

if (!string.IsNullOrEmpty(builder.Configuration["Pagination:MaxPageSize"]))
    ToyStore.API.DTOs.PaginationFilter.MaxPageSize = int.Parse(builder.Configuration["Pagination:MaxPageSize"]!);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors("AllowUI");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHealthChecks("/ping");

app.Run();
```markdown
# 🧸 ToyStore - Enterprise Inventory & Catalog Management System

![Version](https://img.shields.io/badge/version-1.0_Release-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Database](https://img.shields.io/badge/MSSQL-Entity_Framework_Core-red.svg)
![Frontend](https://img.shields.io/badge/Frontend-Vanilla_JS_%7C_Tailwind-38B2AC.svg)

ToyStore, Staj Kapsamında, kurumsal standartlara ve Clean Architecture (Temiz Mimari) prensiplerine tam uyumlu olarak geliştirilmiş Full-Stack bir envanter ve katalog yönetim sistemidir. Proje; arka planda Expression Trees ile inşa edilmiş dinamik filtreleme mekanizmaları ve güvenli rol bazlı yetkilendirme (RBAC) sunan güçlü bir ASP.NET Core REST API mimarisini, ön yüzde ise Tailwind CSS ile optimize edilmiş bileşen tabanlı, modüler Vanilla JS arayüzü ile harmanlamaktadır. Staj süresince edinilen kurumsal altyapı, veri yönetimi ve optimizasyon pratiklerini yansıtacak şekilde üretim ortamına hazır (production-ready) olarak tasarlanmıştır.

---

## 🏗️ Proje Klasör Hiyerarşisi

Sistem, sorumlulukların ayrılması (Separation of Concerns) prensibine göre katmanlandırılmış olup frontend katmanında modüler bileşen tabanlı Vanilla JS yapısı tercih edilmiştir.

```text
ToyStore
├── ToyStore.Api                    # BACKEND: REST API Katmanı
│   ├── Controllers                 # API Uç Noktaları (Giriş Kapıları)
│   ├── Data                        # Entity Framework Core Veritabanı Bağlamı
│   ├── DTOs                        # Veri Transfer Nesneleri (Sınır Modelleri)
│   │   └── Auth                    # Yetkilendirme ve Kullanıcı DTO Yapıları
│   ├── Exceptions                  # Merkezi İş Kuralları Hata Sınıfları
│   ├── Extensions                  # JWT ve Dinamik Sıralama Filtre Genişletmeleri
│   ├── Middlewares                 # Global Hata Yakalama (Exception Middleware)
│   ├── Migrations                  # EF Core Veritabanı Versiyon Geçmişi
│   ├── Models                      # Veritabanı Varlıkları (Domain Entities)
│   │   └── Enums                   # Sistem Karar ve Rol Sabitleri
│   ├── Service                     # İş Mantığı (Business Logic) ve Kontratlar
│   ├── Validations                 # Fluent/Manuel Doğrulama Kuralları
│   ├── appsettings.json            # Sistem ve Bağlantı Dizesi Ayarları
│   └── Program.cs                  # Uygulama Önyükleyicisi ve DI Container
│
├── ToyStore.UI                     # FRONTEND: Ön Yüz Katmanı
│   ├── Pages                       # Razor Pages Sayfa Yapıları
│   │   ├── Categories              # Kategori Modalleri ve Listeleme Ekranı
│   │   ├── Products                # Ürün Modalleri ve Listeleme Ekranı
│   │   ├── Shared                  # Ortak Düzen (_Layout) ve Akıllı Sayfalama
│   │   └── Users                   # Kullanıcı Listeleme ve Rol Yönetim Modali
│   ├── css                         # Global ve Özelleştirilmiş Stil Dosyaları
│   ├── js                          # Modüler JavaScript API ve UI Bileşenleri
│   │   ├── auth                    # Kimlik Doğrulama FETCH API ve DOM Yönetimi
│   │   ├── category                # Kategori Listeleme, Silme ve Ekleme Operasyonları
│   │   ├── dashboard               # Dinamik Grafik Verileri ve İstatistik Yönetimi
│   │   ├── product                 # Ürün Katalog Yönetimi ve Filtreleme Skriptleri
│   │   └── users                   # Kullanıcı Listeleme, Aktivasyon ve Rol Yönetimi
│   ├── appsettings.json
│   └── Program.cs
│
└── screenshots                     # GitHub Vitrin Görselleri Klasörü
├── Database                        # DATABASE: SQL Sunucu Başlatma Betikleri
│   └── ToyStore_Init.sql           # Şema ve Kurumsal Verileri İçeren SQL Script

```

---

## ⚡ Gelişmiş Teknik Yetenekler

### 1. Dinamik Sıralama Motoru (Expression Trees)

Sistemde sıralama işlemleri statik `if-else` veya `switch-case` blokları yerine, çalışma zamanında (runtime) dinamik olarak inşa edilen `Lambda Expression` ağaçları üzerinden yürütülür (`SortColumnFilter.cs`). Bu sayede yeni bir filtre kolonu eklendiğinde backend koduna dokunulması gerekmez, veritabanı doğrudan optimize edilmiş SQL kodunu çalıştırır.

### 2. Akıllı Sayfalama (Server-Driven Pagination)

Binlerce satırlık veri yığınları istemci tarafına (client) gönderilmez. `PaginationFilter` ve `PagedResultDTO` mimarisiyle sayfalama doğrudan MSSQL katmanında tamamlanarak ağ trafiği minimize edilir.

### 3. Global Exception Middleware

Uygulama içerisinde meydana gelebilecek tüm beklenmedik hatalar veya `BusinessException`, `NotFoundException` gibi iş kuralı kesintileri `ExceptionMiddlewares.cs` tarafından merkezi olarak yakalanır. İstemciye standartlaştırılmış `ErrorDetails` şemasında güvenli hata çıktıları döndürülür.

### 4. Rol Bazlı Erişim Yönetimi (RBAC)

Kullanıcılar `UserRole` ve `UserStatus` şemalarına göre sıkı bir denetime tabidir. Arayüzdeki hassas CRUD butonları ve işlem yetkileri, API'den dönen JWT kimlik bilgilerine göre dinamik olarak saklanır veya aktifleştirilir.

---

## 🛣️ REST API Endpoint Referansı

### 🔐 Kimlik Doğrulama & Oturum (Auth)

* `GET  /api/Auth/validate-session` - Mevcut oturum geçerliliğini denetler.
* `POST /api/Auth/register` - Yeni kullanıcı kaydı oluşturur (`UserRegisterDTO`).
* `POST /api/Auth/login` - Güvenli oturum açma ve token üretimi (`UserLoginDTO`).
* `POST /api/Auth/logout` - Aktif oturumu sonlandırır.
* `GET  /api/Auth/me` - Giriş yapmış güncel kullanıcı detaylarını döner.
* `PUT  /api/Auth/change-password` - Şifre güncelleme doğrulaması (`UserChangePasswordDTO`).

### 📦 Kategori Yönetimi (Categories)

* `GET  /api/Categories` - Gelişmiş sayfalama ve dinamik sıralama destekli listeleme.
* `POST /api/Categories` - Yeni kategori ekleme (`CategoryCreateDTO`).
* `GET  /api/Categories/{id}` - Tekil kategori detaylarını getirir.
* `PUT  /api/Categories/{id}` - Mevcut kategoriyi günceller (`CategoryUpdateDTO`).
* `DELETE /api/Categories/{id}` - Kategoriyi yumuşak silme (Soft Delete) ile pasife alır.
* `GET  /api/Categories/dropdown` - Ürün formları için hafifletilmiş kategori listesi döner.
* `PUT  /api/Categories/{id}/restore` - Silinmiş/Pasif kategoriyi geri yükler.

### 🧸 Ürün Yönetimi (Products)

* `GET  /api/Products` - Kategori filtreli, dinamik sıralamalı ürün katalog sorgusu.
* `POST /api/Products` - Yeni ürün ekleme (`ProductCreateDTO`).
* `GET  /api/Products/{id}` - Belirli ürünün detay verilerini getirir.
* `PUT  /api/Products/{id}` - Ürün bilgilerini günceller (`ProductUpdateDTO`).
* `DELETE /api/Products/{id}` - Ürünü sistemde yumuşak silme ile pasife çeker.
* `PUT  /api/Products/{id}/restore` - Silinen ürünü stoğa ve kataloğa geri kazandırır.

### 📊 Gösterge Paneli (Dashboard)

* `GET  /api/Dashboard/admin-stats` - Kritik stok, toplam ciro ve sistem log özetleri (Admin Özel).
* `GET  /api/Dashboard/user-stats` - Kullanıcı bazlı favori ve hareket analizleri.

### ⭐ Favori Sistemi (Favorites)

* `POST /api/Favorites/product/{productId}` - Ürünü kullanıcının favorilerine ekler/çıkarır (Toggle).
* `POST /api/Favorites/category/{categoryId}` - Kategoriyi kullanıcının favorilerine ekler/çıkarır.

### 👥 Kullanıcı Yönetimi (Users)

* `GET  /api/Users` - Tüm sistem kullanıcılarının listesi (`UserListDTO`).
* `PUT  /api/Users/deactivate/{id}` - Belirtilen kullanıcı hesabını pasife çeker.
* `PUT  /api/Users/activate/{id}` - Belirtilen kullanıcı hesabını aktifleştirir.
* `PUT  /api/Users/assign-role/{id}` - Yetki seviyesi değiştirme (`UserRole`).
* `PUT  /api/Users/admin-reset-password/{targetUserId}` - Yönetici zoruyla şifre sıfırlama (`AdminResetPasswordDTO`).

---

## ⚙️ Kurulum ve Veritabanı Senkronizasyonu

### 1. Veritabanı Şemasını ve Başlangıç Verilerini Yükleme

Projenin çalışması için gerekli olan kurumsal tablo yapıları, kısıtlamalar (`CHECK CONSTRAINTS`) ve önceden tanımlanmış sistem verilerini yeni ortamınıza aktarmak için:

1. Proje ana dizininde bulunan SQL script dosyasını (örneğin `Database/ToyStore_Init.sql`) açın.
2. Yerel SQL Server veya LocalDB instance'ınıza (`(localdb)\mssqllocaldb`) bağlanarak script'i **Execute** edin.

### 2. Uygulamayı Başlatma

Bağlantı dizesinin `appsettings.json` üzerinde yerel SQL sunucunuzla eşleştiğinden emin olduktan sonra:

```bash
# API Katmanını Başlatın
cd ToyStore.Api
dotnet run

# UI Katmanını Başlatın
cd ../ToyStore.UI
dotnet run

```

Sistem ayağa kalktığında tarayıcınız üzerinden kullanıcı veya yönetici paneline erişim sağlayabilirsiniz.

*Bu repo, modern Clean Architecture prensipleri ile hafif Vanilla JS / Tailwind önyüz mimarisinin uyumunu sergilemek üzere **v1.0** kararlı sürümüyle dökümante edilmiştir.*

---

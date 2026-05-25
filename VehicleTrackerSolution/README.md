# 🚗 Araç Takip ve Maliyet Simülatörü

!!Dikkat : Bu proje, yapay zeka destekli bir Eşli Programlama (Pair Programming) pratiği olarak geliştirilmiştir. Tüm kodlama süreci boyunca yapay zekanın katkılarıyla modern yazılım geliştirme teknikleri uygulanmıştır.!!

Bu proje, araç sahiplerinin rutin giderlerini (yakıt, bakım, sigorta) yerel ortamda güvenle takip edebilmelerini ve gelecek dönem maliyetlerini simüle edebilmelerini sağlayan **çevrimdışı (offline-first)** destekli bir masaüstü uygulamasıdır.

Bu yazılım, modern geliştirme süreçlerini deneyimlemek amacıyla yapay zeka (Gemini) ile bir **Eşli Programlama (Pair Programming)** pratiği olarak baştan sona tasarlanmış ve kodlanmıştır.

## ✨ Öne Çıkan Özellikler

* **Offline-First Mimari:** Uygulama ilk açılışta başlangıç verilerini dış bir kaynaktan (GitHub Gist JSON) çeker ve yerel **SQLite** veritabanına kaydeder. Sonraki tüm işlemler internet bağlantısı olmadan sorunsuz çalışır.
* **Maliyet Simülatörü:** Seçilen aracın markası, modeli, üretim yılı ve motor hacmine göre tahmini yıllık MTV, sigorta, periyodik bakım ve yakıt masraflarını özel algoritmalarla hesaplar.
* **Gider Takibi:** Araçlara ait harcamalar kategorize edilerek eklenebilir ve araç bazlı geçmiş gider raporları DataGridView üzerinden incelenebilir.
* **Kaskad (Bağlı) Listeler:** Araç ekleme ve simülasyon ekranlarında marka seçimine göre dinamik olarak filtrelenen model listeleri.
* **Single File Deployment:** Uygulama onlarca DLL dosyasına veya karmaşık kurulum sihirbazlarına ihtiyaç duymadan, tüm bağımlılıkları içine gömülmüş tek bir `.exe` dosyası (Self-contained) olarak çalışır.

## 🛠️ Kullanılan Teknolojiler ve Mimari

* **Dil / Framework:** C# 12, .NET 8.0 (Windows Forms)
* **Veritabanı:** SQLite
* **ORM:** Entity Framework Core (Code-First yaklaşımı ve Fluent API)
* **Tasarım Deseni:** Dependency Injection (Microsoft.Extensions.DependencyInjection), Repository Pattern, N-Tier Architecture (Katmanlı Mimari)
* **Asenkron Programlama:** UI donmalarını engellemek için `async/await` ve `Task` tabanlı tam asenkron veri erişimi.

## 🚀 Kurulum ve Çalıştırma

Proje **Single File (Tek Dosya)** olarak derlenecek şekilde yapılandırılmıştır. Kaynak kodları indirip kendiniz derlemek isterseniz:

1. Depoyu klonlayın: `git clone https://github.com/KULLANICI_ADINIZ/VehicleTrackerApp.git`
2. Çözüm (Solution) dosyasını Visual Studio ile açın.
3. Package Manager Console üzerinden tek dosya çıktısı almak için şu komutu çalıştırın:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
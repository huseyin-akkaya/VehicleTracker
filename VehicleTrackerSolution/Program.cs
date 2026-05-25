using Microsoft.Extensions.DependencyInjection;
using VehicleTracker.Business.Services;
using VehicleTracker.Business.Sync;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.Entities;

namespace VehicleTracker.UI;

/// <summary>
/// Uygulamanın ana giriş noktasıdır.
/// Dependency Injection (Bağımlılık Enjeksiyonu) konteynerinin yapılandırılması, veritabanının ayağa kaldırılması ve ilk formun yüklenmesi işlemlerini üstlenir.
/// </summary>
internal static class Program
{
	private const string GistRawUrl = "https://gist.githubusercontent.com/huseyin-akkaya/09fd9770407d228f8179a7a78c10e4b2/raw/vehicle-data-v2.json";

	/// <summary>
	/// Uygulamanın başlatıldığı ana metot.
	/// UI stillerini ayarlar, servisleri kaydeder, SQLite veritabanı dosyasının varlığını kontrol eder ve DashboardForm ile uygulamayı başlatır.
	/// </summary>
	[STAThread]
	static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.SetHighDpiMode(HighDpiMode.SystemAware);

		var services = new ServiceCollection();
		ConfigureServices(services);
		ServiceProvider provider = services.BuildServiceProvider();

		using (var scope = provider.CreateScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			context.Database.EnsureCreated();
			SeedSyncUrl(context);
		}

		var dashboard = provider.GetRequiredService<DashboardForm>();
		Application.Run(dashboard);
	}

	/// <summary>
	/// Veritabanı ilk kez oluşturulduğunda veya ayar eksik olduğunda senkronizasyon için kullanılacak GitHub Gist URL'sini Settings tablosuna yazar.
	/// </summary>
	private static void SeedSyncUrl(AppDbContext context)
	{
		var setting = context.Settings.Find("SyncUrl");
		if (setting is null)
		{
			context.Settings.Add(new Setting
			{
				Key = "SyncUrl",
				Value = GistRawUrl,
				Description = "Uzak JSON veri kaynağı",
				UpdatedAt = DateTime.UtcNow
			});
		}
		else if (string.IsNullOrWhiteSpace(setting.Value))
		{
			setting.Value = GistRawUrl;
			setting.UpdatedAt = DateTime.UtcNow;
		}
		context.SaveChanges();
	}

	/// <summary>
	/// Uygulama genelinde kullanılacak olan veritabanı bağlamını (DbContext), veri erişim katmanlarını (Repository), 
	/// iş kuralları servislerini ve arayüz formlarını Inversion of Control (IoC) prensibine uygun olarak DI konteynerine kaydeder.
	/// </summary>
	private static void ConfigureServices(IServiceCollection services)
	{
		services.AddScoped<AppDbContext>();

		services.AddTransient<VehicleRepository>();
		services.AddTransient<ExpenseRepository>();
		services.AddTransient<VehicleBrandRepository>();
		services.AddTransient<VehicleModelRepository>();
		services.AddTransient<GenericRepository<Setting>>();

		services.AddTransient<IVehicleService, VehicleService>();
		services.AddTransient<IExpenseService, ExpenseService>();
		services.AddTransient<TaxCalculatorService>();

		services.AddSingleton<HttpClient>(_ =>
			new HttpClient { Timeout = TimeSpan.FromSeconds(15) });

		services.AddTransient<SyncManager>();

		services.AddSingleton<IServiceProvider>(p => p);

		services.AddTransient<DashboardForm>();
		services.AddTransient<MainForm>();
		services.AddTransient<VehicleForm>();
		services.AddTransient<SimulatorForm>();
		services.AddTransient<ExpenseHistoryForm>();
	}
}
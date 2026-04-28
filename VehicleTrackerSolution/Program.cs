// --- UI/Program.cs ---

using Microsoft.Extensions.DependencyInjection;
using VehicleTracker.Business.Services;
using VehicleTracker.Business.Sync;
using VehicleTracker.DataAccess.Context;
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.Entities;

namespace VehicleTracker.UI;


internal static class Program
{
	// !! Gist Raw URL'ini buraya yapıştır
	private const string GistRawUrl =
		"https://gist.githubusercontent.com/huseyin-akkaya/09fd9770407d228f8179a7a78c10e4b2/raw/f60eea47bd10c13e8c268c1d8eb4d4b061be2697/vehicle-data-v2.json";

	[STAThread]
	static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.SetHighDpiMode(HighDpiMode.SystemAware);

		var services = new ServiceCollection();
		ConfigureServices(services);
		ServiceProvider provider = services.BuildServiceProvider();

		// ── Veritabanı hazırlık ───────────────────────────────
		using (var scope = provider.CreateScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			context.Database.EnsureCreated();

			// SyncUrl seed — yoksa ekle, varsa güncelle
			SeedSyncUrl(context);
		}

		var mainForm = provider.GetRequiredService<MainForm>();
		Application.Run(mainForm);
	}

	// ─────────────────────────────────────────────────────────
	//  SyncUrl Seed
	// ─────────────────────────────────────────────────────────
	private static void SeedSyncUrl(AppDbContext context)
	{
		var setting = context.Settings.Find("SyncUrl");

		if (setting is null)
		{
			// İlk çalıştırma — kayıt yok, ekle
			context.Settings.Add(new Setting
			{
				Key = "SyncUrl",
				Value = GistRawUrl,
				Description = "Uzak JSON veri kaynağı (GitHub Gist Raw URL)",
				UpdatedAt = DateTime.UtcNow
			});
		}
		else if (string.IsNullOrWhiteSpace(setting.Value))
		{
			// Kayıt var ama boş — güncelle
			setting.Value = GistRawUrl;
			setting.UpdatedAt = DateTime.UtcNow;
		}
		// Dolu ise dokunma — kullanıcı farklı bir URL girmiş olabilir

		context.SaveChanges();
	}

	// ─────────────────────────────────────────────────────────
	//  DI Kayıtları
	// ─────────────────────────────────────────────────────────
	private static void ConfigureServices(IServiceCollection services)
	{
		services.AddScoped<AppDbContext>();
		services.AddTransient<VehicleModelRepository>();
		services.AddTransient<VehicleRepository>();
		services.AddTransient<ExpenseRepository>();
		services.AddTransient<VehicleBrandRepository>();
		services.AddTransient<GenericRepository<Setting>>();

		services.AddTransient<IVehicleService, VehicleService>();
		services.AddTransient<IExpenseService, ExpenseService>();
		services.AddTransient<TaxCalculatorService>();

		services.AddSingleton<HttpClient>(_ => new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(15)
		});

		services.AddTransient<SyncManager>();

		// Form'lar
		services.AddTransient<MainForm>();
		services.AddTransient<ExpenseForm>();
		services.AddTransient<SimulatorForm>();
		services.AddTransient<VehicleForm>();   // ← yeni eklendi
	}
}
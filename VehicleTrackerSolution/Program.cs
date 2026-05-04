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
	private const string GistRawUrl =
		"https://gist.githubusercontent.com/huseyin-akkaya/09fd9770407d228f8179a7a78c10e4b2/raw/ca3ba58a2770b540766b06870937fbd38ef6f348/vehicle-data-v2.json";

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

		// ← Artık DashboardForm ile başlıyor
		var dashboard = provider.GetRequiredService<DashboardForm>();
		Application.Run(dashboard);
	}

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

	private static void ConfigureServices(IServiceCollection services)
	{
		services.AddScoped<AppDbContext>();

		// Repositories
		services.AddTransient<VehicleRepository>();
		services.AddTransient<ExpenseRepository>();
		services.AddTransient<VehicleBrandRepository>();
		services.AddTransient<VehicleModelRepository>();
		services.AddTransient<GenericRepository<Setting>>();

		// Business
		services.AddTransient<IVehicleService, VehicleService>();
		services.AddTransient<IExpenseService, ExpenseService>();
		services.AddTransient<TaxCalculatorService>();

		services.AddSingleton<HttpClient>(_ =>
			new HttpClient { Timeout = TimeSpan.FromSeconds(15) });

		services.AddTransient<SyncManager>();

		// Forms
		services.AddSingleton<IServiceProvider>(p => p);  // self-registration
		services.AddTransient<DashboardForm>();
		services.AddTransient<MainForm>();
		services.AddTransient<VehicleForm>();
		services.AddTransient<SimulatorForm>();
		services.AddTransient<ExpenseHistoryForm>();
	}
}
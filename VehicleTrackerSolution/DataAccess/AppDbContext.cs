using Microsoft.EntityFrameworkCore;
using VehicleTracker.Entities;

namespace VehicleTracker.DataAccess.Context;

/// <summary>
/// Uygulamanın veritabanı işlemlerini yürüten ve SQLite bağlantısını yöneten ana Entity Framework bağlam (Context) sınıfı.
/// </summary>
public class AppDbContext : DbContext
{
	public DbSet<Vehicle> Vehicles { get; set; }
	public DbSet<VehicleBrand> VehicleBrands { get; set; }
	public DbSet<VehicleModel> VehicleModels { get; set; }
	public DbSet<Expense> Expenses { get; set; }
	public DbSet<Category> Categories { get; set; }
	public DbSet<Setting> Settings { get; set; }

	/// <summary>
	/// Veritabanı bağlantı ayarlarını yapılandırır. 
	/// Uygulama tamamen bağımsız çalışacağı için SQLite veritabanı dosyasının (VehicleDb.sqlite) direkt uygulamanın çalıştığı dizinde oluşturulması hedeflenmiştir.
	/// </summary>
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		string dbPath = Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			"VehicleDb.sqlite");

		optionsBuilder.UseSqlite($"Data Source={dbPath}");

#if DEBUG
		optionsBuilder.LogTo(Console.WriteLine,
			Microsoft.Extensions.Logging.LogLevel.Information);
#endif
	}

	/// <summary>
	/// Veritabanı tablolarının, ilişkilerinin (Foreign Key), indekslemelerinin ve varsayılan verilerin (Seed Data) Fluent API kullanılarak tanımlandığı ana metot.
	/// </summary>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		ConfigureVehicleBrand(modelBuilder);
		ConfigureVehicleModel(modelBuilder);
		ConfigureVehicle(modelBuilder);
		ConfigureCategory(modelBuilder);
		ConfigureExpense(modelBuilder);
		ConfigureSetting(modelBuilder);

		SeedData(modelBuilder);
	}

	/// <summary>
	/// Araç markaları tablosunun şema ayarlarını, kısıtlamalarını ve SQLite uyumluluğu için DateTime -> String dönüşümlerini yapılandırır.
	/// </summary>
	private static void ConfigureVehicleBrand(ModelBuilder mb)
	{
		mb.Entity<VehicleBrand>(entity =>
		{
			entity.ToTable("VehicleBrands");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
			entity.HasIndex(e => e.Name).IsUnique();
			entity.Property(e => e.Country).HasMaxLength(100);
			entity.Property(e => e.RemoteId).HasMaxLength(100);
			entity.Property(e => e.SyncedAt)
				  .HasConversion(
					  v => v.HasValue ? v.Value.ToString("o") : null,
					  v => v != null ? DateTime.Parse(v) : (DateTime?)null);
		});
	}

	/// <summary>
	/// Araç modelleri tablosunun varsayılan değerlerini (DefaultValue) atar ve markalar tablosu ile olan Cascade (Kademeli Silme) ilişkisini tanımlar.
	/// </summary>
	private static void ConfigureVehicleModel(ModelBuilder mb)
	{
		mb.Entity<VehicleModel>(entity =>
		{
			entity.ToTable("VehicleModels");
			entity.HasKey(e => e.Id);

			entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
			entity.Property(e => e.EngineVolume).HasDefaultValue(0.0);
			entity.Property(e => e.WeightKg).HasDefaultValue(0);
			entity.Property(e => e.AvgFuelConsumption).HasDefaultValue(0.0);
			entity.Property(e => e.RemoteId).HasMaxLength(100);

			entity.Property(e => e.SyncedAt)
				  .HasConversion(
					  v => v.HasValue ? v.Value.ToString("o") : null,
					  v => v != null ? DateTime.Parse(v) : (DateTime?)null);

			entity.HasIndex(e => new { e.BrandId, e.Name }).IsUnique();

			entity.HasOne(e => e.Brand)
				  .WithMany()
				  .HasForeignKey(e => e.BrandId)
				  .OnDelete(DeleteBehavior.Cascade);
		});
	}

	/// <summary>
	/// Kullanıcıya ait araçların kaydedildiği tablonun ayarlarını yapar. 
	/// Veri bütünlüğünü korumak adına marka ve model silinmelerinde "Restrict" (Kısıtlama) davranışı uygulanmıştır.
	/// </summary>
	private static void ConfigureVehicle(ModelBuilder mb)
	{
		mb.Entity<Vehicle>(entity =>
		{
			entity.ToTable("Vehicles");
			entity.HasKey(e => e.Id);

			entity.Property(e => e.Plate).IsRequired().HasMaxLength(20);
			entity.HasIndex(e => e.Plate).IsUnique();

			entity.Property(e => e.Year).IsRequired();
			entity.Property(e => e.CurrentKm).HasDefaultValue(0.0);
			entity.Property(e => e.IsActive).HasDefaultValue(true);

			entity.Property(e => e.CreatedAt).IsRequired()
				  .HasConversion(v => v.ToString("o"), v => DateTime.Parse(v));
			entity.Property(e => e.UpdatedAt).IsRequired()
				  .HasConversion(v => v.ToString("o"), v => DateTime.Parse(v));

			entity.HasOne(e => e.Brand)
				  .WithMany(b => b.Vehicles)
				  .HasForeignKey(e => e.BrandId)
				  .OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(e => e.Model)
				  .WithMany()
				  .HasForeignKey(e => e.ModelId)
				  .OnDelete(DeleteBehavior.Restrict);
		});
	}

	/// <summary>
	/// Gider kategorilerinin yapılandırması. Sistem tarafından zorunlu olan ve sonradan eklenebilir kategoriler için ayarlar içerir.
	/// </summary>
	private static void ConfigureCategory(ModelBuilder mb)
	{
		mb.Entity<Category>(entity =>
		{
			entity.ToTable("Categories");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
			entity.HasIndex(e => e.Name).IsUnique();
			entity.Property(e => e.Icon).HasMaxLength(50);
			entity.Property(e => e.ColorHex).HasMaxLength(10);
			entity.Property(e => e.IsSystem).HasDefaultValue(false);
		});
	}

	/// <summary>
	/// Araçlara ait harcamaların tutulduğu tablonun yapılandırması. Tutar kolonunun Decimal tipi ayarlanmış ve hızlı sorgulama için indeksler oluşturulmuştur.
	/// </summary>
	private static void ConfigureExpense(ModelBuilder mb)
	{
		mb.Entity<Expense>(entity =>
		{
			entity.ToTable("Expenses");
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
			entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(10).HasDefaultValue("TRY");
			entity.Property(e => e.Description).HasMaxLength(500);
			entity.Property(e => e.ReceiptPath).HasMaxLength(500);

			entity.Property(e => e.ExpenseDate).IsRequired()
				  .HasConversion(v => v.ToString("o"), v => DateTime.Parse(v));
			entity.Property(e => e.CreatedAt).IsRequired()
				  .HasConversion(v => v.ToString("o"), v => DateTime.Parse(v));

			entity.HasOne(e => e.Vehicle)
				  .WithMany(v => v.Expenses)
				  .HasForeignKey(e => e.VehicleId)
				  .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(e => e.Category)
				  .WithMany(c => c.Expenses)
				  .HasForeignKey(e => e.CategoryId)
				  .OnDelete(DeleteBehavior.Restrict);

			entity.HasIndex(e => e.VehicleId);
			entity.HasIndex(e => e.ExpenseDate);
			entity.HasIndex(e => e.CategoryId);
		});
	}

	/// <summary>
	/// Uygulama geneli dinamik ayarların tutulduğu Key-Value tablosunun yapılandırması.
	/// </summary>
	private static void ConfigureSetting(ModelBuilder mb)
	{
		mb.Entity<Setting>(entity =>
		{
			entity.ToTable("Settings");
			entity.HasKey(e => e.Key);
			entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
			entity.Property(e => e.Value).IsRequired().HasMaxLength(1000);
			entity.Property(e => e.Description).HasMaxLength(500);
			entity.Property(e => e.UpdatedAt).IsRequired()
				  .HasConversion(v => v.ToString("o"), v => DateTime.Parse(v));
		});
	}

	/// <summary>
	/// Veritabanı ilk kez oluşturulduğunda sistemin zorunlu ihtiyaç duyduğu temel harcama kategorilerini ve uygulama ayarlarını tablolara enjekte eder (Seed).
	/// </summary>
	private static void SeedData(ModelBuilder mb)
	{
		var now = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		mb.Entity<Category>().HasData(
			new Category { Id = 1, Name = "Yakıt", Icon = "fuel", IsSystem = true, ColorHex = "#F59E0B" },
			new Category { Id = 2, Name = "Bakım", Icon = "wrench", IsSystem = true, ColorHex = "#3B82F6" },
			new Category { Id = 3, Name = "Vergi", Icon = "document", IsSystem = true, ColorHex = "#EF4444" },
			new Category { Id = 4, Name = "Sigorta", Icon = "shield", IsSystem = true, ColorHex = "#8B5CF6" },
			new Category { Id = 5, Name = "Diğer", Icon = "package", IsSystem = false, ColorHex = "#6B7280" }
		);

		mb.Entity<Setting>().HasData(
			new Setting { Key = "SyncUrl", Value = "", Description = "Uzak JSON dosyası URL'si", UpdatedAt = now },
			new Setting { Key = "LastSyncAt", Value = "", Description = "Son başarılı senkronizasyon zamanı", UpdatedAt = now },
			new Setting { Key = "LastSyncVersion", Value = "0", Description = "Son indirilen veri versiyonu", UpdatedAt = now },
			new Setting { Key = "TaxCoeff_Fuel", Value = "1.0", Description = "Yakıt vergi katsayısı", UpdatedAt = now },
			new Setting { Key = "TaxCoeff_Maintain", Value = "1.0", Description = "Bakım vergi katsayısı", UpdatedAt = now },
			new Setting { Key = "DefaultCurrency", Value = "TRY", Description = "Varsayılan para birimi", UpdatedAt = now }
		);
	}
}
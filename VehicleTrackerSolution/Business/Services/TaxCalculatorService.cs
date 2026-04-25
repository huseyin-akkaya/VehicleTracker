// --- Business/Services/TaxCalculatorService.cs ---

using VehicleTracker.DataAccess.Repositories;

namespace VehicleTracker.Business.Services;

/// <summary>
/// Gider tutarına, kategoriye ve aracın yaşına göre
/// Settings tablosundaki çarpanları kullanarak vergi hesaplar.
/// </summary>
public class TaxCalculatorService
{
	private readonly GenericRepository<VehicleTracker.Entities.Setting> _settingRepository;

	// Kategori adı → Settings tablosundaki anahtar eşlemesi
	private static readonly Dictionary<string, string> CategoryToSettingKey = new(
		StringComparer.OrdinalIgnoreCase)
	{
		{ "Yakıt",   "TaxCoeff_Fuel"     },
		{ "Bakım",   "TaxCoeff_Maintain" },
		{ "Vergi",   "TaxCoeff_Tax"      },
		{ "Sigorta", "TaxCoeff_Insure"   },
		{ "Diğer",   "TaxCoeff_Other"    },
	};

	public TaxCalculatorService(
		GenericRepository<VehicleTracker.Entities.Setting> settingRepository)
	{
		_settingRepository = settingRepository;
	}

	/// <summary>
	/// Vergi tutarını hesaplar.
	/// Formül: Amount × CategoryCoeff × AgeMultiplier
	/// </summary>
	/// <param name="amount">Gider tutarı</param>
	/// <param name="categoryName">Kategori adı (Yakıt, Bakım, vb.)</param>
	/// <param name="vehicleYear">Aracın üretim yılı</param>
	/// <returns>Vergi tutarı (giderin üstüne eklenir)</returns>
	public async Task<decimal> CalculateTaxAsync(
		decimal amount,
		string categoryName,
		int vehicleYear)
	{
		decimal baseCoeff = await GetCategoryCoeffAsync(categoryName);
		decimal ageMultiplier = CalculateAgeMultiplier(vehicleYear);

		// Net vergi = tutar × (katsayı - 1) × yaş çarpanı
		// Örn: 1000 TL yakıt, katsayı 1.25, yaş çarpanı 1.10
		//      → vergi = 1000 × 0.25 × 1.10 = 275 TL
		decimal taxRate = (baseCoeff - 1.0m) * ageMultiplier;

		return Math.Round(amount * taxRate, 2);
	}

	/// <summary>
	/// Toplam maliyeti döner (gider + vergi).
	/// </summary>
	public async Task<(decimal Tax, decimal Total)> CalculateTotalCostAsync(
		decimal amount,
		string categoryName,
		int vehicleYear)
	{
		decimal tax = await CalculateTaxAsync(amount, categoryName, vehicleYear);
		return (tax, amount + tax);
	}

	// ── Yardımcı metodlar ──────────────────────────────────────

	private async Task<decimal> GetCategoryCoeffAsync(string categoryName)
	{
		// Kategori → Settings anahtarı eşlemesini bul
		if (!CategoryToSettingKey.TryGetValue(categoryName, out string? settingKey))
			settingKey = "TaxCoeff_Other";

		var settings = await _settingRepository.GetAllAsync();
		var setting = settings.FirstOrDefault(s => s.Key == settingKey);

		if (setting is null || !decimal.TryParse(
				setting.Value,
				System.Globalization.NumberStyles.Any,
				System.Globalization.CultureInfo.InvariantCulture,
				out decimal coeff))
		{
			return 1.0m; // Settings'de tanımlı değilse vergi yok
		}

		return coeff < 1.0m ? 1.0m : coeff; // 1.0'dan küçük katsayı anlamsız
	}

	/// <summary>
	/// Araç yaşına göre ek vergi çarpanı.
	/// Eski araçlar daha yüksek çarpana tabidir.
	///
	///  0-3 yıl  → ×1.00 (yeni araç, standart)
	///  4-7 yıl  → ×1.10
	///  8-15 yıl → ×1.25
	///  16+ yıl  → ×1.40
	/// </summary>
	private static decimal CalculateAgeMultiplier(int vehicleYear)
	{
		int age = DateTime.Now.Year - vehicleYear;

		return age switch
		{
			<= 3 => 1.00m,
			<= 7 => 1.10m,
			<= 15 => 1.25m,
			_ => 1.40m
		};
	}
}
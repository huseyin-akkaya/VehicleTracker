
using VehicleTracker.DataAccess.Repositories;

namespace VehicleTracker.Business.Services;

public class TaxCalculatorService
{
	private readonly GenericRepository<VehicleTracker.Entities.Setting> _settingRepository;

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

									public async Task<decimal> CalculateTaxAsync(
		decimal amount,
		string categoryName,
		int vehicleYear)
	{
		decimal baseCoeff = await GetCategoryCoeffAsync(categoryName);
		decimal ageMultiplier = CalculateAgeMultiplier(vehicleYear);

								decimal taxRate = (baseCoeff - 1.0m) * ageMultiplier;

		return Math.Round(amount * taxRate, 2);
	}

				public async Task<(decimal Tax, decimal Total)> CalculateTotalCostAsync(
		decimal amount,
		string categoryName,
		int vehicleYear)
	{
		decimal tax = await CalculateTaxAsync(amount, categoryName, vehicleYear);
		return (tax, amount + tax);
	}

	
	private async Task<decimal> GetCategoryCoeffAsync(string categoryName)
	{
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
			return 1.0m; 		}

		return coeff < 1.0m ? 1.0m : coeff; 	}

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
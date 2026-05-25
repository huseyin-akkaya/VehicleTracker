
using System.Text.Json;
using System.Text.Json.Serialization;
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.Entities;

namespace VehicleTracker.Business.Sync;

public class SyncManager
{
	private readonly HttpClient _httpClient;
	private readonly VehicleBrandRepository _brandRepository;
	private readonly VehicleModelRepository _modelRepository;   	private readonly GenericRepository<Setting> _settingRepository;

	public event EventHandler<SyncResultEventArgs>? SyncCompleted;

	public SyncManager(
		HttpClient httpClient,
		VehicleBrandRepository brandRepository,
		VehicleModelRepository modelRepository,    		GenericRepository<Setting> settingRepository)
	{
		_httpClient = httpClient;
		_brandRepository = brandRepository;
		_modelRepository = modelRepository;
		_settingRepository = settingRepository;
	}

	
	public async Task SynchronizeAsync()
	{
		try
		{
			bool isOnline = await CheckConnectivityAsync();
			if (!isOnline)
			{
				OnSyncCompleted(SyncStatus.Offline, "Çevrimdışı — yerel veriler kullanılıyor.");
				return;
			}

			string? syncUrl = await GetSettingValueAsync("SyncUrl");
			if (string.IsNullOrWhiteSpace(syncUrl))
			{
				OnSyncCompleted(SyncStatus.Skipped, "SyncUrl ayarlanmamış, senkronizasyon atlandı.");
				return;
			}

			RemoteSyncData? remoteData = await FetchRemoteDataAsync(syncUrl);
			if (remoteData is null)
			{
				OnSyncCompleted(SyncStatus.Failed, "Uzak veri indirilemedi, yerel verilerle devam ediliyor.");
				return;
			}

			int localVersion = int.TryParse(
				await GetSettingValueAsync("LastSyncVersion"), out int v) ? v : 0;

			if (remoteData.Version <= localVersion)
			{
				OnSyncCompleted(SyncStatus.UpToDate,
					$"Veri güncel (versiyon {localVersion}), senkronizasyon atlandı.");
				return;
			}

			int brandCount = await UpsertBrandsAsync(remoteData.Brands);
			int modelCount = await UpsertModelsAsync(remoteData.Models);    			int coeffCount = await UpsertTaxCoefficientsAsync(remoteData.TaxCoefficients);

			await UpdateSyncMetaAsync(remoteData.Version);

			string message =
				$"Senkronizasyon tamamlandı — " +
				$"{brandCount} marka, {modelCount} model, {coeffCount} katsayı güncellendi.";

			OnSyncCompleted(SyncStatus.Success, message);
		}
		catch (Exception ex)
		{
			OnSyncCompleted(SyncStatus.Failed,
				$"Senkronizasyon hatası (yerel verilerle devam): {ex.Message}");
		}
	}

	
	private async Task<bool> CheckConnectivityAsync()
	{
		try
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
			var response = await _httpClient.GetAsync(
				"https://www.gstatic.com/generate_204",
				HttpCompletionOption.ResponseHeadersRead,
				cts.Token);
			return response.IsSuccessStatusCode || (int)response.StatusCode == 204;
		}
		catch { return false; }
	}

	
	private async Task<RemoteSyncData?> FetchRemoteDataAsync(string url)
	{
		try
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			string jsonText = await _httpClient.GetStringAsync(url, cts.Token);
			return JsonSerializer.Deserialize<RemoteSyncData>(jsonText,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}
		catch { return null; }
	}

	
	private async Task<int> UpsertBrandsAsync(IList<RemoteBrand>? remoteBrands)
	{
		if (remoteBrands is null || remoteBrands.Count == 0) return 0;

		var localBrands = await _brandRepository.GetAllAsync();
		int count = 0;

		foreach (var remote in remoteBrands)
		{
			if (string.IsNullOrWhiteSpace(remote.Id) || string.IsNullOrWhiteSpace(remote.Name))
				continue;

			var local = localBrands.FirstOrDefault(b => b.RemoteId == remote.Id);
			if (local is not null)
			{
				local.Name = remote.Name;
				local.Country = remote.Country;
				local.SyncedAt = DateTime.UtcNow;
				await _brandRepository.UpdateAsync(local);
			}
			else
			{
				await _brandRepository.AddAsync(new VehicleBrand
				{
					Name = remote.Name,
					Country = remote.Country,
					RemoteId = remote.Id,
					SyncedAt = DateTime.UtcNow
				});
			}
			count++;
		}

		await _brandRepository.SaveChangesAsync();
		return count;
	}

	
	private async Task<int> UpsertModelsAsync(IList<RemoteModel>? remoteModels)
	{
		if (remoteModels is null || remoteModels.Count == 0) return 0;

				var localBrands = await _brandRepository.GetAllAsync();
		var brandIdMap = localBrands
			.Where(b => b.RemoteId != null)
			.ToDictionary(b => b.RemoteId!, b => b.Id);

		var localModels = await _modelRepository.GetAllAsync();
		int count = 0;

		foreach (var remote in remoteModels)
		{
			if (string.IsNullOrWhiteSpace(remote.Id)
				|| string.IsNullOrWhiteSpace(remote.Name)
				|| string.IsNullOrWhiteSpace(remote.BrandId))
				continue;

						if (!brandIdMap.TryGetValue(remote.BrandId, out int localBrandId))
				continue; 
			var local = localModels.FirstOrDefault(m => m.RemoteId == remote.Id);

			if (local is not null)
			{
								local.Name = remote.Name;
				local.BrandId = localBrandId;
				local.EngineVolume = remote.EngineVolume;
				local.WeightKg = remote.WeightKg;
				local.AvgFuelConsumption = remote.AvgFuelConsumption;
				local.SyncedAt = DateTime.UtcNow;
				await _modelRepository.UpdateAsync(local);
			}
			else
			{
								await _modelRepository.AddAsync(new VehicleModel
				{
					Name = remote.Name,
					BrandId = localBrandId,
					EngineVolume = remote.EngineVolume,
					WeightKg = remote.WeightKg,
					AvgFuelConsumption = remote.AvgFuelConsumption,
					RemoteId = remote.Id,
					SyncedAt = DateTime.UtcNow
				});
			}
			count++;
		}

		await _modelRepository.SaveChangesAsync();
		return count;
	}

	
	private async Task<int> UpsertTaxCoefficientsAsync(
		Dictionary<string, decimal>? coefficients)
	{
		if (coefficients is null || coefficients.Count == 0) return 0;

		var keyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "fuel",        "TaxCoeff_Fuel"     },
			{ "maintenance", "TaxCoeff_Maintain" },
			{ "tax",         "TaxCoeff_Tax"       },
			{ "insurance",   "TaxCoeff_Insure"   },
			{ "other",       "TaxCoeff_Other"    }
		};

		var allSettings = await _settingRepository.GetAllAsync();
		int count = 0;

		foreach (var (jsonKey, value) in coefficients)
		{
			if (!keyMap.TryGetValue(jsonKey, out string? settingKey)) continue;

			var setting = allSettings.FirstOrDefault(s => s.Key == settingKey);
			string strValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture);

			if (setting is not null)
			{
				setting.Value = strValue;
				setting.UpdatedAt = DateTime.UtcNow;
				await _settingRepository.UpdateAsync(setting);
			}
			else
			{
				await _settingRepository.AddAsync(new Setting
				{
					Key = settingKey,
					Value = strValue,
					Description = $"Uzak kaynaktan senkronize ({jsonKey})",
					UpdatedAt = DateTime.UtcNow
				});
			}
			count++;
		}

		await _settingRepository.SaveChangesAsync();
		return count;
	}

	
	private async Task UpdateSyncMetaAsync(int newVersion)
	{
		await UpsertSingleSettingAsync("LastSyncVersion", newVersion.ToString());
		await UpsertSingleSettingAsync("LastSyncAt",
			DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
		await _settingRepository.SaveChangesAsync();
	}

	private async Task UpsertSingleSettingAsync(string key, string value)
	{
		var all = await _settingRepository.GetAllAsync();
		var setting = all.FirstOrDefault(s => s.Key == key);

		if (setting is not null)
		{
			setting.Value = value;
			setting.UpdatedAt = DateTime.UtcNow;
			await _settingRepository.UpdateAsync(setting);
		}
		else
		{
			await _settingRepository.AddAsync(new Setting
			{ Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
		}
	}

	private async Task<string?> GetSettingValueAsync(string key)
	{
		var all = await _settingRepository.GetAllAsync();
		return all.FirstOrDefault(s => s.Key == key)?.Value;
	}

	private void OnSyncCompleted(SyncStatus status, string message)
		=> SyncCompleted?.Invoke(this, new SyncResultEventArgs(status, message));
}


public enum SyncStatus { Success, UpToDate, Offline, Skipped, Failed }

public class SyncResultEventArgs(SyncStatus status, string message) : EventArgs
{
	public SyncStatus Status { get; } = status;
	public string Message { get; } = message;
}

internal class RemoteSyncData
{
	[JsonPropertyName("version")]
	public int Version { get; set; }

	[JsonPropertyName("brands")]
	public List<RemoteBrand> Brands { get; set; } = new();

	[JsonPropertyName("models")]
	public List<RemoteModel> Models { get; set; } = new();    
	[JsonPropertyName("taxCoefficients")]
	public Dictionary<string, decimal> TaxCoefficients { get; set; } = new();
}

internal class RemoteBrand
{
	[JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
	[JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
	[JsonPropertyName("country")] public string? Country { get; set; }
}

internal class RemoteModel                                      {
	[JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
	[JsonPropertyName("brandId")] public string BrandId { get; set; } = string.Empty;
	[JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
	[JsonPropertyName("engineVolume")] public double EngineVolume { get; set; }
	[JsonPropertyName("weightKg")] public int WeightKg { get; set; }
	[JsonPropertyName("avgFuelConsumption")] public double AvgFuelConsumption { get; set; }
}
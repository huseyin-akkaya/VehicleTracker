// --- Business/Sync/SyncManager.cs ---

using System.Text.Json;
using System.Text.Json.Serialization;
using VehicleTracker.DataAccess.Repositories;
using VehicleTracker.Entities;

namespace VehicleTracker.Business.Sync;

/// <summary>
/// Uygulama açılışında çağrılır.
/// İnternet varsa uzak JSON'dan marka/vergi verilerini çeker ve Upsert yapar.
/// İnternet yoksa veya herhangi bir hata oluşursa sessizce yerel veriyle devam eder.
/// </summary>
public class SyncManager
{
	private readonly HttpClient _httpClient;
	private readonly VehicleBrandRepository _brandRepository;
	private readonly GenericRepository<Setting> _settingRepository;

	// Senkronizasyon durumunu UI'ya bildirmek için event
	public event EventHandler<SyncResultEventArgs>? SyncCompleted;

	public SyncManager(
		HttpClient httpClient,
		VehicleBrandRepository brandRepository,
		GenericRepository<Setting> settingRepository)
	{
		_httpClient = httpClient;
		_brandRepository = brandRepository;
		_settingRepository = settingRepository;
	}

	// ── Ana metod ─────────────────────────────────────────────

	public async Task SynchronizeAsync()
	{
		try
		{
			// ADIM 1 — Bağlantı kontrolü
			bool isOnline = await CheckConnectivityAsync();
			if (!isOnline)
			{
				OnSyncCompleted(SyncStatus.Offline, "Çevrimdışı — yerel veriler kullanılıyor.");
				return;
			}

			// ADIM 2 — Sync URL'yi oku
			string? syncUrl = await GetSettingValueAsync("SyncUrl");
			if (string.IsNullOrWhiteSpace(syncUrl))
			{
				OnSyncCompleted(SyncStatus.Skipped, "SyncUrl ayarlanmamış, senkronizasyon atlandı.");
				return;
			}

			// ADIM 3 — Uzak JSON'u indir
			RemoteSyncData? remoteData = await FetchRemoteDataAsync(syncUrl);
			if (remoteData is null)
			{
				OnSyncCompleted(SyncStatus.Failed, "Uzak veri indirilemedi, yerel verilerle devam ediliyor.");
				return;
			}

			// ADIM 4 — Versiyon karşılaştır
			int localVersion = int.TryParse(await GetSettingValueAsync("LastSyncVersion"), out int v) ? v : 0;
			if (remoteData.Version <= localVersion)
			{
				OnSyncCompleted(SyncStatus.UpToDate, $"Veri güncel (versiyon {localVersion}), senkronizasyon atlandı.");
				return;
			}

			// ADIM 5 — Marka listesini upsert et
			int brandCount = await UpsertBrandsAsync(remoteData.Brands);

			// ADIM 6 — Vergi katsayılarını upsert et
			int coeffCount = await UpsertTaxCoefficientsAsync(remoteData.TaxCoefficients);

			// ADIM 7 — Meta verileri güncelle
			await UpdateSyncMetaAsync(remoteData.Version);

			string message = $"Senkronizasyon tamamlandı — {brandCount} marka, {coeffCount} katsayı güncellendi.";
			OnSyncCompleted(SyncStatus.Success, message);
		}
		catch (Exception ex)
		{
			// Herhangi bir beklenmedik hata → sessizce yerel moda geç
			OnSyncCompleted(SyncStatus.Failed, $"Senkronizasyon hatası (yerel verilerle devam): {ex.Message}");
		}
	}

	// ── Bağlantı kontrolü ─────────────────────────────────────

	private async Task<bool> CheckConnectivityAsync()
	{
		try
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
			var response = await _httpClient.GetAsync(
				"https://www.gstatic.com/generate_204",  // Google'ın bağlantı-test endpointi
				HttpCompletionOption.ResponseHeadersRead,
				cts.Token
			);
			return response.IsSuccessStatusCode || (int)response.StatusCode == 204;
		}
		catch
		{
			return false;
		}
	}

	// ── Uzak JSON indirme ─────────────────────────────────────

	private async Task<RemoteSyncData?> FetchRemoteDataAsync(string url)
	{
		try
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			string jsonText = await _httpClient.GetStringAsync(url, cts.Token);

			return JsonSerializer.Deserialize<RemoteSyncData>(jsonText, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});
		}
		catch
		{
			return null;
		}
	}

	// ── Marka upsert ──────────────────────────────────────────

	private async Task<int> UpsertBrandsAsync(IList<RemoteBrand>? remoteBrands)
	{
		if (remoteBrands is null || remoteBrands.Count == 0) return 0;

		var localBrands = await _brandRepository.GetAllAsync();
		int count = 0;

		foreach (var remote in remoteBrands)
		{
			if (string.IsNullOrWhiteSpace(remote.Id) || string.IsNullOrWhiteSpace(remote.Name))
				continue;

			// RemoteId'ye göre yerel kaydı bul
			var local = localBrands.FirstOrDefault(b => b.RemoteId == remote.Id);

			if (local is not null)
			{
				// Güncelle
				local.Name = remote.Name;
				local.Country = remote.Country;
				local.SyncedAt = DateTime.UtcNow;
				await _brandRepository.UpdateAsync(local);
			}
			else
			{
				// Yeni ekle
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

	// ── Vergi katsayısı upsert ────────────────────────────────

	private async Task<int> UpsertTaxCoefficientsAsync(
		Dictionary<string, decimal>? coefficients)
	{
		if (coefficients is null || coefficients.Count == 0) return 0;

		// JSON anahtarı → Settings tablosu anahtarı eşlemesi
		var keyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "fuel",        "TaxCoeff_Fuel"     },
			{ "maintenance", "TaxCoeff_Maintain" },
			{ "tax",         "TaxCoeff_Tax"       },
			{ "insurance",   "TaxCoeff_Insure"   },
			{ "other",       "TaxCoeff_Other"    },
		};

		var allSettings = await _settingRepository.GetAllAsync();
		int count = 0;

		foreach (var (jsonKey, value) in coefficients)
		{
			if (!keyMap.TryGetValue(jsonKey, out string? settingKey)) continue;

			var setting = allSettings.FirstOrDefault(s => s.Key == settingKey);

			if (setting is not null)
			{
				setting.Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
				setting.UpdatedAt = DateTime.UtcNow;
				await _settingRepository.UpdateAsync(setting);
			}
			else
			{
				await _settingRepository.AddAsync(new Setting
				{
					Key = settingKey,
					Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture),
					Description = $"Uzak kaynaktan senkronize edildi ({jsonKey})",
					UpdatedAt = DateTime.UtcNow
				});
			}

			count++;
		}

		await _settingRepository.SaveChangesAsync();
		return count;
	}

	// ── Meta veri güncelleme ──────────────────────────────────

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
			{
				Key = key,
				Value = value,
				UpdatedAt = DateTime.UtcNow
			});
		}
	}

	private async Task<string?> GetSettingValueAsync(string key)
	{
		var all = await _settingRepository.GetAllAsync();
		return all.FirstOrDefault(s => s.Key == key)?.Value;
	}

	// ── Event tetikleme ───────────────────────────────────────

	private void OnSyncCompleted(SyncStatus status, string message)
		=> SyncCompleted?.Invoke(this, new SyncResultEventArgs(status, message));
}

// ─────────────────────────────────────────────────────────────
// Yardımcı tipler — aynı dosyada tutulabilir
// ─────────────────────────────────────────────────────────────

public enum SyncStatus { Success, UpToDate, Offline, Skipped, Failed }

public class SyncResultEventArgs(SyncStatus status, string message) : EventArgs
{
	public SyncStatus Status { get; } = status;
	public string Message { get; } = message;
}

/// <summary>
/// Uzak JSON yapısı.
/// GitHub Gist'teki dosyanın bu şemaya uygun olması gerekir.
/// </summary>
internal class RemoteSyncData
{
	[JsonPropertyName("version")]
	public int Version { get; set; }

	[JsonPropertyName("brands")]
	public List<RemoteBrand> Brands { get; set; } = new();

	[JsonPropertyName("taxCoefficients")]
	public Dictionary<string, decimal> TaxCoefficients { get; set; } = new();
}

internal class RemoteBrand
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("country")]
	public string? Country { get; set; }
}
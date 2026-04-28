// --- Entities/VehicleModel.cs ---

namespace VehicleTracker.Entities;

public class VehicleModel
{
	public int Id { get; set; }
	public int BrandId { get; set; }
	public string Name { get; set; } = string.Empty;

	/// <summary>Motor hacmi (litre). Elektrikli araçlar için 0.0</summary>
	public double EngineVolume { get; set; }

	/// <summary>Araç ağırlığı (kg)</summary>
	public int WeightKg { get; set; }

	/// <summary>Ortalama yakıt tüketimi (lt/100km). Elektrikli için 0.0</summary>
	public double AvgFuelConsumption { get; set; }

	/// <summary>Uzak JSON kaynağındaki özgün kimlik — Upsert için anahtar</summary>
	public string? RemoteId { get; set; }

	public DateTime? SyncedAt { get; set; }

	// Navigation property
	public VehicleBrand? Brand { get; set; }
}
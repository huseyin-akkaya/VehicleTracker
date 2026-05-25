
namespace VehicleTracker.Entities;

public class VehicleModel
{
	public int Id { get; set; }
	public int BrandId { get; set; }
	public string Name { get; set; } = string.Empty;

		public double EngineVolume { get; set; }

		public int WeightKg { get; set; }

		public double AvgFuelConsumption { get; set; }

		public string? RemoteId { get; set; }

	public DateTime? SyncedAt { get; set; }

		public VehicleBrand? Brand { get; set; }
}
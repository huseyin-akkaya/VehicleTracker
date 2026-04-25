namespace VehicleTracker.Entities;

using VehicleTracker.Entities;

public class VehicleBrand
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Country { get; set; }
	public string? RemoteId { get; set; }
	public DateTime? SyncedAt { get; set; }

	public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
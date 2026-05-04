// --- UI/ViewModels/VehicleViewModel.cs ---

using VehicleTracker.Entities;

namespace VehicleTracker.UI.ViewModels;

public class VehicleViewModel
{
	public int Id { get; }
	public string Plate { get; }
	public string BrandName { get; }
	public string ModelName { get; }   // Model.Name'den doldurulur
	public int Year { get; }
	public string CurrentKmFormatted { get; }
	public string IsActiveText { get; }

	public VehicleViewModel(Vehicle vehicle)
	{
		Id = vehicle.Id;
		Plate = vehicle.Plate;
		BrandName = vehicle.Brand?.Name ?? "—";
		ModelName = vehicle.Model?.Name ?? "—";  // ← güncellendi
		Year = vehicle.Year;
		CurrentKmFormatted = $"{vehicle.CurrentKm:N0} km";
		IsActiveText = vehicle.IsActive ? "✅ Aktif" : "❌ Pasif";
	}
}
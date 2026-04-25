// --- UI/ViewModels/VehicleViewModel.cs ---

using VehicleTracker.Entities;

namespace VehicleTracker.UI.ViewModels;

/// <summary>
/// DataGridView binding için düzleştirilmiş, formatlanmış UI modeli.
/// Entity'nin doğrudan bağlanmasını engeller — UI ile Entities arasında tampon.
/// </summary>
public class VehicleViewModel
{
	public int Id { get; }
	public string Plate { get; }
	public string BrandName { get; }
	public string ModelName { get; }
	public int Year { get; }
	public string CurrentKmFormatted { get; }
	public string IsActiveText { get; }

	public VehicleViewModel(Vehicle vehicle)
	{
		Id = vehicle.Id;
		Plate = vehicle.Plate;
		BrandName = vehicle.Brand?.Name ?? "—";
		ModelName = vehicle.ModelName;
		Year = vehicle.Year;
		CurrentKmFormatted = $"{vehicle.CurrentKm:N0} km";
		IsActiveText = vehicle.IsActive ? "✅ Aktif" : "❌ Pasif";
	}
}
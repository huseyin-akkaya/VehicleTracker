// --- Business/Services/IVehicleService.cs ---

using VehicleTracker.Entities;

namespace VehicleTracker.Business.Services;

public interface IVehicleService
{
	Task<IList<Vehicle>> GetAllVehiclesAsync();
	Task<Vehicle?> GetVehicleByIdAsync(int id);
	Task<Vehicle?> GetVehicleByPlateAsync(string plate);
	Task AddVehicleAsync(Vehicle vehicle);
	Task UpdateVehicleAsync(Vehicle vehicle);
	Task DeleteVehicleAsync(int id);

	/// <summary>
	/// Aracın kilometre sayacını günceller.
	/// Yeni değer mevcut değerden küçükse iş kuralı hatası fırlatır.
	/// </summary>
	Task UpdateKilometerAsync(int vehicleId, double newKm);
}
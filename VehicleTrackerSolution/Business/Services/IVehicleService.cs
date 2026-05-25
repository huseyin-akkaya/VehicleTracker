
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

					Task UpdateKilometerAsync(int vehicleId, double newKm);
}
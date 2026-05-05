import { useState, useEffect } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import { vehicleService, Vehicle } from "../../services/vehicleService";
import { monthlyPassService } from "../../services/monthlyPassService";

const today = new Date().toISOString().slice(0, 10);
const nextMonth = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000)
  .toISOString()
  .slice(0, 10);

export default function VehiclesManagement() {
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [showMonthlyPassForm, setShowMonthlyPassForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    plateNumber: "",
    brand: "",
    model: "",
    color: "",
    ownerId: "",
  });
  const [monthlyPassForm, setMonthlyPassForm] = useState({
    licensePlate: "",
    ownerName: "",
    ownerPhone: "",
    validFrom: today,
    validTo: nextMonth,
  });

  useEffect(() => {
    loadVehicles();
  }, []);

  const loadVehicles = async () => {
    setIsLoading(true);
    try {
      const data = await vehicleService.getAll();
      setVehicles(Array.isArray(data) ? data : data.items || []);
      setError("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load vehicles");
    } finally {
      setIsLoading(false);
    }
  };

  const handleAddClick = () => {
    setEditingId(null);
    setFormData({
      plateNumber: "",
      brand: "",
      model: "",
      color: "",
      ownerId: "",
    });
    setShowForm(true);
  };

  const handleEditClick = (vehicle: Vehicle) => {
    setEditingId(vehicle.id);
    setFormData({
      plateNumber: vehicle.plateNumber,
      brand: vehicle.brand,
      model: vehicle.model,
      color: vehicle.color,
      ownerId: vehicle.ownerId,
    });
    setShowForm(true);
  };

  const handleMonthlyPassClick = (vehicle: Vehicle) => {
    setMonthlyPassForm({
      licensePlate: vehicle.plateNumber,
      ownerName: vehicle.ownerName || "",
      ownerPhone: "",
      validFrom: today,
      validTo: nextMonth,
    });
    setShowMonthlyPassForm(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (editingId) {
        await vehicleService.update(editingId, {
          brand: formData.brand,
          model: formData.model,
          color: formData.color,
        });
      } else {
        await vehicleService.create(formData as any);
      }

      setShowForm(false);
      loadVehicles();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save vehicle");
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Are you sure you want to delete this vehicle?")) return;

    try {
      await vehicleService.delete(id);
      loadVehicles();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete vehicle");
    }
  };

  const handleMonthlyPassSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      await monthlyPassService.upsert({
        licensePlate: monthlyPassForm.licensePlate.toUpperCase().trim(),
        ownerName: monthlyPassForm.ownerName.trim(),
        ownerPhone: monthlyPassForm.ownerPhone.trim() || undefined,
        validFrom: monthlyPassForm.validFrom,
        validTo: monthlyPassForm.validTo,
        isActive: true,
      });
      setShowMonthlyPassForm(false);
      setError("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save monthly pass");
    }
  };

  return (
    <>
      <PageMeta
        title="Vehicles Management | Smart Parking Admin"
        description="Manage registered vehicles"
      />
      <div className="space-y-6">
        {/* Header */}
        <div className="flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-gray-800 dark:text-white">
              Vehicles Management
            </h1>
            <p className="text-gray-600 dark:text-gray-400">
              View, edit, and manage registered vehicles
            </p>
          </div>
          <button
            onClick={handleAddClick}
            className="px-4 py-2 rounded-lg font-medium text-white bg-blue-500 hover:bg-blue-600 transition"
          >
            + Add Vehicle
          </button>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="p-4 rounded-lg bg-red-50 dark:bg-red-900/10 border border-red-200 dark:border-red-800">
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          </div>
        )}

        {/* Form Modal */}
        {showForm && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
            <ComponentCard className="w-full max-w-md">
              <h2 className="text-xl font-semibold text-gray-800 dark:text-white mb-4">
                {editingId ? "Edit Vehicle" : "Add New Vehicle"}
              </h2>

              <form onSubmit={handleSubmit} className="space-y-4">
                <input
                  type="text"
                  placeholder="Plate Number"
                  value={formData.plateNumber}
                  onChange={(e) =>
                    setFormData({ ...formData, plateNumber: e.target.value.toUpperCase() })
                  }
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 font-mono"
                  required
                  disabled={!!editingId}
                />

                <div className="grid grid-cols-2 gap-4">
                  <input
                    type="text"
                    placeholder="Brand"
                    value={formData.brand}
                    onChange={(e) =>
                      setFormData({ ...formData, brand: e.target.value })
                    }
                    className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                    required
                  />
                  <input
                    type="text"
                    placeholder="Model"
                    value={formData.model}
                    onChange={(e) =>
                      setFormData({ ...formData, model: e.target.value })
                    }
                    className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                    required
                  />
                </div>

                <input
                  type="text"
                  placeholder="Color"
                  value={formData.color}
                  onChange={(e) =>
                    setFormData({ ...formData, color: e.target.value })
                  }
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                />

                <input
                  type="text"
                  placeholder="Owner ID"
                  value={formData.ownerId}
                  onChange={(e) =>
                    setFormData({ ...formData, ownerId: e.target.value })
                  }
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                  required
                  disabled={!!editingId}
                />

                <div className="flex gap-3">
                  <button
                    type="submit"
                    className="flex-1 px-4 py-2 rounded-lg font-medium text-white bg-blue-500 hover:bg-blue-600"
                  >
                    Save
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowForm(false)}
                    className="flex-1 px-4 py-2 rounded-lg font-medium text-gray-700 dark:text-gray-300 bg-gray-200 dark:bg-gray-600"
                  >
                    Cancel
                  </button>
                </div>
              </form>
            </ComponentCard>
          </div>
        )}

        {showMonthlyPassForm && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
            <ComponentCard title="Add Monthly Pass" className="w-full max-w-md">
              <form onSubmit={handleMonthlyPassSubmit} className="space-y-4">
                <input
                  type="text"
                  value={monthlyPassForm.licensePlate}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-gray-50 dark:bg-gray-700 font-mono"
                  disabled
                />
                <input
                  type="text"
                  placeholder="Owner name"
                  value={monthlyPassForm.ownerName}
                  onChange={(e) =>
                    setMonthlyPassForm({ ...monthlyPassForm, ownerName: e.target.value })
                  }
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                  required
                />
                <input
                  type="text"
                  placeholder="Owner phone"
                  value={monthlyPassForm.ownerPhone}
                  onChange={(e) =>
                    setMonthlyPassForm({ ...monthlyPassForm, ownerPhone: e.target.value })
                  }
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                />
                <div className="grid grid-cols-2 gap-4">
                  <input
                    type="date"
                    value={monthlyPassForm.validFrom}
                    onChange={(e) =>
                      setMonthlyPassForm({ ...monthlyPassForm, validFrom: e.target.value })
                    }
                    className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                    required
                  />
                  <input
                    type="date"
                    value={monthlyPassForm.validTo}
                    onChange={(e) =>
                      setMonthlyPassForm({ ...monthlyPassForm, validTo: e.target.value })
                    }
                    className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700"
                    required
                  />
                </div>
                <div className="flex gap-3">
                  <button
                    type="submit"
                    className="flex-1 px-4 py-2 rounded-lg font-medium text-white bg-blue-500 hover:bg-blue-600"
                  >
                    Save Pass
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowMonthlyPassForm(false)}
                    className="flex-1 px-4 py-2 rounded-lg font-medium text-gray-700 dark:text-gray-300 bg-gray-200 dark:bg-gray-600"
                  >
                    Cancel
                  </button>
                </div>
              </form>
            </ComponentCard>
          </div>
        )}

        {/* Vehicles Table */}
        <ComponentCard>
          {isLoading ? (
            <div className="text-center py-8">
              <p className="text-gray-600 dark:text-gray-400">Loading vehicles...</p>
            </div>
          ) : vehicles.length === 0 ? (
            <div className="text-center py-8">
              <p className="text-gray-600 dark:text-gray-400">No vehicles found</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b border-gray-200 dark:border-gray-700">
                  <tr>
                    <th className="text-left px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Plate Number
                    </th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Brand / Model
                    </th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Color
                    </th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Status
                    </th>
                    <th className="text-right px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {vehicles.map((vehicle) => (
                    <tr
                      key={vehicle.id}
                      className="border-b border-gray-100 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800"
                    >
                      <td className="px-4 py-3 text-gray-900 dark:text-white font-mono font-semibold">
                        {vehicle.plateNumber}
                      </td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400">
                        {vehicle.brand} {vehicle.model}
                      </td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400">
                        {vehicle.color}
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className={`px-3 py-1 rounded-full text-sm font-medium ${
                            vehicle.status === "active"
                              ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400"
                              : "bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-400"
                          }`}
                        >
                          {vehicle.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <button
                          onClick={() => handleEditClick(vehicle)}
                          className="text-blue-500 hover:text-blue-600 text-sm font-medium mr-3"
                        >
                          Edit
                        </button>
                        <button
                          onClick={() => handleMonthlyPassClick(vehicle)}
                          className="text-green-600 hover:text-green-700 text-sm font-medium mr-3"
                        >
                          Monthly Pass
                        </button>
                        <button
                          onClick={() => handleDelete(vehicle.id)}
                          className="text-red-500 hover:text-red-600 text-sm font-medium"
                        >
                          Delete
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </ComponentCard>
      </div>
    </>
  );
}

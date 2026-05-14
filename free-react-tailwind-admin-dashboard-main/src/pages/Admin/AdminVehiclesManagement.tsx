import { useEffect, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import { Customer, customerService } from "../../services/customerService";
import { CreateVehicleDto, Vehicle, vehicleService } from "../../services/vehicleService";

export default function AdminVehiclesManagement() {
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [users, setUsers] = useState<Customer[]>([]);
  const [error, setError] = useState("");
  const [form, setForm] = useState<CreateVehicleDto>({
    licensePlate: "",
    userId: "",
    vehicleType: 1,
    brand: "",
    color: "",
    isDefault: false,
  });

  const load = async () => {
    try {
      const [vehicleData, userData] = await Promise.all([vehicleService.getAll(), customerService.getAll()]);
      setVehicles(vehicleData);
      setUsers(userData);
      setError("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được danh sách xe");
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.userId) {
      setError("Vui lòng chọn chủ xe.");
      return;
    }
    try {
      await vehicleService.createByAdmin(form);
      setForm({ licensePlate: "", userId: "", vehicleType: 1, brand: "", color: "", isDefault: false });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tạo được xe");
    }
  };

  const remove = async (id: string) => {
    if (!confirm("Xóa xe này? Xe còn vé tháng hiệu lực sẽ không thể xóa.")) return;
    try {
      await vehicleService.deleteByAdmin(id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không xóa được xe");
    }
  };

  return (
    <>
      <PageMeta title="Quản lý xe | Smart Parking" description="Quản lý xe đã đăng ký" />
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Quản lý xe</h1>
          <p className="text-gray-600 dark:text-gray-400">Xem, thêm và xóa xe đã đăng ký.</p>
        </div>

        {error && <div className="rounded-lg bg-red-50 p-3 text-sm text-red-600 dark:bg-red-900/10 dark:text-red-300">{error}</div>}

        <ComponentCard title="Thêm xe">
          <form onSubmit={submit} className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
            <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Biển số" value={form.licensePlate} onChange={(e) => setForm({ ...form, licensePlate: e.target.value.toUpperCase() })} required />
            <select className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" value={form.userId || ""} onChange={(e) => setForm({ ...form, userId: e.target.value })} required>
              <option value="">Chọn chủ xe</option>
              {users.map((user) => <option key={user.id} value={user.id}>{user.fullName || user.email} ({user.roles?.join(", ")})</option>)}
            </select>
            <select className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" value={form.vehicleType} onChange={(e) => setForm({ ...form, vehicleType: Number(e.target.value) })}>
              <option value={1}>Xe máy</option>
              <option value={2}>Ô tô</option>
            </select>
            <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Hãng xe" value={form.brand} onChange={(e) => setForm({ ...form, brand: e.target.value })} required />
            <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Màu xe" value={form.color} onChange={(e) => setForm({ ...form, color: e.target.value })} required />
            <button className="rounded-lg bg-blue-500 px-4 py-2 text-sm font-medium text-white hover:bg-blue-600">Thêm xe</button>
          </form>
        </ComponentCard>

        <ComponentCard title="Xe đã đăng ký">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="border-b dark:border-gray-700">
                <tr>
                  <th className="px-4 py-3 text-left">Biển số</th>
                  <th className="px-4 py-3 text-left">Chủ xe</th>
                  <th className="px-4 py-3 text-left">Loại xe</th>
                  <th className="px-4 py-3 text-left">Hãng xe</th>
                  <th className="px-4 py-3 text-left">Màu xe</th>
                  <th className="px-4 py-3 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {vehicles.map((vehicle) => (
                  <tr key={vehicle.id} className="border-b text-sm dark:border-gray-800">
                    <td className="px-4 py-3 font-mono font-semibold text-gray-900 dark:text-white">{vehicle.licensePlate}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{vehicle.ownerName || vehicle.ownerEmail || "-"}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{String(vehicle.vehicleType)}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{vehicle.brand}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{vehicle.color}</td>
                    <td className="px-4 py-3 text-right">
                      <button onClick={() => remove(vehicle.id)} className="text-sm font-medium text-red-500 hover:text-red-600">Xóa</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </ComponentCard>
      </div>
    </>
  );
}

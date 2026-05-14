import { useEffect, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import {
  MonthlyPass,
  monthlyPassService,
} from "../../services/monthlyPassService";

const today = new Date().toISOString().slice(0, 10);
const nextMonth = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000)
  .toISOString()
  .slice(0, 10);

export default function MonthlyPasses() {
  const [passes, setPasses] = useState<MonthlyPass[]>([]);
  const [licensePlate, setLicensePlate] = useState("");
  const [ownerName, setOwnerName] = useState("");
  const [ownerPhone, setOwnerPhone] = useState("");
  const [validFrom, setValidFrom] = useState(today);
  const [validTo, setValidTo] = useState(nextMonth);
  const [isActive, setIsActive] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState("");

  const loadPasses = async () => {
    setPasses(await monthlyPassService.getAll());
  };

  useEffect(() => {
    void loadPasses();
  }, []);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setIsSaving(true);
    setMessage("");

    try {
      await monthlyPassService.upsert({
        licensePlate: licensePlate.toUpperCase().trim(),
        ownerName: ownerName.trim(),
        ownerPhone: ownerPhone.trim() || undefined,
        validFrom,
        validTo,
        isActive,
      });
      setLicensePlate("");
      setOwnerName("");
      setOwnerPhone("");
      setIsActive(true);
      setMessage("Đã lưu vé tháng.");
      await loadPasses();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Không lưu được vé tháng.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    await monthlyPassService.delete(id);
    await loadPasses();
  };

  return (
    <>
      <PageMeta
        title="Vé tháng | Smart Parking"
        description="Quản lý xe đăng ký vé tháng"
      />

      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">
            Vé tháng
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            Xe trong danh sách này được ra bãi theo hiệu lực vé tháng.
          </p>
        </div>

        {message && (
          <div className="rounded-lg border border-blue-200 bg-blue-50 p-3 text-sm text-blue-700 dark:border-blue-800 dark:bg-blue-900/20 dark:text-blue-300">
            {message}
          </div>
        )}

        <ComponentCard title="Thêm hoặc cập nhật vé tháng">
          <form onSubmit={handleSubmit} className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <input
              value={licensePlate}
              onChange={(event) => setLicensePlate(event.target.value.toUpperCase())}
              placeholder="Biển số"
              className="rounded-lg border border-gray-300 px-3 py-2 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
              required
            />
            <input
              value={ownerName}
              onChange={(event) => setOwnerName(event.target.value)}
              placeholder="Tên chủ xe"
              className="rounded-lg border border-gray-300 px-3 py-2 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
              required
            />
            <input
              value={ownerPhone}
              onChange={(event) => setOwnerPhone(event.target.value)}
              placeholder="Số điện thoại"
              className="rounded-lg border border-gray-300 px-3 py-2 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
            />
            <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(event) => setIsActive(event.target.checked)}
              />
              Đang hiệu lực
            </label>
            <input
              type="date"
              value={validFrom}
              onChange={(event) => setValidFrom(event.target.value)}
              className="rounded-lg border border-gray-300 px-3 py-2 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
            />
            <input
              type="date"
              value={validTo}
              onChange={(event) => setValidTo(event.target.value)}
              className="rounded-lg border border-gray-300 px-3 py-2 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
            />
            <button
              disabled={isSaving}
              className="rounded-lg bg-blue-600 px-4 py-2 font-semibold text-white hover:bg-blue-700 disabled:bg-gray-400 md:col-span-2"
            >
              {isSaving ? "Đang lưu..." : "Lưu vé tháng"}
            </button>
          </form>
        </ComponentCard>

        <ComponentCard title="Xe có vé tháng">
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b text-left text-gray-500 dark:border-gray-800">
                  <th className="px-3 py-2">Biển số</th>
                  <th className="px-3 py-2">Chủ xe</th>
                  <th className="px-3 py-2">Điện thoại</th>
                  <th className="px-3 py-2">Hiệu lực</th>
                  <th className="px-3 py-2">Trạng thái</th>
                  <th className="px-3 py-2">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {passes.map((pass) => (
                  <tr key={pass.id} className="border-b dark:border-gray-800">
                    <td className="px-3 py-2 font-mono font-semibold">{pass.licensePlate}</td>
                    <td className="px-3 py-2">{pass.ownerName}</td>
                    <td className="px-3 py-2">{pass.ownerPhone || "-"}</td>
                    <td className="px-3 py-2">
                      {new Date(pass.validFrom).toLocaleDateString()} - {new Date(pass.validTo).toLocaleDateString()}
                    </td>
                    <td className="px-3 py-2">{pass.isActive ? "Đang hiệu lực" : "Ngưng hiệu lực"}</td>
                    <td className="px-3 py-2">
                      <button
                        onClick={() => void handleDelete(pass.id)}
                        className="rounded bg-red-500 px-3 py-1 text-white hover:bg-red-600"
                      >
                        Xóa
                      </button>
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

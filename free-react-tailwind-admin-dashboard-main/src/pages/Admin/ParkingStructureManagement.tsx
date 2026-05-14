import { useEffect, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import { Branch, BranchFull, parkingStructureService } from "../../services/parkingStructureService";

export default function ParkingStructureManagement() {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [selectedBranchId, setSelectedBranchId] = useState("");
  const [branchFull, setBranchFull] = useState<BranchFull | null>(null);
  const [error, setError] = useState("");
  const [branchForm, setBranchForm] = useState({ name: "", address: "" });
  const [lotName, setLotName] = useState("");
  const [zoneForm, setZoneForm] = useState({ parkingLotId: "", name: "", vehicleType: "Motorbike" });
  const [slotForm, setSlotForm] = useState({ zoneId: "", slotCode: "" });

  const loadBranches = async () => {
    const data = await parkingStructureService.branches();
    setBranches(data);
    const nextId = selectedBranchId || data[0]?.id || "";
    setSelectedBranchId(nextId);
    if (nextId) setBranchFull(await parkingStructureService.branchFull(nextId));
  };

  const reloadFull = async (branchId = selectedBranchId) => {
    if (!branchId) return;
    setBranchFull(await parkingStructureService.branchFull(branchId));
  };

  useEffect(() => {
    loadBranches().catch((err) => setError(err instanceof Error ? err.message : "Không tải được cấu trúc bãi"));
  }, []);

  const run = async (action: () => Promise<void>) => {
    try {
      await action();
      setError("");
      await loadBranches();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Thao tác thất bại");
    }
  };

  return (
    <>
      <PageMeta title="Cấu trúc bãi | Smart Parking" description="Quản lý chi nhánh, bãi, khu và cột" />
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Cấu trúc bãi</h1>
          <p className="text-gray-600 dark:text-gray-400">Quản lý bố cục theo chi nhánh, bãi, khu và cột.</p>
        </div>

        {error && <div className="rounded-lg bg-red-50 p-3 text-sm text-red-600 dark:bg-red-900/10 dark:text-red-300">{error}</div>}

        <ComponentCard title="Chi nhánh">
          <div className="grid grid-cols-1 gap-3 lg:grid-cols-[1fr_1fr_1fr_auto]">
            <select className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" value={selectedBranchId} onChange={async (e) => { setSelectedBranchId(e.target.value); setBranchFull(await parkingStructureService.branchFull(e.target.value)); }}>
              {branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}
            </select>
            <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Tên chi nhánh mới" value={branchForm.name} onChange={(e) => setBranchForm({ ...branchForm, name: e.target.value })} />
            <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Địa chỉ" value={branchForm.address} onChange={(e) => setBranchForm({ ...branchForm, address: e.target.value })} />
            <button className="rounded-lg bg-blue-500 px-4 py-2 text-sm font-medium text-white hover:bg-blue-600" onClick={() => run(async () => { await parkingStructureService.createBranch(branchForm); setBranchForm({ name: "", address: "" }); })}>Thêm chi nhánh</button>
          </div>
        </ComponentCard>

        {branchFull && (
          <ComponentCard title={`Bố cục ${branchFull.name}`}>
            <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-[1fr_auto]">
              <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Tên bãi xe" value={lotName} onChange={(e) => setLotName(e.target.value)} />
              <button className="rounded-lg bg-blue-500 px-4 py-2 text-sm font-medium text-white hover:bg-blue-600" onClick={() => run(async () => { await parkingStructureService.createParkingLot({ name: lotName, branchId: branchFull.id }); setLotName(""); })}>Thêm bãi xe</button>
            </div>

            <div className="space-y-5">
              {branchFull.parkingLots.map((lot) => (
                <div key={lot.id} className="rounded-lg border border-gray-200 p-4 dark:border-gray-800">
                  <div className="mb-3 flex items-center justify-between gap-3">
                    <h3 className="text-lg font-semibold text-gray-800 dark:text-white">{lot.name}</h3>
                    <button className="text-sm font-medium text-red-500" onClick={() => run(() => parkingStructureService.deleteParkingLot(lot.id))}>Xóa bãi</button>
                  </div>

                  <div className="mb-3 grid grid-cols-1 gap-3 md:grid-cols-[1fr_180px_auto]">
                    <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Tên khu" value={zoneForm.parkingLotId === lot.id ? zoneForm.name : ""} onChange={(e) => setZoneForm({ ...zoneForm, parkingLotId: lot.id, name: e.target.value })} />
                    <select className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" value={zoneForm.parkingLotId === lot.id ? zoneForm.vehicleType : "Motorbike"} onChange={(e) => setZoneForm({ ...zoneForm, parkingLotId: lot.id, vehicleType: e.target.value })}>
                      <option value="Motorbike">Xe máy</option>
                      <option value="Car">Ô tô</option>
                    </select>
                    <button className="rounded-lg bg-gray-800 px-4 py-2 text-sm font-medium text-white" onClick={() => run(async () => { await parkingStructureService.createZone({ ...zoneForm, parkingLotId: lot.id }); setZoneForm({ parkingLotId: "", name: "", vehicleType: "Motorbike" }); await reloadFull(); })}>Thêm khu</button>
                  </div>

                  <div className="space-y-3">
                    {lot.zones.map((zone) => (
                      <div key={zone.id} className="rounded-lg bg-gray-50 p-3 dark:bg-gray-900/30">
                        <div className="mb-2 flex items-center justify-between">
                          <p className="font-medium text-gray-800 dark:text-white">{zone.name} <span className="text-xs text-gray-500">({zone.vehicleType})</span></p>
                          <button className="text-xs font-medium text-red-500" onClick={() => run(() => parkingStructureService.deleteZone(zone.id))}>Xóa khu</button>
                        </div>
                        <div className="mb-2 flex gap-2">
                          <input className="min-w-0 flex-1 rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Mã cột / vị trí" value={slotForm.zoneId === zone.id ? slotForm.slotCode : ""} onChange={(e) => setSlotForm({ zoneId: zone.id, slotCode: e.target.value })} />
                          <button className="rounded-lg bg-blue-500 px-3 py-2 text-sm text-white" onClick={() => run(async () => { await parkingStructureService.createSlot({ zoneId: zone.id, slotCode: slotForm.slotCode }); setSlotForm({ zoneId: "", slotCode: "" }); await reloadFull(); })}>Thêm</button>
                        </div>
                        <div className="flex flex-wrap gap-2">
                          {zone.slots.map((slot) => (
                            <span key={slot.id} className="inline-flex items-center gap-2 rounded-full bg-white px-3 py-1 text-xs text-gray-700 dark:bg-gray-800 dark:text-gray-300">
                              {slot.slotCode} - {slot.status}
                              <button className="text-red-500" onClick={() => run(() => parkingStructureService.deleteSlot(slot.id))}>x</button>
                            </span>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </ComponentCard>
        )}
      </div>
    </>
  );
}

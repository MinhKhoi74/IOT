import { useEffect, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import { useAuth } from "../../context/AuthContext";
import { Branch, BranchFull, ParkingMap, ParkingMapElement, parkingStructureService } from "../../services/parkingStructureService";

export default function ParkingStructureManagement() {
  const { user } = useAuth();
  const isAdmin = (user?.roles || user?.Roles || []).includes("Admin") || user?.role === "Admin";
  const [branches, setBranches] = useState<Branch[]>([]);
  const [selectedBranchId, setSelectedBranchId] = useState("");
  const [branchFull, setBranchFull] = useState<BranchFull | null>(null);
  const [error, setError] = useState("");
  const [branchForm, setBranchForm] = useState({ name: "", address: "", maxVehicleCapacity: 100 });
  const [capacityValue, setCapacityValue] = useState("100");
  const [lotName, setLotName] = useState("");
  const [zoneForm, setZoneForm] = useState({ parkingLotId: "", name: "", vehicleType: "Motorbike" });
  const [slotForm, setSlotForm] = useState({ zoneId: "", slotCode: "" });
  const [parkingMap, setParkingMap] = useState<ParkingMap | null>(null);
  const [draggingElementId, setDraggingElementId] = useState("");
  const [customElement, setCustomElement] = useState({ type: "barrier", label: "" });
  const [wallDirection, setWallDirection] = useState<"horizontal" | "vertical">("horizontal");
  const gridColumns = 40;
  const gridRows = 24;

  const normalizeMap = (map: ParkingMap): ParkingMap => ({
    ...map,
    width: map.width > 100 ? gridColumns : map.width || gridColumns,
    height: map.height > 100 ? gridRows : map.height || gridRows,
    elements: (map.elements || []).map((item) => ({
      ...item,
      x: map.width > 100 ? Math.round((item.x / map.width) * gridColumns) : Math.round(item.x),
      y: map.height > 100 ? Math.round((item.y / map.height) * gridRows) : Math.round(item.y),
      width: map.width > 100 ? Math.max(1, Math.round((item.width / map.width) * gridColumns)) : Math.max(1, Math.round(item.width)),
      height: map.height > 100 ? Math.max(1, Math.round((item.height / map.height) * gridRows)) : Math.max(1, Math.round(item.height)),
    })),
  });

  const loadBranches = async () => {
    const data = await parkingStructureService.branches();
    setBranches(data);
    const nextId = selectedBranchId || data[0]?.id || "";
    setSelectedBranchId(nextId);
    if (nextId) {
      const full = await parkingStructureService.branchFull(nextId);
      setBranchFull(full);
      setCapacityValue(String(full.maxVehicleCapacity || 100));
      setParkingMap(normalizeMap(await parkingStructureService.parkingMap(nextId)));
    }
  };

  const reloadFull = async (branchId = selectedBranchId) => {
    if (!branchId) return;
    const full = await parkingStructureService.branchFull(branchId);
    setBranchFull(full);
    setCapacityValue(String(full.maxVehicleCapacity || 100));
    setParkingMap(normalizeMap(await parkingStructureService.parkingMap(branchId)));
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

  const updateMapElement = (id: string, patch: Partial<ParkingMapElement>) => {
    setParkingMap((current) => current
      ? {
          ...current,
          elements: current.elements.map((item) => item.id === id ? { ...item, ...patch } : item),
        }
      : current);
  };

  const addMapElement = (element: Omit<ParkingMapElement, "id" | "x" | "y" | "width" | "height">) => {
    setParkingMap((current) => {
      if (!current) return current;
      const offset = current.elements.length * 18;
      const isWall = element.type === "wall";
      return {
        ...current,
        width: gridColumns,
        height: gridRows,
        elements: [
          ...current.elements,
          {
            ...element,
            id: `${element.type}-${Date.now()}-${Math.random().toString(16).slice(2)}`,
            x: 2 + (offset % 18),
            y: 2 + (offset % 10),
            width: isWall ? (wallDirection === "horizontal" ? 8 : 1) : element.type === "slot" ? 3 : 6,
            height: isWall ? (wallDirection === "horizontal" ? 1 : 8) : element.type === "slot" ? 2 : 3,
          },
        ],
      };
    });
  };

  const removeMapElement = (id: string) => {
    setParkingMap((current) => current
      ? { ...current, elements: current.elements.filter((item) => item.id !== id) }
      : current);
  };

  const saveMap = () => run(async () => {
    if (!branchFull || !parkingMap) return;
    setParkingMap(normalizeMap(await parkingStructureService.saveParkingMap(branchFull.id, { ...parkingMap, width: gridColumns, height: gridRows })));
  });

  const saveCapacity = () => run(async () => {
    if (!branchFull) return;
    const maxVehicleCapacity = Math.max(1, Number(capacityValue) || 1);
    await parkingStructureService.updateBranch(branchFull.id, {
      id: branchFull.id,
      name: branchFull.name,
      address: branchFull.address,
      maxVehicleCapacity,
    });
  });

  const customPresets: Record<string, { label: string; color: string }> = {
    barrier: { label: "Barrier", color: "#dc2626" },
    elevator: { label: "Thang máy", color: "#7c3aed" },
    exit: { label: "Lối thoát", color: "#0891b2" },
    wall: { label: "Tường", color: "#111827" },
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
          <div className="grid grid-cols-1 gap-3 lg:grid-cols-[1fr_1fr_1fr_160px_auto]">
            <select className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" value={selectedBranchId} onChange={async (e) => { setSelectedBranchId(e.target.value); const full = await parkingStructureService.branchFull(e.target.value); setBranchFull(full); setCapacityValue(String(full.maxVehicleCapacity || 100)); setParkingMap(normalizeMap(await parkingStructureService.parkingMap(e.target.value))); }}>
              {branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}
            </select>
            {isAdmin && (
              <>
                <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Tên chi nhánh mới" value={branchForm.name} onChange={(e) => setBranchForm({ ...branchForm, name: e.target.value })} />
                <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Địa chỉ" value={branchForm.address} onChange={(e) => setBranchForm({ ...branchForm, address: e.target.value })} />
                <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" type="number" min={1} placeholder="Sức chứa" value={branchForm.maxVehicleCapacity} onChange={(e) => setBranchForm({ ...branchForm, maxVehicleCapacity: Math.max(1, Number(e.target.value) || 1) })} />
                <button className="rounded-lg bg-blue-500 px-4 py-2 text-sm font-medium text-white hover:bg-blue-600" onClick={() => run(async () => { await parkingStructureService.createBranch(branchForm); setBranchForm({ name: "", address: "", maxVehicleCapacity: 100 }); })}>Thêm chi nhánh</button>
              </>
            )}
          </div>
        </ComponentCard>

        {branchFull && (
          <ComponentCard title={`Bố cục ${branchFull.name}`}>
            <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-[1fr_180px_auto]">
              <div className="rounded-lg bg-gray-50 px-3 py-2 text-sm text-gray-700 dark:bg-gray-900/30 dark:text-gray-300">
                Sức chứa tối đa hiện tại: <span className="font-semibold">{branchFull.maxVehicleCapacity || 100}</span> xe
              </div>
              <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" type="number" min={1} value={capacityValue} onChange={(e) => setCapacityValue(e.target.value)} />
              <button className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700" onClick={saveCapacity}>Lưu sức chứa</button>
            </div>
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
                              {slot.slotCode}
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

        {branchFull && parkingMap && (
          <ComponentCard title={`Sơ đồ bãi ${branchFull.name}`}>
            <div className="grid grid-cols-1 gap-5 xl:grid-cols-[280px_minmax(0,1fr)]">
              <div className="space-y-4">
                <div>
                  <p className="mb-2 text-sm font-semibold text-gray-800 dark:text-white">Thành phần có sẵn</p>
                  <div className="max-h-[320px] space-y-3 overflow-y-auto pr-1">
                    {branchFull.parkingLots.map((lot) => (
                      <div key={lot.id} className="rounded-lg border border-gray-200 p-2 dark:border-gray-700">
                        <button
                          type="button"
                          onClick={() => addMapElement({ type: "parkingLot", sourceType: "parkingLot", sourceId: lot.id, label: lot.name, color: "#2563eb" })}
                          className="w-full rounded-md bg-blue-50 px-2 py-1.5 text-left text-sm font-semibold text-blue-700 dark:bg-blue-900/20 dark:text-blue-300"
                        >
                          {lot.name}
                        </button>
                        <div className="mt-2 space-y-2 pl-3">
                          {lot.zones.map((zone) => (
                            <div key={zone.id}>
                              <button
                                type="button"
                                onClick={() => addMapElement({ type: "zone", sourceType: "zone", sourceId: zone.id, label: zone.name, parentId: lot.id, color: "#16a34a" })}
                                className="w-full rounded-md bg-green-50 px-2 py-1.5 text-left text-sm font-medium text-green-700 dark:bg-green-900/20 dark:text-green-300"
                              >
                                {zone.name}
                              </button>
                              <div className="mt-1 flex flex-wrap gap-1 pl-3">
                                {zone.slots.map((slot) => (
                                  <button
                                    key={slot.id}
                                    type="button"
                                    onClick={() => addMapElement({ type: "slot", sourceType: "slot", sourceId: slot.id, label: slot.slotCode, parentId: zone.id, color: "#f59e0b" })}
                                    className="rounded border border-amber-200 bg-amber-50 px-2 py-1 text-xs font-semibold text-amber-700 dark:border-amber-800 dark:bg-amber-900/20 dark:text-amber-300"
                                  >
                                    {slot.slotCode}
                                  </button>
                                ))}
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="space-y-2">
                  <p className="text-sm font-semibold text-gray-800 dark:text-white">Thêm thành phần</p>
                  <select
                    value={customElement.type}
                    onChange={(event) => setCustomElement({ ...customElement, type: event.target.value })}
                    className="w-full rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white"
                  >
                    <option value="barrier">Barrier</option>
                    <option value="elevator">Thang máy</option>
                    <option value="exit">Lối thoát</option>
                    <option value="wall">Tường</option>
                  </select>
                  {customElement.type === "wall" && (
                    <select
                      value={wallDirection}
                      onChange={(event) => setWallDirection(event.target.value as "horizontal" | "vertical")}
                      className="w-full rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white"
                    >
                      <option value="horizontal">Ngang</option>
                      <option value="vertical">Dọc</option>
                    </select>
                  )}
                  <input
                    value={customElement.label}
                    onChange={(event) => setCustomElement({ ...customElement, label: event.target.value })}
                    placeholder="Tên hiển thị"
                    className="w-full rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white"
                  />
                  <button
                    type="button"
                    onClick={() => {
                      const preset = customPresets[customElement.type];
                      addMapElement({
                        type: customElement.type,
                        sourceType: "custom",
                        label: customElement.label.trim() || preset.label,
                        color: preset.color,
                      });
                      setCustomElement({ ...customElement, label: "" });
                    }}
                    className="w-full rounded-lg bg-gray-800 px-4 py-2 text-sm font-medium text-white"
                  >
                    Thêm vào sơ đồ
                  </button>
                </div>

                <button
                  type="button"
                  onClick={saveMap}
                  className="w-full rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700"
                >
                  Lưu sơ đồ
                </button>
              </div>

              <div
                className="relative min-h-[420px] overflow-hidden rounded-lg border-2 border-gray-300 bg-white dark:border-gray-700 dark:bg-gray-950"
                onMouseMove={(event) => {
                  if (!draggingElementId || !parkingMap) return;
                  const rect = event.currentTarget.getBoundingClientRect();
                  const item = parkingMap.elements.find((element) => element.id === draggingElementId);
                  if (!item) return;
                  const scaleX = parkingMap.width / rect.width;
                  const scaleY = parkingMap.height / rect.height;
                  updateMapElement(draggingElementId, {
                    x: Math.round(Math.max(0, Math.min(parkingMap.width - item.width, (event.clientX - rect.left) * scaleX - item.width / 2))),
                    y: Math.round(Math.max(0, Math.min(parkingMap.height - item.height, (event.clientY - rect.top) * scaleY - item.height / 2))),
                  });
                }}
                onMouseUp={() => setDraggingElementId("")}
                onMouseLeave={() => setDraggingElementId("")}
                style={{
                  backgroundImage: "linear-gradient(to right, rgba(148,163,184,.28) 1px, transparent 1px), linear-gradient(to bottom, rgba(148,163,184,.28) 1px, transparent 1px)",
                  backgroundSize: `${100 / gridColumns}% ${100 / gridRows}%`,
                }}
              >
                {parkingMap.elements.map((element) => (
                  <div
                    key={element.id}
                    onMouseDown={() => setDraggingElementId(element.id)}
                    className={`absolute flex cursor-move items-center justify-center ${element.type === "wall" ? "rounded-none" : "rounded-md"} border border-black/10 px-2 text-center text-xs font-semibold text-white shadow-sm`}
                    style={{
                      left: `${(element.x / parkingMap.width) * 100}%`,
                      top: `${(element.y / parkingMap.height) * 100}%`,
                      width: `${(element.width / parkingMap.width) * 100}%`,
                      height: `${(element.height / parkingMap.height) * 100}%`,
                      backgroundColor: element.color,
                    }}
                  >
                    <span className="line-clamp-2">{element.label}</span>
                    <button
                      type="button"
                      onMouseDown={(event) => event.stopPropagation()}
                      onClick={() => removeMapElement(element.id)}
                      className="absolute -right-2 -top-2 flex h-5 w-5 items-center justify-center rounded-full bg-red-600 text-[10px] text-white"
                    >
                      x
                    </button>
                  </div>
                ))}
              </div>
            </div>
          </ComponentCard>
        )}
      </div>
    </>
  );
}

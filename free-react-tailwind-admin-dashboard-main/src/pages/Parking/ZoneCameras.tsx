import { useEffect, useMemo, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import {
  ZoneCameraEvent,
  ZoneLocationOption,
  ZoneCameraStream,
} from "../../components/parking/ZoneCameraStream";
import { useAuth } from "../../context/AuthContext";
import {
  Branch,
  BranchFull,
  parkingStructureService,
} from "../../services/parkingStructureService";

const cameraPresets = [
  {
    storageKey: "zone-camera-a-col-1",
    title: "Bai A - Cot 1",
    defaultCameraIp: "192.168.1.21",
    defaultCameraPort: "8080",
    defaultApiPort: "5101",
    defaultCameraId: "A_COL_1",
    defaultLocationName: "Bai A - Cot 1",
    defaultParkingLot: "A",
    defaultZone: "A",
    defaultColumn: "1",
  },
  {
    storageKey: "zone-camera-a-col-2",
    title: "Bai A - Cot 2",
    defaultCameraIp: "192.168.1.22",
    defaultCameraPort: "8080",
    defaultApiPort: "5102",
    defaultCameraId: "A_COL_2",
    defaultLocationName: "Bai A - Cot 2",
    defaultParkingLot: "A",
    defaultZone: "A",
    defaultColumn: "2",
  },
  {
    storageKey: "zone-camera-b-col-1",
    title: "Bai B - Cot 1",
    defaultCameraIp: "192.168.1.23",
    defaultCameraPort: "8080",
    defaultApiPort: "5103",
    defaultCameraId: "B_COL_1",
    defaultLocationName: "Bai B - Cot 1",
    defaultParkingLot: "B",
    defaultZone: "B",
    defaultColumn: "1",
  },
  {
    storageKey: "zone-camera-b-col-2",
    title: "Bai B - Cot 2",
    defaultCameraIp: "192.168.1.24",
    defaultCameraPort: "8080",
    defaultApiPort: "5104",
    defaultCameraId: "B_COL_2",
    defaultLocationName: "Bai B - Cot 2",
    defaultParkingLot: "B",
    defaultZone: "B",
    defaultColumn: "2",
  },
];

export default function ParkingZoneCameras() {
  const { user } = useAuth();
  const [events, setEvents] = useState<ZoneCameraEvent[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [branchFull, setBranchFull] = useState<BranchFull | null>(null);
  const [selectedBranchId, setSelectedBranchId] = useState(user?.branch?.id || "");
  const activeBranchId = selectedBranchId || user?.branch?.id || undefined;

  const latestEvents = useMemo(() => events.slice(0, 8), [events]);
  const locationOptions = useMemo<ZoneLocationOption[]>(() => {
    if (!branchFull) return [];
    return branchFull.parkingLots.flatMap((lot) =>
      lot.zones.flatMap((zone) =>
        zone.slots.map((slot) => ({
          locationName: `${lot.name} - ${zone.name} - ${slot.slotCode}`,
          parkingLot: lot.name,
          zone: zone.name,
          column: slot.slotCode,
        }))
      )
    );
  }, [branchFull]);

  useEffect(() => {
    const loadBranches = async () => {
      try {
        const items = await parkingStructureService.branches();
        setBranches(items);
        if (!selectedBranchId && items.length > 0) {
          setSelectedBranchId(user?.branch?.id || items[0].id);
        }
      } catch (error) {
        console.error("Failed to load branches:", error);
      }
    };

    void loadBranches();
  }, [selectedBranchId, user?.branch?.id]);

  useEffect(() => {
    if (!activeBranchId) return;
    parkingStructureService.branchFull(activeBranchId)
      .then(setBranchFull)
      .catch((error) => console.error("Failed to load branch structure:", error));
  }, [activeBranchId]);

  const handleEvent = (event: ZoneCameraEvent) => {
    setEvents((previous) => [
      event,
      ...previous.filter(
        (item) =>
          !(
            item.cameraTitle === event.cameraTitle &&
            item.plate === event.plate &&
            item.action === event.action &&
            item.timestamp === event.timestamp
          )
      ),
    ].slice(0, 20));
  };

  return (
    <>
      <PageMeta
        title="Camera khu vực | Smart Parking"
        description="Giám sát camera trong bãi và vị trí xe"
      />

      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">
            Giám sát camera khu vực
          </h1>
        </div>

        {branches.length > 0 && (
          <div className="max-w-md">
            <label className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Chi nhánh
            </label>
            <select
              value={activeBranchId || ""}
              onChange={(e) => setSelectedBranchId(e.target.value)}
              disabled={Boolean(user?.branch?.id)}
              className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-70 dark:border-gray-600 dark:bg-gray-700 dark:text-white"
            >
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}
                </option>
              ))}
            </select>
          </div>
        )}

        <ComponentCard title="Sự kiện khu vực mới nhất">
          {latestEvents.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400">
              Đang chờ nhận diện từ camera khu vực.
            </p>
          ) : (
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4">
              {latestEvents.map((event, index) => (
                <div
                  key={`${event.cameraTitle}-${event.plate}-${event.timestamp}-${index}`}
                  className={`rounded-lg border p-3 ${
                    event.action === "sent"
                      ? "border-green-200 bg-green-50 dark:border-green-800 dark:bg-green-900/10"
                      : event.action === "cooldown"
                        ? "border-yellow-200 bg-yellow-50 dark:border-yellow-800 dark:bg-yellow-900/10"
                        : "border-gray-200 bg-gray-50 dark:border-gray-700 dark:bg-gray-700/40"
                  }`}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <p className="font-mono text-base font-bold text-gray-800 dark:text-white">
                        {event.plate}
                      </p>
                      <p className="text-xs text-gray-500 dark:text-gray-400">
                        {event.cameraTitle}
                      </p>
                    </div>
                    <span className="rounded-full bg-white px-2 py-1 text-xs font-semibold text-gray-700 dark:bg-gray-800 dark:text-gray-200">
                      {event.action}
                    </span>
                  </div>
                  <p className="mt-2 text-xs text-gray-600 dark:text-gray-300">
                    Độ tin cậy {(event.confidence * 100).toFixed(1)}%
                  </p>
                  {event.message && (
                    <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">
                      {event.message}
                    </p>
                  )}
                </div>
              ))}
            </div>
          )}
        </ComponentCard>

        <div className="grid grid-cols-1 gap-6">
          {cameraPresets.map((camera) => (
            <ZoneCameraStream
              key={camera.storageKey}
              {...camera}
              branchId={activeBranchId}
              locationOptions={locationOptions}
              onEvent={handleEvent}
            />
          ))}
        </div>
      </div>
    </>
  );
}

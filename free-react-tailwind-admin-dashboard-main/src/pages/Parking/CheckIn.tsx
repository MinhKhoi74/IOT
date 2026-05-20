import { useEffect, useRef, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import {
  CameraStream,
  CheckInCameraEvent,
} from "../../components/parking/CameraStream";
import { parkingService } from "../../services/parkingService";
import { useAuth } from "../../context/AuthContext";
import {
  Branch,
  parkingStructureService,
} from "../../services/parkingStructureService";

export default function ParkingCheckIn() {
  const { user } = useAuth();
  const [plateNumber, setPlateNumber] = useState("");
  const [stationId, setStationId] = useState("STATION-01");
  const [branches, setBranches] = useState<Branch[]>([]);
  const [selectedBranchId, setSelectedBranchId] = useState(user?.branch?.id || "");
  const [confidence, setConfidence] = useState(0);
  const [checkInEvent, setCheckInEvent] = useState<CheckInCameraEvent | null>(null);
  const [isWatchingLatestCheckIn, setIsWatchingLatestCheckIn] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const lastDisplayedCheckInIdRef = useRef<string | number | null>(null);
  const activeBranchId = selectedBranchId || user?.branch?.id || undefined;

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

  const handleDetection = (plate: string, conf: number) => {
    setPlateNumber(plate);
    setConfidence(conf);
  };

  const handleCheckInEvent = (event: CheckInCameraEvent) => {
    if (event.message === "__STREAM_STARTED__") {
      setIsWatchingLatestCheckIn(true);
      return;
    }

    setPlateNumber(event.plateNumber);
    setConfidence(event.confidence);
    setCheckInEvent(event);
  };

  useEffect(() => {
    if (!isWatchingLatestCheckIn) return;

    const fetchLatestCheckIn = async () => {
      try {
        const latest = await parkingService.getLatestCheckIn(activeBranchId);
        if (!latest?.id) {
          return;
        }

        if (lastDisplayedCheckInIdRef.current === null) {
          lastDisplayedCheckInIdRef.current = latest.id;
          return;
        }

        if (latest.id === lastDisplayedCheckInIdRef.current) {
          return;
        }

        lastDisplayedCheckInIdRef.current = latest.id;
        setPlateNumber(latest.plateNumber);
        setConfidence(1);
        setCheckInEvent({
          plateNumber: latest.plateNumber,
          confidence: 1,
          success: true,
          message: `Ghi nhận xe vào thành công: ${latest.plateNumber}`,
          imageBase64: latest.checkInImageBase64,
        });
      } catch (error) {
        console.error("Failed to fetch latest check-in:", error);
      }
    };

    void fetchLatestCheckIn();
    const intervalId = setInterval(() => {
      void fetchLatestCheckIn();
    }, 1000);

    return () => clearInterval(intervalId);
  }, [isWatchingLatestCheckIn, activeBranchId]);

  const checkInButtonText = checkInEvent
    ? checkInEvent.message
    : plateNumber
      ? "Ghi nhận xe vào"
      : "Đang chờ biển số...";

  const handleManualCheckIn = async () => {
    const plate = plateNumber.trim().toUpperCase();
    if (!plate) {
      setCheckInEvent({
        plateNumber: "",
        confidence: 0,
        success: false,
        message: "Vui lòng nhập hoặc quét biển số trước.",
      });
      return;
    }

    setIsProcessing(true);
    try {
      const response = await parkingService.checkIn({
        plateNumber: plate,
        stationId,
        confidence: confidence || 0.9,
        branchId: activeBranchId,
      });

      setPlateNumber(plate);
      setCheckInEvent({
        plateNumber: plate,
        confidence: confidence || 0.9,
        success: response.success,
        message: response.message,
      });
    } catch (error) {
      setCheckInEvent({
        plateNumber: plate,
        confidence: confidence || 0,
        success: false,
        message: error instanceof Error ? error.message : "Ghi nhận xe vào thất bại.",
      });
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <>
      <PageMeta
        title="Xe vào | Smart Parking"
        description="Ghi nhận xe vào bằng nhận diện biển số"
      />
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">
            Xe vào
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            Ghi nhận xe vào bằng camera nhận diện biển số
          </p>
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

        {checkInEvent && (
          <div
            className={`p-4 rounded-lg border ${
              checkInEvent.success
                ? "bg-green-50 dark:bg-green-900/10 border-green-200 dark:border-green-800"
                : "bg-red-50 dark:bg-red-900/10 border-red-200 dark:border-red-800"
            }`}
          >
            <p
              className={`text-sm font-medium ${
                checkInEvent.success
                  ? "text-green-600 dark:text-green-400"
                  : "text-red-600 dark:text-red-400"
              }`}
            >
              {checkInEvent.message}
            </p>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <CameraStream
              onDetection={handleDetection}
              onCheckInEvent={handleCheckInEvent}
              stationId={stationId}
              stationMode="entrance"
              branchId={activeBranchId}
            />
          </div>

          <div className="lg:col-span-1">
            <ComponentCard title="Thông tin xe vào">
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Mã trạm
                  </label>
                  <select
                    value={stationId}
                    onChange={(e) => setStationId(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option>STATION-01</option>
                    <option>STATION-02</option>
                    <option>STATION-03</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Biển số
                  </label>
                  <input
                    type="text"
                    value={plateNumber}
                    onChange={(e) => setPlateNumber(e.target.value.toUpperCase())}
                    placeholder="ABC-1234"
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white font-mono font-bold text-lg text-center focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Độ tin cậy nhận diện
                  </label>
                  <div className="w-full bg-gray-200 dark:bg-gray-600 rounded-full h-2">
                    <div
                      className="bg-blue-500 h-2 rounded-full transition-all"
                      style={{ width: `${confidence * 100}%` }}
                    />
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                    {(confidence * 100).toFixed(1)}%
                  </p>
                </div>

                <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
                  <p className="text-xs text-blue-700 dark:text-blue-400">
                    <span className="font-semibold">Thông tin:</span> Camera Python tự động ghi nhận xe vào. Khung này hiển thị trạng thái mới nhất và ảnh đã chụp.
                  </p>
                </div>

                {checkInEvent?.success && checkInEvent.imageBase64 && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Ảnh xe vào
                    </label>
                    <img
                      src={`data:image/jpeg;base64,${checkInEvent.imageBase64}`}
                      alt={`Check-in capture for ${checkInEvent.plateNumber}`}
                      className="w-full rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-900 object-contain"
                    />
                  </div>
                )}

                <button
                  onClick={handleManualCheckIn}
                  disabled={!plateNumber || isProcessing}
                  className={`w-full px-4 py-3 rounded-lg font-semibold text-white shadow-theme-sm transition ${
                    checkInEvent
                      ? checkInEvent.success
                        ? "bg-gradient-to-r from-success-500 to-blue-light-500"
                        : "bg-gradient-to-r from-error-500 to-orange-500"
                      : "bg-gradient-to-r from-brand-500 via-blue-light-500 to-success-500 hover:from-brand-600 hover:via-blue-light-600 hover:to-success-600 disabled:from-gray-400 disabled:via-gray-400 disabled:to-gray-400"
                  }`}
                >
                  {isProcessing ? "Đang xử lý..." : checkInButtonText}
                </button>
              </div>
            </ComponentCard>
          </div>
        </div>
      </div>
    </>
  );
}

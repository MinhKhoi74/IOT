import { useEffect, useRef, useState } from "react";
import { cameraService } from "../../services/cameraService";

interface ZoneCameraStreamProps {
  storageKey: string;
  title: string;
  defaultCameraIp: string;
  defaultCameraPort: string;
  defaultApiPort: string;
  defaultCameraId: string;
  defaultLocationName: string;
  defaultParkingLot?: string;
  defaultZone?: string;
  defaultColumn?: string;
  locationOptions?: ZoneLocationOption[];
  branchId?: string;
  onEvent?: (event: ZoneCameraEvent) => void;
}

export interface ZoneLocationOption {
  locationName: string;
  parkingLot: string;
  zone: string;
  column: string;
}

export interface ZoneCameraEvent {
  cameraTitle: string;
  plate: string;
  confidence: number;
  action: string;
  message: string;
  timestamp: number;
}

interface DetectionResult {
  plate: string;
  confidence: number;
  action?: string;
  bufferStatus?: string;
  bbox: {
    x: number;
    y: number;
    width: number;
    height: number;
  };
}

interface DetectionResponse {
  results: DetectionResult[];
  count: number;
  timestamp?: number;
  scanStatus?: {
    scanning: boolean;
    scanWindowSeconds?: number;
    scanRemainingSeconds: number;
    pauseSeconds?: number;
    pauseRemainingSeconds: number;
    lockedPlates: string[];
    pendingCount: number;
    batchStatus?: "collecting" | "sending" | "sent" | "failed" | "empty";
  } | null;
  lockedDetections?: LockedDetection[];
  lastEvent?: {
    id: string;
    plate: string;
    confidence: number;
    action: string;
    success: boolean;
    message: string;
    timestamp: number;
  } | null;
}

interface LockedDetection {
  plateNumber: string;
  confidence: number;
  detectedAt?: string;
  status?: "locked" | "sending" | "sent" | "failed";
}

const loadValue = (key: string, fallback: string) =>
  localStorage.getItem(key) || fallback;

const loadApiPort = (key: string, fallback: string) => {
  const savedPort = localStorage.getItem(key);
  return savedPort === "5000" ? fallback : savedPort || fallback;
};

export const ZoneCameraStream: React.FC<ZoneCameraStreamProps> = ({
  storageKey,
  title,
  defaultCameraIp,
  defaultCameraPort,
  defaultApiPort,
  defaultCameraId,
  defaultLocationName,
  defaultParkingLot = "",
  defaultZone = "",
  defaultColumn = "",
  locationOptions = [],
  branchId,
  onEvent,
}) => {
  const imgRef = useRef<HTMLImageElement>(null);
  const detectionIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const lastEventRef = useRef("");
  const [cameraIp, setCameraIp] = useState(() => loadValue(`${storageKey}:cameraIp`, defaultCameraIp));
  const [cameraPort, setCameraPort] = useState(() => loadValue(`${storageKey}:cameraPort`, defaultCameraPort));
  const [apiHost, setApiHost] = useState(() => loadValue(`${storageKey}:apiHost`, "localhost"));
  const [apiPort, setApiPort] = useState(() => loadApiPort(`${storageKey}:apiPort`, defaultApiPort));
  const [cameraId, setCameraId] = useState(() => loadValue(`${storageKey}:cameraId`, defaultCameraId));
  const [parkingLot, setParkingLot] = useState(() => loadValue(`${storageKey}:parkingLot`, defaultParkingLot));
  const [zone, setZone] = useState(() => loadValue(`${storageKey}:zone`, defaultZone));
  const [column, setColumn] = useState(() => loadValue(`${storageKey}:column`, defaultColumn));
  const [locationName, setLocationName] = useState(() => loadValue(`${storageKey}:locationName`, defaultLocationName));
  const [isActive, setIsActive] = useState(false);
  const [streamUrl, setStreamUrl] = useState("");
  const [connectionError, setConnectionError] = useState("");
  const [detectionError, setDetectionError] = useState("");
  const [lastPlate, setLastPlate] = useState("");
  const [lastAction, setLastAction] = useState("");
  const [confidence, setConfidence] = useState(0);
  const [detectedCount, setDetectedCount] = useState(0);
  const [isStarting, setIsStarting] = useState(false);
  const [serviceMessage, setServiceMessage] = useState("");
  const [scanStatus, setScanStatus] = useState<DetectionResponse["scanStatus"]>(null);
  const [lockedDetections, setLockedDetections] = useState<LockedDetection[]>([]);
  const [batchMessage, setBatchMessage] = useState("");

  useEffect(() => {
    return () => stopPolling();
  }, []);

  const persistConfig = () => {
    localStorage.setItem(`${storageKey}:cameraIp`, cameraIp.trim());
    localStorage.setItem(`${storageKey}:cameraPort`, cameraPort.trim());
    localStorage.setItem(`${storageKey}:apiHost`, apiHost.trim());
    localStorage.setItem(`${storageKey}:apiPort`, apiPort.trim());
    localStorage.setItem(`${storageKey}:cameraId`, cameraId.trim());
    localStorage.setItem(`${storageKey}:parkingLot`, parkingLot.trim());
    localStorage.setItem(`${storageKey}:zone`, zone.trim());
    localStorage.setItem(`${storageKey}:column`, column.trim());
    localStorage.setItem(`${storageKey}:locationName`, locationName.trim());
  };

  const chooseLocation = (value: string) => {
    const option = locationOptions.find((item) => item.locationName === value);
    setLocationName(value);
    if (option) {
      setParkingLot(option.parkingLot);
      setZone(option.zone);
      setColumn(option.column);
    }
  };

  const connectStream = async () => {
    try {
      setIsStarting(true);
      setConnectionError("");
      setDetectionError("");
      setServiceMessage("");
      setBatchMessage("");
      setLockedDetections([]);
      setScanStatus(null);

      const numericCameraPort = Number(cameraPort);
      const numericApiPort = Number(apiPort);
      if (!cameraIp.trim() || !Number.isFinite(numericCameraPort) || !Number.isFinite(numericApiPort)) {
        throw new Error("IP camera, cổng camera và cổng Python API phải hợp lệ.");
      }
      if (!cameraId.trim() || !locationName.trim()) {
        throw new Error("Cần nhập mã camera và tên vị trí.");
      }
      if (numericApiPort === 5000) {
        throw new Error("Cổng 5000 đang dùng cho backend .NET. Hãy dùng cổng Python zone như 5101, 5102, 5103 hoặc 5104.");
      }

      persistConfig();

      const apiBaseUrl = `http://${apiHost.trim() || "localhost"}:${numericApiPort}`;
      const status = await cameraService.startZone({
        cameraIp: cameraIp.trim(),
        cameraPort: numericCameraPort,
        apiHost: apiHost.trim() || "localhost",
        apiPort: numericApiPort,
        cameraId: cameraId.trim(),
        locationName: locationName.trim(),
        parkingLotCode: parkingLot.trim() || undefined,
        zoneCode: zone.trim() || undefined,
        columnCode: column.trim() || undefined,
        branchId,
      });
      setServiceMessage(status.message);

      const healthResponse = await fetch(`${apiBaseUrl}/api/health?t=${Date.now()}`);
      if (!healthResponse.ok) {
        throw new Error("Luồng Python zone chưa sẵn sàng.");
      }
      const healthPayload = await healthResponse.json().catch(() => null);
      if (!healthPayload || healthPayload.ok !== true || !("frameAvailable" in healthPayload)) {
        throw new Error(`Cổng ${numericApiPort} không phải Python zone stream API. Hãy khởi động webcam_zone_locator.py trên cổng này trước.`);
      }

      const nextStreamUrl = `${apiBaseUrl}/api/stream?t=${Date.now()}`;
      setStreamUrl(nextStreamUrl);
      setIsActive(true);
      if (imgRef.current) {
        imgRef.current.src = nextStreamUrl;
      }

      startDetectionPolling(apiBaseUrl);
    } catch (error) {
      setIsActive(false);
      const message = error instanceof Error ? error.message : "Không kết nối được camera khu vực.";
      setConnectionError(
        message === "Failed to fetch"
          ? `Không gọi được Python zone API tại ${apiHost.trim() || "localhost"}:${apiPort}. Kiểm tra log backend và chờ /api/health sẵn sàng.`
          : message
      );
    } finally {
      setIsStarting(false);
    }
  };

  const stopPolling = () => {
    if (detectionIntervalRef.current) {
      clearInterval(detectionIntervalRef.current);
      detectionIntervalRef.current = null;
    }
  };

  const stopStream = async () => {
    stopPolling();
    if (imgRef.current) {
      imgRef.current.src = "";
    }
    setStreamUrl("");
    setBatchMessage("");
    setLockedDetections([]);
    setScanStatus(null);
    try {
      await cameraService.stopZone({
        cameraId: cameraId.trim(),
        apiPort: Number(apiPort),
      });
    } catch (error) {
      console.error("Failed to stop zone camera service:", error);
    }
    setIsActive(false);
  };

  const startDetectionPolling = (apiBaseUrl: string) => {
    const fetchDetection = async () => {
      try {
        const response = await fetch(`${apiBaseUrl}/api/detection?t=${Date.now()}`);
        if (!response.ok) {
          throw new Error(`Detection API returned ${response.status}`);
        }

        const data: DetectionResponse = await response.json();
        setDetectionError("");
        setDetectedCount(data.count || 0);
        setScanStatus(data.scanStatus || null);

        const nextLockedDetections = (data.lockedDetections || []).map((item) => ({
          ...item,
          status: item.status || "locked" as const,
        }));
        setLockedDetections(nextLockedDetections);

        if (data.lastEvent?.action === "sent") {
          setBatchMessage(data.lastEvent.message || "Đã gửi danh sách biển số đã khóa về backend.");
        } else if (data.lastEvent?.action === "failed") {
          setBatchMessage(data.lastEvent.message || "Gửi danh sách biển số đã khóa về backend thất bại.");
        } else if (data.scanStatus?.scanning && nextLockedDetections.length === 0) {
          setBatchMessage("");
        }

        const bestMatch = data.results
          .filter((result) => result.plate && result.plate !== "unknown")
          .sort((a, b) => b.confidence - a.confidence)[0];

        if (bestMatch) {
          setLastPlate(bestMatch.plate);
          setConfidence(bestMatch.confidence);
          setLastAction(bestMatch.action || "detected");
        }

        if (data.lastEvent && data.lastEvent.id !== lastEventRef.current) {
          lastEventRef.current = data.lastEvent.id;
          onEvent?.({
            cameraTitle: title,
            plate: data.lastEvent.plate,
            confidence: data.lastEvent.confidence,
            action: data.lastEvent.action,
            message: data.lastEvent.message,
            timestamp: data.lastEvent.timestamp,
          });
        }
      } catch (error) {
        setDetectionError(error instanceof Error ? error.message : "Không lấy được dữ liệu nhận diện.");
      }
    };

    void fetchDetection();
    stopPolling();
    detectionIntervalRef.current = setInterval(() => {
      void fetchDetection();
    }, 700);
  };

  return (
    <div className="rounded-lg border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-800">
      <div className="border-b border-gray-200 p-4 dark:border-gray-700">
        <div className="flex flex-col gap-2 md:flex-row md:items-start md:justify-between">
          <div>
            <h3 className="text-lg font-semibold text-gray-800 dark:text-white">{title}</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400">{locationName}</p>
          </div>
          <span
            className={`w-fit rounded-full px-3 py-1 text-xs font-semibold text-white ${
              isActive ? "bg-green-500" : "bg-gray-500"
            }`}
          >
            {isActive ? "Đang chạy" : "Tạm dừng"}
          </span>
        </div>
      </div>

      <div className="space-y-4 p-4">
        <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
          <input className="rounded-lg border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700 dark:text-white" value={cameraIp} onChange={(e) => setCameraIp(e.target.value)} placeholder="Camera IP" disabled={isActive} />
          <input className="rounded-lg border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700 dark:text-white" value={cameraPort} onChange={(e) => setCameraPort(e.target.value)} placeholder="Cổng camera" disabled={isActive} />
          <input className="rounded-lg border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700 dark:text-white" value={apiHost} onChange={(e) => setApiHost(e.target.value)} placeholder="Máy chủ camera API" disabled={isActive} />
          <input className="rounded-lg border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700 dark:text-white" value={apiPort} onChange={(e) => setApiPort(e.target.value)} placeholder="Cổng camera API" disabled={isActive} />
          <input className="rounded-lg border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700 dark:text-white" value={cameraId} onChange={(e) => setCameraId(e.target.value)} placeholder="Mã camera" disabled={isActive} />
          <select className="rounded-lg border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700 dark:text-white" value={locationName} onChange={(e) => chooseLocation(e.target.value)} disabled={isActive}>
            <option value="">Tên vị trí</option>
            {locationOptions.map((option) => (
              <option key={`${option.parkingLot}-${option.zone}-${option.column}`} value={option.locationName}>
                {option.locationName}
              </option>
            ))}
          </select>
          <select className="rounded-lg border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700 dark:text-white" value={parkingLot} onChange={(e) => setParkingLot(e.target.value)} disabled={isActive}>
            <option value="">Bãi xe</option>
            {[...new Set(locationOptions.map((item) => item.parkingLot))].map((value) => (
              <option key={value} value={value}>{value}</option>
            ))}
          </select>
          <div className="grid grid-cols-2 gap-3">
            <select className="rounded-lg border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700 dark:text-white" value={zone} onChange={(e) => setZone(e.target.value)} disabled={isActive}>
              <option value="">Khu</option>
              {[...new Set(locationOptions.filter((item) => !parkingLot || item.parkingLot === parkingLot).map((item) => item.zone))].map((value) => (
                <option key={value} value={value}>{value}</option>
              ))}
            </select>
            <select className="rounded-lg border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700 dark:text-white" value={column} onChange={(e) => setColumn(e.target.value)} disabled={isActive}>
              <option value="">Cột</option>
              {locationOptions
                .filter((item) => (!parkingLot || item.parkingLot === parkingLot) && (!zone || item.zone === zone))
                .map((item) => item.column)
                .filter((value, index, values) => values.indexOf(value) === index)
                .map((value) => (
                  <option key={value} value={value}>{value}</option>
                ))}
            </select>
          </div>
        </div>

        {connectionError && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-600 dark:border-red-800 dark:bg-red-900/10 dark:text-red-400">
            {connectionError}
          </div>
        )}

        {serviceMessage && (
          <div className="rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700 dark:border-green-800 dark:bg-green-900/10 dark:text-green-300">
            {serviceMessage}
          </div>
        )}

        {detectionError && (
          <div className="rounded-lg border border-yellow-200 bg-yellow-50 p-3 text-sm text-yellow-700 dark:border-yellow-800 dark:bg-yellow-900/10 dark:text-yellow-300">
            Lỗi lấy dữ liệu nhận diện: {detectionError}
          </div>
        )}

        <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1.45fr)_minmax(280px,0.85fr)]">
          <div className="relative aspect-video overflow-hidden rounded-lg bg-gray-950">
            <img
              ref={imgRef}
              crossOrigin="anonymous"
              className="h-full w-full object-contain"
              alt={`${title} stream`}
              onError={() => setConnectionError("Không tải được luồng MJPEG. Kiểm tra cổng Python API.")}
            />
            <div className="absolute left-4 top-4 rounded-full bg-black/70 px-3 py-1 text-xs font-medium text-white">
              {detectedCount} biển số
            </div>
            {lastPlate && (
              <div className="absolute bottom-4 left-4 right-4 rounded-lg bg-black/70 p-3 text-white">
                <div className="flex items-center justify-between gap-3">
                  <p className="font-mono text-lg font-bold">{lastPlate}</p>
                  <span className="rounded-full bg-white/15 px-2 py-1 text-xs">{lastAction}</span>
                </div>
                <p className="text-sm text-gray-300">Độ tin cậy: {(confidence * 100).toFixed(1)}%</p>
              </div>
            )}
          </div>

          <div className="flex min-h-[260px] flex-col rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-700 dark:bg-gray-900/30">
            <div className="mb-3 flex items-start justify-between gap-3">
              <div>
                <p className="text-sm font-semibold text-gray-800 dark:text-white">Biển số đã khóa</p>
                <p className="text-xs text-gray-500 dark:text-gray-400">
                  {scanStatus?.scanning
                    ? `Đang quét, còn ${Math.ceil(scanStatus.scanRemainingSeconds || 0)}s`
                    : scanStatus
                      ? `Tạm nghỉ ${Math.ceil(scanStatus.pauseRemainingSeconds || 0)}s`
                      : "Đang chờ"}
                </p>
              </div>
              <span
                className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                  scanStatus?.scanning
                    ? "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300"
                    : "bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-200"
                }`}
              >
                {scanStatus?.pendingCount || lockedDetections.length} đã khóa
              </span>
            </div>

            <div className="flex-1 space-y-2 overflow-y-auto pr-1">
              {lockedDetections.length === 0 ? (
                <div className="flex h-full min-h-[150px] items-center justify-center rounded-lg border border-dashed border-gray-300 px-3 text-center text-sm text-gray-500 dark:border-gray-700 dark:text-gray-400">
                  Chưa có biển số được khóa trong lượt quét này.
                </div>
              ) : (
                lockedDetections.map((item) => (
                  <div
                    key={`${item.plateNumber}-${item.detectedAt || item.status}`}
                    className="rounded-lg border border-gray-200 bg-white p-3 shadow-sm dark:border-gray-700 dark:bg-gray-800"
                  >
                    <div className="flex items-center justify-between gap-3">
                      <p className="font-mono text-base font-bold text-gray-900 dark:text-white">
                        {item.plateNumber}
                      </p>
                      <span
                        className={`rounded-full px-2 py-1 text-xs font-semibold ${
                          item.status === "sent"
                            ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300"
                            : item.status === "failed"
                              ? "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300"
                              : item.status === "sending"
                                ? "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300"
                                : "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300"
                        }`}
                      >
                        {item.status || "locked"}
                      </span>
                    </div>
                    <div className="mt-2 flex items-center justify-between gap-3 text-xs text-gray-500 dark:text-gray-400">
                      <span>Độ tin cậy {(Number(item.confidence || 0) * 100).toFixed(1)}%</span>
                      {item.detectedAt && <span>{new Date(item.detectedAt).toLocaleTimeString("vi-VN")}</span>}
                    </div>
                  </div>
                ))
              )}
            </div>

            {batchMessage && (
              <div className="mt-3 rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-700 dark:border-green-800 dark:bg-green-900/10 dark:text-green-300">
                {batchMessage}
              </div>
            )}
          </div>
        </div>

        {streamUrl && <p className="break-all text-xs text-gray-500 dark:text-gray-400">URL luồng: {streamUrl}</p>}

        <button
          onClick={() => {
            if (isActive) {
              void stopStream();
            } else {
              void connectStream();
            }
          }}
          className={`w-full rounded-lg px-4 py-2 font-medium text-white shadow-theme-sm transition ${
            isActive
              ? "bg-gradient-to-r from-error-500 to-orange-500 hover:from-error-600 hover:to-orange-600"
              : "bg-gradient-to-r from-success-500 to-blue-light-500 hover:from-success-600 hover:to-blue-light-600"
          }`}
        >
          {isStarting ? "Đang kết nối..." : isActive ? "Dừng luồng" : "Kết nối camera"}
        </button>
      </div>
    </div>
  );
};

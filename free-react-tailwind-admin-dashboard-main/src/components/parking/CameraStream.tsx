import { useEffect, useRef, useState } from "react";
import { cameraService } from "../../services/cameraService";

interface CameraStreamProps {
  onDetection?: (plateNumber: string, confidence: number, imageBase64?: string) => void;
  onCheckInEvent?: (event: CheckInCameraEvent) => void;
  stationId?: string;
  stationMode?: "entrance" | "exit";
  branchId?: string;
}

export interface CheckInCameraEvent {
  plateNumber: string;
  confidence: number;
  success: boolean;
  message: string;
  imageBase64?: string;
  action?: string;
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

export const CameraStream: React.FC<CameraStreamProps> = ({
  onDetection,
  onCheckInEvent,
  stationId = "STATION-01",
  stationMode = "entrance",
  branchId,
}) => {
  const imgRef = useRef<HTMLImageElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const detectionIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const lastCheckInEventRef = useRef("");
  const [isActive, setIsActive] = useState(false);
  const [lastPlate, setLastPlate] = useState("");
  const [detectionConfidence, setDetectionConfidence] = useState(0);
  const [cameraIp, setCameraIp] = useState(() => {
    return localStorage.getItem("cameraIp") || "192.168.1.20";
  });
  const [cameraPort, setCameraPort] = useState(() => {
    return localStorage.getItem("cameraPort") || "8080";
  });
  const [apiHost, setApiHost] = useState(() => {
    return localStorage.getItem("cameraApiHost") || "localhost";
  });
  const [apiPort, setApiPort] = useState(() => {
    return localStorage.getItem("cameraApiPort") || "5001";
  });
  const [connectionError, setConnectionError] = useState("");
  const [detectionError, setDetectionError] = useState("");
  const [streamUrl, setStreamUrl] = useState("");
  const [isStarting, setIsStarting] = useState(false);
  const [serviceMessage, setServiceMessage] = useState("");

  useEffect(() => {
    return () => {
      void stopCamera(false);
    };
  }, []);

  const connectToExistingStream = async () => {
    try {
      setIsStarting(true);
      setConnectionError("");
      setServiceMessage("");

      localStorage.setItem("cameraIp", cameraIp.trim());
      localStorage.setItem("cameraPort", cameraPort.trim());
      localStorage.setItem("cameraApiHost", apiHost.trim());
      localStorage.setItem("cameraApiPort", apiPort.trim());

      const numericCameraPort = Number(cameraPort);
      const numericApiPort = Number(apiPort);
      if (!cameraIp.trim() || !Number.isFinite(numericCameraPort) || !Number.isFinite(numericApiPort)) {
        throw new Error("IP camera, cổng camera và cổng Python API phải hợp lệ.");
      }

      const status = await cameraService.start({
        cameraIp: cameraIp.trim(),
        cameraPort: numericCameraPort,
        apiHost: apiHost.trim() || "localhost",
        apiPort: numericApiPort,
        stationMode,
        branchId,
      });
      setServiceMessage(status.message);

      const apiBaseUrl = `http://${apiHost.trim() || "localhost"}:${numericApiPort}`;
      const healthResponse = await fetch(`${apiBaseUrl}/api/health`);
      if (!healthResponse.ok) {
        throw new Error("Dịch vụ Python stream chưa sẵn sàng.");
      }

      const nextStreamUrl = `${apiBaseUrl}/api/stream?t=${Date.now()}`;
      setStreamUrl(nextStreamUrl);
      setIsActive(true);
      onCheckInEvent?.({
        plateNumber: "",
        confidence: 0,
        success: false,
        message: "__STREAM_STARTED__",
      });

      if (imgRef.current) {
        imgRef.current.src = nextStreamUrl;
      }

      startDetectionPolling(apiBaseUrl);
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : "Không kết nối được luồng camera.";
      console.error("Không kết nối được luồng camera:", error);
      setConnectionError(errorMessage);
      setIsActive(false);
    } finally {
      setIsStarting(false);
    }
  };

  const stopCamera = async (shouldStopBackend = true) => {
    if (detectionIntervalRef.current) {
      clearInterval(detectionIntervalRef.current);
      detectionIntervalRef.current = null;
    }

    if (imgRef.current) {
      imgRef.current.src = "";
    }

    setStreamUrl("");

    if (shouldStopBackend) {
      try {
        await cameraService.stop();
      } catch (error) {
        console.error("Failed to stop camera service:", error);
      }
    }

    setIsActive(false);
  };

  const startDetectionPolling = (apiBaseUrl: string) => {
    const fetchDetection = async () => {
      try {
        const response = await fetch(
          `${apiBaseUrl}/api/detection?t=${Date.now()}`
        );
        if (!response.ok) {
          throw new Error(`Detection API returned ${response.status}`);
        }

        setDetectionError("");
        const data: DetectionResponse = await response.json();

        const isRecordEvent =
          data.lastEvent?.action === "sent" || data.lastEvent?.action === "finalized";
        if (data.lastEvent && isRecordEvent && lastCheckInEventRef.current !== data.lastEvent.id) {
          lastCheckInEventRef.current = data.lastEvent.id;
          const capture = await captureFrame();
          setLastPlate(data.lastEvent.plate);
          setDetectionConfidence(data.lastEvent.confidence);
          onDetection?.(data.lastEvent.plate, data.lastEvent.confidence, capture?.imageBase64);
          onCheckInEvent?.({
            plateNumber: data.lastEvent.plate,
            confidence: data.lastEvent.confidence,
            success: data.lastEvent.success,
            message: data.lastEvent.success
              ? `${stationMode === "exit" ? "Xe ra" : "Xe vào"} đã nhận diện: ${data.lastEvent.plate}`
              : data.lastEvent.message || `${stationMode === "exit" ? "Xe ra" : "Xe vào"} chưa được ghi nhận: ${data.lastEvent.plate}`,
            imageBase64: capture?.imageBase64,
            action: data.lastEvent.action,
          });
        }

        const eventMatch = data.results.find((result) => {
          return (
            result.plate &&
            result.plate !== "unknown" &&
            ["sent", "finalized"].includes(result.action || "")
          );
        });
        const bestMatch = eventMatch || data.results
          .filter((result) => result.plate && result.plate !== "unknown")
          .sort((a, b) => b.confidence - a.confidence)[0];

        if (!bestMatch) {
          return;
        }

        setLastPlate(bestMatch.plate);
        setDetectionConfidence(bestMatch.confidence);
        const currentCapture = await captureFrame();
        onDetection?.(bestMatch.plate, bestMatch.confidence, currentCapture?.imageBase64);

        const action = bestMatch.action || "";
        const eventKey = `${bestMatch.plate}-${action}-${data.timestamp || data.count}`;
        if ((action === "sent" || action === "finalized") && lastCheckInEventRef.current !== eventKey) {
          lastCheckInEventRef.current = eventKey;
          onCheckInEvent?.({
            plateNumber: bestMatch.plate,
            confidence: bestMatch.confidence,
            success: true,
            message: `${stationMode === "exit" ? "Xe ra" : "Xe vào"} đã nhận diện: ${bestMatch.plate}`,
            imageBase64: currentCapture?.imageBase64,
            action,
          });
        }
      } catch (error) {
        console.error("Không lấy được dữ liệu nhận diện:", error);
        setDetectionError(
          error instanceof Error ? error.message : "Không lấy được dữ liệu nhận diện."
        );
      }
    };

    void fetchDetection();

    if (detectionIntervalRef.current) {
      clearInterval(detectionIntervalRef.current);
    }

    detectionIntervalRef.current = setInterval(() => {
      void fetchDetection();
    }, 500);
  };

  const captureFrame = async () => {
    if (!imgRef.current || !imgRef.current.src) return;

    try {
      const canvas = canvasRef.current || document.createElement("canvas");
      const width = imgRef.current.naturalWidth || imgRef.current.clientWidth;
      const height = imgRef.current.naturalHeight || imgRef.current.clientHeight;
      if (!width || !height) return;

      canvas.width = width;
      canvas.height = height;

      const ctx = canvas.getContext("2d");
      if (!ctx) return;

      ctx.drawImage(imgRef.current, 0, 0, width, height);

      const imageBase64 = canvas.toDataURL("image/jpeg").split(",")[1];

      return {
        plateNumber: lastPlate,
        imageBase64,
        confidence: detectionConfidence,
      };
    } catch (error) {
      console.error("Error capturing frame:", error);
    }
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md overflow-hidden">
      <div className="p-4 md:p-6">
        <h2 className="text-lg md:text-xl font-semibold text-gray-800 dark:text-white mb-4">
          Khung camera trực tiếp
        </h2>

        <div className="mb-4 p-4 bg-gray-50 dark:bg-gray-700/50 rounded-lg border border-gray-200 dark:border-gray-600">
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
            Cấu hình IPWebcam
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                Địa chỉ IP camera
              </label>
              <input
                type="text"
                value={cameraIp}
                onChange={(e) => setCameraIp(e.target.value)}
                disabled={isActive || isStarting}
                placeholder="192.168.1.20"
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm disabled:opacity-50"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                Cổng camera
              </label>
              <input
                type="text"
                value={cameraPort}
                onChange={(e) => setCameraPort(e.target.value)}
                disabled={isActive || isStarting}
                placeholder="8080"
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm disabled:opacity-50"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                Máy chủ camera API
              </label>
              <input
                type="text"
                value={apiHost}
                onChange={(e) => setApiHost(e.target.value)}
                disabled={isActive || isStarting}
                placeholder="localhost"
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm disabled:opacity-50"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                Cổng camera API
              </label>
              <input
                type="text"
                value={apiPort}
                onChange={(e) => setApiPort(e.target.value)}
                disabled={isActive || isStarting}
                placeholder="5001"
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm disabled:opacity-50"
              />
            </div>
          </div>
        </div>

        {serviceMessage && (
          <div className="mb-4 p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
            <p className="text-sm text-green-700 dark:text-green-300">
              {serviceMessage}
            </p>
          </div>
        )}

        {connectionError && (
          <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
            <p className="text-sm text-red-600 dark:text-red-400">
              {connectionError}
            </p>
          </div>
        )}

        {detectionError && (
          <div className="mb-4 p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
            <p className="text-sm text-yellow-700 dark:text-yellow-300">
              Lỗi lấy dữ liệu nhận diện: {detectionError}
            </p>
          </div>
        )}

        <div className="relative mx-auto mb-4 max-w-3xl bg-gray-900 rounded-lg overflow-hidden aspect-video">
          <img
            ref={imgRef}
            crossOrigin="anonymous"
            className="w-full h-full max-h-[420px] object-contain bg-gray-950"
            alt="Camera Stream"
            onError={() => {
              setConnectionError(
                "Không tải được luồng MJPEG. Kiểm tra IPWebcam và Python API server."
              );
            }}
          />

          <div className="absolute top-4 left-4 right-4 flex items-center justify-between">
            <div
              className={`px-3 py-1 rounded-full text-sm font-medium text-white ${
                isActive ? "bg-green-500" : "bg-gray-500"
              }`}
            >
              {isActive ? "Đang chạy" : "Tạm dừng"}
            </div>
            <div className="text-right text-xs text-gray-300">
              <div>Điện thoại: {cameraIp}:{cameraPort}</div>
              <div>Overlay API: {apiHost}:{apiPort}</div>
            </div>
          </div>

          {lastPlate && (
            <div className="absolute bottom-4 left-4 right-4 bg-black/70 text-white p-3 rounded-lg">
              <p className="text-lg font-mono font-bold">{lastPlate}</p>
              <p className="text-sm text-gray-300">
                Độ tin cậy: {(detectionConfidence * 100).toFixed(1)}%
              </p>
            </div>
          )}
        </div>

        {streamUrl && (
          <p className="mb-4 text-xs text-gray-500 dark:text-gray-400 break-all">
            URL luồng: {streamUrl}
          </p>
        )}

        <canvas ref={canvasRef} className="hidden" />

        <div className="flex gap-3">
          <button
            onClick={() => {
              if (isActive) {
                void stopCamera();
              } else {
                void connectToExistingStream();
              }
            }}
            className={`flex-1 px-4 py-2 rounded-lg font-medium text-white shadow-theme-sm transition ${
              isActive
                ? "bg-gradient-to-r from-error-500 to-orange-500 hover:from-error-600 hover:to-orange-600"
                : "bg-gradient-to-r from-success-500 to-blue-light-500 hover:from-success-600 hover:to-blue-light-600"
            }`}
          >
            {isStarting ? "Đang kết nối..." : isActive ? "Dừng luồng" : "Khởi động camera"}
          </button>

        </div>

        <div className="mt-4 p-3 bg-gray-50 dark:bg-gray-700/50 rounded-lg">
          <p className="text-sm text-gray-600 dark:text-gray-300">
            <span className="font-medium">Mã trạm:</span> {stationId}
          </p>
        </div>
      </div>
    </div>
  );
};

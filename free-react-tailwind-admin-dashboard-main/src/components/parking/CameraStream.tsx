import { useEffect, useRef, useState } from "react";
import { cameraService } from "../../services/cameraService";

interface CameraStreamProps {
  onDetection?: (plateNumber: string, confidence: number, imageBase64?: string) => void;
  onCheckInEvent?: (event: CheckInCameraEvent) => void;
  stationId?: string;
  stationMode?: "entrance" | "exit";
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
        throw new Error("Camera IP, camera port, and Python API port must be valid.");
      }

      const status = await cameraService.start({
        cameraIp: cameraIp.trim(),
        cameraPort: numericCameraPort,
        apiHost: apiHost.trim() || "localhost",
        apiPort: numericApiPort,
        stationMode,
      });
      setServiceMessage(status.message);

      const apiBaseUrl = `http://${apiHost.trim() || "localhost"}:${numericApiPort}`;
      const healthResponse = await fetch(`${apiBaseUrl}/api/health`);
      if (!healthResponse.ok) {
        throw new Error("Python stream service is not available.");
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
        error instanceof Error ? error.message : "Failed to connect camera stream.";
      console.error("Failed to connect camera stream:", error);
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
              ? `${stationMode === "exit" ? "Checkout" : "Check-in"} detected: ${data.lastEvent.plate}`
              : data.lastEvent.message || `${stationMode === "exit" ? "Checkout" : "Check-in"} not recorded: ${data.lastEvent.plate}`,
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
            message: `${stationMode === "exit" ? "Checkout" : "Check-in"} detected: ${bestMatch.plate}`,
            imageBase64: currentCapture?.imageBase64,
            action,
          });
        }
      } catch (error) {
        console.error("Failed to fetch detection:", error);
        setDetectionError(
          error instanceof Error ? error.message : "Failed to fetch detection."
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
          Live Python Camera Frame
        </h2>

        <div className="mb-4 p-4 bg-gray-50 dark:bg-gray-700/50 rounded-lg border border-gray-200 dark:border-gray-600">
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
            IPWebcam Configuration
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                Camera IP Address
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
                Camera Port
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
                Python API Host
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
                Python API Port
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
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
            Python command:
            {" "}
            <code className="bg-gray-200 dark:bg-gray-600 px-1 py-0.5 rounded">
              python webcam_smart_lowlatency.py --ip {cameraIp}:{cameraPort}
            </code>
          </p>
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
              Detection polling issue: {detectionError}
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
                "Failed to load MJPEG stream. Check IPWebcam and Python API server."
              );
            }}
          />

          <div className="absolute top-4 left-4 right-4 flex items-center justify-between">
            <div
              className={`px-3 py-1 rounded-full text-sm font-medium text-white ${
                isActive ? "bg-green-500" : "bg-gray-500"
              }`}
            >
              {isActive ? "Live" : "Offline"}
            </div>
            <div className="text-right text-xs text-gray-300">
              <div>Phone: {cameraIp}:{cameraPort}</div>
              <div>Overlay API: {apiHost}:{apiPort}</div>
            </div>
          </div>

          {lastPlate && (
            <div className="absolute bottom-4 left-4 right-4 bg-black/70 text-white p-3 rounded-lg">
              <p className="text-lg font-mono font-bold">{lastPlate}</p>
              <p className="text-sm text-gray-300">
                Confidence: {(detectionConfidence * 100).toFixed(1)}%
              </p>
            </div>
          )}
        </div>

        {streamUrl && (
          <p className="mb-4 text-xs text-gray-500 dark:text-gray-400 break-all">
            Stream URL: {streamUrl}
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
            className={`flex-1 px-4 py-2 rounded-lg font-medium text-white transition ${
              isActive
                ? "bg-red-500 hover:bg-red-600"
                : "bg-green-500 hover:bg-green-600"
            }`}
          >
            {isStarting ? "Connecting..." : isActive ? "Stop Stream" : "Start Python Stream"}
          </button>

        </div>

        <div className="mt-4 p-3 bg-gray-50 dark:bg-gray-700/50 rounded-lg">
          <p className="text-sm text-gray-600 dark:text-gray-300">
            <span className="font-medium">Station ID:</span> {stationId}
          </p>
        </div>
      </div>
    </div>
  );
};

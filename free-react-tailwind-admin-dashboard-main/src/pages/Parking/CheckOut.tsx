import { useEffect, useRef, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import {
  CameraStream,
  CheckInCameraEvent,
} from "../../components/parking/CameraStream";
import {
  formatCheckoutPaymentMessage,
  parkingService,
  CheckInOutResult,
} from "../../services/parkingService";

export default function ParkingCheckOut() {
  const [plateNumber, setPlateNumber] = useState("");
  const [stationId, setStationId] = useState("STATION-EXIT-01");
  const [confidence, setConfidence] = useState(0);
  const [checkoutEvent, setCheckoutEvent] = useState<CheckInCameraEvent | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [result, setResult] = useState<CheckInOutResult | null>(null);
  const [errorMessage, setErrorMessage] = useState("");
  const lastSyncedPlateRef = useRef("");
  const lastDisplayedCheckoutIdRef = useRef<string | number | null>(null);

  const handleDetection = (plate: string, conf: number, imageBase64?: string) => {
    setPlateNumber(plate);
    setConfidence(conf);
    if (imageBase64) {
      setCheckoutEvent((previous) => ({
        plateNumber: plate,
        confidence: conf,
        imageBase64,
        success: previous?.success ?? false,
        message: previous?.message || "Đã nhận diện biển số tại cổng ra",
        action: previous?.action,
      }));
    }

    if (plate && lastSyncedPlateRef.current !== plate) {
      lastSyncedPlateRef.current = plate;
      setTimeout(() => {
        void syncLatestCheckout(plate, 4);
      }, 700);
    }
  };

  const syncLatestCheckout = async (plate: string, attemptsLeft = 5) => {
    try {
      const history = await parkingService.getHistoryByPlate(plate);
      const latest = history[0];
      if (!latest?.checkOutTime) {
        if (attemptsLeft > 0) {
          setTimeout(() => {
            void syncLatestCheckout(plate, attemptsLeft - 1);
          }, 1000);
        }
        return;
      }

      const isPendingPayment =
        latest.paymentStatus === "Pending" && (latest.feeAmount || 0) > 0;

      setPlateNumber(plate);
      setConfidence((current) => current || checkoutEvent?.confidence || 0);
      setResult({
        success: !isPendingPayment,
        checkOutId: Number(latest.id),
        checkOutTime: latest.checkOutTime,
        durationMinutes: latest.durationMinutes,
        feeAmount: latest.feeAmount,
        paymentStatus: latest.paymentStatus,
        paymentMethod: latest.paymentMethod,
        requiresPaymentAction: isPendingPayment,
        message: formatCheckoutPaymentMessage(
          plate,
          latest.paymentStatus,
          latest.feeAmount || 0
        ),
      });

      setCheckoutEvent((previous) => ({
        plateNumber: plate,
        confidence: previous?.confidence || confidence,
        imageBase64: latest.checkOutImageBase64 || previous?.imageBase64,
        success: !isPendingPayment,
        message: formatCheckoutPaymentMessage(
          plate,
          latest.paymentStatus,
          latest.feeAmount || 0
        ),
      }));
    } catch (error) {
      console.error("Failed to sync latest checkout:", error);
    }
  };

  const applyCheckoutHistory = (
    checkout: Awaited<ReturnType<typeof parkingService.getLatestCheckOut>>
  ) => {
    if (!checkout?.checkOutTime) return;

    const isPendingPayment =
      checkout.paymentStatus === "Pending" && (checkout.feeAmount || 0) > 0;

    setPlateNumber(checkout.plateNumber);
    setConfidence((current) => current || 1);
    setResult({
      success: !isPendingPayment,
      checkOutId: Number(checkout.id),
      checkOutTime: checkout.checkOutTime,
      durationMinutes: checkout.durationMinutes,
      feeAmount: checkout.feeAmount,
      paymentStatus: checkout.paymentStatus,
      paymentMethod: checkout.paymentMethod,
      requiresPaymentAction: isPendingPayment,
      message: formatCheckoutPaymentMessage(
        checkout.plateNumber,
        checkout.paymentStatus,
        checkout.feeAmount || 0
      ),
    });

    setCheckoutEvent((previous) => ({
      plateNumber: checkout.plateNumber,
      confidence: previous?.confidence || 1,
      imageBase64: checkout.checkOutImageBase64 || previous?.imageBase64,
      success: !isPendingPayment,
      message: formatCheckoutPaymentMessage(
        checkout.plateNumber,
        checkout.paymentStatus,
        checkout.feeAmount || 0
      ),
    }));
  };

  useEffect(() => {
    const syncLatest = async () => {
      try {
        const latest = await parkingService.getLatestCheckOut();
        if (!latest?.id || latest.id === lastDisplayedCheckoutIdRef.current) {
          return;
        }

        lastDisplayedCheckoutIdRef.current = latest.id;
        applyCheckoutHistory(latest);
      } catch (error) {
        // No checkout records yet, or staff token is not ready. Keep the form usable.
      }
    };

    void syncLatest();
    const intervalId = setInterval(() => {
      void syncLatest();
    }, 1200);

    return () => clearInterval(intervalId);
  }, []);

  const handleCheckoutEvent = (event: CheckInCameraEvent) => {
    if (event.message === "__STREAM_STARTED__") {
      setCheckoutEvent({
        ...event,
        message: "Đang chờ biển số xe ra...",
      });
      return;
    }

    const detectedPlate = event.plateNumber.toUpperCase().trim();
    const currentPlate = plateNumber.toUpperCase().trim();
    const isDifferentPlate = Boolean(detectedPlate && detectedPlate !== currentPlate);

    setPlateNumber(detectedPlate);
    setConfidence(event.confidence);
    setCheckoutEvent((previous) => ({
      ...event,
      plateNumber: detectedPlate,
      imageBase64: event.imageBase64 || previous?.imageBase64,
    }));
    if (isDifferentPlate) {
      setResult(null);
    }
    setErrorMessage("");

    if (event.success && detectedPlate) {
      setTimeout(() => {
        void syncLatestCheckout(detectedPlate);
      }, 900);
    }
  };

  const handleCheckOut = async () => {
    if (!plateNumber) {
      setErrorMessage("Vui lòng quét biển số trước");
      return;
    }

    setIsProcessing(true);
    setErrorMessage("");

    try {
      const response = await parkingService.checkOut(
        plateNumber.toUpperCase().trim(),
        stationId,
        checkoutEvent?.imageBase64
      );

      const normalizedPlate = plateNumber.toUpperCase().trim();
      setResult({
        ...response,
        message: formatCheckoutPaymentMessage(
          normalizedPlate,
          response.paymentStatus,
          response.feeAmount || 0
        ),
      });
      setCheckoutEvent((previous) => ({
        plateNumber: normalizedPlate,
        confidence,
        imageBase64: previous?.imageBase64,
        success: response.success,
        message: formatCheckoutPaymentMessage(
          normalizedPlate,
          response.paymentStatus,
          response.feeAmount || 0
        ),
      }));
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : "Ghi nhận xe ra thất bại"
      );
    } finally {
      setIsProcessing(false);
    }
  };

  const handleConfirmPayment = async () => {
    if (!result?.checkOutId) return;

    setIsProcessing(true);
    setErrorMessage("");

    try {
      const response = await parkingService.confirmCheckOutPayment(result.checkOutId, "Cash");
      const normalizedPlate = plateNumber.toUpperCase().trim();
      setResult({
        ...response,
        message: formatCheckoutPaymentMessage(
          normalizedPlate,
          response.paymentStatus,
          response.feeAmount || result.feeAmount || 0
        ),
      });
      setCheckoutEvent((previous) => ({
        plateNumber: normalizedPlate,
        confidence,
        imageBase64: previous?.imageBase64,
        success: response.success,
        message: formatCheckoutPaymentMessage(
          normalizedPlate,
          response.paymentStatus,
          response.feeAmount || result.feeAmount || 0
        ),
      }));
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : "Xác nhận thanh toán thất bại"
      );
    } finally {
      setIsProcessing(false);
    }
  };

  const needsCashConfirmation = Boolean(result?.requiresPaymentAction && result.checkOutId);
  const checkoutStatusText = result?.message || "";

  return (
    <>
      <PageMeta
        title="Xe ra | Smart Parking"
        description="Ghi nhận xe ra bằng nhận diện biển số"
      />
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">
            Xe ra
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            Ghi nhận xe ra bằng camera nhận diện biển số
          </p>
        </div>

        {/* Alert Messages */}
        {errorMessage && (
          <div className="p-4 rounded-lg bg-red-50 dark:bg-red-900/10 border border-red-200 dark:border-red-800">
            <p className="text-sm text-red-600 dark:text-red-400">
              {errorMessage}
            </p>
          </div>
        )}

        {result && (
          <div
            className={`p-4 rounded-lg border ${
              result.success
                ? "bg-green-50 dark:bg-green-900/10 border-green-200 dark:border-green-800"
                : "bg-red-50 dark:bg-red-900/10 border-red-200 dark:border-red-800"
            }`}
          >
            <p
              className={`text-sm font-medium ${
                result.success
                  ? "text-green-600 dark:text-green-400"
                  : "text-red-600 dark:text-red-400"
              }`}
            >
              {checkoutStatusText}
            </p>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Camera Stream */}
          <div className="lg:col-span-2">
            <CameraStream
              onDetection={handleDetection}
              onCheckInEvent={handleCheckoutEvent}
              stationId={stationId}
              stationMode="exit"
            />
          </div>

          {/* Check-Out Form */}
          <div className="lg:col-span-1">
            <ComponentCard title="Thông tin xe ra">
              <div className="space-y-4">
                {/* Station Selection */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Mã trạm ra
                  </label>
                  <select
                    value={stationId}
                    onChange={(e) => setStationId(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option>STATION-EXIT-01</option>
                    <option>STATION-EXIT-02</option>
                    <option>STATION-EXIT-03</option>
                  </select>
                </div>

                {/* Plate Number */}
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

                {/* Confidence */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Độ tin cậy nhận diện
                  </label>
                  <div className="w-full bg-gray-200 dark:bg-gray-600 rounded-full h-2">
                    <div
                      className="bg-blue-500 h-2 rounded-full transition-all"
                      style={{ width: `${confidence * 100}%` }}
                    ></div>
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                    {(confidence * 100).toFixed(1)}%
                  </p>
                </div>

                {/* Info Box */}
                <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
                  <p className="text-xs text-blue-700 dark:text-blue-400">
                    <span className="font-semibold">Thông tin:</span> Mở camera, quét biển số tại cổng ra và bấm ghi nhận xe ra nếu cần thao tác thủ công.
                  </p>
                </div>

                {checkoutEvent?.imageBase64 && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Ảnh xe ra
                    </label>
                    <img
                      src={`data:image/jpeg;base64,${checkoutEvent.imageBase64}`}
                      alt={`Checkout capture for ${checkoutEvent.plateNumber}`}
                      className="w-full rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-900 object-contain"
                    />
                  </div>
                )}

                <div className="grid grid-cols-1 gap-3">
                  <button
                    onClick={handleCheckOut}
                    disabled={!plateNumber || isProcessing || needsCashConfirmation}
                    className={`w-full px-4 py-3 rounded-lg font-semibold text-white transition ${
                      result?.success
                        ? "bg-green-500"
                        : needsCashConfirmation
                          ? "bg-yellow-500"
                          : "bg-orange-500 hover:bg-orange-600 disabled:bg-gray-400"
                    }`}
                  >
                    {isProcessing ? "Đang xử lý..." : checkoutStatusText || "Ghi nhận xe ra"}
                  </button>

                  <button
                    onClick={handleConfirmPayment}
                    disabled={!needsCashConfirmation || isProcessing}
                    className="w-full px-4 py-3 rounded-lg font-semibold text-white bg-green-600 hover:bg-green-700 disabled:bg-gray-400 transition"
                  >
                    {isProcessing ? "Đang xác nhận..." : "Xác nhận thu tiền mặt"}
                  </button>
                </div>
              </div>
            </ComponentCard>
          </div>
        </div>
      </div>
    </>
  );
}

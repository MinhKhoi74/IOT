import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import {
  VehicleLocationAlert,
  vehicleLocationService,
} from "../../services/vehicleLocationService";
import { useAuth } from "../../context/AuthContext";

const formatDateTime = (value?: string) =>
  value ? new Date(value).toLocaleString("vi-VN") : "-";

const valueOrDash = (value?: string | number | boolean | null) => {
  if (value === null || value === undefined || value === "") return "-";
  return String(value);
};

const Field: React.FC<{ label: string; value?: string | number | boolean | null }> = ({
  label,
  value,
}) => (
  <div className="rounded-lg border border-gray-200 bg-white px-4 py-3 dark:border-gray-800 dark:bg-gray-800">
    <p className="text-xs font-medium uppercase text-gray-500 dark:text-gray-400">{label}</p>
    <p className="mt-1 break-words text-sm font-medium text-gray-800 dark:text-white/90">
      {valueOrDash(value)}
    </p>
  </div>
);

export default function VehicleLocationNotificationDetail() {
  const { id } = useParams();
  const { user } = useAuth();
  const [alert, setAlert] = useState<VehicleLocationAlert | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showTechnicalDetails, setShowTechnicalDetails] = useState(false);
  const roles = user?.roles || user?.Roles || (user?.role ? [user.role] : []);
  const isAdmin = roles.includes("Admin");

  useEffect(() => {
    const load = async () => {
      try {
        const numericId = Number(id);
        if (!Number.isFinite(numericId)) {
          throw new Error("Mã thông báo không hợp lệ.");
        }

        setAlert(await vehicleLocationService.alertDetail(numericId));
        setError("");
      } catch (err) {
        setError(err instanceof Error ? err.message : "Không tải được thông báo.");
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, [id]);

  return (
    <>
      <PageMeta
        title="Thông báo vị trí xe | Smart Parking"
        description="Chi tiết thông báo vị trí xe"
      />

      <div className="space-y-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-800 dark:text-white">
              Thông báo vị trí xe
            </h1>
            <p className="text-gray-600 dark:text-gray-400">
              Chi tiết vị trí xe dành cho nhân viên bãi.
            </p>
          </div>
          <Link
            to="/parking/zone-cameras"
            className="w-fit rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100 dark:border-gray-700 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            Quay lại camera khu vực
          </Link>
        </div>

        {loading && (
          <ComponentCard>
            <p className="text-sm text-gray-500 dark:text-gray-400">Đang tải thông báo...</p>
          </ComponentCard>
        )}

        {error && (
          <ComponentCard>
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          </ComponentCard>
        )}

        {alert && (
          <>
            <ComponentCard title={`${alert.licensePlate} - ${alert.severity}`}>
              <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_320px]">
                <div className="space-y-4">
                  <div className="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-800 dark:bg-gray-900/30">
                    <p className="text-sm font-semibold text-gray-800 dark:text-white">Nội dung</p>
                    <p className="mt-2 text-sm text-gray-600 dark:text-gray-300">{alert.message}</p>
                  </div>

                  <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
                    <Field label="Biển số" value={alert.licensePlate} />
                    <Field label="Vị trí" value={alert.locationName} />
                    <Field label="Độ tin cậy" value={`${(alert.confidence * 100).toFixed(1)}%`} />
                    <Field label="Thời gian nhận diện" value={formatDateTime(alert.detectedAt)} />
                    <Field label="Trạng thái" value={alert.status} />
                    <Field label="Mức độ" value={alert.severity} />
                  </div>

                  {isAdmin && (
                    <div className="space-y-3">
                      <button
                        type="button"
                        onClick={() => setShowTechnicalDetails((value) => !value)}
                        className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100 dark:border-gray-700 dark:text-gray-300 dark:hover:bg-gray-800"
                      >
                        {showTechnicalDetails ? "Ẩn chi tiết kỹ thuật" : "Xem chi tiết kỹ thuật"}
                      </button>

                      {showTechnicalDetails && (
                        <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
                          <Field label="Tên chủ xe" value={alert.ownerName} />
                          <Field label="UserId" value={alert.userId} />
                          <Field label="VehicleId" value={alert.vehicleId} />
                          <Field label="Record Id" value={alert.id} />
                          <Field label="CheckInOutId" value={alert.checkInOutId} />
                          <Field label="CameraId" value={alert.cameraId} />
                          <Field label="ParkingLotCode" value={alert.parkingLotCode} />
                          <Field label="ZoneCode" value={alert.zoneCode} />
                          <Field label="ColumnCode" value={alert.columnCode} />
                          <Field label="CreatedAt" value={formatDateTime(alert.createdAt)} />
                          <Field label="IsLatest" value={alert.isLatest} />
                        </div>
                      )}
                    </div>
                  )}
                </div>

                <div className="space-y-4">
                  <div className="rounded-lg border border-gray-200 bg-white p-3 dark:border-gray-800 dark:bg-gray-800">
                    <p className="mb-2 text-sm font-semibold text-gray-800 dark:text-white">Ảnh biển số</p>
                    {alert.imageBase64 ? (
                      <img
                        src={`data:image/jpeg;base64,${alert.imageBase64}`}
                        alt="Detected plate"
                        className="max-h-48 w-full rounded-lg object-contain bg-gray-950"
                      />
                    ) : (
                      <p className="text-sm text-gray-500 dark:text-gray-400">Chưa có ảnh biển số.</p>
                    )}
                  </div>

                  <div className="rounded-lg border border-gray-200 bg-white p-3 dark:border-gray-800 dark:bg-gray-800">
                    <p className="mb-2 text-sm font-semibold text-gray-800 dark:text-white">Ảnh toàn cảnh</p>
                    {alert.fullFrameImageBase64 ? (
                      <img
                        src={`data:image/jpeg;base64,${alert.fullFrameImageBase64}`}
                        alt="Full detection frame"
                        className="max-h-80 w-full rounded-lg object-contain bg-gray-950"
                      />
                    ) : (
                      <p className="text-sm text-gray-500 dark:text-gray-400">Chưa có ảnh toàn cảnh.</p>
                    )}
                  </div>
                </div>
              </div>
            </ComponentCard>
          </>
        )}
      </div>
    </>
  );
}

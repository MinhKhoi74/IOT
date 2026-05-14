import { useEffect, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import { ParkingDashboard, ParkingHistory, parkingService } from "../../services/parkingService";
import { useAuth } from "../../context/AuthContext";
import { createParkingHubConnection } from "../../services/realtimeService";

const formatDateTime = (value?: string) =>
  value ? new Date(value).toLocaleString("vi-VN") : "Chưa ra bãi";

const formatMoney = (value?: number) =>
  (value || 0).toLocaleString("vi-VN", { style: "currency", currency: "VND" });

const translatePaymentStatus = (value?: string) => {
  const normalized = (value || "Pending").toLowerCase();
  if (normalized === "paid") return "Đã thanh toán";
  if (normalized === "pending") return "Chờ thanh toán";
  return value || "Chờ thanh toán";
};

const pageSize = 10;

const toImageSrc = (base64?: string) => {
  if (!base64) return "";
  return base64.startsWith("data:") ? base64 : `data:image/jpeg;base64,${base64}`;
};

export default function Home() {
  const { user } = useAuth();
  const roles = user?.roles || user?.Roles || (user?.role ? [user.role] : []);
  const canViewParkingDashboard = roles.includes("Staff") || roles.includes("Admin");
  const [dashboard, setDashboard] = useState<ParkingDashboard | null>(null);
  const [selectedSession, setSelectedSession] = useState<ParkingHistory | null>(null);
  const [loading, setLoading] = useState(canViewParkingDashboard);
  const [error, setError] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [filters, setFilters] = useState({
    plateNumber: "",
    checkInFrom: "",
    checkInTo: "",
    paymentStatus: "",
  });

  useEffect(() => {
    if (!canViewParkingDashboard) return;

    const loadDashboard = async (showLoading = true) => {
      try {
        if (showLoading) setLoading(true);
        setDashboard(await parkingService.getDashboard());
        setError("");
      } catch (err) {
        setError(err instanceof Error ? err.message : "Không tải được tổng quan bãi xe");
      } finally {
        setLoading(false);
      }
    };

    loadDashboard();

    const connection = createParkingHubConnection();
    connection.on("ParkingDashboardUpdated", () => loadDashboard(false));
    connection.start().catch(() => undefined);

    return () => {
      connection.off("ParkingDashboardUpdated");
      connection.stop().catch(() => undefined);
    };
  }, [canViewParkingDashboard]);

  useEffect(() => {
    setCurrentPage(1);
  }, [dashboard?.sessions.length, filters.plateNumber, filters.checkInFrom, filters.checkInTo, filters.paymentStatus]);

  const openDetail = async (id: string | number) => {
    try {
      setSelectedSession(await parkingService.getSessionDetail(id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được chi tiết lượt gửi xe");
    }
  };

  const resetFilters = () => {
    setFilters({
      plateNumber: "",
      checkInFrom: "",
      checkInTo: "",
      paymentStatus: "",
    });
  };

  if (!canViewParkingDashboard) {
    return (
      <>
        <PageMeta title="Smart Parking | Trang chủ" description="Trang chủ Smart Parking" />
        <ComponentCard title="Xin chào">
          <p className="text-sm text-gray-600 dark:text-gray-400">Sử dụng thanh bên để xem hồ sơ và thông tin bãi xe.</p>
        </ComponentCard>
      </>
    );
  }

  const sessions = dashboard?.sessions || [];
  const filteredSessions = sessions.filter((session) => {
    const plateMatches = !filters.plateNumber.trim()
      || session.plateNumber.toLowerCase().includes(filters.plateNumber.trim().toLowerCase());
    const checkInTime = new Date(session.checkInTime).getTime();
    const fromMatches = !filters.checkInFrom
      || checkInTime >= new Date(`${filters.checkInFrom}T00:00:00`).getTime();
    const toMatches = !filters.checkInTo
      || checkInTime <= new Date(`${filters.checkInTo}T23:59:59`).getTime();
    const paymentMatches = !filters.paymentStatus
      || (session.paymentStatus || "Pending").toLowerCase() === filters.paymentStatus.toLowerCase();

    return plateMatches && fromMatches && toMatches && paymentMatches;
  });
  const totalPages = Math.max(1, Math.ceil(filteredSessions.length / pageSize));
  const safePage = Math.min(currentPage, totalPages);
  const pagedSessions = filteredSessions.slice((safePage - 1) * pageSize, safePage * pageSize);

  return (
    <>
    <PageMeta title="Tổng quan bãi xe | Smart Parking" description="Tổng quan bãi xe theo thời gian thực" />
      <div className="space-y-6">
        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-800 dark:bg-red-900/10">
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          </div>
        )}

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <ComponentCard title="Xe đang trong bãi">
            <p className="text-3xl font-semibold text-gray-900 dark:text-white">
              {dashboard?.activeVehicleCount ?? 0}
            </p>
          </ComponentCard>
          <ComponentCard title="Tổng doanh thu">
            <p className="text-3xl font-semibold text-gray-900 dark:text-white">
              {formatMoney(dashboard?.totalRevenue)}
            </p>
          </ComponentCard>
        </div>

        <ComponentCard title="Lượt xe vào / ra">
          {loading ? (
            <div className="py-8 text-center text-gray-600 dark:text-gray-400">Đang tải dữ liệu...</div>
          ) : !dashboard?.sessions.length ? (
            <div className="py-8 text-center text-gray-600 dark:text-gray-400">Chưa có lượt gửi xe</div>
          ) : (
            <div>
              <div className="mb-5 grid grid-cols-1 gap-3 md:grid-cols-5">
                <input
                  type="text"
                  placeholder="Tìm biển số"
                  value={filters.plateNumber}
                  onChange={(event) => setFilters({ ...filters, plateNumber: event.target.value })}
                  className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-900 dark:text-white"
                />
                <input
                  type="date"
                  value={filters.checkInFrom}
                  onChange={(event) => setFilters({ ...filters, checkInFrom: event.target.value })}
                  className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-900 dark:text-white"
                />
                <input
                  type="date"
                  value={filters.checkInTo}
                  onChange={(event) => setFilters({ ...filters, checkInTo: event.target.value })}
                  className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-900 dark:text-white"
                />
                <select
                  value={filters.paymentStatus}
                  onChange={(event) => setFilters({ ...filters, paymentStatus: event.target.value })}
                  className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-900 dark:text-white"
                >
                  <option value="">Tất cả thanh toán</option>
                  <option value="Paid">Đã thanh toán</option>
                  <option value="Pending">Chờ thanh toán</option>
                </select>
                <button
                  type="button"
                  onClick={resetFilters}
                  className="rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 dark:border-gray-700 dark:text-gray-300 dark:hover:bg-gray-800"
                >
                  Đặt lại
                </button>
              </div>

              {filteredSessions.length === 0 ? (
                <div className="py-8 text-center text-gray-600 dark:text-gray-400">Không có lượt gửi xe phù hợp</div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full">
                    <thead className="border-b border-gray-200 dark:border-gray-700">
                      <tr>
                        <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Biển số</th>
                        <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Vào bãi</th>
                        <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Ra bãi</th>
                        <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Thanh toán</th>
                        <th className="px-4 py-3 text-right font-semibold text-gray-800 dark:text-white">Chi tiết</th>
                      </tr>
                    </thead>
                    <tbody>
                      {pagedSessions.map((session) => (
                        <tr key={session.id} className="border-b border-gray-100 hover:bg-gray-50 dark:border-gray-700 dark:hover:bg-gray-800">
                          <td className="px-4 py-3 font-medium text-gray-900 dark:text-white">{session.plateNumber}</td>
                          <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{formatDateTime(session.checkInTime)}</td>
                          <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{formatDateTime(session.checkOutTime)}</td>
                          <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{translatePaymentStatus(session.paymentStatus)}</td>
                          <td className="px-4 py-3 text-right">
                            <button onClick={() => openDetail(session.id)} className="text-sm font-medium text-blue-500 hover:text-blue-600">
                              Chi tiết
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                  <div className="mt-4 flex items-center justify-between text-sm text-gray-600 dark:text-gray-400">
                    <span>
                      Trang {safePage} / {totalPages} - {filteredSessions.length} lượt
                    </span>
                    <div className="flex gap-2">
                      <button
                        onClick={() => setCurrentPage((page) => Math.max(1, page - 1))}
                        disabled={safePage === 1}
                        className="rounded-lg border border-gray-200 px-3 py-1 disabled:opacity-40 dark:border-gray-700"
                      >
                        Trước
                      </button>
                      <button
                        onClick={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
                        disabled={safePage === totalPages}
                        className="rounded-lg border border-gray-200 px-3 py-1 disabled:opacity-40 dark:border-gray-700"
                      >
                        Sau
                      </button>
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}
        </ComponentCard>

        {selectedSession && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
            <ComponentCard title="Chi tiết lượt gửi xe" className="w-full max-w-3xl">
              <div className="mb-4 flex items-start justify-between">
                <div>
                  <h2 className="text-xl font-semibold text-gray-800 dark:text-white">{selectedSession.plateNumber}</h2>
                  <p className="text-sm text-gray-500 dark:text-gray-400">Lượt gửi xe #{selectedSession.id}</p>
                </div>
                <button onClick={() => setSelectedSession(null)} className="rounded-lg px-3 py-1 text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800">
                  Đóng
                </button>
              </div>
              <div className="grid grid-cols-1 gap-4 text-sm md:grid-cols-2">
                <div><span className="text-gray-500">Vào bãi:</span> {formatDateTime(selectedSession.checkInTime)}</div>
                <div><span className="text-gray-500">Ra bãi:</span> {formatDateTime(selectedSession.checkOutTime)}</div>
                <div><span className="text-gray-500">Thời lượng:</span> {selectedSession.durationMinutes ?? 0} phút</div>
                <div><span className="text-gray-500">Phí:</span> {formatMoney(selectedSession.feeAmount)}</div>
                <div><span className="text-gray-500">Trạng thái phí:</span> {translatePaymentStatus(selectedSession.feeStatus)}</div>
                <div><span className="text-gray-500">Thanh toán:</span> {translatePaymentStatus(selectedSession.paymentStatus)} {selectedSession.paymentMethod ? `(${selectedSession.paymentMethod})` : ""}</div>
                <div><span className="text-gray-500">Trạng thái:</span> {selectedSession.status}</div>
              </div>
              <div className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <p className="mb-2 text-sm font-medium text-gray-800 dark:text-white">Ảnh xe vào</p>
                  {selectedSession.checkInImageBase64 ? (
                    <img
                      src={toImageSrc(selectedSession.checkInImageBase64)}
                      alt="Check-in"
                      className="max-h-72 w-full rounded-lg border border-gray-200 object-contain dark:border-gray-700"
                    />
                  ) : (
                    <div className="rounded-lg border border-dashed border-gray-200 p-6 text-center text-sm text-gray-500 dark:border-gray-700">
                      Chưa có ảnh xe vào
                    </div>
                  )}
                </div>
                <div>
                  <p className="mb-2 text-sm font-medium text-gray-800 dark:text-white">Ảnh xe ra</p>
                  {selectedSession.checkOutImageBase64 ? (
                    <img
                      src={toImageSrc(selectedSession.checkOutImageBase64)}
                      alt="Check-out"
                      className="max-h-72 w-full rounded-lg border border-gray-200 object-contain dark:border-gray-700"
                    />
                  ) : (
                    <div className="rounded-lg border border-dashed border-gray-200 p-6 text-center text-sm text-gray-500 dark:border-gray-700">
                      Chưa có ảnh xe ra
                    </div>
                  )}
                </div>
              </div>
            </ComponentCard>
          </div>
        )}
      </div>
    </>
  );
}

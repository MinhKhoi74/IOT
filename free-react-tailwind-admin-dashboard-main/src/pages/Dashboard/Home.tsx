import { useEffect, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import { ParkingDashboard, ParkingHistory, parkingService } from "../../services/parkingService";
import { useAuth } from "../../context/AuthContext";
import { createParkingHubConnection } from "../../services/realtimeService";

const formatDateTime = (value?: string) =>
  value ? new Date(value).toLocaleString("vi-VN") : "Not checked out";

const formatMoney = (value?: number) =>
  (value || 0).toLocaleString("vi-VN", { style: "currency", currency: "VND" });

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
        setError(err instanceof Error ? err.message : "Failed to load parking dashboard");
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
      setError(err instanceof Error ? err.message : "Failed to load session detail");
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
        <PageMeta title="Smart Parking | Home" description="Smart Parking user home" />
        <ComponentCard title="Welcome">
          <p className="text-sm text-gray-600 dark:text-gray-400">Use the sidebar to view your profile and parking information.</p>
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
      <PageMeta title="Parking Dashboard | Smart Parking" description="Live parking dashboard" />
      <div className="space-y-6">
        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-800 dark:bg-red-900/10">
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          </div>
        )}

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <ComponentCard title="Vehicles In Parking">
            <p className="text-3xl font-semibold text-gray-900 dark:text-white">
              {dashboard?.activeVehicleCount ?? 0}
            </p>
          </ComponentCard>
          <ComponentCard title="Total Revenue">
            <p className="text-3xl font-semibold text-gray-900 dark:text-white">
              {formatMoney(dashboard?.totalRevenue)}
            </p>
          </ComponentCard>
        </div>

        <ComponentCard title="Check-in / Check-out Sessions">
          {loading ? (
            <div className="py-8 text-center text-gray-600 dark:text-gray-400">Loading sessions...</div>
          ) : !dashboard?.sessions.length ? (
            <div className="py-8 text-center text-gray-600 dark:text-gray-400">No sessions found</div>
          ) : (
            <div>
              <div className="mb-5 grid grid-cols-1 gap-3 md:grid-cols-5">
                <input
                  type="text"
                  placeholder="Search plate"
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
                  <option value="">All payments</option>
                  <option value="Paid">Paid</option>
                  <option value="Pending">Pending</option>
                </select>
                <button
                  type="button"
                  onClick={resetFilters}
                  className="rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 dark:border-gray-700 dark:text-gray-300 dark:hover:bg-gray-800"
                >
                  Reset
                </button>
              </div>

              {filteredSessions.length === 0 ? (
                <div className="py-8 text-center text-gray-600 dark:text-gray-400">No sessions match the filters</div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full">
                    <thead className="border-b border-gray-200 dark:border-gray-700">
                      <tr>
                        <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Plate</th>
                        <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Check-in</th>
                        <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Check-out</th>
                        <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Payment</th>
                        <th className="px-4 py-3 text-right font-semibold text-gray-800 dark:text-white">Detail</th>
                      </tr>
                    </thead>
                    <tbody>
                      {pagedSessions.map((session) => (
                        <tr key={session.id} className="border-b border-gray-100 hover:bg-gray-50 dark:border-gray-700 dark:hover:bg-gray-800">
                          <td className="px-4 py-3 font-medium text-gray-900 dark:text-white">{session.plateNumber}</td>
                          <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{formatDateTime(session.checkInTime)}</td>
                          <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{formatDateTime(session.checkOutTime)}</td>
                          <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{session.paymentStatus || "Pending"}</td>
                          <td className="px-4 py-3 text-right">
                            <button onClick={() => openDetail(session.id)} className="text-sm font-medium text-blue-500 hover:text-blue-600">
                              Details
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                  <div className="mt-4 flex items-center justify-between text-sm text-gray-600 dark:text-gray-400">
                    <span>
                      Page {safePage} / {totalPages} - {filteredSessions.length} sessions
                    </span>
                    <div className="flex gap-2">
                      <button
                        onClick={() => setCurrentPage((page) => Math.max(1, page - 1))}
                        disabled={safePage === 1}
                        className="rounded-lg border border-gray-200 px-3 py-1 disabled:opacity-40 dark:border-gray-700"
                      >
                        Previous
                      </button>
                      <button
                        onClick={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
                        disabled={safePage === totalPages}
                        className="rounded-lg border border-gray-200 px-3 py-1 disabled:opacity-40 dark:border-gray-700"
                      >
                        Next
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
            <ComponentCard title="Session Details" className="w-full max-w-3xl">
              <div className="mb-4 flex items-start justify-between">
                <div>
                  <h2 className="text-xl font-semibold text-gray-800 dark:text-white">{selectedSession.plateNumber}</h2>
                  <p className="text-sm text-gray-500 dark:text-gray-400">Parking session #{selectedSession.id}</p>
                </div>
                <button onClick={() => setSelectedSession(null)} className="rounded-lg px-3 py-1 text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800">
                  Close
                </button>
              </div>
              <div className="grid grid-cols-1 gap-4 text-sm md:grid-cols-2">
                <div><span className="text-gray-500">Check-in:</span> {formatDateTime(selectedSession.checkInTime)}</div>
                <div><span className="text-gray-500">Check-out:</span> {formatDateTime(selectedSession.checkOutTime)}</div>
                <div><span className="text-gray-500">Duration:</span> {selectedSession.durationMinutes ?? 0} minutes</div>
                <div><span className="text-gray-500">Fee:</span> {formatMoney(selectedSession.feeAmount)}</div>
                <div><span className="text-gray-500">Fee status:</span> {selectedSession.feeStatus || "Pending"}</div>
                <div><span className="text-gray-500">Payment:</span> {selectedSession.paymentStatus || "Pending"} {selectedSession.paymentMethod ? `(${selectedSession.paymentMethod})` : ""}</div>
                <div><span className="text-gray-500">Status:</span> {selectedSession.status}</div>
              </div>
              <div className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <p className="mb-2 text-sm font-medium text-gray-800 dark:text-white">Check-in Image</p>
                  {selectedSession.checkInImageBase64 ? (
                    <img
                      src={toImageSrc(selectedSession.checkInImageBase64)}
                      alt="Check-in"
                      className="max-h-72 w-full rounded-lg border border-gray-200 object-contain dark:border-gray-700"
                    />
                  ) : (
                    <div className="rounded-lg border border-dashed border-gray-200 p-6 text-center text-sm text-gray-500 dark:border-gray-700">
                      No check-in image
                    </div>
                  )}
                </div>
                <div>
                  <p className="mb-2 text-sm font-medium text-gray-800 dark:text-white">Check-out Image</p>
                  {selectedSession.checkOutImageBase64 ? (
                    <img
                      src={toImageSrc(selectedSession.checkOutImageBase64)}
                      alt="Check-out"
                      className="max-h-72 w-full rounded-lg border border-gray-200 object-contain dark:border-gray-700"
                    />
                  ) : (
                    <div className="rounded-lg border border-dashed border-gray-200 p-6 text-center text-sm text-gray-500 dark:border-gray-700">
                      No check-out image
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

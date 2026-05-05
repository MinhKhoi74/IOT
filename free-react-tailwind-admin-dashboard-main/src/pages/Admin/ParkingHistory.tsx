import { useState, useEffect } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import { parkingService, ParkingHistory } from "../../services/parkingService";

export default function ParkingHistoryPage() {
  const [history, setHistory] = useState<ParkingHistory[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [filters, setFilters] = useState({
    plateNumber: "",
    startDate: "",
    endDate: "",
  });

  useEffect(() => {
    loadHistory();
  }, []);

  const loadHistory = async () => {
    setIsLoading(true);
    try {
      const data = await parkingService.getParkingHistory({
        plateNumber: filters.plateNumber || undefined,
        startDate: filters.startDate || undefined,
        endDate: filters.endDate || undefined,
      });

      setHistory(Array.isArray(data) ? data : []);
      setError("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load history");
    } finally {
      setIsLoading(false);
    }
  };

  const handleFilter = () => {
    loadHistory();
  };

  const handleReset = () => {
    setFilters({
      plateNumber: "",
      startDate: "",
      endDate: "",
    });
    loadHistory();
  };

  const getTotalDuration = (start: string, end?: string) => {
    if (!end) return "Ongoing";
    const startTime = new Date(start);
    const endTime = new Date(end);
    const duration = Math.floor(
      (endTime.getTime() - startTime.getTime()) / (1000 * 60)
    );
    const hours = Math.floor(duration / 60);
    const minutes = duration % 60;
    return `${hours}h ${minutes}m`;
  };

  return (
    <>
      <PageMeta
        title="Parking History | Smart Parking Admin"
        description="View parking history and vehicle records"
      />
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">
            Parking History
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            View all vehicle check-in and check-out records
          </p>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="p-4 rounded-lg bg-red-50 dark:bg-red-900/10 border border-red-200 dark:border-red-800">
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          </div>
        )}

        {/* Filters */}
        <ComponentCard>
          <h2 className="text-lg font-semibold text-gray-800 dark:text-white mb-4">
            Filters
          </h2>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Plate Number
              </label>
              <input
                type="text"
                value={filters.plateNumber}
                onChange={(e) =>
                  setFilters({ ...filters, plateNumber: e.target.value.toUpperCase() })
                }
                placeholder="ABC-1234"
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white font-mono"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Start Date
              </label>
              <input
                type="date"
                value={filters.startDate}
                onChange={(e) =>
                  setFilters({ ...filters, startDate: e.target.value })
                }
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                End Date
              </label>
              <input
                type="date"
                value={filters.endDate}
                onChange={(e) =>
                  setFilters({ ...filters, endDate: e.target.value })
                }
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
              />
            </div>

            <div className="flex items-end gap-2">
              <button
                onClick={handleFilter}
                className="flex-1 px-4 py-2 rounded-lg font-medium text-white bg-blue-500 hover:bg-blue-600 transition"
              >
                Search
              </button>
              <button
                onClick={handleReset}
                className="flex-1 px-4 py-2 rounded-lg font-medium text-gray-700 dark:text-gray-300 bg-gray-200 dark:bg-gray-600 transition"
              >
                Reset
              </button>
            </div>
          </div>
        </ComponentCard>

        {/* History Table */}
        <ComponentCard>
          {isLoading ? (
            <div className="text-center py-8">
              <p className="text-gray-600 dark:text-gray-400">Loading history...</p>
            </div>
          ) : history.length === 0 ? (
            <div className="text-center py-8">
              <p className="text-gray-600 dark:text-gray-400">No parking records found</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b border-gray-200 dark:border-gray-700">
                  <tr>
                    <th className="text-left px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Plate Number
                    </th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Check-In
                    </th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Check-Out
                    </th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Duration
                    </th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Fee
                    </th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-800 dark:text-white">
                      Status
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {history.map((record) => (
                    <tr
                      key={record.id}
                      className="border-b border-gray-100 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800"
                    >
                      <td className="px-4 py-3 text-gray-900 dark:text-white font-mono font-semibold">
                        {record.plateNumber}
                      </td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400 text-sm">
                        {new Date(record.checkInTime).toLocaleString()}
                      </td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400 text-sm">
                        {record.checkOutTime
                          ? new Date(record.checkOutTime).toLocaleString()
                          : "—"}
                      </td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400 text-sm">
                        {getTotalDuration(record.checkInTime, record.checkOutTime)}
                      </td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400 font-medium">
                        {record.fee ? `$${record.fee.toFixed(2)}` : "—"}
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className={`px-3 py-1 rounded-full text-xs font-medium ${
                            record.status === "completed"
                              ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400"
                              : record.status === "parked"
                              ? "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400"
                              : "bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-400"
                          }`}
                        >
                          {record.status}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </ComponentCard>
      </div>
    </>
  );
}

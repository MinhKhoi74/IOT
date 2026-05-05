import { useEffect, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import { BranchInfo, Customer, customerService } from "../../services/customerService";

export default function StaffManagement() {
  const [staff, setStaff] = useState<Customer[]>([]);
  const [branches, setBranches] = useState<BranchInfo[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    phoneNumber: "",
    password: "",
    branchId: "",
  });

  useEffect(() => {
    loadStaff();
    customerService.getBranches().then(setBranches).catch(() => undefined);
  }, []);

  const loadStaff = async () => {
    setIsLoading(true);
    try {
      const data = await customerService.getAll();
      setStaff(Array.isArray(data) ? data : []);
      setError("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load staff");
    } finally {
      setIsLoading(false);
    }
  };

  const handleAddClick = () => {
    setFormData({
      fullName: "",
      email: "",
      phoneNumber: "",
      password: "",
      branchId: branches[0]?.id || "",
    });
    setShowForm(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (!formData.branchId) {
        setError("Please select a branch before creating staff");
        return;
      }

      await customerService.create(formData);
      setShowForm(false);
      loadStaff();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create staff");
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Delete this staff account?")) return;

    try {
      await customerService.delete(id);
      loadStaff();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete staff");
    }
  };

  return (
    <>
      <PageMeta title="Staff Management | Smart Parking Admin" description="Manage parking staff" />
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Staff Management</h1>
            <p className="text-gray-600 dark:text-gray-400">Create and manage staff accounts</p>
          </div>
          <button
            onClick={handleAddClick}
            className="rounded-lg bg-blue-500 px-4 py-2 font-medium text-white transition hover:bg-blue-600"
          >
            + Add Staff
          </button>
        </div>

        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-800 dark:bg-red-900/10">
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          </div>
        )}

        {showForm && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
            <ComponentCard title="Staff Form" className="w-full max-w-md">
              <h2 className="mb-4 text-xl font-semibold text-gray-800 dark:text-white">Add New Staff</h2>

              <form onSubmit={handleSubmit} className="space-y-4">
                <input
                  type="text"
                  placeholder="Full name"
                  value={formData.fullName}
                  onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                  className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 dark:border-gray-600 dark:bg-gray-700"
                  required
                />
                <input
                  type="email"
                  placeholder="Email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 dark:border-gray-600 dark:bg-gray-700"
                  required
                />
                <input
                  type="tel"
                  placeholder="Phone number"
                  value={formData.phoneNumber}
                  onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                  className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 dark:border-gray-600 dark:bg-gray-700"
                />
                <select
                  value={formData.branchId}
                  onChange={(e) => setFormData({ ...formData, branchId: e.target.value })}
                  className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 dark:border-gray-600 dark:bg-gray-700"
                  required
                >
                  <option value="">Select branch</option>
                  {branches.map((branch) => (
                    <option key={branch.id} value={branch.id}>
                      {branch.name}
                    </option>
                  ))}
                </select>
                <input
                  type="password"
                  placeholder="Password, e.g. Staff123"
                  value={formData.password}
                  onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                  className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 dark:border-gray-600 dark:bg-gray-700"
                  required
                />
                <p className="text-xs text-gray-500 dark:text-gray-400">
                  Password must have at least 6 characters, uppercase, lowercase, and a number.
                </p>

                <div className="flex gap-3">
                  <button type="submit" className="flex-1 rounded-lg bg-blue-500 px-4 py-2 font-medium text-white hover:bg-blue-600">
                    Save
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowForm(false)}
                    className="flex-1 rounded-lg bg-gray-200 px-4 py-2 font-medium text-gray-700 dark:bg-gray-600 dark:text-gray-300"
                  >
                    Cancel
                  </button>
                </div>
              </form>
            </ComponentCard>
          </div>
        )}

        <ComponentCard title="Staff Accounts">
          {isLoading ? (
            <div className="py-8 text-center text-gray-600 dark:text-gray-400">Loading staff...</div>
          ) : staff.length === 0 ? (
            <div className="py-8 text-center text-gray-600 dark:text-gray-400">No staff found</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b border-gray-200 dark:border-gray-700">
                  <tr>
                    <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Name</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Email</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Phone</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-800 dark:text-white">Branch</th>
                    <th className="px-4 py-3 text-right font-semibold text-gray-800 dark:text-white">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {staff.map((item) => (
                    <tr key={item.id} className="border-b border-gray-100 hover:bg-gray-50 dark:border-gray-700 dark:hover:bg-gray-800">
                      <td className="px-4 py-3 text-gray-900 dark:text-white">{item.fullName || "N/A"}</td>
                      <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{item.email}</td>
                      <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{item.phoneNumber || "N/A"}</td>
                      <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{item.branch?.name || "N/A"}</td>
                      <td className="px-4 py-3 text-right">
                        <button onClick={() => handleDelete(item.id)} className="text-sm font-medium text-red-500 hover:text-red-600">
                          Delete
                        </button>
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

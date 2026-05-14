import { useEffect, useState } from "react";
import PageMeta from "../../components/common/PageMeta";
import ComponentCard from "../../components/common/ComponentCard";
import { BranchInfo, CreateUserDto, Customer, customerService } from "../../services/customerService";

export default function UsersManagement() {
  const [users, setUsers] = useState<Customer[]>([]);
  const [branches, setBranches] = useState<BranchInfo[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState<CreateUserDto>({
    fullName: "",
    email: "",
    password: "",
    phoneNumber: "",
    role: "Customer",
    branchId: "",
  });

  const load = async () => {
    setLoading(true);
    try {
      const [userData, branchData] = await Promise.all([
        customerService.getAll(),
        customerService.getBranches(),
      ]);
      setUsers(userData);
      setBranches(branchData);
      setError("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được danh sách người dùng");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    try {
      await customerService.createUser(form);
      setForm({ fullName: "", email: "", password: "", phoneNumber: "", role: "Customer", branchId: "" });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tạo được người dùng");
    }
  };

  const remove = async (id: string) => {
    if (!confirm("Xóa người dùng này?")) return;
    try {
      await customerService.delete(id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không xóa được người dùng");
    }
  };

  return (
    <>
      <PageMeta title="Quản lý người dùng | Smart Parking" description="Quản lý toàn bộ người dùng" />
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Quản lý người dùng</h1>
          <p className="text-gray-600 dark:text-gray-400">Xem, thêm và xóa người dùng theo vai trò.</p>
        </div>

        {error && <div className="rounded-lg bg-red-50 p-3 text-sm text-red-600 dark:bg-red-900/10 dark:text-red-300">{error}</div>}

        <ComponentCard title="Tạo người dùng">
          <form onSubmit={submit} className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
            <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Họ tên" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} required />
            <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Email" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
            <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Mật khẩu" type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} required />
            <input className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" placeholder="Số điện thoại" value={form.phoneNumber || ""} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} />
            <select className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as CreateUserDto["role"] })}>
              <option value="Customer">Khách hàng</option>
              <option value="Staff">Nhân viên</option>
              <option value="Manager">Quản lý</option>
            </select>
            {(form.role === "Staff" || form.role === "Manager") && (
              <select className="rounded-lg border px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-800 dark:text-white" value={form.branchId || ""} onChange={(e) => setForm({ ...form, branchId: e.target.value })} required>
                <option value="">Chọn chi nhánh</option>
                {branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}
              </select>
            )}
            <button className="rounded-lg bg-blue-500 px-4 py-2 text-sm font-medium text-white hover:bg-blue-600">Thêm người dùng</button>
          </form>
        </ComponentCard>

        <ComponentCard title="Tất cả người dùng">
          {loading ? <p className="text-sm text-gray-500">Đang tải...</p> : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b dark:border-gray-700">
                  <tr>
                    <th className="px-4 py-3 text-left">Tên</th>
                    <th className="px-4 py-3 text-left">Email</th>
                    <th className="px-4 py-3 text-left">Điện thoại</th>
                    <th className="px-4 py-3 text-left">Vai trò</th>
                    <th className="px-4 py-3 text-left">Chi nhánh</th>
                    <th className="px-4 py-3 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((item) => (
                    <tr key={item.id} className="border-b text-sm dark:border-gray-800">
                      <td className="px-4 py-3 text-gray-900 dark:text-white">{item.fullName || item.userName || "-"}</td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{item.email}</td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{item.phoneNumber || "-"}</td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{item.roles?.join(", ") || "-"}</td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{item.branch?.name || "-"}</td>
                      <td className="px-4 py-3 text-right">
                        <button onClick={() => remove(item.id)} className="text-sm font-medium text-red-500 hover:text-red-600">Xóa</button>
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

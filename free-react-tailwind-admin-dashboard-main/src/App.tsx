import { BrowserRouter as Router, Routes, Route } from "react-router";
import SignIn from "./pages/AuthPages/SignIn";
import SignUp from "./pages/AuthPages/SignUp";
import UserProfiles from "./pages/UserProfiles";
import AppLayout from "./layout/AppLayout";
import { ScrollToTop } from "./components/common/ScrollToTop";
import Home from "./pages/Dashboard/Home";
import { AuthProvider } from "./context/AuthContext";
import { ProtectedRoute } from "./components/auth/ProtectedRoute";

// Parking Pages
import ParkingCheckIn from "./pages/Parking/CheckIn";
import ParkingCheckOut from "./pages/Parking/CheckOut";

// Admin Pages
import StaffManagement from "./pages/Admin/CustomersManagement";
import MonthlyPasses from "./pages/Admin/MonthlyPasses";

export default function App() {
  return (
    <AuthProvider>
      <Router>
        <ScrollToTop />
        <Routes>
          {/* Dashboard Layout */}
          <Route
            element={
              <ProtectedRoute
                element={<AppLayout />}
              />
            }
          >
            <Route index path="/" element={<Home />} />

            {/* User Profile */}
            <Route path="/profile" element={<UserProfiles />} />

            {/* Parking Routes */}
            <Route
              path="/parking/check-in"
              element={<ProtectedRoute element={<ParkingCheckIn />} requiredRole={["Staff", "Admin"]} />}
            />
            <Route
              path="/parking/check-out"
              element={<ProtectedRoute element={<ParkingCheckOut />} requiredRole={["Staff", "Admin"]} />}
            />

            {/* Admin Routes */}
            <Route
              path="/admin/staff"
              element={<ProtectedRoute element={<StaffManagement />} requiredRole="Admin" />}
            />
            <Route
              path="/admin/monthly-passes"
              element={<ProtectedRoute element={<MonthlyPasses />} requiredRole="Admin" />}
            />
          </Route>

          {/* Auth Layout */}
          <Route path="/signin" element={<SignIn />} />
          <Route path="/signup" element={<SignUp />} />

          {/* Fallback Route */}
          <Route path="*" element={<SignIn />} />
        </Routes>
      </Router>
    </AuthProvider>
  );
}

import { Navigate } from "react-router";
import { useAuth } from "../../context/AuthContext";

interface ProtectedRouteProps {
  element: React.ReactNode;
  requiredRole?: string | string[];
}

export const ProtectedRoute = ({ element, requiredRole }: ProtectedRouteProps) => {
  const { isAuthenticated, user, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <div className="inline-block">
            <div className="w-12 h-12 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          </div>
          <p className="mt-4 text-gray-600 dark:text-gray-400">Loading...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/signin" replace />;
  }

  if (requiredRole) {
    const allowedRoles = Array.isArray(requiredRole) ? requiredRole : [requiredRole];
    const userRoles = user?.roles || user?.Roles || (user?.role ? [user.role] : []);
    if (!allowedRoles.some((role) => userRoles.includes(role))) {
      return <Navigate to="/" replace />;
    }
  }

  return <>{element}</>;
};

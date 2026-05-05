import apiCall from "./api";

export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterDto {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  user: {
    id: string;
    email: string;
    fullName?: string;
    firstName?: string;
    lastName?: string;
    role?: string;
    roles?: string[];
    Roles?: string[];
  };
}

const normalizeAuthResponse = (response: AuthResponse): AuthResponse => {
  const roles = response.user.roles || response.user.Roles || (response.user.role ? [response.user.role] : []);
  const primaryRole = roles[0] || response.user.role || "";

  return {
    ...response,
    user: {
      ...response.user,
      role: primaryRole,
      roles,
    },
  };
};

export const authService = {
  login: async (credentials: LoginDto): Promise<AuthResponse> => {
    const response = await apiCall<AuthResponse>("/auth/login", {
      method: "POST",
      body: credentials,
    });

    return normalizeAuthResponse(response);
  },

  register: async (data: RegisterDto): Promise<AuthResponse> => {
    const response = await apiCall<AuthResponse>("/auth/register", {
      method: "POST",
      body: data,
    });

    return normalizeAuthResponse(response);
  },

  refreshToken: async (refreshToken: string): Promise<AuthResponse> => {
    return apiCall<AuthResponse>("/auth/refresh-token", {
      method: "POST",
      body: { refreshToken },
    });
  },

  logout: () => {
    localStorage.removeItem("authToken");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("user");
  },

  saveAuthData: (response: AuthResponse) => {
    const normalized = normalizeAuthResponse(response);

    localStorage.setItem("authToken", normalized.token);
    localStorage.setItem("refreshToken", normalized.refreshToken);
    localStorage.setItem("user", JSON.stringify(normalized.user));
  },

  getCurrentUser: () => {
    const user = localStorage.getItem("user");
    if (!user) return null;

    const parsedUser = JSON.parse(user);
    const roles = parsedUser.roles || parsedUser.Roles || (parsedUser.role ? [parsedUser.role] : []);

    return {
      ...parsedUser,
      role: roles[0] || parsedUser.role || "",
      roles,
    };
  },
};

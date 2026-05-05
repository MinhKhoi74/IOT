import apiCall from "./api";

export interface BranchInfo {
  id: string;
  name: string;
  address?: string;
}

export interface ProfileVehicle {
  id: string;
  licensePlate: string;
  vehicleType: string | number;
  brand: string;
  color: string;
  isDefault: boolean;
}

export interface ProfileHistory {
  id: number;
  licensePlate: string;
  checkInTime: string;
  checkOutTime?: string;
  durationMinutes?: number;
  feeAmount?: number;
  paymentStatus?: string;
  paymentMethod?: string;
  status: string;
}

export interface Customer {
  id: string;
  userName?: string;
  email: string;
  fullName?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  roles?: string[];
  isActive?: boolean;
  branch?: BranchInfo;
  wallet?: {
    id?: string;
    balance: number;
    createdAt?: string;
    updatedAt?: string;
  };
  vehicles?: ProfileVehicle[];
  monthlyPasses?: any[];
  recentParkingHistory?: ProfileHistory[];
  registeredVehicles?: string[];
  createdAt?: string;
}

export interface CreateStaffDto {
  email: string;
  password: string;
  fullName: string;
  phoneNumber?: string;
  branchId: string;
}

export const customerService = {
  getProfile: async (): Promise<Customer> => {
    return apiCall<Customer>("/users/profile", { method: "GET" });
  },

  updateProfile: async (data: { fullName: string; phoneNumber?: string }): Promise<void> => {
    return apiCall<void>("/users/profile", {
      method: "PUT",
      body: data,
    });
  },

  getAll: async (): Promise<Customer[]> => {
    return apiCall<Customer[]>("/users/staff-list", { method: "GET" });
  },

  getBranches: async (): Promise<BranchInfo[]> => {
    return apiCall<BranchInfo[]>("/branches", { method: "GET" });
  },

  create: async (data: CreateStaffDto): Promise<{ message: string; staffId: string }> => {
    return apiCall<{ message: string; staffId: string }>("/users/create-staff", {
      method: "POST",
      body: data,
    });
  },

  delete: async (id: string): Promise<void> => {
    return apiCall<void>(`/users/staff/${id}`, { method: "DELETE" });
  },
};

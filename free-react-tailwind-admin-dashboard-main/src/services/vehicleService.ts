import apiCall from "./api";

export interface Vehicle {
  id: string;
  licensePlate: string;
  userId?: string | null;
  ownerName?: string | null;
  ownerEmail?: string | null;
  vehicleType: number | string;
  brand: string;
  color: string;
  isDefault: boolean;
}

export interface CreateVehicleDto {
  licensePlate: string;
  userId?: string;
  vehicleType: number;
  brand: string;
  color: string;
  isDefault?: boolean;
}

export interface UpdateVehicleDto {
  brand: string;
  color: string;
  isDefault: boolean;
}

export const vehicleService = {
  getMine: async (): Promise<Vehicle[]> => {
    return apiCall<Vehicle[]>("/vehicles", { method: "GET" });
  },

  getAll: async (): Promise<Vehicle[]> => {
    return apiCall<Vehicle[]>("/vehicles/admin", { method: "GET" });
  },

  create: async (data: CreateVehicleDto): Promise<void> => {
    return apiCall<void>("/vehicles", { method: "POST", body: data });
  },

  createByAdmin: async (data: CreateVehicleDto): Promise<void> => {
    return apiCall<void>("/vehicles/admin", { method: "POST", body: data });
  },

  update: async (id: string, data: UpdateVehicleDto): Promise<void> => {
    return apiCall<void>(`/vehicles/${id}`, { method: "PUT", body: data });
  },

  delete: async (id: string): Promise<void> => {
    return apiCall<void>(`/vehicles/${id}`, { method: "DELETE" });
  },

  deleteByAdmin: async (id: string): Promise<void> => {
    return apiCall<void>(`/vehicles/admin/${id}`, { method: "DELETE" });
  },
};

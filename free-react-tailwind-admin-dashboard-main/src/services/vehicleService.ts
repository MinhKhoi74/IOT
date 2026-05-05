import apiCall from "./api";

export interface Vehicle {
  id: string;
  plateNumber: string;
  brand: string;
  model: string;
  color: string;
  ownerId: string;
  ownerName: string;
  registrationDate: string;
  status: string;
}

export interface CreateVehicleDto {
  plateNumber: string;
  brand: string;
  model: string;
  color: string;
  ownerId: string;
}

export interface UpdateVehicleDto {
  brand?: string;
  model?: string;
  color?: string;
  status?: string;
}

export const vehicleService = {
  // Get all vehicles
  getAll: async (page?: number, pageSize?: number): Promise<any> => {
    const query = new URLSearchParams();
    if (page) query.append("page", page.toString());
    if (pageSize) query.append("pageSize", pageSize.toString());

    return apiCall<any>(`/vehicles?${query.toString()}`, {
      method: "GET",
    });
  },

  // Get vehicle by plate number
  getByPlateNumber: async (plateNumber: string): Promise<Vehicle> => {
    return apiCall<Vehicle>(`/vehicles/plate/${plateNumber}`, {
      method: "GET",
    });
  },

  // Get vehicle by ID
  getById: async (id: string): Promise<Vehicle> => {
    return apiCall<Vehicle>(`/vehicles/${id}`, {
      method: "GET",
    });
  },

  // Create new vehicle
  create: async (data: CreateVehicleDto): Promise<Vehicle> => {
    return apiCall<Vehicle>("/vehicles", {
      method: "POST",
      body: data,
    });
  },

  // Update vehicle
  update: async (id: string, data: UpdateVehicleDto): Promise<Vehicle> => {
    return apiCall<Vehicle>(`/vehicles/${id}`, {
      method: "PUT",
      body: data,
    });
  },

  // Delete vehicle
  delete: async (id: string): Promise<void> => {
    return apiCall<void>(`/vehicles/${id}`, {
      method: "DELETE",
    });
  },

  // Get vehicle parking history
  getParkingHistory: async (plateNumber: string): Promise<any[]> => {
    return apiCall<any[]>(`/vehicles/${plateNumber}/parking-history`, {
      method: "GET",
    });
  },
};

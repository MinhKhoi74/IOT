import apiCall from "./api";

export interface SlotDetail {
  id: string;
  slotCode: string;
  status: string;
}

export interface ZoneDetail {
  id: string;
  name: string;
  vehicleType: string;
  slots: SlotDetail[];
}

export interface ParkingLotDetail {
  id: string;
  name: string;
  zones: ZoneDetail[];
}

export interface Branch {
  id: string;
  name: string;
  address: string;
  maxVehicleCapacity: number;
}

export interface BranchFull extends Branch {
  parkingLots: ParkingLotDetail[];
}

export interface ParkingMapElement {
  id: string;
  type: string;
  label: string;
  sourceId?: string;
  sourceType?: string;
  parentId?: string;
  x: number;
  y: number;
  width: number;
  height: number;
  color: string;
}

export interface ParkingMap {
  branchId: string;
  width: number;
  height: number;
  elements: ParkingMapElement[];
  updatedAt?: string;
}

export const parkingStructureService = {
  branches: async (): Promise<Branch[]> => apiCall<Branch[]>("/branches", { method: "GET" }),
  branchFull: async (id: string): Promise<BranchFull> => apiCall<BranchFull>(`/branches/${id}/full`, { method: "GET" }),
  parkingMap: async (branchId: string): Promise<ParkingMap> => apiCall<ParkingMap>(`/parking-map/branches/${branchId}`, { method: "GET" }),
  saveParkingMap: async (branchId: string, body: ParkingMap): Promise<ParkingMap> => apiCall<ParkingMap>(`/parking-map/branches/${branchId}`, { method: "PUT", body }),
  createBranch: async (body: { name: string; address: string; maxVehicleCapacity: number }): Promise<void> => apiCall<void>("/branches", { method: "POST", body }),
  updateBranch: async (id: string, body: { id: string; name: string; address: string; maxVehicleCapacity: number }): Promise<void> => apiCall<void>(`/branches/${id}`, { method: "PUT", body }),
  deleteBranch: async (id: string): Promise<void> => apiCall<void>(`/branches/${id}`, { method: "DELETE" }),
  createParkingLot: async (body: { name: string; branchId: string }): Promise<void> => apiCall<void>("/parkinglots", { method: "POST", body }),
  deleteParkingLot: async (id: string): Promise<void> => apiCall<void>(`/parkinglots/${id}`, { method: "DELETE" }),
  createZone: async (body: { name: string; vehicleType: string; parkingLotId: string }): Promise<void> => apiCall<void>("/zones", { method: "POST", body }),
  deleteZone: async (id: string): Promise<void> => apiCall<void>(`/zones/${id}`, { method: "DELETE" }),
  createSlot: async (body: { slotCode: string; zoneId: string }): Promise<void> => apiCall<void>("/slots", { method: "POST", body }),
  deleteSlot: async (id: string): Promise<void> => apiCall<void>(`/slots/${id}`, { method: "DELETE" }),
};

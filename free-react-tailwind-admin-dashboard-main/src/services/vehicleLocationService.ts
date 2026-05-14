import apiCall from "./api";

export interface VehicleLocationAlert {
  id: number;
  licensePlate: string;
  vehicleId?: string | null;
  userId?: string | null;
  ownerName?: string | null;
  checkInOutId?: number | null;
  cameraId: string;
  parkingLotCode?: string | null;
  zoneCode?: string | null;
  columnCode?: string | null;
  locationName: string;
  confidence: number;
  imageBase64?: string | null;
  fullFrameImageBase64?: string | null;
  detectedAt: string;
  createdAt: string;
  isLatest: boolean;
  status: string;
  severity: string;
  message: string;
}

export const vehicleLocationService = {
  alerts: async (take = 20): Promise<VehicleLocationAlert[]> => {
    return apiCall<VehicleLocationAlert[]>(`/parking/vehicle-location-alerts?take=${take}`, {
      method: "GET",
    });
  },

  alertDetail: async (id: number): Promise<VehicleLocationAlert> => {
    return apiCall<VehicleLocationAlert>(`/parking/vehicle-location-alerts/${id}`, {
      method: "GET",
    });
  },
};

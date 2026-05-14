import apiCall from "./api";

export interface CheckInRequest {
  plateNumber: string;
  stationId: string;
  confidence: number;
  imageBase64?: string;
}

export interface CheckInOutResult {
  success: boolean;
  errorCode?: string;
  message: string;
  checkOutId?: number;
  checkOutTime?: string;
  durationMinutes?: number;
  feeAmount?: number;
  paymentStatus?: string;
  paymentMethod?: string;
  requiresPaymentAction?: boolean;
  data?: any;
}

export interface Vehicle {
  id: string;
  plateNumber: string;
  ownerName: string;
  ownerPhone: string;
  checkedInAt?: string;
  parkingSlot?: string;
}

export interface ParkingHistory {
  id: string | number;
  plateNumber: string;
  checkInTime: string;
  checkOutTime?: string;
  checkInImageBase64?: string;
  checkOutImageBase64?: string;
  duration?: string;
  durationMinutes?: number;
  fee?: number;
  feeAmount?: number;
  feeStatus?: string;
  paymentStatus?: string;
  paymentMethod?: string;
  status: string;
}

export interface ParkingDashboard {
  activeVehicleCount: number;
  totalRevenue: number;
  sessions: ParkingHistory[];
}

const mapParkingHistory = (item: any): ParkingHistory => ({
  id: item.id,
  plateNumber: item.licensePlate || item.plateNumber,
  checkInTime: item.checkInTime,
  checkOutTime: item.checkOutTime,
  checkInImageBase64: item.checkInImageBase64,
  checkOutImageBase64: item.checkOutImageBase64,
  durationMinutes: item.durationMinutes,
  feeAmount: item.feeAmount,
  feeStatus: item.feeStatus,
  paymentStatus: item.paymentStatus,
  paymentMethod: item.paymentMethod,
  status: item.status || "unknown",
  fee: item.feeAmount,
  duration: item.durationMinutes
    ? `${Math.floor(item.durationMinutes / 60)}h ${item.durationMinutes % 60}m`
    : undefined,
});

export const formatCheckoutPaymentMessage = (
  plateNumber: string,
  paymentStatus?: string,
  feeAmount = 0
) => {
  const plate = plateNumber.toUpperCase().trim();

  if (paymentStatus?.toLowerCase() === "paid") {
    return `Xe ${plate} đã thanh toán`;
  }

  return `Xe ${plate} chưa thanh toán. Phí cần thu: ${feeAmount.toLocaleString("vi-VN")}đ`;
};

export const parkingService = {
  checkIn: async (request: CheckInRequest): Promise<CheckInOutResult> => {
    return apiCall<CheckInOutResult>("/parking/check-in", {
      method: "POST",
      body: request,
    });
  },

  checkOut: async (
    plateNumber: string,
    stationId: string,
    imageBase64?: string
  ): Promise<CheckInOutResult> => {
    return apiCall<CheckInOutResult>("/parking/check-out", {
      method: "POST",
      body: { plateNumber, stationId, imageBase64 },
    });
  },

  getParkingHistory: async (filters?: {
    plateNumber?: string;
    startDate?: string;
    endDate?: string;
  }): Promise<ParkingHistory[]> => {
    const query = new URLSearchParams();
    if (filters?.plateNumber) query.append("plateNumber", filters.plateNumber);
    if (filters?.startDate) query.append("startDate", filters.startDate);
    if (filters?.endDate) query.append("endDate", filters.endDate);

    const data = await apiCall<any[]>(
      `/parking/history?${query.toString()}`,
      { method: "GET" }
    );

    return Array.isArray(data) ? data.map(mapParkingHistory) : [];
  },

  getHistoryByPlate: async (plateNumber: string): Promise<ParkingHistory[]> => {
    const data = await apiCall<any[]>(
      `/parking/history/${encodeURIComponent(plateNumber)}`,
      { method: "GET" }
    );

    return Array.isArray(data) ? data.map(mapParkingHistory) : [];
  },

  confirmCheckOutPayment: async (
    checkOutId: number,
    paymentMethod = "Cash"
  ): Promise<CheckInOutResult> => {
    return apiCall<CheckInOutResult>(`/parking/check-out/${checkOutId}/confirm-payment`, {
      method: "POST",
      body: { paymentMethod },
    });
  },

  getLatestCheckIn: async (): Promise<ParkingHistory> => {
    const item = await apiCall<any>("/parking/latest-check-in", {
      method: "GET",
    });

    return mapParkingHistory(item);
  },

  getLatestCheckOut: async (): Promise<ParkingHistory> => {
    const item = await apiCall<any>("/parking/latest-check-out", {
      method: "GET",
    });

    return mapParkingHistory(item);
  },

  getDashboard: async (): Promise<ParkingDashboard> => {
    const data = await apiCall<any>("/parking/dashboard", { method: "GET" });

    return {
      activeVehicleCount: data.activeVehicleCount || 0,
      totalRevenue: data.totalRevenue || 0,
      sessions: Array.isArray(data.sessions) ? data.sessions.map(mapParkingHistory) : [],
    };
  },

  getSessionDetail: async (id: string | number): Promise<ParkingHistory> => {
    const item = await apiCall<any>(`/parking/history/${id}/detail`, { method: "GET" });
    return mapParkingHistory(item);
  },

  getParkedVehicles: async (): Promise<Vehicle[]> => {
    return apiCall<Vehicle[]>("/parking/parked-vehicles", {
      method: "GET",
    });
  },

  getVehicleDetails: async (plateNumber: string): Promise<Vehicle> => {
    return apiCall<Vehicle>(`/parking/vehicle/${plateNumber}`, {
      method: "GET",
    });
  },
};

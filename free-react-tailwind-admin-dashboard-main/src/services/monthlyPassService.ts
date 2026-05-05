import apiCall from "./api";

export interface MonthlyPass {
  id: number;
  licensePlate: string;
  ownerName: string;
  ownerPhone?: string;
  validFrom: string;
  validTo: string;
  isActive: boolean;
}

export interface MonthlyPassUpsertRequest {
  licensePlate: string;
  ownerName: string;
  ownerPhone?: string;
  validFrom: string;
  validTo: string;
  isActive: boolean;
}

export const monthlyPassService = {
  getAll: async (): Promise<MonthlyPass[]> => {
    return apiCall<MonthlyPass[]>("/monthly-passes", { method: "GET" });
  },

  upsert: async (request: MonthlyPassUpsertRequest): Promise<MonthlyPass> => {
    return apiCall<MonthlyPass>("/monthly-passes", {
      method: "POST",
      body: request,
    });
  },

  delete: async (id: number): Promise<void> => {
    await apiCall<void>(`/monthly-passes/${id}`, { method: "DELETE" });
  },
};

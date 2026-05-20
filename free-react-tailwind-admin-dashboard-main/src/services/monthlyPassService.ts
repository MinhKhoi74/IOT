import apiCall from "./api";

export interface MonthlyPass {
  id: number;
  licensePlate: string;
  ownerName: string;
  ownerPhone?: string;
  validFrom: string;
  validTo: string;
  amount: number;
  isActive: boolean;
}

export interface MonthlyPassUpsertRequest {
  licensePlate: string;
  ownerName: string;
  ownerPhone?: string;
  validFrom: string;
  validTo: string;
  amount: number;
  isActive: boolean;
}

export const monthlyPassService = {
  getAll: async (): Promise<MonthlyPass[]> => {
    return apiCall<MonthlyPass[]>("/monthly-passes", { method: "GET" });
  },

  getPrice: async (): Promise<number> => {
    const data = await apiCall<{ monthlyAmount: number }>("/monthly-passes/price", { method: "GET" });
    return data.monthlyAmount || 0;
  },

  updatePrice: async (monthlyAmount: number): Promise<number> => {
    const data = await apiCall<{ monthlyAmount: number }>("/monthly-passes/price", {
      method: "PUT",
      body: { monthlyAmount },
    });
    return data.monthlyAmount || 0;
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

import apiCall from "./api";

export interface CameraStatus {
  isRunning: boolean;
  processId?: number | null;
  streamUrl: string;
  detectionUrl: string;
  healthUrl: string;
  message: string;
}

export interface StartCameraRequest {
  cameraIp: string;
  cameraPort: number;
  apiHost: string;
  apiPort: number;
  stationMode: "entrance" | "exit";
}

export const cameraService = {
  start: async (request: StartCameraRequest): Promise<CameraStatus> => {
    return apiCall<CameraStatus>("/camera/start", {
      method: "POST",
      body: request,
    });
  },

  stop: async (): Promise<CameraStatus> => {
    return apiCall<CameraStatus>("/camera/stop", {
      method: "POST",
      body: {},
    });
  },

  status: async (): Promise<CameraStatus> => {
    return apiCall<CameraStatus>("/camera/status", {
      method: "GET",
    });
  },
};

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
  branchId?: string;
}

export interface StartZoneCameraRequest {
  cameraIp: string;
  cameraPort: number;
  apiHost: string;
  apiPort: number;
  cameraId: string;
  locationName: string;
  parkingLotCode?: string;
  zoneCode?: string;
  columnCode?: string;
  branchId?: string;
}

export interface StopZoneCameraRequest {
  cameraId: string;
  apiPort: number;
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

  startZone: async (request: StartZoneCameraRequest): Promise<CameraStatus> => {
    return apiCall<CameraStatus>("/camera/zone/start", {
      method: "POST",
      body: request,
    });
  },

  stopZone: async (request: StopZoneCameraRequest): Promise<CameraStatus> => {
    return apiCall<CameraStatus>("/camera/zone/stop", {
      method: "POST",
      body: request,
    });
  },
};

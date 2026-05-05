import * as signalR from "@microsoft/signalr";
import { API_ORIGIN } from "./api";

export const createParkingHubConnection = () =>
  new signalR.HubConnectionBuilder()
    .withUrl(`${API_ORIGIN}/parkingHub`, {
      accessTokenFactory: () => localStorage.getItem("authToken") || "",
    })
    .withAutomaticReconnect()
    .build();

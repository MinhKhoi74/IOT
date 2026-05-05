// API Configuration and base utilities
const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";
export const API_ORIGIN = API_BASE_URL.replace(/\/api\/?$/, "");

interface RequestOptions {
  method?: string;
  headers?: Record<string, string>;
  body?: any;
}

const getAuthToken = (): string | null => {
  return localStorage.getItem("authToken");
};

export const apiCall = async <T>(
  endpoint: string,
  options: RequestOptions = {}
): Promise<T> => {
  const url = `${API_BASE_URL}${endpoint}`;
  const token = getAuthToken();

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...options.headers,
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    ...options,
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    let message = `API Error: ${response.status} ${response.statusText}`;
    try {
      const error = await response.json();
      message = error.message || error.title || message;
    } catch {
      const text = await response.text().catch(() => "");
      if (text) message = text;
    }
    throw new Error(message);
  }

  const contentType = response.headers.get("content-type");
  if (contentType && contentType.includes("application/json")) {
    return response.json();
  }
  
  // Return empty object if response is empty
  return {} as T;
};

export default apiCall;

import axios from "axios";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080/api",
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("dourak_token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

/**
 * Endpoints where a 401 is an *expected answer*, not an expired session. Bouncing the browser
 * to /login on a failed sign-in was what wiped the login form and made bad credentials look
 * like a page reload (prompt02 §Login screen) — those responses must reach the caller instead.
 */
const AUTH_ENDPOINTS = ["/auth/login", "/auth/register"];

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const url: string = error.config?.url ?? "";
    const isAuthAttempt = AUTH_ENDPOINTS.some((path) => url.includes(path));

    if (error.response?.status === 401 && !isAuthAttempt) {
      localStorage.removeItem("dourak_token");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);

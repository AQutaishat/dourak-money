import { apiClient } from "./client";

export interface AuthResult {
  succeeded: boolean;
  userId?: string;
  token?: string;
  expiresAt?: string;
  errors: string[];
}

export const authApi = {
  register: (data: { name: string; email: string; password: string; preferredLanguage: string }) =>
    apiClient.post<AuthResult>("/auth/register", data).then((r) => r.data),
  login: (data: { email: string; password: string }) =>
    apiClient.post<AuthResult>("/auth/login", data).then((r) => r.data),
};

import { apiClient } from "./client";
import type { UserProfile, UserSearchResult } from "./types";

export interface AuthResult {
  succeeded: boolean;
  userId?: string;
  token?: string;
  expiresAt?: string;
  errors: string[];
}

export const authApi = {
  /** Phase 2 §7 — registration only asks for email + password. */
  register: (data: { email: string; password: string }) =>
    apiClient.post<AuthResult>("/auth/register", data).then((r) => r.data),
  login: (data: { email: string; password: string }) =>
    apiClient.post<AuthResult>("/auth/login", data).then((r) => r.data),

  profile: () => apiClient.get<UserProfile>("/auth/profile").then((r) => r.data),
  updateProfile: (data: { name?: string | null; phone?: string | null; preferredLanguage?: string | null }) =>
    apiClient.put("/auth/profile", data),

  /** "Resend verification email" action, e.g. from the unverified-email banner. */
  sendVerification: () => apiClient.post("/auth/send-verification"),
  verifyEmail: (data: { userId: string; token: string }) => apiClient.post("/auth/verify-email", data),
  /** Always resolves regardless of whether the email is registered — never reveals which emails exist. */
  forgotPassword: (data: { email: string }) => apiClient.post("/auth/forgot-password", data),
  resetPassword: (data: { userId: string; token: string; newPassword: string }) =>
    apiClient.post("/auth/reset-password", data),
};

export const usersApi = {
  /** Phase 2 §2 — autocomplete data source; matches name, email or phone. */
  search: (q: string) =>
    apiClient.get<UserSearchResult[]>("/users/search", { params: { q } }).then((r) => r.data),
};

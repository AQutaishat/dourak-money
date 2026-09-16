import { apiClient } from "./client";
import type { AuthResult, AdminStats, AdminUser } from "./types";

export const authApi = {
  login: (data: { email: string; password: string }) =>
    apiClient.post<AuthResult>("/auth/login", data).then((r) => r.data),
};

export const adminApi = {
  stats: () => apiClient.get<AdminStats>("/admin/stats").then((r) => r.data),
  users: () => apiClient.get<AdminUser[]>("/admin/users").then((r) => r.data),
  resetPassword: (userId: string, newPassword: string) =>
    apiClient.post(`/admin/users/${userId}/reset-password`, { newPassword }),
  deactivate: (userId: string) => apiClient.post(`/admin/users/${userId}/deactivate`),
  activate: (userId: string) => apiClient.post(`/admin/users/${userId}/activate`),
  deleteUser: (userId: string) => apiClient.delete(`/admin/users/${userId}`),
};

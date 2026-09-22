import { apiClient } from "./client";
import type { AuthResult, AdminStats, AdminUser, SupportRequest, PagedResult, AuditLog, AuditLogFilters, AppSetting } from "./types";

export const authApi = {
  login: (data: { email: string; password: string }) =>
    apiClient.post<AuthResult>("/auth/login", data).then((r) => r.data),
};

export const adminApi = {
  stats: () => apiClient.get<AdminStats>("/admin/stats").then((r) => r.data),
  users: (params: { page: number; pageSize: number }) =>
    apiClient.get<PagedResult<AdminUser>>("/admin/users", { params }).then((r) => r.data),
  resetPassword: (userId: string, newPassword: string) =>
    apiClient.post(`/admin/users/${userId}/reset-password`, { newPassword }),
  deactivate: (userId: string) => apiClient.post(`/admin/users/${userId}/deactivate`),
  activate: (userId: string) => apiClient.post(`/admin/users/${userId}/activate`),
  deleteUser: (userId: string) => apiClient.delete(`/admin/users/${userId}`),
  /** Dev-only (also 404s server-side outside Development). */
  createTestCircles: (userId: string) => apiClient.post(`/admin/users/${userId}/test-circles`),
  deleteUserCircles: (userId: string) => apiClient.delete(`/admin/users/${userId}/circles`),

  supportRequests: () => apiClient.get<SupportRequest[]>("/admin/support-requests").then((r) => r.data),
  supportRequestAttachmentUrl: async (id: number) => {
    const response = await apiClient.get(`/admin/support-requests/${id}/attachment`, { responseType: "blob" });
    return URL.createObjectURL(response.data as Blob);
  },

  auditLogs: (params: AuditLogFilters) =>
    apiClient.get<PagedResult<AuditLog>>("/admin/audit-logs", { params }).then((r) => r.data),
  auditLogActions: () => apiClient.get<string[]>("/admin/audit-log-actions").then((r) => r.data),

  settings: () => apiClient.get<AppSetting[]>("/admin/settings").then((r) => r.data),
  updateSettings: (values: Record<string, string | null>) => apiClient.put("/admin/settings", values),
};

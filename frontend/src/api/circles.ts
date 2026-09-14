import { apiClient } from "./client";
import type {
  CircleDetail, CircleHistoryCycle, CircleSummary, CurrentCycleDashboard,
  Member, MemberHistory, PayoutOrderEntry, ScheduleCycle,
} from "./types";

export interface CreateCirclePayload {
  name: string;
  description?: string;
  currency: string;
  contributionAmount: number;
  startDate: string; // ISO date
  organizerIsMember: boolean;
  organizerMemberName?: string;
}

export const circlesApi = {
  list: () => apiClient.get<CircleSummary[]>("/circles").then((r) => r.data),
  detail: (id: number) => apiClient.get<CircleDetail>(`/circles/${id}`).then((r) => r.data),
  create: (payload: CreateCirclePayload) => apiClient.post<number>("/circles", payload).then((r) => r.data),
  activate: (id: number) => apiClient.post(`/circles/${id}/activate`),
  pause: (id: number) => apiClient.post(`/circles/${id}/pause`),
  resume: (id: number) => apiClient.post(`/circles/${id}/resume`),
  cancel: (id: number) => apiClient.post(`/circles/${id}/cancel`),

  members: (id: number) => apiClient.get<Member[]>(`/circles/${id}/members`).then((r) => r.data),
  addMember: (id: number, data: { name: string; phone?: string; email?: string; notes?: string }) =>
    apiClient.post<number>(`/circles/${id}/members`, data).then((r) => r.data),
  updateMember: (id: number, memberId: number, data: { name: string; phone?: string; email?: string; notes?: string }) =>
    apiClient.put(`/circles/${id}/members/${memberId}`, data),
  deactivateMember: (id: number, memberId: number) =>
    apiClient.post(`/circles/${id}/members/${memberId}/deactivate`),
  replaceMember: (id: number, oldMemberId: number, newMemberId: number) =>
    apiClient.post(`/circles/${id}/members/replace`, { oldMemberId, newMemberId }),

  payoutOrder: (id: number) => apiClient.get<PayoutOrderEntry[]>(`/circles/${id}/payout-order`).then((r) => r.data),
  setManualOrder: (id: number, memberIdsInOrder: number[]) =>
    apiClient.put(`/circles/${id}/payout-order/manual`, { memberIdsInOrder }),
  runDraw: (id: number) => apiClient.post<{ memberId: number; position: number }[]>(`/circles/${id}/payout-order/draw`).then((r) => r.data),
  confirmOrder: (id: number) => apiClient.post(`/circles/${id}/payout-order/confirm`),
  resetOrder: (id: number) => apiClient.post(`/circles/${id}/payout-order/reset`),

  schedule: (id: number) => apiClient.get<ScheduleCycle[]>(`/circles/${id}/schedule`).then((r) => r.data),
  dashboard: (id: number) => apiClient.get<CurrentCycleDashboard | null>(`/circles/${id}/dashboard`).then((r) => r.data),
  history: (id: number) => apiClient.get<CircleHistoryCycle[]>(`/circles/${id}/history`).then((r) => r.data),
  memberHistory: (id: number, memberId: number) =>
    apiClient.get<MemberHistory>(`/circles/${id}/members/${memberId}/history`).then((r) => r.data),
};

export const cyclesApi = {
  recordContribution: (cycleId: number, data: { memberId: number; paidAmount: number; paidAt?: string; paymentMethod?: number; notes?: string }) =>
    apiClient.post(`/cycles/${cycleId}/contributions`, data),
  recordPayout: (cycleId: number, data: { actualAmount: number; paidAt?: string; paymentMethod?: number; notes?: string }) =>
    apiClient.post(`/cycles/${cycleId}/payout`, data),
  reopenPayout: (cycleId: number) => apiClient.post(`/cycles/${cycleId}/payout/reopen`),
};

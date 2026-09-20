import { apiClient } from "./client";
import type {
  CircleDetail, CircleHistoryCycle, CircleMonth, CircleSummary, CurrentCycleDashboard,
  Member, MemberHistory, PaymentClaim, PayoutOrderEntry, PendingInvitation, ScheduleCycle,
} from "./types";

export interface CreateCirclePayload {
  name: string;
  description?: string;
  currency: string;
  contributionAmount: number;
  startDate: string; // ISO date
}

export const circlesApi = {
  list: () => apiClient.get<CircleSummary[]>("/circles").then((r) => r.data),
  detail: (id: number) => apiClient.get<CircleDetail>(`/circles/${id}`).then((r) => r.data),
  create: (payload: CreateCirclePayload) => apiClient.post<number>("/circles", payload).then((r) => r.data),
  remove: (id: number) => apiClient.delete(`/circles/${id}`),
  /** prompt03 §1 — edit name/description/start date/contribution amount while still a draft. */
  updateBasicInfo: (id: number, data: { name: string; description?: string; startDate: string; contributionAmount: number }) =>
    apiClient.put(`/circles/${id}/basic-info`, data),
  activate: (id: number) => apiClient.post(`/circles/${id}/activate`),
  pause: (id: number) => apiClient.post(`/circles/${id}/pause`),
  resume: (id: number) => apiClient.post(`/circles/${id}/resume`),
  cancel: (id: number) => apiClient.post(`/circles/${id}/cancel`),

  members: (id: number) => apiClient.get<Member[]>(`/circles/${id}/members`).then((r) => r.data),
  addMember: (id: number, data: { name: string; phone?: string; email?: string; notes?: string }) =>
    apiClient.post<number>(`/circles/${id}/members`, data).then((r) => r.data),
  /** Phase 2 §2 — add an already-registered user; they must accept before they join. */
  addUserMember: (id: number, userId: string) =>
    apiClient.post<number>(`/circles/${id}/members/by-user`, { userId }).then((r) => r.data),
  /** Phase 2 — organizer's "add me as a member" shortcut. */
  addSelfAsMember: (id: number) => apiClient.post<number>(`/circles/${id}/members/self`).then((r) => r.data),
  /** Invite someone not yet on Dourak, by name, over a WhatsApp link carrying a one-time token. */
  inviteUnregisteredMember: (id: number, name: string) =>
    apiClient.post<{ memberId: number; token: string }>(`/circles/${id}/members/invite-unregistered`, { name }).then((r) => r.data),
  reinviteMember: (id: number, memberId: number) => apiClient.post(`/circles/${id}/members/${memberId}/reinvite`),
  updateMember: (id: number, memberId: number, data: { name: string; phone?: string; email?: string; notes?: string }) =>
    apiClient.put(`/circles/${id}/members/${memberId}`, data),
  deactivateMember: (id: number, memberId: number) =>
    apiClient.post(`/circles/${id}/members/${memberId}/deactivate`),
  /** prompt03 §1 — fully remove a member row, draft circles only. */
  removeMember: (id: number, memberId: number) =>
    apiClient.delete(`/circles/${id}/members/${memberId}`),
  replaceMember: (id: number, oldMemberId: number, newMemberId: number) =>
    apiClient.post(`/circles/${id}/members/replace`, { oldMemberId, newMemberId }),

  payoutOrder: (id: number) => apiClient.get<PayoutOrderEntry[]>(`/circles/${id}/payout-order`).then((r) => r.data),
  setManualOrder: (id: number, memberIdsInOrder: number[]) =>
    apiClient.put(`/circles/${id}/payout-order/manual`, { memberIdsInOrder }),
  /** Moves one member up (-1) or down (+1) one slot, persisted immediately. */
  movePayoutPosition: (id: number, memberId: number, direction: -1 | 1) =>
    apiClient.post(`/circles/${id}/payout-order/${memberId}/move`, null, { params: { direction } }),
  runDraw: (id: number) => apiClient.post<{ memberId: number; position: number }[]>(`/circles/${id}/payout-order/draw`).then((r) => r.data),
  resetOrder: (id: number) => apiClient.post(`/circles/${id}/payout-order/reset`),

  schedule: (id: number) => apiClient.get<ScheduleCycle[]>(`/circles/${id}/schedule`).then((r) => r.data),
  monthsDetail: (id: number) => apiClient.get<CircleMonth[]>(`/circles/${id}/months-detail`).then((r) => r.data),
  dashboard: (id: number) => apiClient.get<CurrentCycleDashboard | null>(`/circles/${id}/dashboard`).then((r) => r.data),
  history: (id: number) => apiClient.get<CircleHistoryCycle[]>(`/circles/${id}/history`).then((r) => r.data),
  memberHistory: (id: number, memberId: number) =>
    apiClient.get<MemberHistory>(`/circles/${id}/members/${memberId}/history`).then((r) => r.data),

  paymentClaims: (id: number, pendingOnly = false) =>
    apiClient.get<PaymentClaim[]>(`/circles/${id}/payment-claims`, { params: { pendingOnly } }).then((r) => r.data),
};

export const cyclesApi = {
  recordContribution: (cycleId: number, data: { memberId: number; paidAmount: number; paidAt?: string; paymentMethod?: number; notes?: string }) =>
    apiClient.post(`/cycles/${cycleId}/contributions`, data),
  /** Multipart so an evidence file (e.g. a transfer screenshot) can ride along, like a payment claim. */
  recordPayout: (cycleId: number, data: { actualAmount: number; paidAt?: string; paymentMethod?: number; notes?: string; evidence?: File | null }) => {
    const form = new FormData();
    form.append("actualAmount", String(data.actualAmount));
    if (data.paidAt) form.append("paidAt", data.paidAt);
    if (data.notes) form.append("notes", data.notes);
    if (data.evidence) form.append("evidence", data.evidence);
    return apiClient.post(`/cycles/${cycleId}/payout`, form);
  },
  reopenPayout: (cycleId: number) => apiClient.post(`/cycles/${cycleId}/payout/reopen`),
  payoutEvidenceUrl: async (payoutPaymentId: number) => {
    const response = await apiClient.get(`/cycles/payout-payments/${payoutPaymentId}/evidence`, { responseType: "blob" });
    return URL.createObjectURL(response.data as Blob);
  },
};

/** Phase 2 §4 — the invitee's own accept/decline surface. */
export const invitationsApi = {
  pending: () => apiClient.get<PendingInvitation[]>("/invitations/pending").then((r) => r.data),
  accept: (memberId: number) => apiClient.post(`/invitations/${memberId}/accept`),
  decline: (memberId: number) => apiClient.post(`/invitations/${memberId}/decline`),
  /** Links the signed-in user's account to a WhatsApp invite token they just opened. */
  linkToken: (token: string) => apiClient.post(`/invitations/link/${token}`),
};

/** Phase 2 §6 — member self-report + organizer review. */
export const paymentClaimsApi = {
  mine: () => apiClient.get<PaymentClaim[]>("/payment-claims/mine").then((r) => r.data),

  submit: (cycleId: number, data: { claimedAmount: number; note?: string; evidence?: File | null }) => {
    // Multipart so the evidence image/document rides along with the claim.
    const form = new FormData();
    form.append("claimedAmount", String(data.claimedAmount));
    if (data.note) form.append("note", data.note);
    if (data.evidence) form.append("evidence", data.evidence);
    return apiClient.post<number>(`/payment-claims/cycles/${cycleId}`, form).then((r) => r.data);
  },

  review: (claimId: number, approve: boolean, rejectionReason?: string) =>
    apiClient.post(`/payment-claims/${claimId}/review`, { approve, rejectionReason }),

  /** The submitting member can still correct amount/note/evidence while the claim is Pending. */
  update: (claimId: number, data: { claimedAmount: number; note?: string; removeEvidence?: boolean; evidence?: File | null }) => {
    const form = new FormData();
    form.append("claimedAmount", String(data.claimedAmount));
    if (data.note) form.append("note", data.note);
    form.append("removeEvidence", String(!!data.removeEvidence));
    if (data.evidence) form.append("evidence", data.evidence);
    return apiClient.put(`/payment-claims/${claimId}`, form);
  },

  /** The submitting member can withdraw ("unsend") a claim while it's still Pending. */
  withdraw: (claimId: number) => apiClient.delete(`/payment-claims/${claimId}`),

  /** Fetched as a blob because the endpoint requires the bearer token (privacy rule, §6). */
  evidenceUrl: async (claimId: number) => {
    const response = await apiClient.get(`/payment-claims/${claimId}/evidence`, { responseType: "blob" });
    return URL.createObjectURL(response.data as Blob);
  },
};

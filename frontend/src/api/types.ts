export type CircleStatus = "Draft" | "Active" | "Paused" | "Completed" | "Cancelled";
export type PayoutOrderMethod = "Manual" | "RandomDraw";
export type PaymentMethod = 0 | 1 | 2 | 3; // Cash, BankTransfer, DigitalWallet, Other

/** Phase 2 §5 — whether a member linked to a real account responded to their invitation. */
export type InvitationStatus = "NotInvited" | "Pending" | "Accepted" | "Declined";

/** Phase 2 §6 — lifecycle of a member's self-reported payment. */
export type PaymentClaimStatus = "Pending" | "Approved" | "Rejected";

export interface CircleSummary {
  id: number;
  name: string;
  currency: string;
  contributionAmount: number;
  frequency: string;
  startDate: string;
  status: CircleStatus;
  memberCount: number;
  organizerName: string;
  createdAt: string;
  isOrganizer: boolean;
}

export interface CircleDetail {
  id: number;
  name: string;
  description?: string | null;
  currency: string;
  contributionAmount: number;
  frequency: string;
  startDate: string;
  status: CircleStatus;
  payoutOrderMethod?: PayoutOrderMethod | null;
  payoutOrderConfirmed: boolean;
  memberCount: number;
  organizerName: string;
  createdAt: string;
  isOrganizer: boolean;
  totalMonthlyAmount: number;
  lastPaymentMonth?: string | null;
  canDelete: boolean;
  myMemberId?: number | null;
}

export interface Member {
  id: number;
  name: string;
  phone?: string | null;
  email?: string | null;
  notes?: string | null;
  isActive: boolean;
  payoutPosition?: number | null;
  invitationStatus: InvitationStatus;
  userId?: string | null;
  isParticipating: boolean;
}

export interface PayoutOrderEntry {
  position: number;
  memberId: number;
  memberName: string;
}

export interface ScheduleCycle {
  cycleId: number;
  sequenceNumber: number;
  dueDate: string;
  recipientMemberId: number;
  recipientName: string;
  expectedPoolAmount: number;
  status: "Pending" | "Completed";
  payoutStatus: "Pending" | "Paid";
}

export interface CurrentCycleMemberRow {
  memberId: number;
  memberName: string;
  expectedAmount: number;
  paidAmount: number;
  status: "Unpaid" | "PartiallyPaid" | "Paid" | "Late";
  paidAt?: string | null;
  /** Only ever populated for the organizer and for the member's own row (privacy rule, §6). */
  myClaimStatus?: PaymentClaimStatus | null;
  hasPendingClaim: boolean;
}

export interface CurrentCycleDashboard {
  cycleId: number;
  sequenceNumber: number;
  dueDate: string;
  membersTotal: number;
  membersPaid: number;
  membersUnpaid: number;
  membersLate: number;
  collected: number;
  expected: number;
  outstanding: number;
  recipientMemberId: number;
  recipientName: string;
  payoutStatus: "Pending" | "Paid";
  nextRecipientMemberId?: number | null;
  nextRecipientName?: string | null;
  members: CurrentCycleMemberRow[];
  pendingClaimCount: number;
}

export interface MemberHistoryEntry {
  cycleId: number;
  sequenceNumber: number;
  dueDate: string;
  expectedAmount: number;
  paidAmount: number;
  status: string;
  paidAt?: string | null;
  isRecipientThisCycle: boolean;
  payoutStatusIfRecipient?: string | null;
}

export interface MemberHistory {
  memberId: number;
  memberName: string;
  payoutPosition?: number | null;
  entries: MemberHistoryEntry[];
}

export interface CircleHistoryCycle {
  cycleId: number;
  sequenceNumber: number;
  dueDate: string;
  recipientName: string;
  expectedPool: number;
  collected: number;
  unpaidMembers: string[];
  lateMembers: string[];
  payoutStatus: "Pending" | "Paid";
  payoutPaidAt?: string | null;
}

// ---------- Phase 2 ----------

export interface UserProfile {
  userId: string;
  name?: string | null;
  email?: string | null;
  phone?: string | null;
  preferredLanguage: string;
  displayLabel: string;
}

export interface UserSearchResult {
  userId: string;
  name?: string | null;
  email?: string | null;
  phone?: string | null;
  displayLabel: string;
}

export interface PendingInvitation {
  circleId: number;
  memberId: number;
  circleName: string;
  description?: string | null;
  currency: string;
  contributionAmount: number;
  startDate: string;
  organizerName: string;
  memberCount: number;
  invitedAt?: string | null;
}

export interface PaymentClaim {
  id: number;
  circleId: number;
  circleName: string;
  cycleId: number;
  sequenceNumber: number;
  dueDate: string;
  memberId: number;
  memberName: string;
  claimedAmount: number;
  expectedAmount: number;
  currency: string;
  status: PaymentClaimStatus;
  note?: string | null;
  rejectionReason?: string | null;
  submittedAt: string;
  reviewedAt?: string | null;
  hasEvidence: boolean;
  evidenceFileName?: string | null;
  evidenceContentType?: string | null;
}

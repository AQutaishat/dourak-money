export type CircleStatus = "Draft" | "Active" | "Paused" | "Completed" | "Cancelled";
export type PayoutOrderMethod = "Manual" | "RandomDraw";
export type PaymentMethod = 0 | 1 | 2 | 3; // Cash, BankTransfer, DigitalWallet, Other

export interface CircleSummary {
  id: number;
  name: string;
  currency: string;
  contributionAmount: number;
  frequency: string;
  startDate: string;
  status: CircleStatus;
  memberCount: number;
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
}

export interface Member {
  id: number;
  name: string;
  phone?: string | null;
  email?: string | null;
  notes?: string | null;
  isActive: boolean;
  payoutPosition?: number | null;
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

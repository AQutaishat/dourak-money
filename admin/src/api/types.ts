export interface AuthResult {
  succeeded: boolean;
  userId?: string;
  token?: string;
  expiresAt?: string;
  errors: string[];
}

export interface AdminStats {
  totalUsers: number;
  verifiedUsers: number;
  activeUsers: number;
  totalCircles: number;
  draftCircles: number;
  activeCircles: number;
}

export interface AdminCircleSummary {
  circleId: number;
  name: string;
  status: string;
}

export interface SupportRequest {
  id: number;
  name?: string | null;
  email: string;
  message: string;
  createdAt: string;
  hasAttachment: boolean;
  attachmentOriginalFileName?: string | null;
}

export interface AdminUser {
  userId: string;
  name?: string | null;
  email?: string | null;
  phone?: string | null;
  emailConfirmed: boolean;
  isActive: boolean;
  organizedCircles: AdminCircleSummary[];
  memberCircles: AdminCircleSummary[];
  displayLabel: string;
  createdAt?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AuditLog {
  id: string;
  userId?: string | null;
  userDisplayName?: string | null;
  action: string;
  details?: string | null;
  createdAt: string;
  ipAddress?: string | null;
}

export interface AuditLogFilters {
  page: number;
  pageSize: number;
  userId?: string;
  dateFrom?: string;
  dateTo?: string;
  action?: string;
}

export interface AppSetting {
  key: string;
  value: string | null;
  updatedAt: string | null;
  updatedByUserId: string | null;
}

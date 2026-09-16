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
}

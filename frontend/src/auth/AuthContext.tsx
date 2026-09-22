import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from "react";
import axios from "axios";
import { authApi } from "../api/auth";
import type { UserProfile } from "../api/types";

interface AuthContextValue {
  isAuthenticated: boolean;
  /** Phase 2 §8 — the account menu shows the display name, falling back to the email. */
  profile: UserProfile | null;
  displayLabel: string;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  googleLogin: (idToken: string) => Promise<void>;
  logout: () => void;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

const TOKEN_KEY = "dourak_token";

/**
 * Turns an auth failure into a message the login/register screens can show inline, instead of
 * a generic "Authentication failed" (prompt02 §Login/§Register). A 401 from /auth/login is
 * always "invalid credentials"; everything else surfaces the server's own explanation, which
 * is how the register screen can say specifically that the email is already registered.
 */
export class AuthError extends Error {
  readonly kind: "invalid-credentials" | "server";

  constructor(message: string, kind: "invalid-credentials" | "server") {
    super(message);
    this.kind = kind;
  }
}

function toAuthError(err: unknown, fallback: string): AuthError {
  if (axios.isAxiosError(err)) {
    const errors: string[] | undefined = err.response?.data?.errors;
    if (errors?.length) {
      return new AuthError(errors.join(" "), err.response?.status === 401 ? "invalid-credentials" : "server");
    }
    if (err.response?.status === 401) return new AuthError(fallback, "invalid-credentials");
  }
  if (err instanceof AuthError) return err;
  return new AuthError(err instanceof Error ? err.message : fallback, "server");
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(() => !!localStorage.getItem(TOKEN_KEY));
  const [profile, setProfile] = useState<UserProfile | null>(null);

  const refreshProfile = useCallback(async () => {
    if (!localStorage.getItem(TOKEN_KEY)) {
      setProfile(null);
      return;
    }
    try {
      setProfile(await authApi.profile());
    } catch {
      // A missing profile must never block the app — the menu falls back to the email.
      setProfile(null);
    }
  }, []);

  useEffect(() => {
    if (isAuthenticated) void refreshProfile();
  }, [isAuthenticated, refreshProfile]);

  const applyResult = async (result: { succeeded: boolean; token?: string; errors: string[] }) => {
    if (!result.succeeded || !result.token) {
      throw new AuthError(result.errors.join(" ") || "Authentication failed.", "server");
    }
    localStorage.setItem(TOKEN_KEY, result.token);
    setIsAuthenticated(true);
    await refreshProfile();
  };

  const login = async (email: string, password: string) => {
    try {
      await applyResult(await authApi.login({ email, password }));
    } catch (err) {
      throw toAuthError(err, "Invalid credentials");
    }
  };

  const register = async (email: string, password: string) => {
    try {
      await applyResult(await authApi.register({ email, password }));
    } catch (err) {
      throw toAuthError(err, "Registration failed");
    }
  };

  const googleLogin = async (idToken: string) => {
    try {
      await applyResult(await authApi.googleLogin({ idToken }));
    } catch (err) {
      throw toAuthError(err, "Google sign-in failed");
    }
  };

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY);
    setProfile(null);
    setIsAuthenticated(false);
  };

  const displayLabel = profile?.displayLabel ?? profile?.name ?? profile?.email ?? "";

  return (
    <AuthContext.Provider value={{ isAuthenticated, profile, displayLabel, login, register, googleLogin, logout, refreshProfile }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}

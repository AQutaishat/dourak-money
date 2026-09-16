import { createContext, useContext, useState, type ReactNode } from "react";
import axios from "axios";
import { authApi } from "../api/admin";
import { adminTokenStorage } from "../api/client";
import { decodeJwtRoles } from "./jwt";

interface AuthContextValue {
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export class AuthError extends Error {}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(() => !!adminTokenStorage.get());

  const login = async (email: string, password: string) => {
    try {
      const result = await authApi.login({ email, password });
      if (!result.succeeded || !result.token) {
        throw new AuthError(result.errors.join(" ") || "Invalid credentials.");
      }
      // Client-side only, for immediate feedback — see jwt.ts. The server rejects any
      // /api/admin/* call from a non-Admin token regardless of this check.
      if (!decodeJwtRoles(result.token).includes("Admin")) {
        throw new AuthError("This account does not have admin access.");
      }
      adminTokenStorage.set(result.token);
      setIsAuthenticated(true);
    } catch (err) {
      if (err instanceof AuthError) throw err;
      if (axios.isAxiosError(err)) {
        const errors: string[] | undefined = err.response?.data?.errors;
        throw new AuthError(errors?.join(" ") || "Invalid credentials.");
      }
      throw new AuthError("Login failed.");
    }
  };

  const logout = () => {
    adminTokenStorage.clear();
    setIsAuthenticated(false);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}

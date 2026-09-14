import { createContext, useContext, useState, type ReactNode } from "react";
import { authApi } from "../api/auth";

interface AuthContextValue {
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (name: string, email: string, password: string, preferredLanguage: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

const TOKEN_KEY = "dourak_token";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(() => !!localStorage.getItem(TOKEN_KEY));

  const applyResult = (result: { succeeded: boolean; token?: string; errors: string[] }) => {
    if (!result.succeeded || !result.token) {
      throw new Error(result.errors.join(", ") || "Authentication failed.");
    }
    localStorage.setItem(TOKEN_KEY, result.token);
    setIsAuthenticated(true);
  };

  const login = async (email: string, password: string) => applyResult(await authApi.login({ email, password }));

  const register = async (name: string, email: string, password: string, preferredLanguage: string) =>
    applyResult(await authApi.register({ name, email, password, preferredLanguage }));

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY);
    setIsAuthenticated(false);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}

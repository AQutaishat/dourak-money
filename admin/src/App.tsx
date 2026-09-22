import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider, CssBaseline, createTheme } from "@mui/material";
import { AuthProvider, useAuth } from "./auth/AuthContext";
import { AdminLayout } from "./layouts/AdminLayout";
import { LoginPage } from "./pages/LoginPage";
import { DashboardPage } from "./pages/DashboardPage";
import { UsersPage } from "./pages/UsersPage";
import { SupportRequestsPage } from "./pages/SupportRequestsPage";

const queryClient = new QueryClient();
// Same brand color as the main app/mobile app for visual consistency, English-only UI (no
// RTL/i18n needed for an internal admin tool).
const theme = createTheme({ palette: { primary: { main: "#0F6B57" } } });

function ProtectedRoute({ children }: { children: React.ReactElement }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? children : <Navigate to="/login" replace />;
}

function Router() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute><AdminLayout /></ProtectedRoute>}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/users" element={<UsersPage />} />
          <Route path="/support-requests" element={<SupportRequestsPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <AuthProvider>
          <Router />
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

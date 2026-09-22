import { useEffect, useMemo } from "react";
import { useTranslation } from "react-i18next";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { CacheProvider } from "@emotion/react";
import { AuthProvider, useAuth } from "./auth/AuthContext";
import { queryClient } from "./app/queryClient";
import { createDourakTheme } from "./theme/theme";
import { createEmotionCache } from "./theme/emotionCache";
import { isRtl } from "./i18n";
import { AppLayout } from "./layouts/AppLayout";
import { LoginPage } from "./pages/Login/LoginPage";
import { ForgotPasswordPage } from "./pages/Login/ForgotPasswordPage";
import { ResetPasswordPage } from "./pages/Login/ResetPasswordPage";
import { VerifyEmailPage } from "./pages/Login/VerifyEmailPage";
import { RegisterPage } from "./pages/Register/RegisterPage";
import { DashboardPage } from "./pages/Dashboard/DashboardPage";
import { MyCirclesPage } from "./pages/MyCircles/MyCirclesPage";
import { CreateCirclePage } from "./pages/CreateCircle/CreateCirclePage";
import { CircleOverviewPage } from "./pages/CircleOverview/CircleOverviewPage";
import { MemberHistoryPage } from "./pages/CircleOverview/MemberHistoryPage";
import { ProfilePage } from "./pages/Profile/ProfilePage";
import { DeleteAccountPage } from "./pages/Legal/DeleteAccountPage";
import { SupportPage } from "./pages/Legal/SupportPage";
import { InvitePage } from "./pages/Invite/InvitePage";

function ProtectedRoute({ children }: { children: React.ReactElement }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? children : <Navigate to="/login" replace />;
}

function ThemedApp() {
  const { i18n } = useTranslation();
  const direction = isRtl(i18n.language) ? "rtl" : "ltr";

  useEffect(() => {
    document.documentElement.dir = direction;
    document.documentElement.lang = i18n.language;
  }, [direction, i18n.language]);

  const theme = useMemo(() => createDourakTheme(direction), [direction]);
  const cache = useMemo(() => createEmotionCache(direction), [direction]);

  return (
    <CacheProvider value={cache}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            {/* Also reachable while signed in — clicking the verification link in a new tab
                shouldn't require signing out first. */}
            <Route path="/verify-email" element={<VerifyEmailPage />} />
            <Route path="/delete-account" element={<DeleteAccountPage />} />
            <Route path="/support" element={<SupportPage />} />
            <Route path="/invite/:token" element={<InvitePage />} />
            <Route element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>
              <Route path="/" element={<DashboardPage />} />
              <Route path="/circles" element={<MyCirclesPage />} />
              <Route path="/circles/new" element={<CreateCirclePage />} />
              <Route path="/circles/:circleId" element={<CircleOverviewPage />} />
              <Route path="/circles/:circleId/members/:memberId/history" element={<MemberHistoryPage />} />
              <Route path="/profile" element={<ProfilePage />} />
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </ThemeProvider>
    </CacheProvider>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ThemedApp />
      </AuthProvider>
    </QueryClientProvider>
  );
}

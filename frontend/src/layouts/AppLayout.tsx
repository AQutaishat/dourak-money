import { AppBar, Box, Button, Container, Stack, Toolbar, Typography, MenuItem, Select } from "@mui/material";
import { Outlet, Link as RouterLink, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../auth/AuthContext";

export function AppLayout() {
  const { t, i18n } = useTranslation();
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <AppBar position="static" color="inherit" elevation={0} sx={{ borderBottom: "1px solid #eee" }}>
        <Toolbar>
          <Typography variant="h6" component={RouterLink} to="/" sx={{ textDecoration: "none", color: "primary.main", fontWeight: 700, flexGrow: 1 }}>
            {t("app.name")}
          </Typography>
          <Stack direction="row" spacing={1} alignItems="center">
            <Button component={RouterLink} to="/">{t("nav.dashboard")}</Button>
            <Button component={RouterLink} to="/circles">{t("nav.myCircles")}</Button>
            <Select
              size="small"
              value={i18n.language.startsWith("ar") ? "ar" : "en"}
              onChange={(e) => i18n.changeLanguage(e.target.value)}
            >
              <MenuItem value="ar">العربية</MenuItem>
              <MenuItem value="en">English</MenuItem>
            </Select>
            <Button onClick={handleLogout} color="inherit">{t("nav.logout")}</Button>
          </Stack>
        </Toolbar>
      </AppBar>
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Outlet />
      </Container>
    </Box>
  );
}

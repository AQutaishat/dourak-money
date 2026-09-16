import { useState } from "react";
import {
  AppBar, Box, Button, Container, Divider, ListItemIcon, ListItemText, Stack, Toolbar,
  Typography, MenuItem, Menu, Select,
} from "@mui/material";
import AccountCircleIcon from "@mui/icons-material/AccountCircle";
import PersonIcon from "@mui/icons-material/Person";
import LogoutIcon from "@mui/icons-material/Logout";
import { Outlet, Link as RouterLink, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../auth/AuthContext";
import { UnverifiedEmailBanner } from "../components/UnverifiedEmailBanner";
import dourakLogo from "../assets/dourak-logo.png";

/**
 * prompt02 §Top navigation: nav links follow the app name on the *start* side of the reading
 * direction, while the language switcher and the account menu always sit on the *end* side.
 * Flexbox already mirrors itself under `dir="rtl"`, so a single `flexGrow` spacer gives the
 * correct layout in both directions with no per-language branching.
 */
export function AppLayout() {
  const { t, i18n } = useTranslation();
  const { logout, displayLabel, profile } = useAuth();
  const navigate = useNavigate();
  const [accountAnchor, setAccountAnchor] = useState<null | HTMLElement>(null);

  const handleLogout = () => {
    setAccountAnchor(null);
    logout();
    navigate("/login");
  };

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <AppBar position="static" color="inherit" elevation={0} sx={{ borderBottom: "1px solid #eee" }}>
        <Toolbar sx={{ gap: 1 }}>
          <Box
            component={RouterLink}
            to="/"
            sx={{ display: "flex", alignItems: "center", gap: 1, textDecoration: "none", marginInlineEnd: 3 }}
          >
            <Box component="img" src={dourakLogo} alt="" width={32} height={32} sx={{ borderRadius: 1 }} />
            <Typography variant="h6" sx={{ color: "primary.main", fontWeight: 700 }}>
              {t("app.name")}
            </Typography>
          </Box>

          {/* Nav links: immediately after the app name, on the start side. */}
          <Stack direction="row" spacing={1} alignItems="center" sx={{ marginInlineStart: 2 }}>
            <Button component={RouterLink} to="/">{t("nav.dashboard")}</Button>
            <Button component={RouterLink} to="/circles">{t("nav.myCircles")}</Button>
          </Stack>

          <Box sx={{ flexGrow: 1 }} />

          {/* Language switcher + account menu: always on the end side. */}
          <Stack direction="row" spacing={1} alignItems="center">
            <Select
              size="small"
              value={i18n.language.startsWith("ar") ? "ar" : "en"}
              onChange={(e) => i18n.changeLanguage(e.target.value)}
            >
              <MenuItem value="ar">العربية</MenuItem>
              <MenuItem value="en">English</MenuItem>
            </Select>

            <Button
              onClick={(e) => setAccountAnchor(e.currentTarget)}
              color="inherit"
              startIcon={<AccountCircleIcon />}
              sx={{ textTransform: "none", maxWidth: 220 }}
            >
              <Box component="span" sx={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                {displayLabel || t("nav.account")}
              </Box>
            </Button>
            <Menu anchorEl={accountAnchor} open={!!accountAnchor} onClose={() => setAccountAnchor(null)}>
              <MenuItem component={RouterLink} to="/profile" onClick={() => setAccountAnchor(null)}>
                <ListItemIcon><PersonIcon fontSize="small" /></ListItemIcon>
                <ListItemText>{t("nav.myProfile")}</ListItemText>
              </MenuItem>
              <Divider />
              <MenuItem onClick={handleLogout}>
                <ListItemIcon><LogoutIcon fontSize="small" /></ListItemIcon>
                <ListItemText>{t("nav.logout")}</ListItemText>
              </MenuItem>
            </Menu>
          </Stack>
        </Toolbar>
      </AppBar>
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Outlet />
      </Container>
      {/* Never gates anything — every action stays available while unverified, this is
          purely encouragement, never blocking (prompt: "all actions are allowed for
          unverified email"). */}
      {profile && !profile.emailConfirmed && <UnverifiedEmailBanner />}
    </Box>
  );
}

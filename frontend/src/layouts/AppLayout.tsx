import { useState } from "react";
import {
  AppBar, Box, Button, Container, Divider, Drawer, IconButton, ListItemIcon, ListItemText, Stack,
  Toolbar, Typography, MenuItem, Menu, Select, useMediaQuery,
} from "@mui/material";
import type { Theme } from "@mui/material/styles";
import MenuIcon from "@mui/icons-material/Menu";
import AccountCircleIcon from "@mui/icons-material/AccountCircle";
import PersonIcon from "@mui/icons-material/Person";
import LogoutIcon from "@mui/icons-material/Logout";
import DashboardIcon from "@mui/icons-material/Dashboard";
import GroupsIcon from "@mui/icons-material/Groups";
import HelpOutlineIcon from "@mui/icons-material/HelpOutline";
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
 *
 * Below `sm`, the desktop row (logo + 2 nav buttons + language select + account button) doesn't
 * fit a phone width at all — it used to just overflow horizontally, which is also what made
 * every Dialog/Menu backdrop on mobile look broken (a stray horizontal scrollbar throws off
 * MUI's scroll-lock math for every fixed-position overlay). Below `sm` it collapses to a
 * hamburger button opening a side Drawer with the same links/actions instead.
 */
export function AppLayout() {
  const { t, i18n } = useTranslation();
  const { logout, displayLabel, profile } = useAuth();
  const navigate = useNavigate();
  const isMobile = useMediaQuery((theme: Theme) => theme.breakpoints.down("sm"));
  const [accountAnchor, setAccountAnchor] = useState<null | HTMLElement>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);

  const handleLogout = () => {
    setAccountAnchor(null);
    setDrawerOpen(false);
    logout();
    navigate("/login");
  };

  // prompt: a static illustrated how-to guide, shipped as a plain HTML file under public/help/
  // (not part of the SPA/router) so it works standalone — the correct language file is picked
  // from the current UI language, same `startsWith("ar")` check used for the language select.
  const userGuideUrl = i18n.language.startsWith("ar") ? "/help/ar.html" : "/help/en.html";

  const languageSelect = (
    <Select
      size="small"
      fullWidth={isMobile}
      value={i18n.language.startsWith("ar") ? "ar" : "en"}
      onChange={(e) => i18n.changeLanguage(e.target.value)}
    >
      <MenuItem value="ar">العربية</MenuItem>
      <MenuItem value="en">English</MenuItem>
    </Select>
  );

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <AppBar position="static" color="inherit" elevation={0} sx={{ borderBottom: "1px solid #eee" }}>
        <Toolbar sx={{ gap: 1 }}>
          <Box
            component={RouterLink}
            to="/"
            sx={{ display: "flex", alignItems: "center", gap: 1, textDecoration: "none", marginInlineEnd: 3, flexGrow: { xs: 1, sm: 0 } }}
          >
            <Box component="img" src={dourakLogo} alt="" width={32} height={32} sx={{ borderRadius: 1 }} />
            <Typography variant="h6" sx={{ color: "primary.main", fontWeight: 700 }}>
              {t("app.name")}
            </Typography>
          </Box>

          {isMobile ? (
            <IconButton edge="end" color="inherit" onClick={() => setDrawerOpen(true)} aria-label="menu">
              <MenuIcon />
            </IconButton>
          ) : (
            <>
              {/* Nav links: immediately after the app name, on the start side. */}
              <Stack direction="row" spacing={1} alignItems="center" sx={{ marginInlineStart: 2 }}>
                <Button component={RouterLink} to="/">{t("nav.dashboard")}</Button>
                <Button component={RouterLink} to="/circles">{t("nav.myCircles")}</Button>
                <Button component="a" href={userGuideUrl} target="_blank" rel="noopener">{t("nav.userGuide")}</Button>
              </Stack>

              <Box sx={{ flexGrow: 1 }} />

              {/* Language switcher + account menu: always on the end side. */}
              <Stack direction="row" spacing={1} alignItems="center">
                {languageSelect}

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
            </>
          )}
        </Toolbar>
      </AppBar>

      {/* Mobile-only nav drawer — same links/actions as the desktop row above, stacked instead
          of squeezed into one row. Anchored to the reading-direction end side. */}
      <Drawer
        anchor={i18n.language.startsWith("ar") ? "left" : "right"}
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
      >
        <Box sx={{ width: 260, p: 2 }} role="presentation">
          <Stack spacing={1}>
            <Typography variant="subtitle2" color="text.secondary" noWrap>
              {displayLabel || t("nav.account")}
            </Typography>
            <Divider />
            <Button
              fullWidth startIcon={<DashboardIcon />} sx={{ justifyContent: "flex-start" }}
              component={RouterLink} to="/" onClick={() => setDrawerOpen(false)}
            >
              {t("nav.dashboard")}
            </Button>
            <Button
              fullWidth startIcon={<GroupsIcon />} sx={{ justifyContent: "flex-start" }}
              component={RouterLink} to="/circles" onClick={() => setDrawerOpen(false)}
            >
              {t("nav.myCircles")}
            </Button>
            <Button
              fullWidth startIcon={<PersonIcon />} sx={{ justifyContent: "flex-start" }}
              component={RouterLink} to="/profile" onClick={() => setDrawerOpen(false)}
            >
              {t("nav.myProfile")}
            </Button>
            <Button
              fullWidth startIcon={<HelpOutlineIcon />} sx={{ justifyContent: "flex-start" }}
              component="a" href={userGuideUrl} target="_blank" rel="noopener"
              onClick={() => setDrawerOpen(false)}
            >
              {t("nav.userGuide")}
            </Button>
            <Divider />
            {languageSelect}
            <Divider />
            <Button fullWidth color="error" startIcon={<LogoutIcon />} sx={{ justifyContent: "flex-start" }} onClick={handleLogout}>
              {t("nav.logout")}
            </Button>
          </Stack>
        </Box>
      </Drawer>

      <Container maxWidth="xl" sx={{ py: 4 }}>
        <Outlet />
      </Container>
      {/* Never gates anything — every action stays available while unverified, this is
          purely encouragement, never blocking (prompt: "all actions are allowed for
          unverified email"). */}
      {profile && !profile.emailConfirmed && <UnverifiedEmailBanner />}
    </Box>
  );
}

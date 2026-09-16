import { AppBar, Box, Button, Container, Stack, Toolbar, Typography } from "@mui/material";
import { Outlet, Link as RouterLink, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function AdminLayout() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "#f4f4f7" }}>
      <AppBar position="static" color="inherit" elevation={0} sx={{ borderBottom: "1px solid #e0e0e0" }}>
        <Toolbar sx={{ gap: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, color: "primary.main" }}>Dourak Admin</Typography>
          <Stack direction="row" spacing={1} sx={{ flexGrow: 1 }}>
            <Button component={RouterLink} to="/">Dashboard</Button>
            <Button component={RouterLink} to="/users">Users</Button>
          </Stack>
          <Button onClick={handleLogout}>Sign out</Button>
        </Toolbar>
      </AppBar>
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Outlet />
      </Container>
    </Box>
  );
}

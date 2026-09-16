import { useQuery } from "@tanstack/react-query";
import { Grid, Paper, Typography, CircularProgress } from "@mui/material";
import { adminApi } from "../api/admin";

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <Paper sx={{ p: 3, textAlign: "center" }}>
      <Typography variant="h3" fontWeight={700} color="primary.main">{value}</Typography>
      <Typography variant="body2" color="text.secondary">{label}</Typography>
    </Paper>
  );
}

/** Home page — totals only, per spec. Details live on the Users page. */
export function DashboardPage() {
  const { data: stats, isLoading } = useQuery({ queryKey: ["admin-stats"], queryFn: adminApi.stats });

  if (isLoading || !stats) return <CircularProgress />;

  return (
    <>
      <Typography variant="h5" fontWeight={700} gutterBottom>Overview</Typography>
      <Grid container spacing={2} sx={{ mt: 1 }}>
        <Grid item xs={6} md={3}><StatCard label="Total Users" value={stats.totalUsers} /></Grid>
        <Grid item xs={6} md={3}><StatCard label="Verified Users" value={stats.verifiedUsers} /></Grid>
        <Grid item xs={6} md={3}><StatCard label="Active Users" value={stats.activeUsers} /></Grid>
        <Grid item xs={6} md={3}><StatCard label="Total Circles" value={stats.totalCircles} /></Grid>
        <Grid item xs={6} md={3}><StatCard label="Active Circles" value={stats.activeCircles} /></Grid>
        <Grid item xs={6} md={3}><StatCard label="Draft Circles" value={stats.draftCircles} /></Grid>
      </Grid>
    </>
  );
}

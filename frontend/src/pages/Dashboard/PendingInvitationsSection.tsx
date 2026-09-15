import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, Button, Card, CardContent, Stack, Typography } from "@mui/material";
import MailIcon from "@mui/icons-material/MarkEmailUnread";
import { useTranslation } from "react-i18next";
import { invitationsApi } from "../../api/circles";

/**
 * prompt02 §4: where an invitee sees and acts on circle invitations. The notification bell with
 * an unread badge is deferred to `docs/future-work.md`; this main-page section is what ships.
 */
export function PendingInvitationsSection() {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const { data: invitations } = useQuery({ queryKey: ["invitations"], queryFn: invitationsApi.pending });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["invitations"] });
    // Accepting adds the circle to "My Circles" straight away.
    queryClient.invalidateQueries({ queryKey: ["circles"] });
  };

  const respond = useMutation({
    mutationFn: ({ memberId, accept }: { memberId: number; accept: boolean }) =>
      accept ? invitationsApi.accept(memberId) : invitationsApi.decline(memberId),
    onSuccess: invalidate,
  });

  if (!invitations || invitations.length === 0) return null;

  return (
    <Stack spacing={1.5}>
      <Stack direction="row" spacing={1} alignItems="center">
        <MailIcon color="primary" />
        <Typography variant="h6">{t("circle.pendingInvitations")}</Typography>
      </Stack>

      {respond.isError && <Alert severity="error">{t("common.error")}</Alert>}

      {invitations.map((invitation) => (
        <Card key={invitation.memberId} variant="outlined" sx={{ borderColor: "primary.light" }}>
          <CardContent>
            <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>
              <div>
                <Typography variant="subtitle1" fontWeight={700}>{invitation.circleName}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {t("circle.invitedBy", { name: invitation.organizerName })}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {invitation.contributionAmount} {invitation.currency} · {invitation.memberCount} {t("circle.members")} ·{" "}
                  {new Date(invitation.startDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" })}
                </Typography>
              </div>
              <Stack direction="row" spacing={1} alignItems="center">
                <Button
                  variant="contained"
                  disabled={respond.isPending}
                  onClick={() => respond.mutate({ memberId: invitation.memberId, accept: true })}
                >
                  {t("circle.accept")}
                </Button>
                <Button
                  color="inherit"
                  disabled={respond.isPending}
                  onClick={() => respond.mutate({ memberId: invitation.memberId, accept: false })}
                >
                  {t("circle.decline")}
                </Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      ))}
    </Stack>
  );
}

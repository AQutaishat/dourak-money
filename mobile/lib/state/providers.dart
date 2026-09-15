import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/api_client.dart';
import '../api/auth_api.dart';
import '../api/circles_api.dart';
import '../api/models.dart';
import '../auth/auth_state.dart';

// ---- Wiring / DI -----------------------------------------------------------------

final apiClientProvider = Provider<ApiClient>((ref) => ApiClient());
final authApiProvider = Provider<AuthApi>((ref) => AuthApi(ref.watch(apiClientProvider)));
final usersApiProvider = Provider<UsersApi>((ref) => UsersApi(ref.watch(apiClientProvider)));
final circlesApiProvider = Provider<CirclesApi>((ref) => CirclesApi(ref.watch(apiClientProvider)));
final cyclesApiProvider = Provider<CyclesApi>((ref) => CyclesApi(ref.watch(apiClientProvider)));
final invitationsApiProvider = Provider<InvitationsApi>((ref) => InvitationsApi(ref.watch(apiClientProvider)));
final paymentClaimsApiProvider = Provider<PaymentClaimsApi>((ref) => PaymentClaimsApi(ref.watch(apiClientProvider)));

final authControllerProvider = StateNotifierProvider<AuthController, AuthData>((ref) {
  final controller = AuthController(ref.watch(authApiProvider));
  // Mirrors the axios response interceptor bouncing to /login on an unexpected 401.
  ref.watch(apiClientProvider).onUnauthorized = controller.forceLogout;
  return controller;
});

/// Bumped after any mutation that should invalidate cached server state — the
/// Riverpod equivalent of TanStack Query's `invalidateQueries`. Each family below
/// watches this so incrementing it triggers a refetch everywhere it's used.
final refreshTickProvider = StateProvider<int>((ref) => 0);

// Call `ref.read(refreshTickProvider.notifier).state++` after any mutation
// (create/update/delete) to refetch every screen watching the providers below —
// the Riverpod equivalent of TanStack Query's `queryClient.invalidateQueries()`.

// ---- Server-state (mirrors TanStack Query hooks) ----------------------------------

final circlesListProvider = FutureProvider.autoDispose<List<CircleSummary>>((ref) {
  ref.watch(refreshTickProvider);
  return ref.watch(circlesApiProvider).list();
});

final circleDetailProvider = FutureProvider.autoDispose.family<CircleDetail, int>((ref, id) {
  ref.watch(refreshTickProvider);
  return ref.watch(circlesApiProvider).detail(id);
});

final membersProvider = FutureProvider.autoDispose.family<List<Member>, int>((ref, circleId) {
  ref.watch(refreshTickProvider);
  return ref.watch(circlesApiProvider).members(circleId);
});

final payoutOrderProvider = FutureProvider.autoDispose.family<List<PayoutOrderEntry>, int>((ref, circleId) {
  ref.watch(refreshTickProvider);
  return ref.watch(circlesApiProvider).payoutOrder(circleId);
});

final scheduleProvider = FutureProvider.autoDispose.family<List<ScheduleCycle>, int>((ref, circleId) {
  ref.watch(refreshTickProvider);
  return ref.watch(circlesApiProvider).schedule(circleId);
});

final dashboardProvider = FutureProvider.autoDispose.family<CurrentCycleDashboard?, int>((ref, circleId) {
  ref.watch(refreshTickProvider);
  return ref.watch(circlesApiProvider).dashboard(circleId);
});

final historyProvider = FutureProvider.autoDispose.family<List<CircleHistoryCycle>, int>((ref, circleId) {
  ref.watch(refreshTickProvider);
  return ref.watch(circlesApiProvider).history(circleId);
});

final memberHistoryProvider =
    FutureProvider.autoDispose.family<MemberHistory, (int circleId, int memberId)>((ref, args) {
  ref.watch(refreshTickProvider);
  return ref.watch(circlesApiProvider).memberHistory(args.$1, args.$2);
});

final paymentClaimsProvider = FutureProvider.autoDispose.family<List<PaymentClaim>, int>((ref, circleId) {
  ref.watch(refreshTickProvider);
  return ref.watch(circlesApiProvider).paymentClaims(circleId);
});

final pendingInvitationsProvider = FutureProvider.autoDispose<List<PendingInvitation>>((ref) {
  ref.watch(refreshTickProvider);
  return ref.watch(invitationsApiProvider).pending();
});

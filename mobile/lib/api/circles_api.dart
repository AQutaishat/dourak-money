import 'dart:io';

import 'package:dio/dio.dart';

import 'api_client.dart';
import 'models.dart';

/// Mirrors frontend/src/api/circles.ts route-for-route.
class CirclesApi {
  CirclesApi(this._client);
  final ApiClient _client;

  Future<List<CircleSummary>> list() async {
    final res = await _client.dio.get('/circles');
    return (res.data as List<dynamic>).map((e) => CircleSummary.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<CircleDetail> detail(int id) async {
    final res = await _client.dio.get('/circles/$id');
    return CircleDetail.fromJson(res.data as Map<String, dynamic>);
  }

  Future<int> create({
    required String name,
    String? description,
    required String currency,
    required double contributionAmount,
    required String startDate,
  }) async {
    final res = await _client.dio.post('/circles', data: {
      'name': name,
      'description': description,
      'currency': currency,
      'contributionAmount': contributionAmount,
      'startDate': startDate,
    });
    return res.data as int;
  }

  Future<void> remove(int id) => _client.dio.delete('/circles/$id');

  Future<void> updateBasicInfo(int id, {required String name, String? description, required String startDate, required double contributionAmount}) {
    return _client.dio.put('/circles/$id/basic-info', data: {
      'name': name,
      'description': description,
      'startDate': startDate,
      'contributionAmount': contributionAmount,
    });
  }

  Future<void> activate(int id) => _client.dio.post('/circles/$id/activate');
  Future<void> pause(int id) => _client.dio.post('/circles/$id/pause');
  Future<void> resume(int id) => _client.dio.post('/circles/$id/resume');
  Future<void> cancel(int id) => _client.dio.post('/circles/$id/cancel');

  Future<List<Member>> members(int id) async {
    final res = await _client.dio.get('/circles/$id/members');
    return (res.data as List<dynamic>).map((e) => Member.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<int> addMember(int id, {required String name, String? phone, String? email, String? notes}) async {
    final res = await _client.dio.post('/circles/$id/members', data: {
      'name': name,
      'phone': phone,
      'email': email,
      'notes': notes,
    });
    return res.data as int;
  }

  Future<int> addUserMember(int id, String userId) async {
    final res = await _client.dio.post('/circles/$id/members/by-user', data: {'userId': userId});
    return res.data as int;
  }

  /// Mirrors `circlesApi.inviteUnregisteredMember` — creates a Pending, name-only member row
  /// carrying a one-time invite token, which the WhatsApp message then links to.
  Future<InviteUnregisteredResult> inviteUnregistered(int id, String name) async {
    final res = await _client.dio.post('/circles/$id/members/invite-unregistered', data: {'name': name});
    return InviteUnregisteredResult.fromJson(res.data as Map<String, dynamic>);
  }

  Future<int> addSelfAsMember(int id) async {
    final res = await _client.dio.post('/circles/$id/members/self');
    return res.data as int;
  }

  Future<void> reinviteMember(int id, int memberId) => _client.dio.post('/circles/$id/members/$memberId/reinvite');

  Future<void> updateMember(int id, int memberId, {required String name, String? phone, String? email, String? notes}) {
    return _client.dio.put('/circles/$id/members/$memberId', data: {
      'name': name,
      'phone': phone,
      'email': email,
      'notes': notes,
    });
  }

  Future<void> deactivateMember(int id, int memberId) => _client.dio.post('/circles/$id/members/$memberId/deactivate');

  /// prompt03 §1 — full removal, draft circles only.
  Future<void> removeMember(int id, int memberId) => _client.dio.delete('/circles/$id/members/$memberId');

  Future<void> replaceMember(int id, int oldMemberId, int newMemberId) {
    return _client.dio.post('/circles/$id/members/replace', data: {
      'oldMemberId': oldMemberId,
      'newMemberId': newMemberId,
    });
  }

  Future<List<PayoutOrderEntry>> payoutOrder(int id) async {
    final res = await _client.dio.get('/circles/$id/payout-order');
    return (res.data as List<dynamic>).map((e) => PayoutOrderEntry.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<void> setManualOrder(int id, List<int> memberIdsInOrder) {
    return _client.dio.put('/circles/$id/payout-order/manual', data: {'memberIdsInOrder': memberIdsInOrder});
  }

  Future<void> runDraw(int id) => _client.dio.post('/circles/$id/payout-order/draw');
  Future<void> resetOrder(int id) => _client.dio.post('/circles/$id/payout-order/reset');

  /// Persists a single up/down move immediately — mirrors the new
  /// `POST /circles/{id}/payout-order/{memberId}/move?direction=-1|1` endpoint that
  /// replaced the old "reorder locally then Save the whole list" flow.
  Future<void> movePayoutPosition(int id, int memberId, {required int direction}) {
    return _client.dio.post('/circles/$id/payout-order/$memberId/move', queryParameters: {'direction': direction});
  }

  Future<List<ScheduleCycle>> schedule(int id) async {
    final res = await _client.dio.get('/circles/$id/schedule');
    return (res.data as List<dynamic>).map((e) => ScheduleCycle.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<CurrentCycleDashboard?> dashboard(int id) async {
    final res = await _client.dio.get('/circles/$id/dashboard');
    if (res.data == null) return null;
    return CurrentCycleDashboard.fromJson(res.data as Map<String, dynamic>);
  }

  Future<List<CircleHistoryCycle>> history(int id) async {
    final res = await _client.dio.get('/circles/$id/history');
    return (res.data as List<dynamic>).map((e) => CircleHistoryCycle.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<MemberHistory> memberHistory(int id, int memberId) async {
    final res = await _client.dio.get('/circles/$id/members/$memberId/history');
    return MemberHistory.fromJson(res.data as Map<String, dynamic>);
  }

  /// `GET /circles/{id}/months-detail` — every cycle with its full per-member payment
  /// breakdown, backing the Monthly Cycles tab.
  Future<List<CircleMonth>> monthsDetail(int id) async {
    final res = await _client.dio.get('/circles/$id/months-detail');
    return (res.data as List<dynamic>).map((e) => CircleMonth.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<PaymentClaim>> paymentClaims(int id, {bool pendingOnly = false}) async {
    final res = await _client.dio.get('/circles/$id/payment-claims', queryParameters: {'pendingOnly': pendingOnly});
    return (res.data as List<dynamic>).map((e) => PaymentClaim.fromJson(e as Map<String, dynamic>)).toList();
  }
}

/// Mirrors the `cyclesApi` export.
class CyclesApi {
  CyclesApi(this._client);
  final ApiClient _client;

  Future<void> recordContribution(int cycleId, {required int memberId, required double paidAmount}) {
    return _client.dio.post('/cycles/$cycleId/contributions', data: {
      'memberId': memberId,
      'paidAmount': paidAmount,
    });
  }

  /// `POST /cycles/{id}/payout` is now multipart/form-data and additive (adds to
  /// whatever's already been paid, capped server-side at what's still outstanding)
  /// instead of the old single-shot JSON all-or-nothing call.
  Future<void> recordPayout(
    int cycleId, {
    required double actualAmount,
    DateTime? paidAt,
    String? paymentMethod,
    String? notes,
    File? evidence,
  }) async {
    final form = FormData.fromMap({
      'actualAmount': actualAmount.toString(),
      'paidAt': (paidAt ?? DateTime.now()).toIso8601String(),
      if (paymentMethod != null && paymentMethod.isNotEmpty) 'paymentMethod': paymentMethod,
      if (notes != null && notes.isNotEmpty) 'notes': notes,
      if (evidence != null) 'evidence': await MultipartFile.fromFile(evidence.path),
    });
    await _client.dio.post('/cycles/$cycleId/payout', data: form);
  }

  Future<void> reopenPayout(int cycleId) => _client.dio.post('/cycles/$cycleId/payout/reopen');

  /// Fetched as bytes because the endpoint requires the bearer token — mirrors the web app's
  /// `payoutEvidenceUrl()` building a blob URL instead of a plain link.
  Future<EvidenceDownload> payoutEvidence(int payoutPaymentId) =>
      downloadEvidence(_client, '/cycles/payout-payments/$payoutPaymentId/evidence');
}

/// A token-authenticated evidence file pulled down as bytes, with the content type/file name
/// the server reported so the viewer knows whether it can render it inline.
class EvidenceDownload {
  const EvidenceDownload({required this.bytes, required this.contentType, this.fileName});
  final List<int> bytes;
  final String contentType;
  final String? fileName;

  bool get isImage => contentType.startsWith('image/');
}

Future<EvidenceDownload> downloadEvidence(ApiClient client, String path) async {
  final res = await client.dio.get<List<int>>(path, options: Options(responseType: ResponseType.bytes));
  final contentType = res.headers.value('content-type') ?? 'application/octet-stream';
  final disposition = res.headers.value('content-disposition');
  String? fileName;
  if (disposition != null) {
    final match = RegExp(r'filename="?([^";]+)"?').firstMatch(disposition);
    fileName = match?.group(1);
  }
  return EvidenceDownload(bytes: res.data ?? const [], contentType: contentType.split(';').first.trim(), fileName: fileName);
}

/// Mirrors `invitationsApi` — Phase 2 §4.
class InvitationsApi {
  InvitationsApi(this._client);
  final ApiClient _client;

  Future<List<PendingInvitation>> pending() async {
    final res = await _client.dio.get('/invitations/pending');
    return (res.data as List<dynamic>).map((e) => PendingInvitation.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<void> accept(int memberId) => _client.dio.post('/invitations/$memberId/accept');
  Future<void> decline(int memberId) => _client.dio.post('/invitations/$memberId/decline');

  /// Attaches the signed-in user's account to the Pending member row created by an
  /// unregistered-invite token (the `/invite/{token}` deep link they just opened).
  Future<void> linkToken(String token) => _client.dio.post('/invitations/link/$token');
}

/// Mirrors `paymentClaimsApi` — Phase 2 §6.
class PaymentClaimsApi {
  PaymentClaimsApi(this._client);
  final ApiClient _client;

  Future<List<PaymentClaim>> mine() async {
    final res = await _client.dio.get('/payment-claims/mine');
    return (res.data as List<dynamic>).map((e) => PaymentClaim.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<int> submit(int cycleId, {required double claimedAmount, String? note, File? evidence}) async {
    final form = FormData.fromMap({
      'claimedAmount': claimedAmount.toString(),
      if (note != null && note.isNotEmpty) 'note': note,
      if (evidence != null) 'evidence': await MultipartFile.fromFile(evidence.path),
    });
    final res = await _client.dio.post('/payment-claims/cycles/$cycleId', data: form);
    return res.data as int;
  }

  Future<void> review(int claimId, {required bool approve, String? rejectionReason}) {
    return _client.dio.post('/payment-claims/$claimId/review', data: {
      'approve': approve,
      'rejectionReason': rejectionReason,
    });
  }

  /// The submitting member can still correct amount/note/evidence while the claim is Pending.
  Future<void> update(
    int claimId, {
    required double claimedAmount,
    String? note,
    bool removeEvidence = false,
    File? evidence,
  }) async {
    final form = FormData.fromMap({
      'claimedAmount': claimedAmount.toString(),
      if (note != null && note.isNotEmpty) 'note': note,
      'removeEvidence': removeEvidence.toString(),
      if (evidence != null) 'evidence': await MultipartFile.fromFile(evidence.path),
    });
    await _client.dio.put('/payment-claims/$claimId', data: form);
  }

  /// "Unsend" a still-Pending claim.
  Future<void> withdraw(int claimId) => _client.dio.delete('/payment-claims/$claimId');

  /// Fetched as bytes because the endpoint requires the bearer token (privacy rule, §6) —
  /// mirrors evidenceUrl() creating a blob URL in the web app.
  Future<EvidenceDownload> evidence(int claimId) => downloadEvidence(_client, '/payment-claims/$claimId/evidence');
}

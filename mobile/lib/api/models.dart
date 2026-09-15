// Data models mirroring frontend/src/api/types.ts 1:1.
// Hand-written fromJson/toJson (no build_runner) so the app has zero code-generation
// step — see docs/progress.md "Mobile App" section for why this was chosen.

T? _n<T>(Map<String, dynamic> j, String key) => j[key] as T?;

class CircleSummary {
  final int id;
  final String name;
  final String currency;
  final double contributionAmount;
  final String frequency;
  final String startDate;
  final String status; // Draft/Active/Paused/Completed/Cancelled
  final int memberCount;
  final String organizerName;
  final String createdAt;
  final bool isOrganizer;

  CircleSummary({
    required this.id,
    required this.name,
    required this.currency,
    required this.contributionAmount,
    required this.frequency,
    required this.startDate,
    required this.status,
    required this.memberCount,
    required this.organizerName,
    required this.createdAt,
    required this.isOrganizer,
  });

  factory CircleSummary.fromJson(Map<String, dynamic> j) => CircleSummary(
        id: j['id'] as int,
        name: j['name'] as String,
        currency: j['currency'] as String,
        contributionAmount: (j['contributionAmount'] as num).toDouble(),
        frequency: j['frequency'] as String? ?? 'Monthly',
        startDate: j['startDate'] as String,
        status: j['status'] as String,
        memberCount: j['memberCount'] as int,
        organizerName: j['organizerName'] as String? ?? '',
        createdAt: j['createdAt'] as String,
        isOrganizer: j['isOrganizer'] as bool? ?? false,
      );
}

class CircleDetail {
  final int id;
  final String name;
  final String? description;
  final String currency;
  final double contributionAmount;
  final String frequency;
  final String startDate;
  final String status;
  final String? payoutOrderMethod;
  final bool payoutOrderConfirmed;
  final int memberCount;
  final String organizerName;
  final String createdAt;
  final bool isOrganizer;
  final double totalMonthlyAmount;
  final String? lastPaymentMonth;
  final bool canDelete;
  final int? myMemberId;

  CircleDetail({
    required this.id,
    required this.name,
    this.description,
    required this.currency,
    required this.contributionAmount,
    required this.frequency,
    required this.startDate,
    required this.status,
    this.payoutOrderMethod,
    required this.payoutOrderConfirmed,
    required this.memberCount,
    required this.organizerName,
    required this.createdAt,
    required this.isOrganizer,
    required this.totalMonthlyAmount,
    this.lastPaymentMonth,
    required this.canDelete,
    this.myMemberId,
  });

  factory CircleDetail.fromJson(Map<String, dynamic> j) => CircleDetail(
        id: j['id'] as int,
        name: j['name'] as String,
        description: j['description'] as String?,
        currency: j['currency'] as String,
        contributionAmount: (j['contributionAmount'] as num).toDouble(),
        frequency: j['frequency'] as String? ?? 'Monthly',
        startDate: j['startDate'] as String,
        status: j['status'] as String,
        payoutOrderMethod: j['payoutOrderMethod'] as String?,
        payoutOrderConfirmed: j['payoutOrderConfirmed'] as bool? ?? false,
        memberCount: j['memberCount'] as int,
        organizerName: j['organizerName'] as String? ?? '',
        createdAt: j['createdAt'] as String,
        isOrganizer: j['isOrganizer'] as bool? ?? false,
        totalMonthlyAmount: (j['totalMonthlyAmount'] as num?)?.toDouble() ?? 0,
        lastPaymentMonth: j['lastPaymentMonth'] as String?,
        canDelete: j['canDelete'] as bool? ?? false,
        myMemberId: j['myMemberId'] as int?,
      );

  bool get isDraft => status == 'Draft';
  bool get isActive => status == 'Active';
}

class Member {
  final int id;
  final String name;
  final String? phone;
  final String? email;
  final String? notes;
  final bool isActive;
  final int? payoutPosition;
  final String invitationStatus; // NotInvited/Pending/Accepted/Declined
  final String? userId;
  final bool isParticipating;

  Member({
    required this.id,
    required this.name,
    this.phone,
    this.email,
    this.notes,
    required this.isActive,
    this.payoutPosition,
    required this.invitationStatus,
    this.userId,
    required this.isParticipating,
  });

  factory Member.fromJson(Map<String, dynamic> j) => Member(
        id: j['id'] as int,
        name: j['name'] as String,
        phone: j['phone'] as String?,
        email: j['email'] as String?,
        notes: j['notes'] as String?,
        isActive: j['isActive'] as bool? ?? true,
        payoutPosition: j['payoutPosition'] as int?,
        invitationStatus: j['invitationStatus'] as String? ?? 'NotInvited',
        userId: j['userId'] as String?,
        isParticipating: j['isParticipating'] as bool? ?? true,
      );
}

class PayoutOrderEntry {
  final int position;
  final int memberId;
  final String memberName;

  PayoutOrderEntry({required this.position, required this.memberId, required this.memberName});

  factory PayoutOrderEntry.fromJson(Map<String, dynamic> j) => PayoutOrderEntry(
        position: j['position'] as int,
        memberId: j['memberId'] as int,
        memberName: j['memberName'] as String,
      );
}

class ScheduleCycle {
  final int cycleId;
  final int sequenceNumber;
  final String dueDate;
  final int recipientMemberId;
  final String recipientName;
  final double expectedPoolAmount;
  final String status;
  final String payoutStatus;

  ScheduleCycle({
    required this.cycleId,
    required this.sequenceNumber,
    required this.dueDate,
    required this.recipientMemberId,
    required this.recipientName,
    required this.expectedPoolAmount,
    required this.status,
    required this.payoutStatus,
  });

  factory ScheduleCycle.fromJson(Map<String, dynamic> j) => ScheduleCycle(
        cycleId: j['cycleId'] as int,
        sequenceNumber: j['sequenceNumber'] as int,
        dueDate: j['dueDate'] as String,
        recipientMemberId: j['recipientMemberId'] as int,
        recipientName: j['recipientName'] as String,
        expectedPoolAmount: (j['expectedPoolAmount'] as num).toDouble(),
        status: j['status'] as String,
        payoutStatus: j['payoutStatus'] as String,
      );
}

class CurrentCycleMemberRow {
  final int memberId;
  final String memberName;
  final double expectedAmount;
  final double paidAmount;
  final String status; // Unpaid/PartiallyPaid/Paid/Late
  final String? paidAt;
  final String? myClaimStatus;
  final bool hasPendingClaim;

  CurrentCycleMemberRow({
    required this.memberId,
    required this.memberName,
    required this.expectedAmount,
    required this.paidAmount,
    required this.status,
    this.paidAt,
    this.myClaimStatus,
    required this.hasPendingClaim,
  });

  factory CurrentCycleMemberRow.fromJson(Map<String, dynamic> j) => CurrentCycleMemberRow(
        memberId: j['memberId'] as int,
        memberName: j['memberName'] as String,
        expectedAmount: (j['expectedAmount'] as num).toDouble(),
        paidAmount: (j['paidAmount'] as num).toDouble(),
        status: j['status'] as String,
        paidAt: j['paidAt'] as String?,
        myClaimStatus: j['myClaimStatus'] as String?,
        hasPendingClaim: j['hasPendingClaim'] as bool? ?? false,
      );
}

class CurrentCycleDashboard {
  final int cycleId;
  final int sequenceNumber;
  final String dueDate;
  final int membersTotal;
  final int membersPaid;
  final int membersUnpaid;
  final int membersLate;
  final double collected;
  final double expected;
  final double outstanding;
  final int recipientMemberId;
  final String recipientName;
  final String payoutStatus;
  final int? nextRecipientMemberId;
  final String? nextRecipientName;
  final List<CurrentCycleMemberRow> members;
  final int pendingClaimCount;

  CurrentCycleDashboard({
    required this.cycleId,
    required this.sequenceNumber,
    required this.dueDate,
    required this.membersTotal,
    required this.membersPaid,
    required this.membersUnpaid,
    required this.membersLate,
    required this.collected,
    required this.expected,
    required this.outstanding,
    required this.recipientMemberId,
    required this.recipientName,
    required this.payoutStatus,
    this.nextRecipientMemberId,
    this.nextRecipientName,
    required this.members,
    required this.pendingClaimCount,
  });

  factory CurrentCycleDashboard.fromJson(Map<String, dynamic> j) => CurrentCycleDashboard(
        cycleId: j['cycleId'] as int,
        sequenceNumber: j['sequenceNumber'] as int,
        dueDate: j['dueDate'] as String,
        membersTotal: j['membersTotal'] as int,
        membersPaid: j['membersPaid'] as int,
        membersUnpaid: j['membersUnpaid'] as int,
        membersLate: j['membersLate'] as int,
        collected: (j['collected'] as num).toDouble(),
        expected: (j['expected'] as num).toDouble(),
        outstanding: (j['outstanding'] as num).toDouble(),
        recipientMemberId: j['recipientMemberId'] as int,
        recipientName: j['recipientName'] as String,
        payoutStatus: j['payoutStatus'] as String,
        nextRecipientMemberId: j['nextRecipientMemberId'] as int?,
        nextRecipientName: j['nextRecipientName'] as String?,
        members: (j['members'] as List<dynamic>? ?? [])
            .map((e) => CurrentCycleMemberRow.fromJson(e as Map<String, dynamic>))
            .toList(),
        pendingClaimCount: j['pendingClaimCount'] as int? ?? 0,
      );
}

class MemberHistoryEntry {
  final int cycleId;
  final int sequenceNumber;
  final String dueDate;
  final double expectedAmount;
  final double paidAmount;
  final String status;
  final String? paidAt;
  final bool isRecipientThisCycle;
  final String? payoutStatusIfRecipient;

  MemberHistoryEntry({
    required this.cycleId,
    required this.sequenceNumber,
    required this.dueDate,
    required this.expectedAmount,
    required this.paidAmount,
    required this.status,
    this.paidAt,
    required this.isRecipientThisCycle,
    this.payoutStatusIfRecipient,
  });

  factory MemberHistoryEntry.fromJson(Map<String, dynamic> j) => MemberHistoryEntry(
        cycleId: j['cycleId'] as int,
        sequenceNumber: j['sequenceNumber'] as int,
        dueDate: j['dueDate'] as String,
        expectedAmount: (j['expectedAmount'] as num).toDouble(),
        paidAmount: (j['paidAmount'] as num).toDouble(),
        status: j['status'] as String,
        paidAt: j['paidAt'] as String?,
        isRecipientThisCycle: j['isRecipientThisCycle'] as bool? ?? false,
        payoutStatusIfRecipient: j['payoutStatusIfRecipient'] as String?,
      );
}

class MemberHistory {
  final int memberId;
  final String memberName;
  final int? payoutPosition;
  final List<MemberHistoryEntry> entries;

  MemberHistory({
    required this.memberId,
    required this.memberName,
    this.payoutPosition,
    required this.entries,
  });

  factory MemberHistory.fromJson(Map<String, dynamic> j) => MemberHistory(
        memberId: j['memberId'] as int,
        memberName: j['memberName'] as String,
        payoutPosition: j['payoutPosition'] as int?,
        entries: (j['entries'] as List<dynamic>? ?? [])
            .map((e) => MemberHistoryEntry.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class CircleHistoryCycle {
  final int cycleId;
  final int sequenceNumber;
  final String dueDate;
  final String recipientName;
  final double expectedPool;
  final double collected;
  final List<String> unpaidMembers;
  final List<String> lateMembers;
  final String payoutStatus;
  final String? payoutPaidAt;

  CircleHistoryCycle({
    required this.cycleId,
    required this.sequenceNumber,
    required this.dueDate,
    required this.recipientName,
    required this.expectedPool,
    required this.collected,
    required this.unpaidMembers,
    required this.lateMembers,
    required this.payoutStatus,
    this.payoutPaidAt,
  });

  factory CircleHistoryCycle.fromJson(Map<String, dynamic> j) => CircleHistoryCycle(
        cycleId: j['cycleId'] as int,
        sequenceNumber: j['sequenceNumber'] as int,
        dueDate: j['dueDate'] as String,
        recipientName: j['recipientName'] as String,
        expectedPool: (j['expectedPool'] as num).toDouble(),
        collected: (j['collected'] as num).toDouble(),
        unpaidMembers: (j['unpaidMembers'] as List<dynamic>? ?? []).cast<String>(),
        lateMembers: (j['lateMembers'] as List<dynamic>? ?? []).cast<String>(),
        payoutStatus: j['payoutStatus'] as String,
        payoutPaidAt: j['payoutPaidAt'] as String?,
      );
}

class UserProfile {
  final String userId;
  final String? name;
  final String? email;
  final String? phone;
  final String preferredLanguage;
  final String displayLabel;

  UserProfile({
    required this.userId,
    this.name,
    this.email,
    this.phone,
    required this.preferredLanguage,
    required this.displayLabel,
  });

  factory UserProfile.fromJson(Map<String, dynamic> j) => UserProfile(
        userId: j['userId'] as String,
        name: j['name'] as String?,
        email: j['email'] as String?,
        phone: j['phone'] as String?,
        preferredLanguage: j['preferredLanguage'] as String? ?? 'ar',
        displayLabel: j['displayLabel'] as String? ?? (j['email'] as String? ?? ''),
      );
}

class UserSearchResult {
  final String userId;
  final String? name;
  final String? email;
  final String? phone;
  final String displayLabel;

  UserSearchResult({
    required this.userId,
    this.name,
    this.email,
    this.phone,
    required this.displayLabel,
  });

  factory UserSearchResult.fromJson(Map<String, dynamic> j) => UserSearchResult(
        userId: j['userId'] as String,
        name: j['name'] as String?,
        email: j['email'] as String?,
        phone: j['phone'] as String?,
        displayLabel: j['displayLabel'] as String? ?? '',
      );
}

class PendingInvitation {
  final int circleId;
  final int memberId;
  final String circleName;
  final String? description;
  final String currency;
  final double contributionAmount;
  final String startDate;
  final String organizerName;
  final int memberCount;
  final String? invitedAt;

  PendingInvitation({
    required this.circleId,
    required this.memberId,
    required this.circleName,
    this.description,
    required this.currency,
    required this.contributionAmount,
    required this.startDate,
    required this.organizerName,
    required this.memberCount,
    this.invitedAt,
  });

  factory PendingInvitation.fromJson(Map<String, dynamic> j) => PendingInvitation(
        circleId: j['circleId'] as int,
        memberId: j['memberId'] as int,
        circleName: j['circleName'] as String,
        description: j['description'] as String?,
        currency: j['currency'] as String,
        contributionAmount: (j['contributionAmount'] as num).toDouble(),
        startDate: j['startDate'] as String,
        organizerName: j['organizerName'] as String? ?? '',
        memberCount: j['memberCount'] as int,
        invitedAt: j['invitedAt'] as String?,
      );
}

class PaymentClaim {
  final int id;
  final int circleId;
  final String circleName;
  final int cycleId;
  final int sequenceNumber;
  final String dueDate;
  final int memberId;
  final String memberName;
  final double claimedAmount;
  final double expectedAmount;
  final String currency;
  final String status; // Pending/Approved/Rejected
  final String? note;
  final String? rejectionReason;
  final String submittedAt;
  final String? reviewedAt;
  final bool hasEvidence;
  final String? evidenceFileName;
  final String? evidenceContentType;

  PaymentClaim({
    required this.id,
    required this.circleId,
    required this.circleName,
    required this.cycleId,
    required this.sequenceNumber,
    required this.dueDate,
    required this.memberId,
    required this.memberName,
    required this.claimedAmount,
    required this.expectedAmount,
    required this.currency,
    required this.status,
    this.note,
    this.rejectionReason,
    required this.submittedAt,
    this.reviewedAt,
    required this.hasEvidence,
    this.evidenceFileName,
    this.evidenceContentType,
  });

  factory PaymentClaim.fromJson(Map<String, dynamic> j) => PaymentClaim(
        id: j['id'] as int,
        circleId: j['circleId'] as int,
        circleName: j['circleName'] as String? ?? '',
        cycleId: j['cycleId'] as int,
        sequenceNumber: j['sequenceNumber'] as int,
        dueDate: j['dueDate'] as String,
        memberId: j['memberId'] as int,
        memberName: j['memberName'] as String,
        claimedAmount: (j['claimedAmount'] as num).toDouble(),
        expectedAmount: (j['expectedAmount'] as num).toDouble(),
        currency: j['currency'] as String,
        status: j['status'] as String,
        note: j['note'] as String?,
        rejectionReason: j['rejectionReason'] as String?,
        submittedAt: j['submittedAt'] as String,
        reviewedAt: j['reviewedAt'] as String?,
        hasEvidence: j['hasEvidence'] as bool? ?? false,
        evidenceFileName: j['evidenceFileName'] as String?,
        evidenceContentType: j['evidenceContentType'] as String?,
      );
}

class AuthResult {
  final bool succeeded;
  final String? userId;
  final String? token;
  final String? expiresAt;
  final List<String> errors;

  AuthResult({
    required this.succeeded,
    this.userId,
    this.token,
    this.expiresAt,
    required this.errors,
  });

  factory AuthResult.fromJson(Map<String, dynamic> j) => AuthResult(
        succeeded: j['succeeded'] as bool? ?? false,
        userId: j['userId'] as String?,
        token: j['token'] as String?,
        expiresAt: j['expiresAt'] as String?,
        errors: (j['errors'] as List<dynamic>? ?? []).map((e) => e.toString()).toList(),
      );
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../widgets/app_scaffold.dart';
import '../../widgets/status_chips.dart';

/// Mirrors MyCirclesPage.tsx.
class MyCirclesScreen extends ConsumerWidget {
  const MyCirclesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final circlesAsync = ref.watch(circlesListProvider);

    return AppScaffold(
      title: context.t('circle.myCircles'),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/circles/new'),
        icon: const Icon(Icons.add),
        label: Text(context.t('circle.createCircle')),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.read(refreshTickProvider.notifier).state++,
        child: circlesAsync.when(
          loading: () => Center(child: Text(context.t('common.loading'))),
          error: (e, _) => Center(child: Text(context.t('common.error'))),
          data: (circles) {
            if (circles.isEmpty) {
              return ListView(
                padding: const EdgeInsets.all(32),
                children: [Center(child: Text(context.t('app.tagline')))],
              );
            }
            return ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: circles.length,
              separatorBuilder: (_, __) => const SizedBox(height: 10),
              itemBuilder: (context, i) {
                final circle = circles[i];
                final created = DateFormat.yMMMd(Localizations.localeOf(context).toString()).format(DateTime.parse(circle.createdAt));
                return Card(
                  child: InkWell(
                    borderRadius: BorderRadius.circular(12),
                    onTap: () => context.push('/circles/${circle.id}'),
                    child: Padding(
                      padding: const EdgeInsets.all(14),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Expanded(child: Text(circle.name, style: const TextStyle(fontWeight: FontWeight.w600))),
                              Chip(
                                label: Text(context.t('circle.${circle.status.toLowerCase()}'), style: const TextStyle(color: Colors.white, fontSize: 11)),
                                backgroundColor: circleStatusColor(circle.status),
                                visualDensity: VisualDensity.compact,
                              ),
                            ],
                          ),
                          Text(
                            '${context.t('circle.memberCount')}: ${circle.memberCount} · ${context.t('circle.perMemberAmount')}: ${circle.contributionAmount} ${circle.currency}',
                            style: Theme.of(context).textTheme.bodySmall,
                          ),
                          Text('${context.t('circle.organizer')}: ${circle.organizerName}', style: Theme.of(context).textTheme.bodySmall),
                          Text('${context.t('circle.createdAt')}: $created', style: Theme.of(context).textTheme.bodySmall),
                        ],
                      ),
                    ),
                  ),
                );
              },
            );
          },
        ),
      ),
    );
  }
}

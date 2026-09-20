import 'dart:io';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../api/circles_api.dart';
import '../l10n/app_localizations.dart';

/// Shows a token-authenticated evidence file the app has already downloaded as bytes.
///
/// The web app just opens a blob URL in a new tab; on mobile there's no browser tab to hand
/// the bytes to, so an image is rendered inline (the overwhelmingly common case — transfer
/// screenshots) and anything else (PDFs) is written to the app's temp directory and handed to
/// whatever the OS has registered for it. No extra package is pulled in for this: `dart:io`
/// covers the temp file and `url_launcher` (already a dependency) covers the hand-off.
Future<void> showEvidence(BuildContext context, EvidenceDownload download) async {
  if (download.bytes.isEmpty) {
    _toast(context, context.t('common.error'));
    return;
  }

  if (download.isImage) {
    if (!context.mounted) return;
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(download.fileName ?? dialogContext.t('circle.viewEvidence')),
        content: SingleChildScrollView(child: Image.memory(Uint8List.fromList(download.bytes))),
        actions: [
          TextButton(onPressed: () => Navigator.of(dialogContext).pop(), child: Text(dialogContext.t('common.close'))),
        ],
      ),
    );
    return;
  }

  try {
    final safeName = (download.fileName ?? 'evidence.pdf').replaceAll(RegExp(r'[^A-Za-z0-9._-]'), '_');
    final file = File('${Directory.systemTemp.path}${Platform.pathSeparator}$safeName');
    await file.writeAsBytes(download.bytes, flush: true);
    final opened = await launchUrl(Uri.file(file.path), mode: LaunchMode.externalApplication);
    if (!opened && context.mounted) _toast(context, context.t('common.error'));
  } catch (_) {
    if (context.mounted) _toast(context, context.t('common.error'));
  }
}

void _toast(BuildContext context, String message) {
  if (!context.mounted) return;
  ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
}

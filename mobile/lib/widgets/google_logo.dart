import 'package:flutter/material.dart';

/// The official multicolor Google "G" mark, traced from Google's 18x18 brand SVG so the
/// sign-in button reads as genuinely "Google" rather than a generic login icon — there's no
/// bundled asset for this in Flutter, and pulling a network image for a static brand mark
/// would be fragile (offline, rate limits) for something this small.
class GoogleLogo extends StatelessWidget {
  const GoogleLogo({super.key, this.size = 20});

  final double size;

  @override
  Widget build(BuildContext context) {
    return SizedBox(width: size, height: size, child: CustomPaint(painter: _GoogleLogoPainter()));
  }
}

class _GoogleLogoPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final scale = size.width / 18;
    canvas.save();
    canvas.scale(scale, scale);

    final blue = Paint()..color = const Color(0xFF4285F4);
    final green = Paint()..color = const Color(0xFF34A853);
    final yellow = Paint()..color = const Color(0xFFFBBC05);
    final red = Paint()..color = const Color(0xFFEA4335);

    final bluePath = Path()
      ..moveTo(17.64, 9.2045)
      ..cubicTo(17.64, 8.5664, 17.5827, 7.9527, 17.4764, 7.3636)
      ..lineTo(9, 7.3636)
      ..lineTo(9, 10.845)
      ..lineTo(13.8436, 10.845)
      ..cubicTo(13.635, 11.97, 13.0009, 12.9232, 12.0477, 13.5614)
      ..lineTo(12.0477, 15.8195)
      ..lineTo(14.9564, 15.8195)
      ..cubicTo(16.6582, 14.2527, 17.64, 11.9455, 17.64, 9.2045)
      ..close();

    final greenPath = Path()
      ..moveTo(9, 18)
      ..cubicTo(11.43, 18, 13.4673, 17.1945, 14.9564, 15.8195)
      ..lineTo(12.0477, 13.5614)
      ..cubicTo(11.2418, 14.1014, 10.2109, 14.4205, 9.0, 14.4205)
      ..cubicTo(6.656, 14.4205, 4.6718, 12.8374, 3.964, 10.7101)
      ..lineTo(0.9573, 10.7101)
      ..lineTo(0.9573, 13.0419)
      ..cubicTo(2.4382, 15.9832, 5.4818, 18.0, 9.0, 18.0)
      ..close();

    final yellowPath = Path()
      ..moveTo(3.964, 10.71)
      ..cubicTo(3.784, 10.17, 3.6818, 9.5932, 3.6818, 9.0)
      ..cubicTo(3.6818, 8.4068, 3.7841, 7.83, 3.9641, 7.29)
      ..lineTo(3.9641, 4.9582)
      ..lineTo(0.9573, 4.9582)
      ..arcToPoint(const Offset(0, 9), radius: const Radius.circular(8.9965))
      ..cubicTo(0, 10.4523, 0.3477, 11.8268, 0.9573, 13.0418)
      ..close();

    final redPath = Path()
      ..moveTo(9, 3.5795)
      ..cubicTo(10.3214, 3.5795, 11.5077, 4.0336, 12.4405, 4.9255)
      ..lineTo(15.0218, 2.3441)
      ..cubicTo(13.4632, 0.8918, 11.426, 0, 9, 0)
      ..cubicTo(5.4818, 0, 2.4382, 2.0168, 0.9573, 4.9582)
      ..lineTo(3.964, 7.29)
      ..cubicTo(4.6718, 5.1627, 6.656, 3.5795, 9, 3.5795)
      ..close();

    canvas.drawPath(bluePath, blue);
    canvas.drawPath(greenPath, green);
    canvas.drawPath(yellowPath, yellow);
    canvas.drawPath(redPath, red);
    canvas.restore();
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

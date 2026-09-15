# Dourak Mobile (Flutter, Android)

A native Android mirror of `frontend/` (React web app), talking to the same
ASP.NET Core backend (`backend/src/Dourak.Api`). See `docs/mobile-plan.md` for
the approved scope and `docs/progress.md` ("Mobile App (Flutter) — Progress")
for what was built, judgment calls, and what's deferred.

## Running

```bash
cd mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5210/api
```

- `10.0.2.2` is the Android emulator's alias for the host machine — use it
  when the backend runs on your dev machine via `docker compose up` /
  `dotnet run` (adjust the port to match your backend's actual listen port).
- On a physical device on the same network, use your machine's LAN IP instead
  of `10.0.2.2`, e.g. `http://192.168.1.50:5210/api`.
- Omitting `--dart-define=API_BASE_URL=...` falls back to the emulator address
  above (see `lib/api/api_client.dart`).

## Verifying

```bash
flutter analyze
flutter build apk --debug
```

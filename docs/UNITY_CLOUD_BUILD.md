# Black Alert: actual Unity APK building from a phone

## Readiness
Open Actions > **Android readiness** and inspect the latest run. It reports only whether activation settings exist, never their values.

## Secure Unity activation
Unity's cloud editor requires an activated Unity license. Use [GameCI's current activation guide](https://game.ci/docs/github/activation/). If you have Unity Personal, the standard documented process starts by activating a Personal license in Unity Hub on a computer and storing the generated license (.ulf) content in GitHub Actions secrets. For professional licenses, supply the secret trio UNITY_SERIAL, UNITY_EMAIL, UNITY_PASSWORD.

**Never paste a Unity license, password, signing key or player photo into GitHub source code, GitHub issues, or ChatGPT.** Use [GitHub Actions repository secrets](https://github.com/echallita3-sudo/Black-Alert/settings/secrets/actions) only.

## Build APK
Actions > **Android APK (Unity)** > Run workflow. A successful run must contain **BlackAlert-Android-SmokeTest** artifact. Download the ZIP from that run on your phone, extract the APK and install.

The produced APK is a development smoke test, not the visual-quality release. Real Android runtime and gameplay remain unverified until installation, screenshot and device logs.

## Android failures
Build failed: inspect the Unity Builder job logs in Actions. Installation failed: capture exact Android error and APK version/signature. Blank scene: capture logcat and screenshot. Two phones are required for multiplayer runtime tests.

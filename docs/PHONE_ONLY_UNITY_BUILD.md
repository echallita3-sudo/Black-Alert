# Build Black Alert using a phone (no local Unity Hub)

Use **Unity Build Automation (UBA)**, Unity's hosted builder. This avoids the GitHub GameCI Unity Personal .ulf license bootstrap.

## Phone-only setup

1. Open [Unity Cloud Dashboard](https://cloud.unity.com/) on Android Chrome, sign in to your Unity ID, and turn on **Desktop site** if the menus are hard to use.
2. Create/select a Unity Cloud project **Black Alert**. In Development/DevOps, select **Build Automation** and activate/onboard if requested. Check free quota and any payment requirements before enabling.
3. In Build Automation > Settings > Source Control, connect the Git repository: `https://github.com/echallita3-sudo/Black-Alert.git`. Current repo is public. The GitHub provider may request a personal access token; use the least privileges (Contents read-only for source; Webhooks write only for auto-build) and **never commit/share a token**. UBA also supports a manual Git connection.
4. Build Automation > Configurations > create target, choose **Android**, branch **main**, Unity version from [ProjectSettings/ProjectVersion.txt](../ProjectSettings/ProjectVersion.txt) if available (currently `6000.0.60f1`), repository root as project folder.
5. Configure **APK** (not AAB) for direct phone installation. Use **Auto-generated debug keystore (development only)** for tests; release signing needs a separately managed key.
6. First trial: Run/Build, download resulting APK on phone if successful. A first build may fail because this source has not been compiled by Unity; preserve the full job logs for fixes.
7. Keep GameCI GitHub Action as optional path if a compatible Unity Personal license file becomes available later.

## Important project-specific warning

The current GameCI build script lives in `Assets/Editor/AndroidBuilder.cs` and generates an empty bootstrap scene before invoking Unity's Android build. Unity Build Automation **may require its own build profile or custom build script configuration**, and the source may need fixes from its first true compilation. Do not call the source foundation a working release.

## Relevant official docs

- https://docs.unity.com/en-us/build-automation
- https://docs.unity.com/en-us/build-automation/get-started-with-build-automation/connect-your-version-control-system
- https://docs.unity.com/en-us/build-automation/basic-build-configuration/overview
- https://docs.unity.com/en-us/build-automation/sign-build-artifacts/sign-an-android-application

## Security

Never paste Unity account passwords, license contents, GitHub tokens, or signing key passwords into issues, commits, chat, or source files. Enter provider credentials only in official login/secret screens. Watch free quota / build minutes; some account/region/organization settings may require extra activation.

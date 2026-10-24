# BLACK ALERT — native Unity 3D RTS

An original 3D mobile RTS targeting the approved high-end isometric military-scifi gameplay visuals.

**Current release gate: source scaffold only. This is not a finished, Android-tested game.**

## Open in Unity
Open the repository root as a project in Unity Hub (Unity 6 version in ProjectSettings/ProjectVersion.txt). Android build support, SDK/NDK and OpenJDK are required to export APKs.

The first bootstrap creates a simple interactive 3D skirmish for compile and input testing. These simple generated models are temporary engineering geometry, NOT the approved detailed art target.

## GitHub Actions
- Source sanity: automatically checks presence of critical files.
- Android APK: manually triggered. The workflow needs a valid authorized Unity license through Actions secrets. Build output, once successful, appears under a workflow run's Artifacts.
- Never upload licensing credentials, Android signing keys, or private photos into this public repository.

See [BLACK_ALERT_REQUIREMENTS.md](BLACK_ALERT_REQUIREMENTS.md) and [STATUS.md](STATUS.md).

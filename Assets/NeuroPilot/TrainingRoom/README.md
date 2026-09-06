# Imported TrainingRoom

Source: user-provided Unity project `D:\workspace\11`, scene `Assets/Scenes/TrainingRoom.unity` (GUID preserved).

Only runtime scripts, the target scene and SessionConfig_Default were imported. Source Editor auto-build scripts and project/package/XR settings were deliberately excluded to preserve the main application's configuration. The source folder is unchanged.

Integration changes: URP room materials, main project XR rig, one-time tracked-view alignment, Chinese world-space HUD, Input System keyboard plus XR trigger/grip simulation, selected difficulty display, and navigation routing. The newer source depth range (3.5–8.5 m) is imported; other stimulus parameters stay unchanged. Difficulty-specific stimulus settings and real EEG acquisition are not implemented.

Use `NeuroPilot > Training Room > Integrate Imported Scene` only when rebuilding the adaptation; it saves scenes and build settings. Start regular testing from NeuroPilotNavigation. See the repository root guide `1.1场景接入与验收.md`.

Version 1.1.2 captures a FIXED forward training frame on entry. Later head turns do not move the field or redirect new targets. World-space HUD adds three stats cards, a ready countdown and a result card. Controllers/rays remain visible. TrainingEegPort reserves endpoint settings and JSON hooks, not transport or classification. Default input remains simulated. White TrainingRoom is the sole supported training scene. See `1.1.2白色房间更新与脑电接口.md` for update steps and limits.

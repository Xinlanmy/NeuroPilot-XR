# Imported TrainingRoom

Source: user-provided Unity project `D:\workspace\11`, scene `Assets/Scenes/TrainingRoom.unity` (GUID preserved).

Only runtime scripts, the target scene and SessionConfig_Default were imported. Source Editor auto-build scripts and project/package/XR settings were deliberately excluded to preserve the main application's configuration. The source folder is unchanged.

Integration changes: URP room materials, main project XR rig, one-time tracked-view alignment, readable Chinese world-space HUD, Input System keyboard plus XR trigger/grip simulation, selected difficulty display, and navigation build routing. Stimulus configuration asset remains byte-identical to the source. Difficulty-specific stimulus settings and real EEG acquisition are not implemented.

Use `NeuroPilot > Training Room > Integrate Imported Scene` only when rebuilding the adaptation; it saves scenes and build settings. Start regular testing from NeuroPilotNavigation. See the repository root guide `1.1场景接入与验收.md`.

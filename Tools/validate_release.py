from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8-sig")

def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit("FAIL: " + message)
    print("PASS:", message)

project = read("ProjectSettings/ProjectSettings.asset")
require(re.search(r"^  bundleVersion: 2\.3\.0$", project, re.M) is not None, "bundle version is 2.3.0")
require(re.search(r"^  productName: NeuroPilot XR 2\.3\.0$", project, re.M) is not None,
        "product name is NeuroPilot XR 2.3.0")
require(re.search(r"^  AndroidBundleVersionCode: 13$", project, re.M) is not None,
        "Android versionCode is 13")
require(re.search(r"scriptingBackend:\s+Android: 1", project) is not None, "Android uses IL2CPP")
require("AndroidTargetArchitectures: 2" in project, "Android is ARM64-only")

single = read("Assets/NeuroPilot/TrainingRoom/Config/SessionConfig_Default.asset")
pool = read("Assets/NeuroPilot/TrainingRoom/Config/MultiFrequencyPool.asset")
require("flickerHz: 12" in single, "single-target SSVEP is 12 Hz")
require(all(f"- {hz}" in pool for hz in (12, 10, 8)), "multi-target SSVEP is 12/10/8 Hz")

bridge_guid = "6f4c2a91d7e34b58a0c9e12f83b7d604"
for scene in ("TrainingRoom", "MultiTargetRoom"):
    data = read(f"Assets/NeuroPilot/TrainingRoom/Scenes/{scene}.unity")
    require(data.count(bridge_guid) == 1, f"{scene} contains exactly one FusionEegBridge")

# L2 门控子集闪烁：多球房 = 眼动门控 + 脑电确认（参数权威值在场景，TOML [ssvep.gate] 是镜像）
# 2.2.1 起为视线区域成员制：dwell 0.1s，区域 = 球心到视线射线 ≤ gazeRegionRadiusMeters（1.2m）
multi_scene = read("Assets/NeuroPilot/TrainingRoom/Scenes/MultiTargetRoom.unity")
require(multi_scene.count("8279a339474823c4ab7c28c388d822ed") == 1,
        "MultiTargetRoom wires exactly one GazeSubsetGate")
require(multi_scene.count("0861f66a787993648a87b849af93316e") == 1,
        "MultiTargetRoom keeps the eye gaze provider for gating")
require(all(value in multi_scene for value in ("policy: 0", "dwellSeconds: 0.1",
                                               "leaveHysteresisSeconds: 0.5", "rampSeconds: 0.4",
                                               "maxSimultaneous: 3", "rearmSeconds: 6",
                                               "gazeRegionRadiusMeters: 1.2")),
        "gate parameters match the L2 contract")
require("neighborRadiusMeters" not in multi_scene and "maxViewAngleDeg" not in multi_scene,
        "obsolete anchor-radius / view-angle gate params removed from scene")
require(all(value in multi_scene for value in ("multiTargetCount: 6", "multiSpawnHalfWidth: 1.9",
                                               "multiSpawnDepth: 4.1", "multiMinSpacing: 0.9")),
        "six targets with a bounded non-overlapping respawn region")
gate = read("Assets/NeuroPilot/Scripts/GazeSubsetGate.cs")
require("TryAssign" in read("Assets/NeuroPilot/Scripts/MultiFrequencyConfig.cs") and
        "TakenFrequencies" in read("Assets/NeuroPilot/Scripts/SsvepTargetGroup.cs") and
        "StepGaze" in gate and "StepAlwaysOn" in gate,
        "gating uses the shared max-min allocator and the always-on degradation path")

nav = read("Assets/NeuroPilot/Scenes/NeuroPilotNavigation.unity")
require("m_Name: TestConnButton" in nav and "m_MethodName: TestConnection" in nav,
        "settings scene binds the connection test button")
require("m_AnchoredPosition: {x: -400, y: -365}" in nav and
        "m_AnchoredPosition: {x: 0, y: -365}" in nav and
        "m_AnchoredPosition: {x: 400, y: -365}" in nav,
        "settings action buttons use non-overlapping positions")

drag = read("Assets/NeuroPilot/Scripts/SpatialWindowDragController.cs")
require("private void LateUpdate()" in drag and "TryCalculateFacingRotation" in drag and
        "grabInteractable.trackRotation = false" in drag,
        "dragged navigation window stays upright and faces the viewer")

gaze = read("Assets/NeuroPilot/Scripts/EyeGazeProvider.cs")
require("TryOpenXrEyeGaze" in gaze and "TryViveEyeTracker" in gaze and "ConvertVivePose" in gaze,
        "eye tracking uses OpenXR with a converted VIVE fallback")
eye_scene = read("Assets/NeuroPilot/TrainingRoom/Scenes/EyeTrackingRoom.unity")
require(all(value in eye_scene for value in ("dwellSeconds: 0.8", "eyeTargetDiameter: 0.24",
                                             "eyeHitDiameter: 0.36")),
        "eye scene uses 0.8-second fixation and a smaller visible target")

fusion = read("Assets/NeuroPilot/Training/FusionEegBridge.cs")
telemetry = read("Assets/NeuroPilot/Training/VrTelemetryPanel.cs")
require(all(event in fusion for event in ("attention_update", "fatigue_update", "cognitive_profile")) and
        "ResultSummary" in telemetry,
        "attention, fatigue and cognitive profile telemetry are connected")
# 2.3.0 起训练中不再显示实时遥测（悬浮读数挡在靶区里），数值只进居中结算卡。
require("TextMesh" not in telemetry and "Canvas" not in telemetry and
        "ResultSummary" in read("Assets/NeuroPilot/TrainingRoom/Scripts/Core/SessionManager.cs"),
        "live in-VR telemetry stays retired; the summary only surfaces in the result card")

# 房间按钮曾引用 ButtonGradient 里已被重建掉的子资产 fileID，渲染成纯白块（白字不可见）。
# 贴图必须来自真实子资产：房间按钮的 sprite 引用不得再出现那个悬空 fileID。
for scene in ("TrainingRoom", "EyeTrackingRoom", "MultiTargetRoom"):
    room = read(f"Assets/NeuroPilot/TrainingRoom/Scenes/{scene}.unity")
    require("7952024925285346104" not in room,
            f"{scene} has no dangling ButtonGradient sprite reference")
    require("guid: eebf8f221d9d5fc4e8a65291355c5797" in room,
            f"{scene} buttons reference the shared ButtonGradient sprite asset")

openxr = read("Assets/XR/Settings/OpenXR Package Settings.asset")
for name in ("VIVEFocus3Profile Android", "VIVEFocus3Feature Android", "ViveEyeTracker Android"):
    block = re.search(rf"m_Name: {re.escape(name)}.*?(?=--- !u!|\Z)", openxr, re.S)
    require(block is not None and "m_enabled: 1" in block.group(0), name + " is enabled")

print("Release contract validation passed.")

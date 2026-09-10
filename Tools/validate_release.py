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
require("bundleVersion: 2.0" in project, "bundle version is 2.0")
require("productName: NeuroPilot XR 2.0" in project, "product name is NeuroPilot XR 2.0")
require("AndroidBundleVersionCode: 7" in project, "Android versionCode is 7")
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

nav = read("Assets/NeuroPilot/Scenes/NeuroPilotNavigation.unity")
require("m_Name: TestConnButton" in nav and "m_MethodName: TestConnection" in nav,
        "settings scene binds the connection test button")
require("m_AnchoredPosition: {x: -400, y: -365}" in nav and
        "m_AnchoredPosition: {x: 0, y: -365}" in nav and
        "m_AnchoredPosition: {x: 400, y: -365}" in nav,
        "settings action buttons use non-overlapping positions")

openxr = read("Assets/XR/Settings/OpenXR Package Settings.asset")
for name in ("VIVEFocus3Profile Android", "VIVEFocus3Feature Android", "ViveEyeTracker Android"):
    block = re.search(rf"m_Name: {re.escape(name)}.*?(?=--- !u!|\Z)", openxr, re.S)
    require(block is not None and "m_enabled: 1" in block.group(0), name + " is enabled")

print("Release contract validation passed.")

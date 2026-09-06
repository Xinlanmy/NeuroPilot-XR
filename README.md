# NeuroPilot-XR

基于 Unity 开发的注意力训练场景。目标 VR 设备为 VIVE Focus Vision，脑机设备为博瑞康。当前版本 **1.1.2**，以后以白色 TrainingRoom 为训练主线；真实脑机数据尚未接入。

## 当前功能

- MR Passthrough 真实环境上的空间窗口。
- 欢迎页 → 三档训练强度选择 → 倒计时 → TrainingRoom 白色训练房间。
- 左右手射线点击；指向顶部并按住食指扳机，可在空间中拖动窗口，松手后固定。
- 每次启动及从后台返回入口，按当前头显位置重新摆放到正前方约 1.5 米，中心低于眼睛约 10 厘米；正常转头时窗口保持世界空间位置。
- 默认选择“标准”，并将所选难度传递到训练场景。
- 训练房间支持闪烁球、180 秒计时、命中/漏失、结算和重新开始。扳机或空格为模拟命中；结算后侧握键或 R 重开。
- 三档强度当前只保留选择与显示，均沿用原场景配置，没有擅自改动刺激参数。

## 打开项目

1. 克隆或下载本仓库。
2. 在 Unity Hub 中添加仓库根目录（包含 `Assets`、`Packages`、`ProjectSettings` 的目录）。
3. 使用 **Unity 2022.3.62f1c1** 打开；版本信息见 `ProjectSettings/ProjectVersion.txt`。Android 构建需要 Android Build Support、SDK、NDK 和 OpenJDK。
4. 等待包和资源导入，打开 `Assets/NeuroPilot/Scenes/NeuroPilotNavigation.unity`。
5. 普通 Editor 可预览界面；真实 MR 透视及佩戴体验需要在 VIVE Focus Vision 上确认。

电脑上的 SDK 安装路径因人而异，请在 Unity `Preferences > External Tools` 中使用自己的配置。教程中的 `D:\workspace\NeuroPilotXR` 是开发机示例路径，其他电脑请替换为本地克隆路径。

## 下载与文档

- [1.1.2 正前方出球与 EEG 接口预留版](https://github.com/Xinlanmy/NeuroPilot-XR/releases/tag/v1.1.2)
- [1.1.2 队友更新教程、接口约定与验收](1.1.2白色房间更新与脑电接口.md)
- [1.1.1 TrainingRoom 修复安装包](https://github.com/Xinlanmy/NeuroPilot-XR/releases/tag/v1.1.1)
- [1.1 场景接入与安装教程](1.1场景接入与验收.md)
- [新手操作教程](新手操作教程.md)
- [拖动与启动定位修复验收](拖动与启动定位修复验收.md)

APK 包名为 `com.neuropilot.xr`。1.1.2 是开发测试构建，versionCode 为 `5`；可覆盖安装之前的同签名测试版本。

## 目录

| 目录 | 内容 |
| --- | --- |
| `Assets/NeuroPilot/Scenes` | 入口导航；旧 SpaceTraining 历史资产已停用、不再回退 |
| `Assets/NeuroPilot/TrainingRoom` | 从用户提供的项目导入并适配的训练场景、配置、脚本和 URP 材质 |
| `Assets/NeuroPilot/Scripts` | 窗口拖动、启动定位、页面切换、难度选择与 MR 转场 |
| `Assets/NeuroPilot/Editor` | 场景生成/修复工具与编辑器交互回归工具 |
| `Assets/NeuroPilot/Art`、`Fonts` | UI 资源、中文字体与字体许可 |
| `Assets/NeuroPilot/Previews` | 开发过程预览截图，真机效果以头显为准 |
| `Packages` | 依赖清单、锁文件及内嵌 VIVE OpenXR / Unity MCP 包 |
| `ProjectSettings` | Unity、Android、OpenXR 和渲染设置 |

Unity 缓存、日志、本地备份和构建产物不进入源码历史。最新 APK 作为 GitHub Release 附件提供。

## 依赖与验证

主要依赖：URP 14.0.12、XR Interaction Toolkit 2.5.4、OpenXR 1.12.1、Input System 1.7.0、XR Hands 1.4.1、VIVE OpenXR 2.5.1、MCP for Unity 10.0.0。完整版本以 `Packages/manifest.json` 和锁文件为准。

1.0.1 已通过编译、Android APK 构建、左右手模拟输入拖动/松手/按钮点击，以及启动与恢复定位验证。正式头显手感、暂停恢复与 MR 合成仍需真机验收。当前 SDK 组合有 Android 15 16 KB 对齐相关警告，商店发布前需要单独处理。

1.1.1 已通过编辑器端到端验证：入口按钮转场、唯一摄像机/XR Origin、强度传递、出球、键盘模拟命中、漏失、结算与重开。为避免与旧 APK 混淆，头显应用名显示为 `NeuroPilot XR 1.1.1`，入口显示 `v1.1.1 TRAININGROOM`。训练/结算预览及验收步骤见 [1.1 场景接入与验收](1.1场景接入与验收.md)。

1.1.2 的训练场景为 `Assets/NeuroPilot/TrainingRoom/Scenes/TrainingRoom.unity`。旧 `SpaceTraining` 仅作历史记录，不参与构建，工具也不再回退到它。新球在入场确定的固定前方靶区逐个出现，转头不会移动靶区。采用更新材料的 3.5–8.5 米深度，其余刺激参数不变。顶部三块统计、中央准备倒计时和统一结算卡参考 Aim Lab 的简洁布局；手柄与射线保留。默认仍为手柄/键盘模拟；`TrainingEegPort` 预留 8765 端口、启停事件和目标编号校验，但没有网络传输和真实 EEG 识别。完整接入及训练架构改动须先确认，详见新版教程。

内嵌第三方组件和字体遵循各自目录中的许可文件；本仓库不变更这些许可。
详情见 [第三方组件说明](THIRD_PARTY_NOTICES.md)。

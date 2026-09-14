# NeuroPilot-XR

基于 Unity 开发的注意力训练场景。目标 VR 设备为 VIVE Focus Vision，脑机设备为博瑞康。当前工程为 **2.3.0 测试版**，以白色房间为基础；Unity 与算法服务的 WebSocket 桥已接入，真实 EEG 分类器仍需现场联调。

## 当前功能

- MR Passthrough 真实环境上的空间窗口。
- 欢迎页 → 三个独立模式：视线追踪、单球追踪、多球定位；每级设置页有返回按钮，房间内可返回模式选择。
- 首页右上角“设置”可通过手柄数字键盘修改 WSL 通信 IP，默认 `192.168.1.131:8765`。保存后单球/多球共用，重启后保留；地址已保存不等于脑电服务已连接。
- 视线追踪：同时出现 4/5 个彩球，可见球径 0.24 米、眼动判定直径 0.36 米；累计注视蓝球 0.8 秒后闪光消去并播放奖励音。短暂眨眼由宽容时间吸收，持续移开视线会逐渐失去进度。优先使用 OpenXR 眼动，无法取样时回退到 VIVE HTC 眼动接口；VIVE 姿态会转换到 Unity 坐标系。该模式不接 EEG。
- 单球追踪：保留原强度页和 TrainingRoom，一个 SSVEP 球，仅由脑电确认消息命中；扳机/空格不再模拟命中。
- 多球定位（L2 门控子集闪烁，2.2 起）：同屏 6 颗球，初始与复现都在限定区域内随机布点（不重叠、不出界）；**注视任一球 0.1 秒**，以视线为中心 1.2 米半径内的球（有几颗闪几颗、最多 3 颗，注视球必在内）开始闪烁，各自从 12/10/8 Hz 取一个互不相同的频率（随起闪事件下发，不再绑定槽位）；只由脑电确认目标，注视不参与得分。视线离开 0.5 秒后停闪，在闪超时未命中会原地换新目标 ID 重开判定窗。**眼动无效 3 秒自动转"齐闪"备用模式**，恢复后自动切回；场景内把 `GazeSubsetGate.policy` 设为 `AlwaysOn` 可定死为纯齐闪（= 2.1 行为）。详见 [2.2 门控子集闪烁说明](2.2门控子集闪烁说明.md)。
- 三个模式的成功反馈：小球散成星形碎片消失，播放约 0.38 秒奖励音。单球和多球通过 WebSocket 接收算法侧确认，不会在未连接分类器时自动产生命中。
- 单球和多球接收算法侧 `attention_update`、`fatigue_update`、`cognitive_profile`。**2.3 起训练中不再显示实时读数**（原来的头锁浮字只有约 1.6 厘米高、几乎读不出来，还压在靶区上），注意力均值与低注意持续只在居中结算卡里给出。
- 2.3 视觉统一：三个房间的 HUD 卡片改成与导航面板同款（`rounded` 圆角底 + 深蓝卡片色 + 青色描边），房间内的“返回模式选择 / 重新训练”按钮修复了贴图引用悬空导致渲染成白块的问题（房间按钮改为每轮重新解析贴图，与导航一致）；训练房启用主光软阴影，天花板与四壁不投影、只有练习球落地投影（闭合盒子若整壳投影会把房间压黑）；SSVEP 目标球改为无镜面高光的磨砂材质。**刺激的明暗颜色、频率、占空比与时相一律未改。**
- 左右手射线点击；指向顶部并按住食指扳机，可在空间中拖动窗口。拖动过程中窗口保持竖直并持续正面朝向头显，松手后固定最终位置和朝向。
- 每次启动及从后台返回入口，按当前头显位置重新摆放到正前方约 1.5 米，中心低于眼睛约 10 厘米；正常转头时窗口保持世界空间位置。
- 默认选择“标准”，并将所选难度传递到训练场景。
- 训练房间支持 180 秒计时、统计、结算和重新开始。单球结算后侧握键或 R 重开；新模式使用“重新训练”按钮。
- 三档强度当前只保留选择与显示，均沿用原场景配置，没有擅自改动刺激参数。

## 打开项目

1. 克隆或下载本仓库。
2. 在 Unity Hub 中添加仓库根目录（包含 `Assets`、`Packages`、`ProjectSettings` 的目录）。
3. 使用 **Unity 2022.3.62f1c1** 打开；版本信息见 `ProjectSettings/ProjectVersion.txt`。Android 构建需要 Android Build Support、SDK、NDK 和 OpenJDK。
4. 等待包和资源导入，打开 `Assets/NeuroPilot/Scenes/NeuroPilotNavigation.unity`。
5. 普通 Editor 可预览界面；真实 MR 透视及佩戴体验需要在 VIVE Focus Vision 上确认。

电脑上的 SDK 安装路径因人而异，请在 Unity `Preferences > External Tools` 中使用自己的配置。教程中的 `D:\workspace\NeuroPilotXR` 是开发机示例路径，其他电脑请替换为本地克隆路径。

## 下载与文档

- [VIVE USB 串流驱动与安装说明（Windows）](Tools/Drivers/VIVE/README.md)
- [2.2 门控子集闪烁说明](2.2门控子集闪烁说明.md)
- [2.1 三模式使用、眼动、脑电协议与验收说明](2.0三模式与反馈说明.md)
- 2.3.0 本地 APK 输出：`Builds/Android/NeuroPilotXR_2.3.0.apk`（versionCode 13），改动见 [2.3.0 发布说明](ReleaseNotes_v2.3.0.md)；2.2.1 输出 `Builds/Android/NeuroPilotXR_2.2.1.apk`（versionCode 12）。
- [1.1.2 正前方出球与 EEG 接口预留版](https://github.com/Xinlanmy/NeuroPilot-XR/releases/tag/v1.1.2)
- [1.1.2 队友更新教程、接口约定与验收](1.1.2白色房间更新与脑电接口.md)
- [1.1.1 TrainingRoom 修复安装包](https://github.com/Xinlanmy/NeuroPilot-XR/releases/tag/v1.1.1)
- [1.1 场景接入与安装教程](1.1场景接入与验收.md)
- [新手操作教程](新手操作教程.md)
- [拖动与启动定位修复验收](拖动与启动定位修复验收.md)

APK 包名为 `com.neuropilot.xr`。2.3.0 测试构建的 versionCode 为 `13`；同签名时可覆盖安装之前的测试版本。

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
| `Tools/Drivers/VIVE` | Windows PC 端 VIVE USB 串流驱动原包、安装说明与 SHA-256 校验值 |

Unity 缓存、日志、本地备份和构建产物不进入源码历史。最新 APK 作为 GitHub Release 附件提供。

## 依赖与验证

主要依赖：URP 14.0.12、XR Interaction Toolkit 2.5.4、OpenXR 1.12.1、Input System 1.7.0、XR Hands 1.4.1、VIVE OpenXR 2.5.1、MCP for Unity 10.0.0。完整版本以 `Packages/manifest.json` 和锁文件为准。

2.3.0 为视觉统一版：房间 HUD 卡片与导航面板同款、房间按钮修复贴图悬空白块、训练房启用主光软阴影（外壳不投影）、SSVEP 目标球改磨砂、训练中移除实时遥测浮字（只进结算卡）。改动不受编辑器约束的部分已过 `python Tools/validate_release.py`；**三个房间的场景重存、EditMode 契约测试、三模式验证与出包仍需在编辑器里跑一次确认**，真机观感（阴影、按钮、结算卡）待头显验收。详见 [2.3.0 发布说明](ReleaseNotes_v2.3.0.md)。2.2.1 已通过 Unity 编译与发布契约校验；三模式软件验证脚本覆盖 L2 门控断言（注视前不闪、成员制区域与频率、迟滞、抢占、再武装、降级）。2.2.0 已通过 Unity 编译、EditMode 发布契约测试、三模式软件验证、Android ARM64 IL2CPP 构建及 APK 签名与清单检查，并已在 VIVE Focus Vision 真机上安装进入场景。正式头显眼动源切换与精度、实际闪烁频率和 EEG 硬件闭环仍需真机验收。**2.2 的门控玩法依赖串流下眼动可用；眼动不可用时自动降级齐闪，真机门控验收尚未完成。** 当前 VIVE OpenXR 原生库有 Android 15 16 KB 对齐相关警告，商店发布前需要随 SDK 升级处理。

1.1.1 已通过编辑器端到端验证：入口按钮转场、唯一摄像机/XR Origin、强度传递、出球、键盘模拟命中、漏失、结算与重开。为避免与旧 APK 混淆，头显应用名显示为 `NeuroPilot XR 1.1.1`，入口显示 `v1.1.1 TRAININGROOM`。训练/结算预览及验收步骤见 [1.1 场景接入与验收](1.1场景接入与验收.md)。

1.1.2 的训练场景为 `Assets/NeuroPilot/TrainingRoom/Scenes/TrainingRoom.unity`。旧 `SpaceTraining` 仅作历史记录，不参与构建，工具也不再回退到它。新球在入场确定的固定前方靶区逐个出现，转头不会移动靶区。采用更新材料的 3.5–8.5 米深度，其余刺激参数不变。顶部三块统计、中央准备倒计时和统一结算卡参考 Aim Lab 的简洁布局；手柄与射线保留。默认仍为手柄/键盘模拟；`TrainingEegPort` 预留 8765 端口、启停事件和目标编号校验，但没有网络传输和真实 EEG 识别。完整接入及训练架构改动须先确认，详见新版教程。

内嵌第三方组件和字体遵循各自目录中的许可文件；本仓库不变更这些许可。
详情见 [第三方组件说明](THIRD_PARTY_NOTICES.md)。

# NeuroPilot XR 2.3.0 发布说明

版本 `2.3.0`，Android `versionCode 13`，出包 `Builds/Android/NeuroPilotXR_2.3.0.apk`。

本轮是**纯视觉统一版**：不改玩法、不改命中判据、不改门控参数、不改 SSVEP 刺激的明暗颜色/频率/占空比/时相。

## 修掉的问题

### 1. 房间按钮渲染成白块（三个房间共 5 处）

`Art/ButtonGradient.asset` 里真正的 Sprite 子资产 fileID 是 `184831021838468897`，但三个训练房里的按钮引用的是 `7952024925285346104` —— 该子资产在资产里已经不存在。引用解析失败后 `Image` 回退成默认白色 1×1，白字压在白底上完全不可见。

| 场景 | 悬空引用 | 对象 |
| --- | --- | --- |
| `TrainingRoom.unity` | 1 | `ReturnToModesButton` |
| `EyeTrackingRoom.unity` | 2 | `ReturnToModesButton`、`RestartPracticeButton` |
| `MultiTargetRoom.unity` | 2 | 同上 |

**根因**：导航页每次都整体销毁重建，按钮拿到的都是当前 fileID；房间按钮却被 `if (Find(...) == null)` 守卫成"只在首次创建一次"，贴图资产被项目生成器重建过之后就永久悬空。本版把房间按钮的贴图改成每轮重新解析，并让 `ThreeModeSetup.PresentRoom` 对**三个房间**都跑一遍统一呈现，消除"拷贝只在复制那一刻继承一次"的结构性漂移。`RestartPracticeButton` 的禁用态颜色也改成可读（原来是 0.784/α0.5，字糊在底上）。

### 2. 训练房的实时阴影：试过，已回退（性能取舍，不是回归）

Key Light 的 `m_Shadows.m_Type` 是 `0`（三间房都是），小球因此悬空、少一层深度线索。本版一度打开主光软阴影，并把阴影预算从 50 米收到 20 米、天花板与四壁设为只接收不投影（房间是闭合盒子，整壳投影会让天花板把内部压黑 —— 这正是当初关掉阴影的原因）。

**真机实测不可接受，已回退**：

| 观测 | 读数 | 来源 |
| --- | --- | --- |
| 开阴影：三个训练房 | `FPS=4.0/0.0`，`GpuBd=1`（GPU 受限），运行时反复打 `RENDER_ATWC` 错过显示截止时间 | HTC `VRMetricXR` |
| 同机对照：未改的导航场景 | `FPS=98~108/120`，`GpuBd=0` | 同上 |
| 主观 | 转头呈"幻灯片"，一顿一顿 | 头显实测 |
| 2.2.1 同房间 | 顺滑，无此现象 | 头显实测 |

结论：**保持阴影关闭**。`TrainingRoomPresentation.Prepare` 现在显式把 Key Light 置 `LightShadows.None`、并把外壳投射标记恢复成默认，防止以后再漂移回开启状态。小球没有落地投影是这个取舍的代价 —— 如果以后要拿回深度线索，可行的方向是硬阴影 + 512 阴影贴图，并且必须先测 `VRMetricXR` 的 `FPS/GpuBd` 再决定，不要凭观感开。

### 3. 训练中挂着头锁遥测浮字

`VrTelemetryPanel` 用 TextMesh、`characterSize 0.008` × fontSize 20，字高约 1.6 厘米，在 1.55 米处只有约 0.6° 视角，既读不出来又压在靶区上。

本版**移除训练中的所有实时遥测显示**，`VrTelemetryPanel` 只保留汇总职责：`attention_update` / `fatigue_update` / `cognitive_profile` 继续接收，平均注意力与低注意持续改为在**居中结算卡**里给出（无有效注意力样本时不出这一行，不编数字）。

### 4. HUD 与导航的质感不一致

导航是深蓝玻璃卡 + 青色描边，房间 HUD 是纯色实心圆角矩形。本版把统计卡、底部提示条、准备卡、结算卡统一成导航同款：`rounded` 圆角底 + 卡片色 `(0.06, 0.13, 0.26, 0.94)` + 青色描边 `(0.08, 0.6, 1, 0.85)`。强调色条、`Muted`、`Accent` 三个既有 token 未动。

## 目标球（不干扰 SSVEP）

**一个字都没改**：`onColor` 白 / `offColor` `#121212`、单目标 `flickerHz 12`、多目标频率池 `12 / 10 / 8 Hz`、`dutyCycle 0.5`、`FrequencyStimulus` 的灰白 lerp、时相与波形。`Tools/validate_release.py` 与 `ReleaseContractTests.SsvepFrequenciesMatchFbccaContract` 会逐项核对。

改的只有镜面：`_Smoothness` `0.4 → 0.12`、关闭 `_SpecularHighlights`（`_SPECULARHIGHLIGHTS_OFF`），`_BaseColor (.04, .8, .95)` 不变。理由不是口味：球上那团高光亮度恒定、不随闪烁变化，会稀释亮/暗态的调制深度；去掉后整个球面亮度均匀调制，闪烁对比更干净，观感也从"塑料球带一颗亮斑"变成均匀磨砂。眼动房的彩球不参与 SSVEP，本轮只做同样的磨砂统一。

## 验证状态

已在 Unity 2022.3.62f1 batchmode 下实测通过：

- `python Tools/validate_release.py` 31/31 全绿（含新增两条：训练中不得再有实时遥测渲染；三个房间不得再出现悬空的 ButtonGradient 引用、必须引用真实贴图资产）。
- Unity 编译 0 错误。
- `TrainingRoomIntegration.Prepare` 重存三个房间与导航场景，改动落到场景 YAML：三个房间一致为 6 张玻璃卡、按钮贴图指向真实子资产、Key Light `m_Shadows.m_Type: 0`（阴影保持关闭）。
- EditMode 发布契约测试 11/11 通过（含改写后的遥测用例）。
- Play mode `TrainingRoomVerification` 全绿，预览图重出为 `Previews/TrainingRoom{,_Ready,_Result}_v2.3.0.png`。
- Play mode `ThreeModeVerification`（L2 门控全部断言）全绿。

顺带修掉两个**验证器自身**的缺陷（与本轮视觉改动无关，两者都在未改动的 `origin/main` 上复现）：

- `TrainingRoomVerification` 第 109 行要求 `modeText` 含"挑战"、第 110 行又要求它为空且隐藏，两条断言互斥 —— 自 2.2 移除场景内难度标签后该脚本不可能通过。
- `ThreeModeVerification` 的门控探针在 `Physics.SyncTransforms()` 之前发射线，打不到本帧刚布好的球，锚点恒为 `null`、误报"一颗都没闪"（显示为 `expected N, got 0`）。补一次物理同步后全绿；**门控自身逻辑一个字未改**。

仍需头显确认：

1. 出包 `Builds/Android/NeuroPilotXR_2.3.0.apk` 并 `adb install -r`。
2. 四件事：阴影落地且房间没有变黑、按钮是蓝底白字可读、训练中视野干净无浮字、结算卡里有平均注意力/低注意持续。
3. 若磨砂球观感不如原版，回退只是一个 `_Smoothness` 常量加一个 keyword。

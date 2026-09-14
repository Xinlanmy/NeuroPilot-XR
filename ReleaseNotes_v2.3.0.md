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

### 2. 训练房没有任何实时阴影

Key Light 的 `m_Shadows.m_Type` 是 `0`（三间房都是），小球因此悬空，而多球定位本来就在考深度判断。

本版打开主光软阴影，并把阴影预算从 50 米收到 20 米（房间只有 12 米深）。**关键配套**：天花板与四壁的 `shadowCastingMode` 设为 `Off`、只保留接收阴影 —— 房间是闭合盒子，整壳投影会让天花板把整个内部压黑，这正是当初把阴影关掉的原因。地板不投影，只有练习球投射。

### 3. 训练中挂着头锁遥测浮字

`VrTelemetryPanel` 用 TextMesh、`characterSize 0.008` × fontSize 20，字高约 1.6 厘米，在 1.55 米处只有约 0.6° 视角，既读不出来又压在靶区上。

本版**移除训练中的所有实时遥测显示**，`VrTelemetryPanel` 只保留汇总职责：`attention_update` / `fatigue_update` / `cognitive_profile` 继续接收，平均注意力与低注意持续改为在**居中结算卡**里给出（无有效注意力样本时不出这一行，不编数字）。

### 4. HUD 与导航的质感不一致

导航是深蓝玻璃卡 + 青色描边，房间 HUD 是纯色实心圆角矩形。本版把统计卡、底部提示条、准备卡、结算卡统一成导航同款：`rounded` 圆角底 + 卡片色 `(0.06, 0.13, 0.26, 0.94)` + 青色描边 `(0.08, 0.6, 1, 0.85)`。强调色条、`Muted`、`Accent` 三个既有 token 未动。

## 目标球（不干扰 SSVEP）

**一个字都没改**：`onColor` 白 / `offColor` `#121212`、单目标 `flickerHz 12`、多目标频率池 `12 / 10 / 8 Hz`、`dutyCycle 0.5`、`FrequencyStimulus` 的灰白 lerp、时相与波形。`Tools/validate_release.py` 与 `ReleaseContractTests.SsvepFrequenciesMatchFbccaContract` 会逐项核对。

改的只有镜面：`_Smoothness` `0.4 → 0.12`、关闭 `_SpecularHighlights`（`_SPECULARHIGHLIGHTS_OFF`），`_BaseColor (.04, .8, .95)` 不变。理由不是口味：球上那团高光亮度恒定、不随闪烁变化，会稀释亮/暗态的调制深度；去掉后整个球面亮度均匀调制，闪烁对比更干净，观感也从"塑料球带一颗亮斑"变成均匀磨砂。眼动房的彩球不参与 SSVEP，本轮只做同样的磨砂统一。

## 验证状态

已在无编辑器条件下完成：

- `python Tools/validate_release.py` 全绿（含新增的两条检查：训练中不得再有实时遥测渲染；三个房间不得再出现悬空的 ButtonGradient 引用、必须引用真实贴图资产）。

仍需在 Unity 编辑器与头显上完成：

1. 跑 `NeuroPilot/Training Room/Integrate Imported Scene` 重存三个房间场景与导航场景。
2. EditMode 发布契约测试（含改写后的遥测用例）。
3. Play mode `TrainingRoomVerification` 与三模式验证，重新生成 `Assets/NeuroPilot/Previews/*_v2.3.0.png`。
4. 出包并在头显上确认四件事：阴影落地且房间没有变黑、按钮是蓝底白字可读、训练中视野干净无浮字、结算卡里有平均注意力/低注意持续。
5. 若磨砂球观感不如原版，回退只是一个 `_Smoothness` 常量加一个 keyword。

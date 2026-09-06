# NeuroPilot.Training —— SSVEP 靶标训练循环（幕2「持续注意·锁航」）

把 9/5 验证过的训练机制（频闪/出球/命中循环）正式落进本仓库，并接上主仓库定义的
Unity ↔ Python 事件契约。对应 openspec add-core-system §5.0-§5.2。

## 组件清单（Assets/NeuroPilot/Training/）

| 组件 | 职责 |
|---|---|
| `SsvepFlicker.cs` | 频闪控制器：Stopwatch 相位驱动 50% 方波（默认 15Hz，纯白↔深灰 0.07），MaterialPropertyBlock 切 `_BaseColor`/`_Color`（URP/Built-in 通用） |
| `TargetSpawner.cs` | 出球器：相机局部空间视野锥取点（深度 1.5-5.5m、FOV 半宽高-0.5m 边距、房间 clamp、最小间距 1.2m 不重复） |
| `TrialSessionManager.cs` | 会话状态机：Ready(3s)→Spawn→AwaitHit(12s 超时)→HitFeedback(绿 0.5s)/MissFeedback(灰 0.6s)→下一球；180s 结束出结算（按 R 重开） |
| `IHitSource.cs` + `KeyboardHitSource.cs` / `EegHitSource.cs` | 命中源抽象：空格模拟 / 融合层 command_fire（`NotifyHit()` 判定即执行） |
| `FusionLink.cs` | WebSocket 客户端（System.Net.WebSockets，零依赖）：契约信封、1s ping 心跳、断线自动重连。每轮连接持有独立 socket/token，断开即取消收发循环、Dispose、清空上行队列（离线事件不补发） |
| `FusionJson.cs` | 极简 JSON 解析（零依赖）：信封顶层字段 + `payload` 嵌套对象单独解析（契约见主仓库 app/unity/README.md） |
| `TrainingHud.cs` | HUD：剩余时间/统计/提示/结算（World Space Canvas） |
| `Editor/SpaceTrainingBootstrapper.cs` | 菜单一键搭建：**NeuroPilot → 搭建 SSVEP 训练场景 (SpaceTraining)** |

## 快速开始（Editor 冒烟，无需 Python）

1. 打开 `Assets/NeuroPilot/Scenes/SpaceTraining.unity`
2. 菜单 **NeuroPilot → 搭建 SSVEP 训练场景 (SpaceTraining)**（生成球预制体/TrainingRoot/HUD 并接线）
3. Play：3s 准备 → 球出现并 15Hz 闪烁 → **空格 = 模拟命中**（球变绿 0.5s → 立即下一球）；12s 未命中变灰；180s 结束出结算
4. 结算面板按 **R** 重新开始

## 融合层联调（主仓库 neuropilot-xr）

    uv run python -m app.server.fusion.ws_server          # ws://127.0.0.1:8765，事件 JSONL 落盘 logs/

- 串流部署：Unity 跑在采集工作站，FusionLink 默认 `ws://127.0.0.1:8765` 即可
- **APK 直装部署**：头显与工作站同网，把 FusionLink 的 `serverUrl` 改成 `ws://<工作站IP>:8765`
- Python 下行 `command_fire` → 仅在 AwaitHit 且 `target_id` 与当前目标一致时接受（空 ID/过期/错配命令拒绝并告警，target_id 编号跨轮次不复用）→ EegHitSource.NotifyHit → 命中；`command_auto` → 兜底自动完成不计分（同样校验 target_id）；`command_reset` → 重开
- 切换命中源：TrainingRoot 的 TrialSessionManager → `hitSourceMode` 改为 **EegFusion**
- **断线降级**：EegFusion 模式下融合层离线（或 EEG 命中源未接线）自动回退键盘空格模拟，HUD 统计栏显示「EEG 离线·键盘模拟」，恢复连接后自动切回 EEG 命中

## 契约与时间对齐

- 事件信封与字段：见主仓库 `app/unity/README.md`（改契约先改那边并同步涉事人）
- 所有上行带 `ts`（Unix 毫秒，`FusionLink.NowMs()`），与 EEG 时钟对齐供离线核对
- `lock_progress`（锁定环）本期未实现；L2 多目标未做——状态机与契约已留位

## 注意事项

- **首次打开 Unity 会为新文件自动生成 .meta**——请连同代码一并提交
- 若 IL2CPP/Android 下 `ClientWebSocket` 实测异常，备选方案换 NativeWebSocket 包（唯一预期变动点）
- 频率精度受刷新率离散化影响（15Hz@120Hz 精确；其余有亚帧误差）——5.0 串流闸门实测 FFT 峰值偏差，超阈值只改频率配置
- 头显内 HUD 已尽量克制；正式演示数据全走网页后端（主仓库任务 5.5）

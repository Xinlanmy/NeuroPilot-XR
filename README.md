# NeuroPilot-XR

基于 Unity 开发的注意力训练场景。目标 VR 设备为 VIVE Focus Vision，脑机设备为博瑞康；当前已完成 MR 入口导航和交互原型，脑机数据及正式训练内容尚未接入。

## 当前功能

- MR Passthrough 真实环境上的空间窗口。
- 欢迎页 → 三档训练强度选择 → 倒计时 → 星际占位训练场景。
- 左右手射线点击；指向顶部并按住食指扳机，可在空间中拖动窗口，松手后固定。
- 每次启动及从后台返回入口，按当前头显位置重新摆放到正前方约 1.5 米，中心低于眼睛约 10 厘米；正常转头时窗口保持世界空间位置。
- 默认选择“标准”，并将所选难度传递到训练场景。

## 打开项目

1. 克隆或下载本仓库。
2. 在 Unity Hub 中添加仓库根目录（包含 `Assets`、`Packages`、`ProjectSettings` 的目录）。
3. 使用 **Unity 2022.3.62f1c1** 打开；版本信息见 `ProjectSettings/ProjectVersion.txt`。Android 构建需要 Android Build Support、SDK、NDK 和 OpenJDK。
4. 等待包和资源导入，打开 `Assets/NeuroPilot/Scenes/NeuroPilotNavigation.unity`。
5. 普通 Editor 可预览界面；真实 MR 透视及佩戴体验需要在 VIVE Focus Vision 上确认。

电脑上的 SDK 安装路径因人而异，请在 Unity `Preferences > External Tools` 中使用自己的配置。教程中的 `D:\workspace\NeuroPilotXR` 是开发机示例路径，其他电脑请替换为本地克隆路径。

## 下载与文档

- [1.0.1 Android 测试安装包](https://github.com/Xinlanmy/NeuroPilot-XR/releases/tag/v1.0.1)
- [新手操作教程](新手操作教程.md)
- [拖动与启动定位修复验收](拖动与启动定位修复验收.md)

APK 包名为 `com.neuropilot.xr`。1.0.1 是开发测试构建，版本号 `2`；可覆盖安装之前的同签名测试版本。

## 目录

| 目录 | 内容 |
| --- | --- |
| `Assets/NeuroPilot/Scenes` | 入口导航与 `SpaceTraining` 占位场景 |
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

`SpaceTraining` 目前是流程验证占位场景；未将其他本地星际项目或 EEG 训练逻辑误作为已完成功能。

内嵌第三方组件和字体遵循各自目录中的许可文件；本仓库不变更这些许可。
详情见 [第三方组件说明](THIRD_PARTY_NOTICES.md)。

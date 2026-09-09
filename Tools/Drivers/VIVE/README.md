# VIVE USB 串流驱动（Windows）

此目录保存维护者提供的 VIVE USB 串流驱动，供项目的 Windows PC 串流环境安装使用。驱动放在 Unity `Assets` 目录之外，不参与 Unity 资源导入或 APK 打包。

## 下载与版本

- [下载 UsbDriver.zip](UsbDriver.zip?raw=true)（8,783,129 字节，约 8.4 MiB）
- [SHA-256 校验文件](SHA256SUMS)
- 原包归档日期：2026-09-09；保留原文件名与全部内容。
- 包版本：`1.0.0.5`；包发布日期：`2024/04/17`，取自包内 `Version.txt`。
- INF 驱动版本：`05/06/2021,13.0.0000.0`，取自 `android_winusb.inf` 的 `DriverVer` 字段。
- INF 提供者：`HTC Vive`；接口名称：`VIVE RR Interface`、`VIVE HUB Interface`。
- 包内包含 Windows x86（`i386`）和 x64（`amd64`）组件。

这是 USB 驱动包，串流还需要另行配置 VIVE 串流软件与头显。

## 安装

以下步骤依据压缩包内的 `ReadMe.txt`：

1. 完全退出 **VIVE Streaming Hub** 和 **VIVE Business Console**。
2. 将 `UsbDriver.zip` 完整解压到本地目录，保留 `i386`、`amd64` 子目录及其他文件。
3. 右键解压后的 `android_winusb.inf`，选择“安装”。
4. 等待系统出现安装确认对话框，再启动串流软件。

## 校验原包

在压缩包所在目录打开 PowerShell，运行：

```powershell
Get-FileHash -LiteralPath .\UsbDriver.zip -Algorithm SHA256
```

预期 SHA-256：

```text
9bce74df7e157eb7da157ee962e67c0d7f7f927f053eb1722c72d09d19f9471f
```

此次归档核对了包内说明、版本信息和文件哈希；未执行驱动安装或头显串流测试。第三方归属记录见 [THIRD_PARTY_NOTICES.md](../../../THIRD_PARTY_NOTICES.md)。

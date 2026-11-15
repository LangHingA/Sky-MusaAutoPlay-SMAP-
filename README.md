# SMAP — 光遇自动演奏

[English](README_EN.md) · [源码仓库](https://github.com/langhinga/Sky-MusaAutoPlay-SMAP-)

作者：**LANGHINGA**。Windows 曲谱编辑、试听、练习与自动演奏工具，使用 C# / WPF / .NET 10。

> 待用户审核。本分支为 **v3.0.0-rc.3 工程整顿候选**，尚未发布 v3.0 Stable。生产签名配置与最终验收仍待完成。

## v3.0 已实现能力

- 原生 `.smap` 工程、多轨编辑、Tap/Hold、变速与变拍、继续编辑。
- 四 TAB 统一导入：SMAP 在线分享、离线、TXT/JSON、MIDI。
- 历史 TXT/JSON 转换；仅编辑元信息时保留原音符、键数、时间单位与未知字段。
- SVG/PNG 专业图片谱导出与工程恢复，保持既有恢复协议。
- Muse HTTPS 在线分享、固定版本短码、历史版本与来源关系，服务端核验作品管理权限。
- 本地与云端曲库、封面、收藏夹、播放列表、试听、游戏按键演奏与练习。
- 三主题、简体中文 / 繁体中文 / English / 日本語，G2 连续圆角控件。
- Muse 后台管理公告；设置内正式 / 测试更新通道、后台下载与就绪重启提示。
- 签名分块增量更新与事务恢复。Stable 须配置可信公钥；缺失可信签名时拒绝执行安装包。

**v3.1 游戏内谱面映射仍延期，候选版未开放。** 开发代码的存在不代表功能已经发行。

## 安装与用户数据

Windows x64 发布包自带 .NET 运行时。Release 请求 UAC 权限以支持全局游戏输入，Debug 不提权。自动化测试不能代替真实游戏和真人听觉验收。

程序目录：

```text
SMAP.exe
SMAP.Updater.exe
Assets/          # 音源与不可变示例
Licenses/
```

普通安装的个人数据：

```text
%LOCALAPPDATA%/SMAP/Songs/
%LOCALAPPDATA%/SMAP/Cache/
%LOCALAPPDATA%/SMAP/Updates/
%LOCALAPPDATA%/SMAP/Backups/
%APPDATA%/SMAP/            # 既有设置、收藏夹和私密会话
```

旧安装目录的 `songs/` 会复制并逐文件 SHA-256 校验，映射已知收藏、播放与分享路径。原件保留，重名冲突不静默覆盖。请保留旧目录及备份，确认迁移后再决定是否清理。不要把程序安装到个人数据目录本身。

便携模式须在程序旁明确创建 `portable.mode`，使用 `Data/` 保存个人数据。仅有旧 `songs/` 不会自动启用便携模式。普通账户配置不会自动导出到新便携目录。

更新不管理 Songs、Data、凭据和 `portable.mode`。已知旧运行时仅在精确哈希匹配时归档，未知或修改过的文件保留。程序回滚不会回退个人数据。

## 构建与测试

在 Windows 安装 **.NET SDK 10.0.403**（[global.json](global.json)）及桌面开发环境：

```powershell
dotnet build SMAP.slnx -c Debug
dotnet build SMAP.slnx -c Release
dotnet test Tests/SMAP.Tests.csproj -c Debug
dotnet test Tests/SMAP.Tests.csproj -c Release
./Installer/build-installer.ps1 -Dotnet 'C:/Program Files/dotnet/dotnet.exe' -OutputRoot 'C:/SMAP-build'
```

完整打包输出为 `_smap_publish/`、`Payload/app.zip` 和 `_setup_out/SMAP-Setup.exe`。包含单文件主程序、更新助手、音源和许可。构建中间产物不要提交，SDK 路径须满足 global.json。

## 工程结构

- `src/SMAP.WPF/`：Views、Controls、Core、Services、Updates、Localization、Themes、Assets。
- `Installer/`、`Updater/`：安装器和更新助手。
- `Tests/`：自动化测试及 WPF 窗口验证宿主。
- `Tools/`：分块打包、更新探针、签名、静态安装包审计和隔离布局验证。
- `docs/`：文档；`曲谱库/`：历史原始谱面；`legacy/JavaFX/`：旧实现归档。

整顿不改变程序集身份、核心演奏时间线、云端协议或 `.smap` 格式，不重写原始曲库。

## 发布安全

Alpha 与 Stable 使用独立签名身份。私钥不能进入 Git、构建属性或发行包。Stable 构建只接受发布者核准的 P-256 公钥和 KeyId；签名工具核对身份。完整安装器需要签名发行证明和准确 SHA-256，不仅依赖 HTTPS、大小或 MZ 文件头。

当前候选未配置生产 Stable 公钥，亦未声称持有 Authenticode 证书。最终签名、人工验收和发行批准未完成前不发布稳定版。见[待审核更新说明](docs/RELEASE-NOTES-v3.0-DRAFT.md)。

## 许可

见 [LICENSE](LICENSE) 与[第三方说明](docs/ThirdPartyNotices.txt)。使用者须遵守游戏及曲谱授权规则。

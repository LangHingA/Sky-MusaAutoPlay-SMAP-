# Stable 更新信任身份

已核准公钥：signing/stable-20261115.public.pem。KeyId：stable-20261115。

公钥 DER SHA-256：e6a2f70178628257b373d56dd38c6824aec581016c960a9ae870efc6bb351f3b。

默认构建在 Directory.Build.props 中注入这套公开身份，主程序、Updater 和两个签名工具使用同一配置。Alpha 保留独立公钥。仅覆盖一个构建参数会报错；换钥必须同时提供公钥与 KeyId 并经发布者核准。私钥不属于仓库或发行包。

3.0.0-rc.4 是引入该信任身份的新候选，不是正式 Stable 发行。旧 rc.3 不包含这套公钥，需要用含新信任的候选/安装器建立后续升级入口。当前只进行本地验证，不推送或发布。

签名工具接受匹配本公钥的本地受限私钥文件。最终文件定稿后再生成签名证明；更改 EXE 将使原证明失效。Windows Authenticode 证书另行配置，不能将更新清单签名称为 Windows 发布者认证。

## 本地签名候选打包

在受保护的签名机器上，先指定统一暂存根，再运行：

```powershell
$env:SMAP_BUILD_SCRATCH_ROOT = 'C:/SMAP-BuildTemp'
./Installer/build-installer.ps1 -OutputRoot 'C:/SMAP-BuildTemp/rc4' -SigningKeyId 'stable-20261115' -SigningKeyFile 'C:/secure/stable-private.pem'
```

脚本只把私钥路径交给本地签名工具，不复制密钥。它先签名并核对更新清单，嵌入 .smap-installed.json 后再打包安装器；安装器定稿后再生成并核对 SMAP-installer-manifest.json。每阶段编译中间文件集中到受控 artifacts 目录，发布完成即清理，预算包含所有暂存。

上述命令只生成本地候选，不上传 GitHub。公钥与清单签名不等于 Authenticode。需要单独复核时使用 ReleaseSigning 的 verify-update / verify-installer 命令，无需私钥。

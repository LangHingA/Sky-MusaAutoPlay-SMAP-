# Stable 更新信任身份

已核准公钥：signing/stable-20261115.public.pem。KeyId：stable-20261115。

公钥 DER SHA-256：e6a2f70178628257b373d56dd38c6824aec581016c960a9ae870efc6bb351f3b。

默认构建在 Directory.Build.props 中注入这套公开身份，主程序、Updater 和两个签名工具使用同一配置。Alpha 保留独立公钥。仅覆盖一个构建参数会报错；换钥必须同时提供公钥与 KeyId 并经发布者核准。私钥不属于仓库或发行包。

3.0.0-rc.4 是引入该信任身份的新候选，不是正式 Stable 发行。旧 rc.3 不包含这套公钥，需要用含新信任的候选/安装器建立后续升级入口。当前只进行本地验证，不推送或发布。

签名工具接受匹配本公钥的本地受限私钥文件。最终文件定稿后再生成签名证明；更改 EXE 将使原证明失效。Windows Authenticode 证书另行配置，不能将更新清单签名称为 Windows 发布者认证。

<div align="center">

# <img src="docs/images/logo.png" width="38" height="38" align="absmiddle" alt="Logo" /> CatPawPlayer (AinanPlayer)
### 现代化 Windows 11 影视聚合与 4K 超清硬件加速播放平台

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?style=flat-square&logo=windows)](https://github.com/Ainanya21/CatPawPlayer)
[![Framework](https://img.shields.io/badge/UI-WinUI%203%20%2F%20Windows%20App%20SDK%201.6-8860D0?style=flat-square&logo=microsoft)](https://github.com/Ainanya21/CatPawPlayer)
[![.NET](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Telegram Channel](https://img.shields.io/badge/Telegram-Channel-2CA5E0?style=flat-square&logo=telegram)](https://t.me/CatPawPlayer)
[![Telegram Group](https://img.shields.io/badge/Telegram-Group-2CA5E0?style=flat-square&logo=telegram)](https://t.me/CatPawPlayerChat)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)

[**下载体验**](#下载与安装) • [**使用指南**](docs/USER_GUIDE.md) • [**界面预览**](#界面预览) • [**核心亮点**](#核心亮点) • [**环境要求**](#运行环境要求) • [**配置中心**](#配置中心与网盘授权) • [**社区交流**](#交流与社区反馈) • [**免责声明**](#免责声明) • [**支持项目**](#支持项目) • [**来源与许可**](#来源与许可)

</div>

---

## 界面预览

<div align="center">

### 首页精选推荐与海报流 (Apple TV 级丝滑交互动效)
![首页推荐](docs/images/screenshot_home.png)

<br/>

### 影视详情与多线路极速选集 (支持 Yaozhi-MPV 硬件加速与一键切源)
![影视详情](docs/images/screenshot_detail.png)

<br/>

### 外观与个性化主题配置 (深浅主题无缝切换与强调色拾色器)
![外观设置](docs/images/screenshot_settings.png)

<br/>

### 4K 60FPS 极限硬件加速与弹幕播放 (Yaozhi-MPV 硬件解码/杜比音频/实时码率)
![极速播放](docs/images/screenshot_player.png)

</div>

---

## 核心亮点

* **原生 WinUI 3 + Fluent 2 视觉体系**：基于微软最新 Windows App SDK 1.6 与 Mica / Acrylic 材质构建，支持全功能浅色/深色/跟随系统的主题实时无缝切换。
* **多引擎万能爬虫架构（Universal Multi-Engine Spider）**：
  * 支持 **王二小放牛娃猫源**（`9988`）、**豆源**（`2333`）、以及统一控制网关（`9980`）。
  * 完美支持 `.js.md5`、Base64 加密订阅、标准 TVBox JSON 仓库以及单 CMS 接口的动态自动识别、热下载与多端口隔离加载。
* **双播放内核与 4K HDR 硬件加速**：
  * **Yaozhi-MPV 极速播放**：支持调用外部 MPV 高性能解码器，享受 4K UHD、HEVC/H.265、杜比视界（Dolby Vision）及 120FPS 极限硬件加速。
  * **内置离线 HLS 原生播放引擎**：内嵌完整的离线 `hls.min.js`，零外部 CDN 依赖，启动秒开，断网或离线环境下无障碍播放。
* **内嵌原生配置中心**：
  * 深度集成扫码与授权控制台，无需跳出浏览器，直接在应用内完成夸克、UC、阿里、百度、115 等网盘账号的扫码授权与转存配置。
* **智能媒体流元数据实时探针**：
  * 自动侦测媒体流的分辨率、视频编码、音频编码、网络延迟以及多码率分片轨道，在播放详情页即时呈现 4K / 1080P / HDR / 杜比全景声 徽章。
* **全网聚合搜索与多维分类筛选**：
  * 支持跨源异步并发聚合搜索与多级分类展开筛选，一键检索全网优质资源。

---

## 运行环境要求

* **操作系统**：Windows 10（1809 / 17763 及以上）或 Windows 11（64位）
* **依赖环境**：安装包已自包含全部运行依赖，解压或安装后即开即用（无需额外安装运行库）
* **外部播放器（可选）**：如需开启 MPV 极速硬件加速，可搭配 [mpv-Yaozhi](https://github.com/Yaozhil/mpv-Yaozhi) 使用

---

## 下载与安装

进入 [**Releases 发布页面**](https://github.com/Ainanya21/CatPawPlayer/releases) 下载最新安装包：

| 文件类型 | 说明 | 适用场景 |
| :--- | :--- | :--- |
| **`CatPawPlayer_vX.X.X_Setup.exe`** | 现代图形化安装向导，支持自定义路径、桌面快捷方式 | 推荐大多数用户使用，支持应用内一键在线检测与自动升级 |
| **`CatPawPlayer_vX.X.X_Portable.zip`** | 绿色免安装解压即用版 | 适合便携 U 盘或快速体验 |

---

## 配置中心与网盘授权

为了畅快播放网盘 4K 原画资源（如至臻、玩偶、指南等聚合源），请先完成网盘授权：

1. 打开猫爪播放器，点击左侧导航栏 **「全量分类」**。
2. 站点下拉框选择 **「配置中心」**（或点击网盘片源详情页上的授权引导按钮）。
3. 使用手机对应网盘 APP（夸克 / UC / 阿里云盘 / 百度网盘 / 115）扫码登录。
4. 授权成功后，爬虫后端将在播放 4K 资源时自动进行秒级转存并换取高速原画播放直链！

---

## 交流与社区反馈

欢迎加入官方社区交流群与频道，获取最新源更新资讯与版本通知：

* **Telegram 官方频道**：[https://t.me/CatPawPlayer](https://t.me/CatPawPlayer)
* **Telegram 交流群组**：[https://t.me/CatPawPlayerChat](https://t.me/CatPawPlayerChat)

---

## 播放器设置（MPV 硬件加速）

猫爪播放器支持自由选择内置播放器或外部 MPV 播放器：

1. 点击左侧导航栏 **「设置」**。
2. 在 **「播放器设置」** 中：
   * 勾选 **「启用外部 MPV 硬件加速播放」**。
   * 点击 **「浏览...」** 选择您的 `mpv.exe` 路径（如 `D:\Yaozhi-MPV\mpv.exe`）。
3. 在视频详情页即可使用 MPV 播放，支持硬件解码、记忆断点以及快捷键控制。

---

## 支持项目

如果 **CatPawPlayer (AinanPlayer)** 有幸改善了你的观影与播放体验，非常欢迎在能力范围内支持开发者。

| 微信赞赏 (WeChat Pay) | 支付宝收款 (Alipay) |
| :---: | :---: |
| <img src="docs/wechat_pay.jpg" width="220" alt="微信赞赏码" /> | <img src="docs/alipay.jpg" width="220" alt="支付宝收款码" /> |

> 你的支持可以让我更好地持续维护与开发本项目，不断完善多源生态与极致的播放体验，总之非常感谢大家的支持与喜爱！❤️

---

## 来源与许可

本项目参考或集成了以下优秀开源项目及社区成果，在此向所有开源创作者与社区贡献者致以崇高的敬意与感谢：

* **[mpv](https://mpv.io/) / [mpv-Yaozhi](https://github.com/Yaozhil/mpv-Yaozhi)**：高性能跨平台视频渲染器与硬件加速解码核心，为本项目提供极致的 4K HDR、杜比视界及 120FPS 观影支持。
* **[WinUI 3](https://github.com/microsoft/WindowsAppSDK) / [Windows App SDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/)**：微软现代 Windows 原生桌面 Fluent 2 视觉体系与 UI 框架。
* **[CatVodSpider / TVBox 协议生态](https://github.com/)**：开放影视爬虫与数据解析协议生态，提供强大的多源检索与视频解析规范。
* **[hls.js](https://github.com/video-dev/hls.js/)**：原生流媒体分片传输与 HLS / m3u8 离线解码引擎。
* **[Fastify](https://fastify.dev/) / [Node.js](https://nodejs.org/)**：高性能异步爬虫调度与微服务通信中间件。
* **[Newtonsoft.Json](https://www.newtonsoft.com/json)**：.NET 业界标准的 JSON 序列化与数据处理库。

---

## 免责声明

1. **纯本地工具属性**：**CatPawPlayer (AinanPlayer)** 仅为一款基于 WinUI 3 与开源多媒体内核构建的本地音视频播放工具与接口解析客户端，软件本体**不提供、不存储、不发布、不传播**任何音频、视频、字幕或图片资源，亦不架设任何媒体流服务器。
2. **数据源免责**：软件内所有展示的订阅源、站点规则、网盘转存服务及视频播放链接均由使用者**自行配置、自定义导入或来源于第三方公开网络接口**。开发者不对任何第三方接口与内容的合法性、真实性、准确性、有效性或可用性承担任何直接或连带法律责任。
3. **合法合规使用**：本项目仅供计算机技术研究、编程学习与多媒体解码交流使用。请广大使用者严格遵守所在国家与地区的法律法规，尊重原创与知识产权。严禁将本软件用于任何形式的商业盈利、非法传播或侵权行为。
4. **版权保护**：若任何第三方机构或个人认为用户自行导入的第三方源侵犯了其合法权益，请依法向对应网络内容的源服务提供商或接口维护者提出维权主张。

---

## 开源许可

本项目基于 [MIT 许可证](LICENSE) 开源。

# CASSIE CWC Tool

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-512BD4)](https://github.com/dotnet/wpf)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-2.3.0-blue)](https://github.com/Small-Jeff/Cassiewordcheck/releases)

**CASSIE Word Check** — 逐词比对 CASSIE 配音词库，标记可用/不可用单词，用于 **SCP: Secret Laboratory** 游戏 CASSIE 语音文本校对。

> SCP:SL 中的 CASSIE 系统是一个文本转语音（TTS）合成器，用于播放游戏内广播。但并非所有英文单词都能被 CASSIE 正确朗读——本工具帮助你快速检查广播文本中的单词是否在 CASSIE 词库中，避免「播音事故」。

---

## ✨ 功能一览

| 功能 | 说明 |
|------|------|
| ✅ **词库检查** | 逐词比对 CASSIE 配音词库，绿色=可用，红色=不可用 |
| 📊 **实时统计** | 可用数、不可用数、忽略数、覆盖率一目了然，进度条可视化 |
| 💡 **拼写建议** | 对不可用词自动 Levenshtein 相似词推荐 |
| 📋 **白名单管理** | 添加自定义豁免词，支持导入/导出 |
| 🌐 **多语言界面** | 简体中文 / English / 日本語 / 한국어 / Deutsch / Русский / Français |
| 🔍 **格式过滤** | 自动忽略 link、color、size、split 等 CASSIE 格式标记 |
| 🏷️ **命名过滤** | 屏蔽 MTF/UIU/GOC 等阵营缩写及北约代号、希腊字母 |
| 🌙 **暗色主题** | Mica 毛玻璃效果，全组件平滑动画 |
| 📤 **结果导出** | 检查结果可保存为 `.txt` 文件 |
| ↩️ **撤销重做** | Ctrl+Z / Ctrl+Y，支持清空/粘贴/加载文件 |
| 📂 **批量检查** | 打开多个文本文件，自动合并检查 |
| 🕐 **检查历史** | 最近 50 次检查记录，点击即可恢复文本 |
| 📈 **统计面板** | 折线图展示覆盖率 / 不可用词历史趋势 |
| 🔄 **自动更新** | 启动时自动检查 GitHub 新版本 |
| 🚫 **单实例** | 防止多开，避免数据冲突 |

## 🖼️ 截图

> *（待补充）*

## 📦 安装

### 方法一：下载 Release（推荐）

前往 [Releases 页面](https://github.com/Small-Jeff/Cassiewordcheck/releases) 下载最新版本的 `CassieWordCheck.exe`，直接运行即可。

### 方法二：自行构建

```bash
# 克隆仓库
git clone https://github.com/Small-Jeff/Cassiewordcheck.git
cd CassieWordCheck

# 恢复依赖 & 构建
dotnet restore
dotnet build -c Release

# 单文件发布（输出到 dist/）
dotnet publish -c Release -r win-x64 -o dist --self-contained true
```

或直接双击项目根目录的 `publish.bat`。

## 🎮 使用方式

1. 在输入框中粘贴或输入你的 CASSIE 广播文本
2. 右侧结果卡实时显示检查结果：
   - <span style="color:#10B981">**绿色**</span> — 单词在词库中，CASSIE 可读出
   - <span style="color:#EF4444">**红色 + 下划线**</span> — 单词不在词库中
   - <span style="color:#6B7280">**灰色**</span> — 已被过滤（格式标记/中文等）
3. 点击不可用词下方的建议面板查看相似词推荐
4. 点击顶部工具栏可管理白名单、调整设置、查看统计

## 🏗️ 项目架构

```
CassieWordCheck/
├── CassieWordCheck.csproj    项目配置（.NET 8 WPF）
├── CassieWordCheck.sln       解决方案
├── app.manifest              Windows 清单
│
├── Models/                   数据模型 & 核心逻辑
│   ├── Checker.cs           检查引擎（分词/过滤/统计）
│   ├── CheckResult.cs       检查结果 + 状态枚举
│   ├── WordList.cs          词库加载/查询（FrozenSet）
│   ├── HistoryStore.cs      检查历史持久化
│   └── Settings.cs          设置读写（JSON）
│
├── Resources/
│   ├── Styles.xaml          全局暗色主题样式
│   ├── Locales/             多语言翻译 JSON（7 种语言）
│   └── Services/
│       ├── DocumentBuilder.cs   结果 → 富文本 FlowDocument
│       ├── MarkdownConverter.cs Markdown → FlowDocument 渲染
│       ├── LevenshteinHelper.cs 编辑距离（拼写建议）
│       ├── LocalizationService.cs  多语言管理
│       ├── UpdateService.cs     自动更新（GitHub API）
│       └── WindowHelper.cs     Win32 DWM API（暗色栏 + Mica）
│
├── Views/                    WPF 窗口
│   ├── MainWindow.xaml/cs   主窗口（输入/结果/统计/建议）
│   ├── SettingsWindow       设置（过滤/语言/字体/更新）
│   ├── StatisticsWindow     统计面板（折线图）
│   ├── HistoryWindow        检查历史
│   ├── WhitelistWindow      白名单管理
│   └── AboutWindow          关于（功能/更新日志/关于/声明）
│
├── data/                    运行时数据
│   ├── cassie-text.txt      CASSIE 配音词库
│   ├── AAA.ico              应用图标
│   ├── AAA.JPG              工具栏图标
│   └── qr.JPG               头像
│
├── publish.bat              一键构建脚本
├── .github/workflows/       GitHub Actions 自动发布
└── dist/                    构建输出
```

## 🔧 技术栈

- **.NET 8** + **WPF**（Windows 桌面应用）
- **CommunityToolkit.Mvvm** — MVVM 辅助
- **ClosedXML** — Excel (.xlsx) 导入支持
- **System.Text.Json** — JSON 序列化
- **GitHub API** — 自动更新检查
- **GitHub Actions** — CI/CD 自动构建

## 📋 系统要求

- Windows 10 1809+ / Windows 11
- .NET 8 运行时（自包含发布版不需要）

## 🤝 参与贡献

欢迎提交 Issue 和 Pull Request！详见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 📄 许可证

本项目基于 [MIT 许可证](LICENSE) 开源。

本项目采用 **CC BY-SA 3.0** 协议引用 SCP 基金会相关内容。

## 🙏 鸣谢

- **Awni** — 词库支持与测试
- **虚无** — 早期测试与反馈
- 所有为项目提供建议和反馈的朋友

---

> *本项目由 AI 辅助开发*

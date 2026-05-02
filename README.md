# CASSIE CWC Tool

CASSIE Word Check — 逐词比对 CASSIE 配音词库，标记可用/不可用单词，用于 SCP:SL 游戏 CASSIE 语音文本校对。

## 项目结构

```
CassieWordCheck/
│
├── CassieWordCheck.csproj       项目配置（.NET 8 WPF、版本号、依赖）
├── CassieWordCheck.sln          解决方案文件（Rider / VS）
├── app.manifest                 Windows 清单（高 DPI）
├── GlobalUsings.cs              全局 using
├── .gitignore                   Git 忽略规则
├── publish.bat                  本地一键构建脚本（输出到 dist/）
│
├── data/
│   └── cassie-text.txt          CASSIE 配音词库（核心数据源）
│
├── Models/                      数据模型 & 核心逻辑
│   ├── Checker.cs              检查引擎（分词、过滤、统计）
│   ├── CheckResult.cs           检查结果 + 状态枚举
│   ├── WordList.cs              词库加载 & 查询（FrozenSet）
│   └── Settings.cs              设置读写（JSON 持久化）
│
├── Resources/
│   ├── Styles.xaml              全局 UI 样式
│   ├── Locales/                 多语言翻译（zh-CN / en / ja / ko / de / ru）
│   └── Services/
│       ├── DocumentBuilder.cs   结果 → 富文本排版
│       ├── LevenshteinHelper.cs 编辑距离（拼写建议）
│       ├── LocalizationService.cs  多语言管理
│       └── WindowHelper.cs      Win32 API（暗色标题栏 + Mica）
│
├── Views/                       窗口
│   ├── MainWindow.xaml / .cs    主窗口
│   ├── SettingsWindow.xaml / .cs 设置
│   ├── WhitelistWindow.xaml / .cs 白名单管理
│   └── AboutWindow.xaml / .cs   关于
│
├── App.xaml / App.xaml.cs       应用入口
│
└── .github/workflows/
    └── release.yml              GitHub Actions 自动构建
```

## 功能

- **词库检查** — 逐词比对 CASSIE 配音词库，标记可用/不可用单词
- **实时统计** — 可用数、不可用数、忽略数、覆盖率
- **拼写建议** — 对不可用词自动 Levenshtein 相似词推荐
- **白名单管理** — 添加自定义豁免词，支持导入/导出
- **多语言界面** — 简体中文 / English / 日本語 / 한국어 / Deutsch / Русский
- **格式标记过滤** — 自动忽略 link、color、size、split 等 CASSIE 标签
- **命名标记过滤** — 屏蔽 MTF/UIU/GOC 等阵营缩写及北约代号
- **中文忽略** — 可开关，跳过中文字符
- **暗色主题** — Mica 毛玻璃效果（Windows 11），全组件平滑动画

## 系统要求

- Windows 10 1809+ / Windows 11
- .NET 8 运行时（自包含发布版不需要）

## 构建 & 发布

### 本地构建

```bash
dotnet build CassieWordCheck.csproj
```

### 一键打包

双击 `publish.bat`，输出到 `dist/` 目录。

### GitHub Actions 自动发布

推送 `v*` 格式的 tag 到 GitHub，自动编译、打包并上传到 Releases 页面：

```bash
git tag v2.1.1
git push origin v2.1.1
```

## 许可证

[MIT](LICENSE)

## 鸣谢
虚无
Awni

本项目由 AI 辅助制作

# Contributing

欢迎贡献代码、提交 Issue 或提出改进建议！

## 🍴 协作流程（Fork + Pull Request）

1. **Fork** 本仓库到你的 GitHub 账号
2. **Clone** 你的 Fork 到本地
3. 创建**功能分支**进行开发
4. 推送你的分支到你的 Fork
5. 提交 **Pull Request** 到本仓库的 `main` 分支
6. 审核通过后合并

```bash
# 1. Fork 后克隆你的版本
git clone https://github.com/你的用户名/Cassiewordcheck.git
cd CassieWordCheck

# 2. 添加原仓库为上游（可选，用于同步更新）
git remote add upstream https://github.com/qingranawa/Cassiewordcheck.git

# 3. 创建功能分支
git checkout -b feat/my-awesome-feature

# 4. 开发、提交、推送
git push origin feat/my-awesome-feature

# 5. 在 GitHub 上提交 Pull Request
```
## 📋 报告问题

提交 Issue 时请包含：

- 问题描述（发生了什么，期望什么）
- 复现步骤
- 截图或错误信息（如有）
- 环境信息（Windows 版本、屏幕缩放比例等）

## 💻 开发环境

| 工具 | 版本 |
|------|------|
| .NET SDK | 8.0+ |
| IDE | JetBrains Rider / Visual Studio 2022+ |
| 运行时 | Windows 10 1809+ |

### 快速开始

```bash
git clone https://github.com/qingranawa/Cassiewordcheck.git
cd CassieWordCheck
dotnet restore
dotnet build
```

### 本地一键打包

项目根目录的 `publish.bat` 用于快速生成单文件发布包：

```bat
:: 直接双击运行，输出到 dist/ 目录
publish.bat
```

执行流程：

1. 清理旧的 `dist/` 目录
2. `dotnet restore` 恢复依赖
3. `dotnet build -c Release` 编译发布配置
4. `dotnet publish -c Release -r win-x64 -o dist` 生成单文件
5. 删除多余的 `.pdb` 文件
6. 显示最终的 dist 目录结构

输出结构：

```
dist/
├── CassieWordCheck.exe    单文件可执行程序
├── data/                  运行时数据（词库/图标/配置）
│   ├── cassie-text.txt
│   ├── AAA.ico / AAA.JPG / qr.JPG
│   ├── appsettings.json   （运行时生成）
│   └── history.json        （运行时生成）
└── Resources/Locales/     多语言文件
```

## 🔧 代码规范

### 命名约定

| 类别 | 规范 | 示例 |
|------|------|------|
| 类/方法/属性 | PascalCase | `LoadWordListAsync()` |
| 接口 | `I` 前缀 | `IWordRepository` |
| 私有字段 | `_camelCase` | `_wordlist` |
| 常量 | PascalCase | `MaxRetryCount` |
| 本地变量 | camelCase | `wordCount` |

### 编码原则

- **异步优先**：I/O 操作（文件、数据库、HTTP）始终使用 `async/await`
- **依赖注入**：通过构造函数传递依赖，避免静态单例
- **明确异常**：捕获特定异常类型，避免裸露的 `catch (Exception)`
- **资源管理**：优先使用 `using` 声明
- **现代语法**：优先使用 C# 最新特性（记录类型、集合表达式、模式匹配）

### XAML 规范

- 组件命名：`PascalCase` + 类型后缀（`InputBox`、`ResultLabel`）
- 事件处理：`On` 前缀（`OnLanguageChanged`）
- 资源引用：优先使用 `StaticResource`，避免硬编码值
- 动画：使用 `DoubleAnimation` + `EasingFunction`，避免 Storyboard 过度嵌套

## 🔄 提交信息

格式参考 [Conventional Commits](https://www.conventionalcommits.org/)：

```
<type>: <简短描述>

[可选的详细描述]
```

### 类型

| 类型 | 用途 |
|------|------|
| `feat` | 新功能 |
| `fix` | Bug 修复 |
| `ui` | UI 样式/布局变更 |
| `perf` | 性能优化 |
| `refactor` | 代码重构（无功能变化） |
| `docs` | 文档 |
| `chore` | 构建/工具/依赖 |
| `locale` | 多语言翻译 |

### 示例

```
feat: 新增 CSV 单词导入支持
fix: 修复统计窗口崩溃
ui: 优化入场动画错峰播放
perf: 减少同时并发动画数
```

## 🚀 发版流程

1. 更新 `CassieWordCheck.csproj` 中的 `<Version>` 和 `<FileVersion>`
2. 更新 `Views/AboutWindow.xaml.cs` 中的更新日志 `ChangelogText`
3. 确认所有 locale JSON 文件已更新
4. 提交代码并推送至 `main` 分支
5. 打 tag 并推送，触发 GitHub Actions 自动构建

```bash
git tag v2.3.0
git push origin v2.3.0
```

GitHub Actions 会自动编译、打包并上传到 Releases 页面。

## 📁 项目结构说明

- `Models/` — 纯数据模型和业务逻辑，不依赖 WPF
- `Views/` — WPF 窗口，UI 逻辑分离
- `Resources/Services/` — UI 相关的服务类
- `Resources/Locales/` — 多语言 JSON，新增语言时添加文件即可
- `data/` — 运行时数据和静态资源

## ✅ Pull Request 检查清单

- [ ] 代码编译通过
- [ ] 遵循命名规范
- [ ] 新增功能已添加本地化翻译 key
- [ ] 已测试（如适用）
- [ ] Commit 信息清晰

---

感谢你的贡献！🌟

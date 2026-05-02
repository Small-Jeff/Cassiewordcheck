# Contributing

欢迎提交 Issue 和 Pull Request。

## 开发环境

- .NET 8 SDK
- JetBrains Rider / Visual Studio 2022

## 代码规范

- 文件名、类名、方法名：PascalCase
- 私有字段：`_camelCase`
- 接口：`I` 前缀
- 异步方法：`Async` 后缀
- I/O 操作优先使用 `async/await`

## 提交信息

保持简洁，用英文或中文均可：

```
fix: 修复 HexRegex 误伤纯数字的问题
feat: 新增主题选择
chore: 清理构建产物
```

## 发版流程

1. 更新 `CassieWordCheck.csproj` 中的版本号
2. 更新 `AboutWindow.xaml.cs` 中的更新日志
3. 提交代码并推送
4. 打 tag 并推送触发 GitHub Actions 自动构建

```bash
git tag v2.1.2
git push origin v2.1.2
```

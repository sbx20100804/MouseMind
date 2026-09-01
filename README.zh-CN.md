# MouseMind（灵触）

[English](README.en.md) · [返回主页](README.md)

MouseMind 是一个面向 Windows 的智能鼠标控制中心。它根据当前前台应用自动切换鼠标配置，并把侧键、滚轮和手势转换成快捷键、自动化动作或 AI 工作流。

## 为什么做它

传统鼠标驱动通常把一个按键永久绑定到一个功能。MouseMind 使用“应用上下文 + 触发器 + 动作”的模型，让同一个侧键在不同软件里发挥不同作用。

```text
当前应用 → 鼠标触发器 → 动作或动作链 → 结果反馈
```

例如：

| 应用场景 | 侧键动作 |
|---|---|
| 浏览器 | 翻译或总结选中文字 |
| 代码编辑器 | 撤销、解释代码、快速修复 |
| 视频剪辑 | 分割片段、逐帧移动 |
| 文档编辑 | 润色、提取待办事项 |

## Alpha 0.2 已实现

- Windows 深色桌面控制中心
- 全局监听标准鼠标侧键 XButton1/XButton2
- 读取当前前台进程并匹配应用配置
- 通过 Windows `SendInput` 执行真实快捷键
- 支持 Ctrl、Shift、Alt、Win、字母、数字和功能键组合
- 动作冷却保护，避免短时间重复执行
- 应用配置与动作映射管理
- 本地 JSON 配置持久化
- 实时事件和动作执行日志
- GitHub Actions 自动构建

## 运行

```powershell
dotnet build .\MouseMind.slnx
dotnet run --project .\src\MouseMind.App\MouseMind.App.csproj
```

环境要求：Windows、.NET 10 SDK。

## 配置位置

```text
%LocalAppData%\MouseMind\profiles.json
```

内置“代码工作台”预设中，侧键 2 会执行 `Ctrl+Z`。当前版本保留鼠标按键原始行为，方便公开 Alpha 阶段验证兼容性。

## 路线图

- Alpha 0.3：完整配置编辑器、托盘、动作提示浮窗
- Alpha 0.4：选中文字、翻译、总结和 AI Provider
- Alpha 0.5：鼠标手势、环形菜单和动作链
- Beta：插件 SDK、配置导入导出、安装包和自动更新

## 隐私

基础按键映射和配置匹配完全在本机运行。未来的 AI 功能将明确标识数据流向，并支持本地模型。

## 许可证

[MIT License](LICENSE)


# MouseMind（灵触）

MouseMind 是一个面向 Windows 的智能鼠标控制中心。它根据当前前台应用、鼠标按键和手势，在正确的上下文中执行快捷键、文本处理、截图识别或 AI 动作。

## 产品定位

传统鼠标驱动只解决“一个按键映射一个快捷键”。MouseMind 解决的是：

> 在不同应用、不同内容和不同工作状态下，让同一个鼠标按键执行不同的工作流。

典型用户包括程序员、视频剪辑师、设计师、重度办公用户和需要屏幕翻译的用户。

## 核心概念

- **应用配置（Profile）**：按前台进程自动激活，例如浏览器、剪辑软件、代码编辑器。
- **触发器（Trigger）**：侧键、滚轮键、组合键或鼠标手势。
- **动作（Action）**：快捷键、启动程序、文本翻译、AI 总结、截图 OCR 等。
- **动作链（Workflow）**：将复制、处理、写回、通知等动作串联起来。
- **自然语言配置**：将“在浏览器按侧键时总结选中文字”转换成配置。

## MVP 范围

当前第一阶段包含：

1. Windows 桌面控制中心界面；
2. 按应用管理鼠标映射；
3. 内置浏览器、代码编辑、视频剪辑三类预设；
4. 监听全局鼠标侧键并识别当前前台进程；
5. 启用/停用配置、测试动作和实时事件日志；
6. 本地 JSON 持久化配置。

第一阶段暂不直接注入键盘输入，也不会屏蔽鼠标原有侧键行为；监听器只记录和匹配动作，便于先验证交互与配置模型。

## 后续里程碑

### M2：真实动作执行
- SendInput 快捷键执行器
- 启动程序、打开 URL、系统命令
- 动作链和失败回滚
- 托盘常驻与开机启动

### M3：文本与视觉 AI
- 获取选中文本
- 翻译、总结、润色
- 区域截图、OCR、识图
- 本地模型与云 API Provider 抽象

### M4：手势与插件
- 鼠标轨迹识别和环形菜单
- 插件 SDK
- 配置导入导出与社区预设
- 多设备、云同步和使用统计

## 技术结构

- .NET 10 / WPF
- MVVM-friendly 数据模型（当前以轻量 code-behind 快速验证）
- Win32 `WH_MOUSE_LL` 全局鼠标监听
- Win32 前台窗口进程识别
- `%LocalAppData%/MouseMind/profiles.json` 本地配置

## 构建运行

```powershell
dotnet build D:\MouseMind\MouseMind.slnx
dotnet run --project D:\MouseMind\src\MouseMind.App\MouseMind.App.csproj
```

## 项目目录

```text
D:\MouseMind
├─ MouseMind.slnx
├─ README.md
├─ docs\PRODUCT.md
└─ src\MouseMind.App
   ├─ Models
   ├─ Services
   ├─ MainWindow.xaml
   └─ MainWindow.xaml.cs
```


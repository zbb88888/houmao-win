# 功能清单与 Windows 映射

从 houmao-mac 逐一梳理所有功能点，并给出 Windows 原生对应实现方案。

---

## 1. 全局热键唤起窗口

### Mac 实现
- 全局监听 `NSEvent.flagsChanged`，检测双击 Option 键（间隔 < 400ms）。
- 窗口隐藏时 `makeKeyAndOrderFront`，可见时 `orderOut`。

### Windows 实现
- **触发方式**：双击 `Alt` 键（左 Alt，keyCode `VK_LMENU`），与 Mac Option 键位置对应。
- **API**：
  - `RegisterHotKey(hWnd, id, MOD_NOREPEAT, VK_LMENU)` — 注册单次 Alt 按下事件。
  - 在 `WndProc` 中处理 `WM_HOTKEY`，记录上次按下时间，判断双击间隔（< 400ms）。
  - 可选：`SetWindowsHookEx(WH_KEYBOARD_LL, ...)` 监听低级键盘事件，无需窗口句柄，适合更精确的双击检测。
- **窗口显示**：`Window.Show()` + `Activate()`；隐藏：`Window.Hide()`（不退出进程）。
- **防抖**：最小间隔 50ms，防止误触。

---

## 2. 浮动窗口 UI（Spotlight 风格）

### Mac 实现
- `NSVisualEffectView`（`popover` material）磨砂玻璃背景。
- 无标题栏，圆角 12px，宽 680px，高度自适应（最大 400px 结果区域）。
- 可拖拽移动（`isMovableByWindowBackground = true`）。
- `onExitCommand`（Esc）隐藏窗口。

### Windows 实现
- **窗口样式**：`WindowStyle=None`, `AllowsTransparency=True`, `Background=Transparent`。
- **毛玻璃效果**：
  - Windows 11：通过 `DwmSetWindowAttribute` 设置 `DWMWA_SYSTEMBACKDROP_TYPE = DWMSBT_TRANSIENTWINDOW`（Acrylic）或 `DWMSBT_MAINWINDOW`（Mica）。
  - Windows 10：通过 `SetWindowCompositionAttribute(ACCENT_ENABLE_ACRYLICBLURBEHIND)` 实现亚克力效果。
  - Fallback：半透明纯色背景 `#CC1E1E1E`（深色）/ `#CCF5F5F5`（浅色）。
- **圆角**：Windows 11 原生圆角通过 `DwmSetWindowAttribute(DWMWA_WINDOW_CORNER_PREFERENCE, DWMWCP_ROUND)`；Windows 10 用 `Border.CornerRadius`。
- **无边框拖拽**：监听 `MouseLeftButtonDown` 调用 `DragMove()`。
- **Esc 隐藏**：`KeyDown` 事件拦截 `Key.Escape` → `Hide()`。
- **窗口定位**：每次显示时将窗口居中于当前活动显示器（`SystemParameters.WorkArea`）。
- **宽度**：680px（与 Mac 版一致）。
- **阴影**：WPF `DropShadowEffect` 或 DWM 阴影。

---

## 3. 输入框

### Mac 实现
- 自定义 `IMETextField`（`NSTextField` + `NSTextView` 封装），支持 CJK 输入法、上下箭头历史导航、回车提交。
- Placeholder 动态显示当前默认 Provider 名称。

### Windows 实现
- WPF `TextBox`，`AcceptsReturn=False`，字体 16px Medium。
- **输入法**：WPF 原生支持 IME（中文/日文/韩文），无需额外处理。
- **上下箭头**：重写 `PreviewKeyDown`，`Key.Up` 取 `CommandHistory.Previous()`，`Key.Down` 取 `CommandHistory.Next()`，阻止默认行为。
- **回车提交**：`PreviewKeyDown` 拦截 `Key.Enter`（且非 IME 组合中）→ `ViewModel.Submit()`。
- **Placeholder**：`TextBox` 用 `Style` + `Trigger`（`Text.IsEmpty`）叠加半透明 `TextBlock`。
- **自动聚焦**：窗口 `Loaded` / `IsVisibleChanged` 后调用 `TextBox.Focus()` + `Keyboard.Focus()`。

---

## 4. AI 多轮对话（OpenAI 兼容协议）

### Mac 实现
- `AiTxtClient`：`URLSession` + SSE 流式读取，解析 `data: {...}` 行。
- 支持 `reasoning_content`（DeepSeek 等）。
- 会话历史最多 20 条消息，超出后截断最旧的。
- 取消：`Task.cancel()`。

### Windows 实现
- **HTTP 客户端**：`HttpClient`（单例，通过 DI 注入），`SendAsync` 使用 `HttpCompletionOption.ResponseHeadersRead`。
- **SSE 解析**：`StreamReader.ReadLineAsync()` 循环读取，解析 `data: [DONE]` 和 `data: {...}` 行，`System.Text.Json` 反序列化 delta。
- **取消**：`CancellationTokenSource`，UI 按钮调用 `cts.Cancel()`。
- **流式 Token 回显**：通过 `IProgress<string>` 或 `Channel<string>` 将 token 传回 UI 线程（`Dispatcher.InvokeAsync`）。
- **多轮历史**：`List<ChatMessage>` 保留最近 20 条，超出时 `RemoveRange(0, count - maxHistory)`。
- **reasoning_content**：反序列化时同时读取 `reasoning_content` 字段；当 `content` 为空时回退使用 `reasoning_content`。

### 推理标签过滤（关键细节，勿遗漏）

Mac 版在流式和非流式两个路径都会过滤推理过程，需原样复刻：
- **非流式**：用正则 `^[\s\S]*</think>` 剔除最后一个 `</think>` 及之前的全部内容。
- **流式**：维护 `insideThink` 状态机，逐 token 扫描 `<think>` / `</think>` 成对标签，处于 think 区间内的 token 不输出给 UI。
- **输出收尾**：去除首尾空白；若过滤后为空则回退为原始内容。
- Windows 用 C# `Regex` + 状态机等价实现，放在 `AiClient` 内。

---

## 5. `@model` 路由语法

### Mac 实现
- 解析 `@providerName message` 或 `@modelId message`，先按 Provider 名匹配，再按 Model ID 匹配。
- 无 `@` 前缀时使用默认 Provider（列表第一个）。

### Windows 实现
- 逻辑完全相同，用 C# string 解析：`text.StartsWith("@")` → `Split(' ', 2)` 取 mention 和 message。
- `AppSettings.ResolveModel(mention)` 同 Mac 逻辑。

---

## 6. 附件支持（图片 / 音频）

### Mac 实现
- `NSOpenPanel` 多选文件。
- 图片：`NSImage` → JPEG Base64，发送为 `image_url` content part（`data:image/jpeg;base64,...`）。
- 音频：读取文件字节 → Base64，发送为 `input_audio` content part。
- 附件列表显示在输入框下方，可单独删除。

### Windows 实现
- **文件选择**：`Microsoft.Win32.OpenFileDialog`，`Multiselect=true`，`Filter` 限定图片/音频扩展名。
- **图片处理**：`System.Windows.Media.Imaging.BitmapImage` 加载 → 转为 JPEG：`JpegBitmapEncoder` → `MemoryStream` → `Convert.ToBase64String()`。
- **音频处理**：`File.ReadAllBytes(path)` → `Convert.ToBase64String()`，格式取 `Path.GetExtension()`。
- **附件 UI**：`ItemsControl` 横向排列，每项显示缩略图/文件名和删除按钮（`×`）。
- **拖放**：`Window.AllowDrop=True`，监听 `Drop` 事件，解析 `DataFormats.FileDrop`。

---

## 7. 命令历史（上下箭头）

### Mac / Windows 一致逻辑
- `CommandHistory` 类：循环列表，最多 100 条，去重（相同命令移到末尾）。
- `Previous()` / `Next()` 返回历史条目，`null` 时恢复当前输入。
- `Reset()`：提交或清空对话时重置游标到 -1。

---

## 8. 面板切换（`b` / `h` 命令）

### Mac / Windows 一致逻辑
- 输入单字符 `b` 回车 → 切换历史面板。
- 输入单字符 `h` 回车 → 切换帮助面板。
- 再次输入相同字符 → 收起面板。

### Windows 实现
- `Panel` 枚举：`None / Chat / History / Help`。
- 面板区域用 `ContentControl` + `DataTemplateSelector` 或 `Visibility` 绑定切换。
- 面板最大高度 400px，内含 `ScrollViewer`。

---

## 9. 历史记录（HistoryPanel）

### Mac 实现
- 显示 `UsageRecord` 列表（时间戳、应用名、输入文本片段），倒序，支持分页。
- 底部"Clear All"按钮。
- 数据来自 `HistoryStore`（JSON 文件，Write-Behind 防抖刷盘）。

### Windows 实现
- `ListView` / `ItemsControl` 绑定 `ObservableCollection<UsageRecord>`，虚拟化（`VirtualizingPanel`）。
- 分页：首次加载 100 条，滚动到底部时追加加载（`ScrollViewer.ScrollChanged`）。
- **存储**：JSON 文件，路径 `%LOCALAPPDATA%\houmao\usage-history.json`，`System.Text.Json` 序列化，`SemaphoreSlim(1,1)` 保证写入线程安全，防抖 2 秒后落盘（`Task.Delay(2000, ct)`）。

---

## 10. 使用情况追踪（UsageTracker）

### Mac 实现
- 全局监听 `NSEvent.keyDown`（`addGlobalMonitorForEvents`）。
- 监听 `NSWorkspace.didActivateApplicationNotification` 获取当前前台应用。
- 按 Enter 时通过 Accessibility API 读取当前焦点控件的文本（支持 CJK 输入法）。
- 记录：时间戳、应用名、文本。

### Windows 实现
- **键盘钩子**：`SetWindowsHookEx(WH_KEYBOARD_LL)` 全局低级键盘钩子（需要单独线程运行消息循环），累加按键到 `keystrokeBuffer`，按 Enter 提交。
- **前台应用监控**：`SetWinEventHook(EVENT_SYSTEM_FOREGROUND, ...)` 获取前台窗口变化事件，`GetWindowThreadProcessId` + `Process.GetProcessById` 获取进程名。
- **文本读取（CJK）**：通过 `UI Automation`（`AutomationElement.FocusedElement`）读取焦点控件的 `ValuePattern.Value`，避免键盘缓冲区乱码。
- **控件过滤**：排除非文本控件（Button / CheckBox / Menu / Image / Table 等 `ControlType`），只读文本输入控件。
- **回退策略**：若 UIA 读取失败或返回文本长度远超按键数（> 3 倍）则回退使用 `keystrokeBuffer`（与 Mac 逻辑一致）。
- **应用切换记录**：检测到前台应用切换时丢弃未提交输入，并记录一条 `[Switch] A → B` 轨迹（与 Mac 一致）。
- **权限**：仅需普通用户权限（无需 UAC 提升，`WH_KEYBOARD_LL` 在非管理员下工作）。
- **自我过滤**：当前台为 houmao 自身窗口时不记录按键。
- **隐私提示**：首次启动时弹窗提示用户，可在设置中关闭。

---

## 11. 划选即复制（SelectToCopy）

### Mac 实现
- 全局监听 `mouseDown` / `mouseUp`，拖拽距离 > 5px 判定为选取。
- 向前台 App PID 发送 `CGEvent` 模拟 `Cmd+C`。
- 轮询剪贴板 `changeCount` 判断是否复制成功（最多 500ms）。

### Windows 实现
- **鼠标钩子**：`SetWindowsHookEx(WH_MOUSE_LL)` 监听 `WM_LBUTTONDOWN` / `WM_LBUTTONUP`。
- **拖拽判定**：鼠标坐标变化 > 5px（系统拖拽阈值 `SM_CXDRAG`）。
- **模拟复制**：`SendInput` 发送 `Ctrl+C` 虚拟按键（`VK_CONTROL` + `VK_C`）。
- **剪贴板检测**：`Clipboard.GetText()` 前后对比，或监听 `AddClipboardFormatListener`（`WM_CLIPBOARDUPDATE`）。
- **权限**：普通用户权限，无需 Accessibility 授权（与 Mac 不同）。
- **设置开关**：`AppSettings.SelectToCopyEnabled`，默认 false。

---

## 12. 设置界面

### Mac 实现
- macOS `Settings {}` Scene，`⌘,` 打开。
- 列出所有 Provider，可新增/编辑/删除/设为默认（移至顶部）。
- 每个 Provider：名称、API URL、模型列表（逗号分隔）、API Key（SecureField）。
- URL 自动清理 `/v1` 等常见错误后缀。
- 开关：Copy on Selection。

### Windows 实现
- 独立 `SettingsWindow`，`Ctrl+,` 或托盘菜单打开。
- 相同的 Provider 管理 UI：`DataGrid` 或 `ItemsControl` 列表 + 编辑表单。
- API Key 使用 `PasswordBox`（WPF 原生）。
- 设置持久化：`%LOCALAPPDATA%\houmao\settings.json`，`System.Text.Json`。
- 额外设置项：开机启动（写入 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`）、热键自定义（可选）、Copy on Selection 开关。

---

## 13. Markdown 渲染

### Mac 实现
- `Text("...").textSelection(.enabled)` — SwiftUI 内置基础文本，不做 Markdown 解析（当前版本）。

### Windows 实现
- **库**：`Markdig`（解析）+ 自定义 `MarkdownToFlowDocumentConverter`（渲染为 WPF `FlowDocument`）。
- 支持：标题、粗体、斜体、代码块（等宽字体 + 背景色）、行内代码、有序/无序列表、水平线。
- `FlowDocumentScrollViewer` 或 `RichTextBox`（`IsReadOnly=True`）展示，支持文本选择。
- 代码块带"Copy"按钮（悬停时显示）。

---

## 14. 系统托盘

### Mac 实现
- macOS MenuBar 无托盘，应用通过 `NSApp.setActivationPolicy(.regular)` 显示 Dock 图标，隐藏窗口时保持在后台。

### Windows 实现
- **系统托盘图标**：`System.Windows.Forms.NotifyIcon`，托盘菜单使用 `ContextMenuStrip`。
- 右键菜单：Show（显示主窗口）、Settings（打开设置）、Quit（退出）。
- 双击托盘图标等同于双击 Alt（显示主窗口）。
- 应用启动时不显示主窗口，仅托盘图标常驻（`Application.ShutdownMode=OnExplicitShutdown`）。

---

## 15. 键盘快捷键（窗口内）

| Mac 快捷键 | 功能 | Windows 对应 |
|-----------|------|-------------|
| `⌘K` | 清空当前对话 | `Ctrl+K` |
| `⌘L` | 清空所有历史 | `Ctrl+L` |
| `⌘B` | 切换历史面板 | `Ctrl+B` |
| `⌘W` | 隐藏窗口 | `Ctrl+W` / `Esc` |
| `⌘,` | 打开设置 | `Ctrl+,` |
| `↑` / `↓` | 命令历史导航 | `↑` / `↓`（相同） |
| `Esc` | 隐藏窗口 | `Esc`（相同） |

---

## 16. 深色/浅色主题

### Mac 实现
- `@Environment(\.colorScheme)` 自动跟随系统。
- Adaptive colors（`Color.primary.opacity(...)`）。

### Windows 实现
- 检测系统主题：`Registry.GetValue(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1)`。
- 监听主题变化：`SystemEvents.UserPreferenceChanged`。
- 提供 Light / Dark 两套 `ResourceDictionary`，动态切换 `Application.Current.Resources`。
- 使用 `SystemColors` 和自定义颜色 Token。

---

## 17. 开机自启

### Mac 实现
- 未实现（macOS 一般用 LaunchAgent plist）。

### Windows 实现
- 设置项：开启时写入 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Houmao = "<exe path> --startup"`。
- `--startup` 参数：仅显示托盘图标，不弹出主窗口。

---

## 18. 帮助面板（h 命令）

显示快捷键列表和使用说明，静态 XAML 内容，`Visibility` 绑定切换。
帮助内容包含：快捷键表、命令表（`h` / `b` / `@name msg`）、当前已配置 Provider 列表（带 default 标记）。

---

## 19. 后端适配器（openai_adapter，生态组件）

### 说明
- Mac 仓库内的 `openai_adapter/` 是一个独立 Python 组件，把 OpenAI 协议代理到本地 MiniCPM-o 的 `llama-server`（`http://localhost:8080/v1`）。
- 它**不属于客户端应用**，是 houmao 生态的后端推理网关，跨平台通用。

### Windows 影响
- Windows 客户端**无需重新实现**该组件，直接作为一个 Provider 配置即可（URL 指向 `http://localhost:8080`）。
- 仅需在设置中提供"添加本地 Provider"的便捷预设（可选）。
- 该 Python 服务本身可在 Windows 上原样运行（无需移植）。

# 整体架构设计

## 1. 技术栈决策

### 1.1 为何选择 WPF + .NET 9

| 方案 | 优点 | 缺点 | 结论 |
|------|------|------|------|
| **WPF + .NET 9** | 成熟稳定、文档丰富、Win32 互操作完善、Windows 10/11 全覆盖、工具链完善 | 技术较老（2006 年），无原生响应式布局 | **首选** |
| WinUI 3 (WAS) | 最新 UI、原生 Fluent Design、Mica/Acrylic 更简单 | 需要 Windows App SDK，最低 Win10 1809，生态不如 WPF 成熟 | 备选（Windows 11 专项版可考虑） |
| Avalonia | 跨平台 | Windows 特效集成较弱，与 Mac 版风格对齐代价更高 | 不适合 |
| Electron/Tauri | 跨平台、Web 技术 | 资源占用高，与 Windows 原生集成差 | 不适合 |

### 1.2 核心依赖

```xml
<!-- src/Houmao/Houmao.csproj 关键依赖 -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
<PackageReference Include="H.NotifyIcon.Wpf" Version="2.*" />
<PackageReference Include="Markdig" Version="0.40.*" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.*" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="9.*" />
```

---

## 2. 项目目录结构

```
houmao-win/
├── src/
│   └── Houmao/
│       ├── Houmao.csproj
│       ├── App.xaml                  # 应用入口、资源字典
│       ├── App.xaml.cs               # DI 容器初始化、生命周期
│       │
│       ├── Models/                   # 纯数据模型（无逻辑）
│       │   ├── Provider.cs           # Provider + ResolvedModel
│       │   ├── Attachment.cs         # 图片/音频附件
│       │   ├── ChatMessage.cs        # OpenAI 消息结构
│       │   └── UsageRecord.cs        # 使用情况记录
│       │
│       ├── Services/                 # 业务逻辑层（接口 + 实现）
│       │   ├── IAppSettings.cs / AppSettings.cs     # 配置持久化
│       │   ├── IAiClient.cs / AiClient.cs           # LLM HTTP 客户端
│       │   ├── IHistoryStore.cs / HistoryStore.cs   # 历史记录存储
│       │   ├── IUsageTracker.cs / UsageTracker.cs   # 使用追踪
│       │   ├── HotKeyManager.cs                     # 全局热键
│       │   └── SelectToCopyManager.cs               # 划选复制
│       │
│       ├── ViewModels/               # MVVM ViewModel 层
│       │   ├── MainViewModel.cs      # 主窗口状态 + 业务逻辑
│       │   ├── HistoryViewModel.cs   # 历史面板
│       │   └── SettingsViewModel.cs  # 设置窗口
│       │
│       ├── Views/                    # WPF 窗口和控件
│       │   ├── MainWindow.xaml / .cs
│       │   ├── SettingsWindow.xaml / .cs
│       │   └── Controls/
│       │       ├── AttachmentStrip.xaml / .cs
│       │       └── MarkdownViewer.xaml / .cs
│       │
│       ├── Interop/                  # Win32 P/Invoke 封装
│       │   ├── DwmApi.cs             # Acrylic/Mica/圆角
│       │   ├── User32.cs             # RegisterHotKey, SetWindowsHookEx
│       │   └── WinEventHook.cs       # 前台窗口变化监听
│       │
│       └── Resources/
│           ├── Icons/
│           │   └── houmao.ico
│           └── Styles/
│               ├── Colors.Light.xaml
│               ├── Colors.Dark.xaml
│               └── Controls.xaml     # 通用控件样式
│
├── tests/
│   └── Houmao.Tests/
│       ├── Services/
│       │   ├── AppSettingsTests.cs
│       │   └── AiClientTests.cs
│       └── ViewModels/
│           └── MainViewModelTests.cs
│
├── scripts/
│   ├── build.ps1                     # Debug/Release 构建
│   └── publish.ps1                   # 发布单文件 exe
│
└── docs/
    ├── architecture.md               # 本文件
    ├── feature-spec.md
    └── development-plan.md
```

---

## 3. 模块架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                         Views Layer                             │
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │   MainWindow    │  │  SettingsWindow  │  │   NotifyIcon  │  │
│  │  (XAML + Code)  │  │  (XAML + Code)   │  │   (Tray UI)   │  │
│  └────────┬────────┘  └────────┬─────────┘  └───────┬───────┘  │
└───────────│──────────────────────────────────────────│──────────┘
            │ DataBinding                              │ Commands
┌───────────▼──────────────────────────────────────────▼──────────┐
│                       ViewModels Layer                          │
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │  MainViewModel   │  │ HistoryViewModel │  │SettingsViewModel│ │
│  │  (CommunityToolkit│  │                  │  │               │  │
│  │   ObservableObject│  └──────────────────┘  └───────────────┘  │
│  └────────┬─────────┘                                           │
└───────────│─────────────────────────────────────────────────────┘
            │ Calls Service Interfaces
┌───────────▼─────────────────────────────────────────────────────┐
│                        Services Layer                           │
│  ┌───────────┐  ┌────────────┐  ┌─────────────┐  ┌──────────┐  │
│  │ AiClient  │  │ AppSettings│  │HistoryStore │  │UsageTracker│ │
│  │ (HttpClient│  │(JSON file) │  │ (JSON file) │  │(LL Hooks) │  │
│  │  SSE)     │  └────────────┘  └─────────────┘  └──────────┘  │
│  └───────────┘                                                  │
│  ┌────────────────┐  ┌─────────────────────┐                   │
│  │ HotKeyManager  │  │ SelectToCopyManager │                   │
│  │ (RegisterHotKey│  │ (WH_MOUSE_LL)       │                   │
│  │  / LL Hook)    │  └─────────────────────┘                   │
│  └────────────────┘                                             │
└─────────────────────────────────────────────────────────────────┘
            │
┌───────────▼─────────────────────────────────────────────────────┐
│                       Interop Layer                             │
│  DwmApi.cs  │  User32.cs  │  WinEventHook.cs                   │
│  (Acrylic/Mica/CornerRadius│  RegisterHotKey / Hooks / SendInput)│
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. 核心数据流

### 4.1 用户提交查询流程

```
User types in TextBox → [Enter]
    ↓
MainViewModel.Submit()
    ├─ Parse @mention → AppSettings.ResolveModel()
    ├─ CommandHistory.Add()
    ├─ AttachmentProcessor.Encode()
    └─ AiClient.AskStreamAsync(question, history, attachments, onToken, ct)
            ↓ HttpClient SSE
         foreach token → Dispatcher.InvokeAsync(UpdateUI)
            ↓ Complete
         AppendToConversationHistory()
         HistoryStore.AppendAsync(record)
```

### 4.2 全局热键唤起流程

```
User double-taps Alt
    ↓
HotKeyManager (WH_KEYBOARD_LL 线程)
    ↓ double-click 判定（< 400ms 间隔）
    ↓ Dispatcher.InvokeAsync
MainWindow.ToggleVisibility()
    ├─ if Visible → Hide()
    └─ if Hidden → CenterOnScreen() → Show() → Activate() → TextBox.Focus()
                      ↓
                 MainViewModel.ClearConversation()（重置状态）
```

### 4.3 配置持久化流程

```
AppSettings (内存中的 ObservableObject)
    ↓ 属性变化（CollectionChanged / PropertyChanged）
    ↓ debounce 500ms
    ↓ System.Text.Json.Serialize
    → %LOCALAPPDATA%\houmao\settings.json
```

---

## 5. 关键技术实现细节

### 5.1 Acrylic / Mica 窗口效果

```csharp
// Interop/DwmApi.cs
public static class DwmApi
{
    // Windows 11 22H2+: Acrylic 系统背板
    public static void SetAcrylic(IntPtr hWnd)
    {
        int attr = (int)DwmSystemBackdropType.Acrylic; // 3
        DwmSetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE,
            ref attr, sizeof(int));
    }

    // Windows 11: 圆角
    public static void SetRoundCorners(IntPtr hWnd)
    {
        int pref = (int)DwmWindowCornerPreference.Round;
        DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE,
            ref pref, sizeof(int));
    }

    // Windows 10 Fallback: Composition Attribute
    public static void SetAcrylicWin10(IntPtr hWnd, uint tintColor = 0x990F0F0F)
    {
        var data = new AccentPolicy
        {
            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = 2,
            GradientColor = tintColor
        };
        // ... SetWindowCompositionAttribute
    }
}
```

### 5.2 SSE 流式读取

```csharp
// Services/AiClient.cs
public async IAsyncEnumerable<string> StreamAsync(
    ChatRequest req,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
    request.Content = JsonContent.Create(req);

    using var response = await _http.SendAsync(request,
        HttpCompletionOption.ResponseHeadersRead, ct);
    response.EnsureSuccessStatusCode();

    using var stream = await response.Content.ReadAsStreamAsync(ct);
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream && !ct.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync(ct);
        if (line is null) break;
        if (!line.StartsWith("data: ")) continue;

        var data = line["data: ".Length..];
        if (data == "[DONE]") yield break;

        var chunk = JsonSerializer.Deserialize<ChatStreamChunk>(data, _jsonOptions);
        var token = chunk?.Choices?[0]?.Delta?.Content;
        if (token is not null) yield return token;
    }
}
```

> 注：`AiClient` 还需在 token 流上叠加 `<think>...</think>` 过滤状态机（参见 feature-spec §4），只向 UI 输出非推理内容；非流式路径用正则 `^[\s\S]*</think>` 剔除。

### 5.3 全局低级键盘钩子（双击 Alt 检测）

```csharp
// Services/HotKeyManager.cs
public sealed class HotKeyManager : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int VK_LMENU = 0xA4;       // 左 Alt

    private readonly TimeSpan _doubleClickInterval = TimeSpan.FromMilliseconds(400);
    private readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(50);
    private DateTime _lastAltDown = DateTime.MinValue;
    private IntPtr _hook;
    private readonly Thread _hookThread;

    public event EventHandler? DoubleAltPressed;

    public HotKeyManager()
    {
        // 钩子必须在有消息循环的线程上注册
        _hookThread = new Thread(RunHookLoop) { IsBackground = true };
        _hookThread.Start();
    }

    private void RunHookLoop()
    {
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, HookCallback,
            GetModuleHandle(null), 0);
        // Win32 消息泵
        while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WM_KEYDOWN)
        {
            var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (info.vkCode == VK_LMENU)
            {
                var now = DateTime.UtcNow;
                var elapsed = now - _lastAltDown;
                if (elapsed < _doubleClickInterval && elapsed > _minInterval)
                {
                    DoubleAltPressed?.Invoke(this, EventArgs.Empty);
                    _lastAltDown = DateTime.MinValue;
                }
                else
                {
                    _lastAltDown = now;
                }
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }
}
```

### 5.4 DI 容器初始化（App.xaml.cs）

```csharp
public partial class App : Application
{
    private ServiceProvider _services = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        // 启动全局服务
        _services.GetRequiredService<HotKeyManager>();
        _services.GetRequiredService<UsageTracker>().Start();

        // 显示托盘图标（不显示主窗口）
        var mainWindow = _services.GetRequiredService<MainWindow>();
        mainWindow.Hide();  // 静默启动

        if (!e.Args.Contains("--startup"))
            mainWindow.ShowAndActivate();
    }

    private static void ConfigureServices(IServiceCollection s)
    {
        // Services（单例）
        s.AddSingleton<IAppSettings, AppSettings>();
        s.AddSingleton<IHistoryStore, HistoryStore>();
        s.AddSingleton<IUsageTracker, UsageTracker>();
        s.AddSingleton<HotKeyManager>();
        s.AddSingleton<SelectToCopyManager>();
        s.AddHttpClient<IAiClient, AiClient>();  // HttpClient 托管

        // ViewModels
        s.AddSingleton<MainViewModel>();
        s.AddSingleton<HistoryViewModel>();
        s.AddTransient<SettingsViewModel>();

        // Windows
        s.AddSingleton<MainWindow>();
        s.AddTransient<SettingsWindow>();
    }
}
```

---

## 6. 数据存储方案

| 数据 | 存储位置 | 格式 | 说明 |
|------|---------|------|------|
| Provider 配置 | `%LOCALAPPDATA%\houmao\settings.json` | JSON | 包含 API Key，文件权限 600 |
| 使用历史 | `%LOCALAPPDATA%\houmao\usage-history.json` | JSON Array | 追加写，防抖 2s 落盘 |
| 窗口位置 | `%LOCALAPPDATA%\houmao\settings.json` | JSON | 上次关闭前的位置 |
| 开机启动 | `HKCU\...\Run` | 注册表 | 可选，用户开关 |

**安全注意**：API Key 存储在本地 JSON 文件中，权限设为仅当前用户可读。不将 Key 写入注册表或 `%APPDATA%` 公共目录。

---

## 7. 线程模型

```
UI Thread (STA)
  ├── WPF 主消息循环
  ├── MainWindow / SettingsWindow
  └── ViewModel（通过 Dispatcher 更新 UI）

Background Thread (HotKey)
  └── Win32 消息泵（SetWindowsHookEx 所在线程）
      → DoubleAltPressed 事件 → Dispatcher.InvokeAsync → UI Thread

Background Thread (UsageTracker)
  └── WinEventHook + LL Keyboard Hook 消息循环

ThreadPool / async
  ├── AiClient.StreamAsync（HttpClient I/O）
  ├── HistoryStore 磁盘写入（SemaphoreSlim 保护）
  └── AppSettings 持久化（debounce Task.Delay）
```

---

## 8. 窗口行为规范

- **启动**：仅显示托盘图标；`--startup` 参数同样仅托盘。
- **双击 Alt**：窗口在屏幕中央（当前活动显示器）显示/隐藏。
- **显示时**：清空上次对话内容，输入框自动聚焦。
- **Esc / Ctrl+W**：隐藏窗口（不退出进程）。
- **失去焦点**：不自动隐藏（防止点击附件对话框时窗口消失）。
- **任务栏**：窗口不显示在任务栏（`ShowInTaskbar=False`）。
- **置顶**：`Topmost=True`，确保浮于其他窗口之上。
- **多显示器**：窗口居中于鼠标所在显示器。

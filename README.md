# houmao-win

houmao 的 Windows 原生实现。一个 Spotlight 风格的全局 AI 快捷查询工具，双击 Alt 键召唤浮动输入窗口，支持多 Provider、多轮对话、图片/音频附件、使用历史追踪。

## 项目状态

🚧 **开发中** - 阶段 0-5 已完成，阶段 6（打包发布）待完成

详见 [DEVELOPMENT.md](DEVELOPMENT.md) 了解详细开发进度。

## 快速导航

| 文档 | 说明 |
|------|------|
| [docs/architecture.md](docs/architecture.md) | 整体架构、模块划分、数据流 |
| [docs/feature-spec.md](docs/feature-spec.md) | 从 Mac 版梳理的完整功能清单与 Windows 映射 |
| [docs/development-plan.md](docs/development-plan.md) | 细粒度开发任务、分阶段里程碑 |
| [docs/test-checklist.md](docs/test-checklist.md) | 回归测试清单 |
| [docs/release-notes-2026-06.md](docs/release-notes-2026-06.md) | 最近一轮实现与迁移说明 |

## 技术栈速览

- **语言 / 运行时**：C# 13 · .NET 9
- **UI 框架**：WPF（XAML + MVVM）
- **窗口特效**：DWM Acrylic（Win10）/ Mica（Win11）
- **全局热键**：Win32 `RegisterHotKey` P/Invoke
- **AI 通信**：`HttpClient` SSE 流式
- **系统托盘**：`System.Windows.Forms.NotifyIcon`
- **MVVM 工具**：`CommunityToolkit.Mvvm`
- **Markdown 渲染**：`Markdig` + 自定义 FlowDocument 转换器

## 快速开始

### 前提条件

- .NET 9 SDK
- Windows 10/11

### 使用构建脚本

项目提供了多种构建方式：

#### Windows 批处理（推荐）

```batch
make.bat build       # Debug 构建
make.bat release     # Release 构建
make.bat test        # 运行测试
make.bat publish     # 发布单文件
make.bat run         # 构建并运行
make.bat clean       # 清理构建产物
make.bat check       # 检查项目结构
make.bat install     # 检查并构建
make.bat help        # 显示帮助
```

#### PowerShell

```powershell
.\scripts\make.ps1 build
.\scripts\make.ps1 test
.\scripts\make.ps1 publish
```

#### Makefile（需要安装 make）

```bash
make build
make test
make publish
make run
```

#### 原生 dotnet 命令

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Houmao/Houmao.csproj
```

## 项目结构

```
houmao-win/
├── src/Houmao/          # 主应用
├── tests/Houmao.Tests/  # 单元测试
├── scripts/             # 构建/发布脚本
└── docs/                # 设计文档
```

## 主要功能

- ✅ 全局热键唤起窗口（双击 Alt）
- ✅ Spotlight 风格浮动窗口
- ✅ AI 多轮对话（OpenAI 兼容协议）
- ✅ 流式输出和取消
- ✅ 推理标签过滤
- ✅ 图片/音频附件支持
- ✅ 命令历史导航
- ✅ 面板切换（历史/帮助）
- ✅ 设置界面
- ✅ 系统托盘
- ✅ 使用情况追踪
- ✅ 划选即复制
- ✅ 深色/浅色主题
- ✅ 开机自启

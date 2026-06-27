# 项目总结

## 项目概述

**houmao-win** 是 houmao 的 Windows 原生实现，一个 Spotlight 风格的全局 AI 快捷查询工具。

## 已完成功能

### 核心功能

1. **全局热键唤起** - 双击 Alt 键显示/隐藏窗口
2. **浮动窗口 UI** - Spotlight 风格，毛玻璃效果，圆角，阴影
3. **AI 多轮对话** - 支持 OpenAI 兼容协议，流式输出
4. **推理标签过滤** - 自动过滤 `<think>...</think>` 标签
5. **多 Provider 支持** - 可配置多个 AI 服务提供商
6. **附件支持** - 图片和音频文件上传
7. **命令历史** - 上下箭头导航历史命令
8. **面板切换** - 历史面板和帮助面板
9. **设置界面** - Provider 管理和通用设置
10. **系统托盘** - 托盘图标和右键菜单
11. **使用追踪** - 记录其他应用的使用情况
12. **划选即复制** - 全局鼠标选择自动复制
13. **主题支持** - 深色/浅色主题切换
14. **开机自启** - Windows 启动时自动运行

### 技术实现

- **MVVM 架构** - 使用 CommunityToolkit.Mvvm
- **依赖注入** - Microsoft.Extensions.DependencyInjection
- **Win32 互操作** - 全局热键、窗口特效、UI Automation
- **流式 HTTP** - HttpClient SSE 流式读取
- **Markdown 渲染** - Markdig + 自定义 FlowDocument 转换器
- **配置持久化** - JSON 文件存储

## 项目结构

```
houmao-win/
├── src/Houmao/                    # 主应用
│   ├── App.xaml                   # 应用入口
│   ├── Models/                    # 数据模型
│   ├── Services/                  # 业务逻辑服务
│   ├── ViewModels/                # MVVM ViewModel
│   ├── Views/                     # WPF 窗口和控件
│   ├── Interop/                   # Win32 API 封装
│   ├── Converters/                # 值转换器
│   └── Resources/                 # 资源文件
├── tests/Houmao.Tests/            # 单元测试
├── scripts/                       # 构建脚本
└── docs/                          # 文档
```

## 开发进度

- ✅ 阶段 0：仓库与工程骨架
- ✅ 阶段 1：主窗口与输入链路
- ✅ 阶段 2：AI 客户端
- ✅ 阶段 3：配置与设置
- ✅ 阶段 4：历史与使用追踪
- ✅ 阶段 5：全局热键与辅助功能
- 🚧 阶段 6：打包发布

## 待完成工作

1. **图标文件** - 需要创建真实的 ICO 图标文件
2. **打包发布** - 创建安装程序和便携版
3. **错误处理** - 完善异常处理和用户提示
4. **性能优化** - 历史记录加载和内存管理
5. **测试覆盖** - 添加更多单元测试
6. **文档完善** - API 文档和用户手册

## 使用说明

### 首次使用

1. 运行应用程序
2. 双击 Alt 键打开窗口
3. 输入 `h` 查看帮助信息
4. 在设置中配置 AI Provider

### 配置 Provider

1. 双击 Alt 键打开窗口
2. 按 `Ctrl+,` 打开设置
3. 在 Providers 标签页添加 Provider
4. 填写 API URL、模型列表和 API Key
5. 设置默认 Provider

### 使用技巧

- `@provider 消息` - 使用特定 Provider
- `@model 消息` - 使用特定模型
- `b` - 切换历史面板
- `h` - 切换帮助面板
- `↑`/`↓` - 浏览命令历史
- `Esc` 或 `Ctrl+W` - 隐藏窗口
- `Ctrl+K` - 清空当前对话

## 技术细节

### 依赖包

- CommunityToolkit.Mvvm - MVVM 框架
- H.NotifyIcon.Wpf - 系统托盘图标
- Markdig - Markdown 解析
- Microsoft.Extensions.DependencyInjection - 依赖注入
- Microsoft.Extensions.Logging - 日志记录

### 系统要求

- Windows 10 版本 1903 或更高
- .NET 9 运行时（框架依赖版本）
- 至少 100MB 可用磁盘空间

### 配置文件位置

- 设置：`%LOCALAPPDATA%\houmao\settings.json`
- 历史：`%LOCALAPPDATA%\houmao\usage-history.json`
- 日志：`%LOCALAPPDATA%\houmao\logs\`

## 贡献指南

1. Fork 项目
2. 创建功能分支
3. 提交更改
4. 创建 Pull Request

## 许可证

本项目采用 MIT 许可证。
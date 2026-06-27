# 构建指南

本文档介绍如何构建、测试和发布 Houmao Windows 应用。

## 前提条件

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows 10 或 Windows 11

## 快速开始

### 检查环境

```batch
make.bat check
```

### 一键安装

```batch
make.bat install
```

### 构建并运行

```batch
make.bat run
```

## 命令参考

### Windows 批处理 (make.bat)

| 命令 | 说明 |
|------|------|
| `make.bat build` | Debug 构建 |
| `make.bat release` | Release 构建 |
| `make.bat test` | 运行单元测试 |
| `make.bat publish` | 发布单文件可执行程序 |
| `make.bat clean` | 清理构建产物 |
| `make.bat check` | 检查项目结构和依赖 |
| `make.bat install` | 检查环境并构建 |
| `make.bat run` | 构建并运行 (Debug) |
| `make.bat format` | 格式化代码 |
| `make.bat restore` | 恢复 NuGet 包 |
| `make.bat help` | 显示帮助信息 |

### PowerShell (scripts/make.ps1)

```powershell
.\scripts\make.ps1 build      # Debug 构建
.\scripts\make.ps1 release    # Release 构建
.\scripts\make.ps1 test       # 运行测试
.\scripts\make.ps1 publish    # 发布
.\scripts\make.ps1 clean      # 清理
.\scripts\make.ps1 check      # 检查
.\scripts\make.ps1 install    # 安装
.\scripts\make.ps1 run        # 运行
.\scripts\make.ps1 format     # 格式化
.\scripts\make.ps1 restore    # 恢复包
```

### Makefile

```bash
make build              # Debug 构建
make build-release      # Release 构建
make test               # 运行测试
make publish            # 发布单文件
make publish-self-contained  # 发布自包含版本
make clean              # 清理
make check              # 检查
make install            # 安装
make run                # 运行 (Debug)
make run-release        # 运行 (Release)
make format             # 格式化
make restore            # 恢复包
make help               # 帮助
```

## 构建配置

### Debug 模式

- 包含调试符号
- 优化关闭
- 输出路径: `src/Houmao/bin/Debug/net9.0-windows/`

### Release 模式

- 优化开启
- 输出路径: `src/Houmao/bin/Release/net9.0-windows/`

## 发布选项

### 标准发布

```batch
make.bat publish
```

生成单文件可执行程序，输出到 `publish/` 目录。

### 自包含发布

```powershell
.\scripts\publish.ps1 -Configuration Release -SelfContained -SingleFile
```

生成包含 .NET 运行时的独立可执行文件。

## 测试

### 运行所有测试

```batch
make.bat test
```

### 运行特定测试

```bash
dotnet test --filter "FullyQualifiedName~AiClientTests"
```

## 常见问题

### Q: 找不到 .NET SDK

A: 请安装 .NET 9 SDK: https://dotnet.microsoft.com/download/dotnet/9.0

### Q: 构建失败

A: 尝试清理后重新构建:
```batch
make.bat clean
make.bat build
```

### Q: 测试失败

A: 检查测试输出，确保所有依赖已恢复:
```batch
make.bat restore
make.bat test
```

## 目录结构

```
houmao-win/
├── src/
│   └── Houmao/           # 主应用项目
│       ├── Models/        # 数据模型
│       ├── Services/      # 业务服务
│       ├── ViewModels/    # MVVM 视图模型
│       ├── Views/         # WPF 窗口和控件
│       ├── Interop/       # Win32 P/Invoke
│       ├── Converters/    # 值转换器
│       └── Resources/     # 资源文件
├── tests/
│   └── Houmao.Tests/     # 单元测试项目
├── scripts/               # 构建脚本
│   ├── build.ps1          # 构建脚本
│   ├── publish.ps1        # 发布脚本
│   ├── check.ps1          # 检查脚本
│   └── make.ps1           # 统一入口脚本
├── docs/                  # 设计文档
├── Makefile               # Make 构建文件
├── make.bat               # Windows 批处理入口
└── README.md              # 项目说明
```

# UI 重构方案 · 极简风格

> 目标：把现有"Material 蓝 + 多 Tab 设置 + 角标徽章"的臃肿界面，重构为一套
> **单色克制、留白充足、零冗余**的极简界面。视觉上向 Raycast / Spotlight / Linear
> 这类工具看齐，而不是传统的 Material Design 表单。

---

## 1. 设计原则

| 原则 | 具体做法 |
|------|---------|
| **单一强调色** | 全局只保留一个 accent 色，其余全部用中性灰阶。去掉 Material 蓝 `#1976D2`、藏青 `#1A237E`。 |
| **留白即层次** | 用间距和字重区分层级，而不是用边框、底色块、分隔线。 |
| **去装饰** | 去掉圆形徽章、阴影叠加、渐变、彩色气泡。圆角统一收敛到一个值。 |
| **零冗余控件** | 一个动作一个入口。删除重复按钮、重复开关、独立的编辑 Tab。 |
| **即时反馈** | 状态用文字/透明度变化表达，不弹额外对话框。 |
| **键盘优先** | 所有操作可用键盘完成，鼠标是补充。 |

---

## 2. 设计 Token（统一颜色与尺寸）

### 2.1 颜色 —— 从"彩色"收敛到"灰阶 + 单强调色"

只保留下面这套 Token，删除 `Colors.Dark.xaml` / `Colors.Light.xaml` 中其余条目。

```
Dark 主题
  Bg            #1C1C1E   窗口背景（单层，不再有 TitleBar/StatusBar 多层底色）
  Surface       #262628   悬浮/输入区（仅在需要时叠加，透明度区分）
  Border        #FFFFFF @ 8%   细描边，统一 0.5–1px
  Text          #F2F2F2   主文本
  TextSecondary #8E8E93   次要文本/placeholder/时间戳
  Accent        #0A84FF   唯一强调色（光标、选中、默认标记、链接）
  AccentMuted   #0A84FF @ 14%  强调色弱化背景（hover、选区）

Light 主题
  Bg            #FFFFFF
  Surface       #F5F5F7
  Border        #000000 @ 8%
  Text          #1C1C1E
  TextSecondary #8E8E93
  Accent        #0A84FF
  AccentMuted   #0A84FF @ 12%
```

> 关键变化：**不再用实心色块区分区域**。用户气泡、助手气泡、状态栏、标题栏等
> 原本各自的底色全部取消，统一为透明/单层背景，靠间距和字重分层。

### 2.2 尺寸与排版

```
圆角     10px（窗口）/ 6px（内部元素），全局只用这两个值
间距     基础单位 4px：内边距 12/16，元素间距 8，分组间距 20
字体     Segoe UI Variable（Win11）/ Segoe UI（Win10）
字号     输入 16 · 正文 14 · 次要 12 · 标题 15(SemiBold)
字重     Regular 为主，仅标题/标签用 SemiBold，删除多余 Bold
行高     1.5（正文可读性）
描边     0.5px（Win 高分屏）统一，删除 1px 粗边
阴影     单层柔和阴影：BlurRadius 32, Opacity 0.18, ShadowDepth 8
         （删除现有 0.1 透明度的"几乎不可见"阴影）
```

---

## 3. 主窗口重构（输入 + 结果一体）

### 3.1 现状问题
- 主窗口只是一根 600×48 的输入条，结果面板（`ChatPanel`/`HistoryPanel`/`HelpPanel`）
  并没有真正整合进来。
- 输入条左侧蓝色圆形数字角标 + 右侧蓝色 ↑ 圆钮，视觉重、信息含糊。

### 3.2 目标形态：单窗向下展开（Spotlight 模式）

```
┌──────────────────────────────────────────────┐
│  ⌕   Ask anything…                            │  ← 输入行（高 52，无图标按钮）
├──────────────────────────────────────────────┤  ← 仅在有内容时出现的发丝分隔线
│  [缩略图] [缩略图]                              │  ← 附件行（有附件才显示）
│                                                │
│  Q  上一条问题                                  │  ← 结果区（自适应高度，最大 420）
│                                                │
│  A  助手回复（Markdown）…                       │
│                                                │
└──────────────────────────────────────────────┘
```

要点：
- **宽度 680**（与 Mac 对齐，比现在 600 更舒展），**输入行高 52**。
- **没有发送按钮**：回车发送已足够；删除右侧 ↑ 圆钮。
- **没有蓝色角标**：附件直接以缩略图行展示，不用数字徽章。
- **左侧一个极淡的搜索/状态字形**（`⌕` 或细 caret），仅作视觉锚点，非按钮。
- **空状态**：只显示输入行，窗口高度 = 52 + 边距，干净。
- **有结果**：窗口高度随内容增长，最大 ~480，超出滚动。
- **分隔**：输入区与结果区之间只用一条 `Border @ 8%` 发丝线，不用色块。
- **加载态**：结果区内联显示 `A` + 一个低调的省略号动画或 `Thinking…` 次要文字；
  不再叠加右上角取消控件，`Esc` 继续保留为窗口级隐藏快捷键。

### 3.3 消息呈现：去气泡

- **删除彩色气泡**。用户问句用 `Q` 前缀 + 次要色小标签；助手回复用 `A` 前缀 + 正文。
- 两者左对齐、全宽，不再左右分栏、不再 `MaxWidth=600` 的浮动气泡。
- 模型名用一个**极小的文字标签**（次要色，无底色块），删除当前蓝底白字标签。
- 代码块：`Surface` 背景 + 6px 圆角 + 等宽字体，hover 时右上角出现纯文字 `Copy`。

---

## 4. 设置窗口重构（去 Tab、去冗余）

### 4.1 现状问题
- `TabControl` 三个 Tab：Providers / **Edit Provider** / General。
- "Edit Provider" 独立成 Tab —— 编辑和列表割裂，要在 Tab 间跳。
- `DataGrid` 四列（Name/URL/Models/Default）信息密度过高。
- 按钮泛滥：Add / Edit / Delete / Set Default / Save All / Close。
- General 里 `Follow System Theme` 与 `Theme` 下拉**语义重叠**。

### 4.2 目标形态：单页 + 行内编辑

```
┌──────────────────────────────────────────────┐
│  Settings                                  ✕  │
│                                                │
│  PROVIDERS                                     │  ← 次要色小标题
│  ┌──────────────────────────────────────────┐ │
│  │ ● OpenAI            gpt-4o, gpt-4o-mini   │ │  ← 行内展示，● = 默认
│  │ ○ DeepSeek          deepseek-chat         │ │     hover 显示编辑/删除/设默认
│  └──────────────────────────────────────────┘ │
│  + Add provider                                │  ← 纯文字按钮
│                                                │
│  GENERAL                                       │
│  ▢ Start with Windows                          │  ← 极简开关（Toggle）
│  ▢ Copy on selection                           │
│  ▢ Track usage history                         │
│  Theme        ( System ▾ )                     │  ← 单一下拉含 System/Light/Dark
│                                                │
└──────────────────────────────────────────────┘
```

要点：
- **删除整个"Edit Provider"Tab**。点击某 Provider 行 → **就地展开**编辑（名称 / URL /
  Models / API Key 四个输入），保存即收起。新增也是在列表底部就地展开。
- **删除 `DataGrid`**，改用轻量 `ItemsControl` 列表：每行只显示「名称 + 模型摘要」，
  默认项用一个 `●` 圆点表示，**删掉 Default 复选列**。
- **行内悬停操作**：鼠标悬停某行时，右侧淡出显示三个**纯文字/图标按钮**
  （Set default · Edit · Delete），不常驻、不占位。
- **删除按钮堆**：去掉 `Save All` 和顶部 Add/Edit/Delete/Set Default 一排。
  编辑表单只保留 `Save` / `Cancel`；保存即写盘，无需"Save All"。
- **合并主题设置**：删除 `Follow System Theme` 复选框，主题下拉直接含
  `System / Light / Dark` 三项（默认 System）。
- **窗口本身**：无边框或极简标题，右上角一个 `✕`，整体 480 宽、内容自适应高，
  不要现在的 600×500 固定大框 + 20px 外边距。
- **API Key**：`PasswordBox` 保留，但与其它输入框统一极简样式（无粗边、下划线式）。

---

## 5. 控件样式重构（Controls.xaml）

逐项替换 `Resources/Styles/Controls.xaml`：

| 控件 | 现状 | 重构后 |
|------|------|--------|
| Button | Material 蓝底、MinWidth 80、圆角 4 | 透明底/无边框文字按钮为主；主操作用 Accent 文字色；hover 用 `AccentMuted` 背景；圆角 6 |
| WindowButton | 24×24 灰底 hover | 保留尺寸，hover 用 `Surface`，图标改细线风格 |
| InputTextBox | 两套几乎重复的样式 | **合并为一个** `InputStyle`，placeholder 用 `TextSecondary` |
| PasswordBox | 1px 边框 + 底色 | 改为下划线式：底部 0.5px `Border`，聚焦时变 `Accent` |
| DataGrid | 完整网格线 + 交替行底色 | **删除**（设置不再用 DataGrid） |
| TabControl/TabItem | 自定义 Tab 模板 | **删除**（设置不再用 Tab） |
| ScrollBar | 8px 滑块 | 改 6px、`TextSecondary @ 30%`、hover 加深，overlay 不占布局 |
| ToggleSwitch | 无 | **新增**一个极简开关样式替代 CheckBox（General 设置项） |

删除以下不再需要的转换器引用（设置改版后）：
`ListToStringConverter`（DataGrid 列）、`NullToBoolConverter`（按钮启用态）、
`UserMessageBackgroundConverter` / `UserMessageAlignmentConverter`（去气泡后不需要）。

---

## 6. 窗口外观（毛玻璃 + 圆角）

- 主窗与设置窗统一启用 **Mica/Acrylic**（已在 `Interop/DwmApi.cs`）：
  - Win11：`DWMWA_SYSTEMBACKDROP_TYPE = Acrylic`，`DWMWA_WINDOW_CORNER_PREFERENCE = Round`。
  - Win10：`SetWindowCompositionAttribute` 亚克力，外层 `Border` 做 10px 圆角。
  - Fallback：`Bg` 单色 + 单层柔和阴影。
- **删除**现有主窗 `Background="#1E1E1E"` 的硬编码底色和 `#2D2D2D` 内层 Border，
  改为透明 + 毛玻璃，让背景真正"极简"。

---

## 7. 落地步骤（建议顺序）

1. **Token 层**：重写 `Colors.Dark.xaml` / `Colors.Light.xaml` 为 §2.1 的精简 Token。
2. **控件层**：按 §5 重构 `Controls.xaml`，新增 ToggleSwitch，删除 DataGrid/Tab 样式。
3. **主窗**：`MainWindow.xaml` 去蓝色徽章/发送钮，整合结果区为向下展开（§3）。
4. **消息**：`ChatPanel.xaml` 去气泡、去 MaxWidth、改 Q/A 前缀布局（§3.3）。
5. **设置**：`SettingsWindow.xaml` 删 Tab、删 DataGrid，改单页 + 行内编辑（§4）。
6. **窗口外观**：主窗与设置窗接入毛玻璃 + 圆角，去硬编码底色（§6）。
7. **清理**：删除无用转换器、无用样式、`houmao_hotkey.log` 这类调试写文件代码。

---

## 8. 验收标准

- 全局可见颜色 ≤ 6 个（灰阶 + 1 强调色），无 Material 蓝/藏青。
- 设置窗口无 Tab、无 DataGrid，Provider 增删改在同一页完成。
- 主窗空状态只有一行输入，无任何按钮/徽章；有结果时平滑向下展开。
- 消息无彩色气泡，统一左对齐 Q/A 排版。
- 设置项无语义重复（主题合一），按钮无冗余。
- Win11 下窗口为圆角毛玻璃，Win10 有合理 fallback。

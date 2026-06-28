# Houmao Windows Release Notes - 2026-06

This note summarizes the recent stabilization, compatibility, testing, and documentation work in houmao-win.

## Highlights

- Main Windows implementation is building and tests are passing.
- Legacy UI drift items were rechecked against current code and active docs were aligned.
- Test-project warning cleanup reduced build noise.
- Tray implementation no longer depends on `H.NotifyIcon.Wpf`.
- The tray compatibility warning on `.NET 9` was removed at the root.
- A Windows regression checklist now exists in `docs/test-checklist.md`.

## Build and Test Status

Current verified state:
- `dotnet build src/Houmao/Houmao.csproj` succeeds.
- `dotnet test tests/Houmao.Tests/Houmao.Tests.csproj` passes.
- Test count verified: 26 passed, 0 failed.

## Codebase Stabilization

### UI and panel status revalidated

Behavior confirmed in current code:
- Main window uses a single chat region.
- History and help panels are switched through current panel content binding.
- Current panel controls use the active token set.
- Inline loading indicator behavior is aligned with the current UI direction.

Files revalidated:
- `src/Houmao/Views/MainWindow.xaml`
- `src/Houmao/Views/Controls/HistoryPanel.xaml`
- `src/Houmao/Views/Controls/HelpPanel.xaml`

### Test warning cleanup

Changes:
- Enabled nullable in the test project.
- Fixed local test warnings around async iterator cancellation and unnecessary async usage.

Files changed:
- `tests/Houmao.Tests/Houmao.Tests.csproj`
- `tests/Houmao.Tests/ViewModels/MainViewModelTests.cs`

User impact:
- No runtime behavior change.
- Lower build noise for ongoing development.

## Tray Compatibility Fix

### Root-cause change

Previous state:
- The app used `H.NotifyIcon.Wpf 2.4.1`.
- On `net9.0-windows`, that package restored against older framework assets and produced `NU1701` compatibility warnings.

Current state:
- Tray integration now uses `System.Windows.Forms.NotifyIcon`.
- Tray menu uses `ContextMenuStrip`.
- Tray double-click and context-menu actions preserve the previous behavior.

Files changed:
- `src/Houmao/Houmao.csproj`
- `src/Houmao/App.xaml.cs`

User impact:
- No intended behavior loss.
- Build compatibility is cleaner on `.NET 9`.

## Documentation Alignment

Updated docs:
- `README.md`
- `docs/architecture.md`
- `docs/feature-spec.md`
- `docs/ui-refactor.md`

New docs:
- `docs/test-checklist.md`

What changed:
- Active docs now describe `System.Windows.Forms.NotifyIcon` instead of `H.NotifyIcon.Wpf`.
- UI refactor notes no longer mention outdated loading/cancel copy.
- Windows regression checklist now covers tray behavior, build/test verification, panel switching, chat flow, history, settings, attachments, usage tracking, and SelectToCopy.

## Relevant Commits

- `db3d594` - aligned Windows docs and cleaned test warnings
- `ddbb8c9` - replaced `H.NotifyIcon.Wpf` with WinForms `NotifyIcon`
- `f71b76b` - aligned tray docs and added Windows test checklist

## Recommended Follow-up

1. Run `docs/test-checklist.md` on a real Windows 10 and Windows 11 machine.
2. Verify tray icon, double Alt, and SelectToCopy behavior outside the debugger.
3. Finish packaging/publish validation for the remaining release phase.

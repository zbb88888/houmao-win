# Houmao Windows Test Checklist

This checklist covers the current Windows implementation after the tray refactor and the recent UI stabilization work.

## Scope

Areas covered:
- Global hotkey and window lifecycle
- Main window and panel switching
- AI chat and streaming behavior
- Provider settings and persistence
- History panel and history storage
- Usage tracking and SelectToCopy
- Tray icon behavior
- Build and regression smoke checks

## Environment

Recommended environment:
- Windows 10 or Windows 11
- .NET 9 SDK installed
- At least one configured provider
- `%LOCALAPPDATA%\houmao\` writable
- If testing SelectToCopy and UsageTracker, use a regular desktop session rather than RDP when possible

## 1. Build and Startup

### 1.1 Application project builds cleanly

Steps:
1. Run `dotnet build src/Houmao/Houmao.csproj`.

Expected:
- Build succeeds.
- No `NU1701` warning for tray icon dependencies.

Status: [ ] Pass [ ] Fail

### 1.2 Test project passes

Steps:
1. Run `dotnet test tests/Houmao.Tests/Houmao.Tests.csproj`.

Expected:
- Tests pass.
- No functional regressions introduced by recent tray or UI refactors.

Status: [ ] Pass [ ] Fail

### 1.3 App launches to tray

Steps:
1. Start the app normally.

Expected:
- Process starts successfully.
- Tray icon appears.
- Main window behavior matches current startup rule.

Status: [ ] Pass [ ] Fail

## 2. Tray Icon

### 2.1 Tray icon shows context menu

Steps:
1. Right-click the tray icon.

Expected:
- Menu opens.
- Menu contains `Show`, `Settings` submenu, and `Exit`.

Status: [ ] Pass [ ] Fail

### 2.2 Tray Show opens the main window

Steps:
1. Hide the main window.
2. Right-click tray icon.
3. Click `Show`.

Expected:
- Main window becomes visible and active.
- Input focus returns to the text box.

Status: [ ] Pass [ ] Fail

### 2.3 Tray Settings submenu opens the expected pages

Steps:
1. Right-click tray icon.
2. Open `Settings -> Providers`.
3. Close it.
4. Open `Settings -> General`.

Expected:
- Providers page opens first for the first menu action.
- General page opens first for the second menu action.

Status: [ ] Pass [ ] Fail

### 2.4 Double-click tray icon restores the main window

Steps:
1. Hide the main window.
2. Double-click the tray icon.

Expected:
- Main window becomes visible and active.

Status: [ ] Pass [ ] Fail

### 2.5 Exit from tray closes the app process

Steps:
1. Right-click tray icon.
2. Click `Exit`.

Expected:
- Application exits fully.
- Tray icon disappears.

Status: [ ] Pass [ ] Fail

## 3. Global Hotkey and Window Lifecycle

### 3.1 Double Alt toggles the window

Steps:
1. Press left Alt twice quickly.
2. Repeat once more.

Expected:
- First double press shows the window.
- Second double press hides the window.

Status: [ ] Pass [ ] Fail

### 3.2 Escape hides the window

Steps:
1. Show the window.
2. Press `Esc`.

Expected:
- Window hides.
- Process remains alive in tray.

Status: [ ] Pass [ ] Fail

### 3.3 Ctrl+W hides the window

Steps:
1. Show the window.
2. Press `Ctrl+W`.

Expected:
- Window hides.
- Process remains alive.

Status: [ ] Pass [ ] Fail

## 4. Main Window and UI

### 4.1 Window layout renders once without duplicate chat region

Steps:
1. Show the window.
2. Observe the empty state.
3. Send one prompt and observe the result area.

Expected:
- Only one chat region is visible.
- No duplicated assistant/user message stack appears.

Status: [ ] Pass [ ] Fail

### 4.2 Window width and height behavior match Spotlight-style design

Steps:
1. Show empty window.
2. Add enough content to increase result height.

Expected:
- Window is visually narrow and centered around the input line.
- Height grows with content up to the designed max, then scrolls.

Status: [ ] Pass [ ] Fail

### 4.3 Loading indicator is inline, not overlaying content

Steps:
1. Send a prompt that streams long enough to observe loading state.

Expected:
- Loading indicator appears inline in the content region.
- It does not cover or block streamed content.

Status: [ ] Pass [ ] Fail

## 5. Chat and Streaming

### 5.1 Basic prompt submission works

Steps:
1. Enter a prompt.
2. Press `Enter`.

Expected:
- Request starts.
- Assistant response appears.
- Conversation updates correctly.

Status: [ ] Pass [ ] Fail

### 5.2 Streaming output updates incrementally

Steps:
1. Send a prompt that returns multiple chunks.

Expected:
- Content grows incrementally rather than only appearing at the end.

Status: [ ] Pass [ ] Fail

### 5.3 Cancel request stops active generation

Steps:
1. Start a long-running request.
2. Trigger cancel using the current UI/input behavior.

Expected:
- Loading stops.
- Status reflects cancellation.
- No further tokens appear.

Status: [ ] Pass [ ] Fail

### 5.4 Think-tag filtering works

Steps:
1. Use a model/provider response that emits `<think>...</think>` sections.

Expected:
- Think content is filtered from visible assistant output.
- Final visible content remains readable.

Status: [ ] Pass [ ] Fail

### 5.5 Mention routing still works

Steps:
1. Send `@provider some prompt`.
2. Send `@model some prompt`.

Expected:
- Provider-name routing works.
- Model-id routing works.
- Fallback without mention uses the default provider.

Status: [ ] Pass [ ] Fail

## 6. Panels and Shortcuts

### 6.1 History panel toggles with command input

Steps:
1. Enter `b` and submit.
2. Enter `b` again and submit.

Expected:
- First submit opens history panel.
- Second submit closes it.

Status: [ ] Pass [ ] Fail

### 6.2 Help panel toggles with command input

Steps:
1. Enter `h` and submit.
2. Enter `h` again and submit.

Expected:
- First submit opens help panel.
- Second submit closes it.

Status: [ ] Pass [ ] Fail

### 6.3 Ctrl+B toggles history panel if supported by current app behavior

Steps:
1. Show main window.
2. Press `Ctrl+B`.

Expected:
- History panel toggles if shortcut is wired in current build.
- If not wired, note the mismatch as a documentation or implementation issue.

Status: [ ] Pass [ ] Fail

## 7. History Panel and Persistence

### 7.1 History records render correctly

Preconditions:
- Existing usage/history records available

Steps:
1. Open history panel.

Expected:
- Records list shows summary, app/provider metadata, and time.
- Styling uses current theme tokens consistently.

Status: [ ] Pass [ ] Fail

### 7.2 History refresh works

Steps:
1. Open history panel.
2. Click `Refresh`.

Expected:
- Records reload without crash.

Status: [ ] Pass [ ] Fail

### 7.3 Clear all removes history

Steps:
1. Open history panel.
2. Click `Clear all`.

Expected:
- History list becomes empty.
- Persisted history is cleared.

Status: [ ] Pass [ ] Fail

### 7.4 Load more works when more history exists

Preconditions:
- More than one page of history available

Steps:
1. Open history panel.
2. Trigger bottom pagination or `Load more`.

Expected:
- Older records append correctly.
- No duplicates or scroll glitches appear.

Status: [ ] Pass [ ] Fail

### 7.5 History file exists in LocalAppData

Steps:
1. Generate at least one persisted history item.
2. Inspect `%LOCALAPPDATA%\houmao\usage-history.json`.

Expected:
- File exists.
- Content is valid JSON.

Status: [ ] Pass [ ] Fail

## 8. Settings and Provider Management

### 8.1 Open settings with Ctrl+,

Steps:
1. Show main window.
2. Press `Ctrl+,`.

Expected:
- Settings window opens.

Status: [ ] Pass [ ] Fail

### 8.2 Provider CRUD works

Steps:
1. Add a provider.
2. Edit the provider.
3. Reorder provider to default/top if supported by current UI.
4. Delete the provider.

Expected:
- All actions succeed.
- Data persists after reopen.

Status: [ ] Pass [ ] Fail

### 8.3 URL cleanup strips common /v1 suffixes

Steps:
1. Add provider URL with `/v1` or `/v1/chat/completions` suffix.
2. Save and reopen settings.

Expected:
- Stored URL is normalized to base host.

Status: [ ] Pass [ ] Fail

### 8.4 General settings persist

Steps:
1. Change one or more general settings such as startup or SelectToCopy.
2. Close and reopen settings or restart app.

Expected:
- Updated values persist.

Status: [ ] Pass [ ] Fail

### 8.5 Settings file exists in LocalAppData

Steps:
1. Save settings.
2. Inspect `%LOCALAPPDATA%\houmao\settings.json`.

Expected:
- File exists.
- Content is valid JSON.

Status: [ ] Pass [ ] Fail

## 9. Attachments

### 9.1 Add image attachment

Steps:
1. Add an image file.
2. Observe the attachment strip.
3. Send a prompt using that attachment.

Expected:
- Attachment appears in the strip.
- It can be included in the request.

Status: [ ] Pass [ ] Fail

### 9.2 Add audio attachment

Steps:
1. Add an audio file.
2. Observe the attachment strip.
3. Send a prompt using that attachment.

Expected:
- Attachment appears in the strip.
- It can be included in the request.

Status: [ ] Pass [ ] Fail

### 9.3 Remove attachment

Steps:
1. Add one or more attachments.
2. Remove one item.

Expected:
- Removed item disappears from the strip.
- Remaining items stay intact.

Status: [ ] Pass [ ] Fail

### 9.4 Drag-and-drop attachment works

Steps:
1. Drag a supported file onto the main window.

Expected:
- Attachment is added.

Status: [ ] Pass [ ] Fail

## 10. Usage Tracking and SelectToCopy

### 10.1 Usage tracking does not record houmao itself

Steps:
1. Type and submit inside houmao.
2. Inspect usage history source.

Expected:
- houmao internal typing is excluded from external usage tracking.

Status: [ ] Pass [ ] Fail

### 10.2 Usage tracking records external committed input

Steps:
1. Type in another app.
2. Commit with `Enter`.
3. Inspect usage history.

Expected:
- Record contains app name, text, and timestamp.

Status: [ ] Pass [ ] Fail

### 10.3 SelectToCopy respects the toggle

Steps:
1. Turn SelectToCopy OFF.
2. Drag-select text in another app.
3. Turn SelectToCopy ON.
4. Repeat.

Expected:
- OFF: no copy injection from houmao.
- ON: selected text is copied as designed.

Status: [ ] Pass [ ] Fail

## 11. Regression Smoke Checks

### 11.1 README and docs match current tray implementation

Steps:
1. Inspect README and docs for tray stack description.

Expected:
- Active docs describe `System.Windows.Forms.NotifyIcon`, not `H.NotifyIcon.Wpf`.

Status: [ ] Pass [ ] Fail

### 11.2 Main app still shuts down cleanly

Steps:
1. Start the app.
2. Exit from tray.
3. Start again.

Expected:
- No zombie tray icon remains.
- Restart behaves normally.

Status: [ ] Pass [ ] Fail

## Notes

Tester:

Date:

Build / Commit:

Issues Found:

# Input investigation and implementation rationale

Evidence below comes from the working-copy ILSpy decompilation of the inspected `GH_Data\Managed\Assembly-CSharp.dll`, SHA-256 `40279b7fda17867f4cfc3e3be0e3bbae7df20bdb5b3b3c9fde0a2827d9862838`. Decompiled files remain in the local workspace's `work\decompiled`; proprietary source is not included in this package. Line numbers refer to that local decompilation, not original developer source. These are **static findings**, not Phase 2 runtime observations.

## Verified paths

| Evidence path relative to this workspace | Discovery | Implementation consequence |
| --- | --- | --- |
| `work/decompiled/InputsManager.cs:723`, `:746` | `OnAction` and `IsActionActive(InputAction)` honor `m_TextInputActive`. | Temporarily set the shared text-input flag while our panel owns focus. |
| `work/decompiled/InputsManager.cs:755`, `:760`, `:770` | Trigger overload and `GetActionValue` do not honor that flag. | The flag alone cannot establish full input isolation. Add explicit movement/rotation blocks; document other polling paths as unverified. |
| `work/decompiled/InputsManager.cs:245`, `:271`, `:852` | Script loading and user options populate/update action maps. Private `m_ActionsByInputAction` and `m_ActionsByKeyCode` contain live bindings. | Read both maps before every opening; check keyboard and PS4 key fields, refuse conflicts or unavailable metadata. Reflection only reads these fields. |
| `work/decompiled/Player.cs:1429`, `:1434`, `:1456`, `:1470` | `BlockMoves`/`UnblockMoves` and `BlockRotation`/`UnblockRotation` own counted requests. Rotation transition calls controller block/unblock hooks. | Acquire one of each, release one of each; never reset counters or replace global state. |
| `work/decompiled/Player.cs:1442`, `:1487` | Movement/rotation queries also consult loading/splash state. | Require HUD/loading/player objects before acquisition; guard lifecycle transitions and catch independent release errors. |
| `work/decompiled/FPPController.cs:777`, `:804`, `:817`, `:840` | First-person movement/look consult the block queries; `OnBlockRotation` clears stored inputs. | Use public block APIs, including transition hooks, rather than just toggling a private flag. |
| `work/decompiled/SwimController.cs:355`, `:639`, `:672` | Swimming input/look also consult rotation block; transition hook clears inputs. | This is a supported static path; swimming still needs runtime testing. |
| `work/decompiled/CursorManager.cs:38`, `:295`, `:338` | `ShowCursor(bool,bool)` manages counted visibility requests; normal cursor updates resolve visibility/locking. | Add/remove one cursor request. Do not overwrite `Cursor.lockState`, `Cursor.visible`, force a position or restore a stale global snapshot. |
| `work/decompiled/HUDTextChat.cs:18`, `:25`, `:31`, `:38`, `:64` | Native chat owns the same text flag and sends through `P2PSession.SendTextChatMessage`; its update polls `GetActionValue`. | Keep our text/history separate; never call the send path. Detect native chat and yield, preserving its text flag. |
| `work/decompiled/HUDBase.cs:58` | `Show(bool)` toggles the component's enabled state. | Existing native `HUDTextChat.isActiveAndEnabled` is used to detect that owner; no native HUD is instantiated. |
| `work/decompiled/HUDNameAnimal.cs`, `work/decompiled/HUDNameItem.cs` | Naming widgets own movement/rotation/cursor state. | Refuse opening over these widgets; yield if they appear. Also inspect focused `InputField` / `InputFieldEx` on the existing EventSystem. |

No literal `KeyCode.F8` was found in the decompiled gameplay C# search. That does **not** prove F8 is free: bindings originate in loaded data/options, and overlays/mods can use raw input. The runtime map audit is required. F6–F11 are configurable candidates, not a guarantee of availability. The opening keystroke can still reach an unknown raw-input consumer before any UI suppression.

## Focus ownership and lifecycle

On opening, require a started, active, unpaused level; no loading/save/cutscene/menu; a live local player and matching condition module; initialized input/cursor/HUD objects; no native text focus; zero existing movement, rotation and cursor requests. Then set text input and acquire one move, rotation and cursor request. The input/controller checks run on the main thread; `DefaultExecutionOrder(-10000)` requests early callbacks, whose actual interaction with game script ordering remains untested.

Normal closing keeps the lease through two consecutive keyboard/mouse-neutral frames to reduce leakage from closing clicks/keys or stale cached actions. Menus/lifecycle changes, focus loss, errors and unload release immediately. Cleanup is idempotent and attempts each resource independently; already-zero counters are not decremented. Native chat retains ownership of the shared flag if active. A detected extra counted owner causes our panel to yield without clearing the other owner's count. If another mod resets counters and subsequently reuses them, count-based ownership cannot fully distinguish requests; runtime interactions need testing.

The panel uses Unity IMGUI and creates no Canvas, EventSystem or game HUD instance. UI and game access stay in `Update`, `OnGUI` and lifecycle callbacks; mock replies are immediate and synchronous. Unity documents that [OnGUI can run multiple times per frame](https://docs.unity3d.com/2021.2/Documentation/ScriptReference/MonoBehaviour.OnGUI.html); gameplay ownership changes are therefore coordinated by `Update`, while button events request changes. [GUI.ModalWindow](https://docs.unity3d.com/2021.2/Documentation/ScriptReference/GUI.ModalWindow.html) blocks other IMGUI controls; it does not promise to block uGUI or arbitrary raw-input scripts. Cleanup clears the named field's focus and a tracked owned mouse hot-control only, without clearing another UI's selection. No new Unity UI object needs destruction.

## Proof boundary

Verified offline: target API/field existence, compiled x64 managed output, bounded policy behavior under simulated failures, absent direct networking/PInvoke/patch calls, unchanged original managed DLL hashes and unchanged installed Phase 1 plugin hash. Reflection is restricted to `FieldInfo.GetValue` for the known maps/counters; writes to game state are limited to the shared input flag and public balanced focus APIs. Vitals remain read-only.

Unverified until manual Phase 2 testing: actual IMGUI appearance, focus/cursor transitions, script execution ordering, frame times, raw polling consumers, all controller modes, multiplayer native-chat interaction and every scene transition. Phase 1 live-stat evidence is separate and does not validate the new UI.

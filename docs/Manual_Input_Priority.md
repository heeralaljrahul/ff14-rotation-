# Manual button priority

Follow-up to the native-input change, integrating branch head `6c2e3aa42fe43ab11dbb6858b2aa3ba17353f1f2` (2026-10-21).

## Problem

Passing a button press straight to `ActionManager.UseAction` was only half the fix. A press during an existing cast/GCD can be too early for the game's own queue. The old code returned the native failure, discarded the user's intent and continued submitting automatic actions. A queued automatic Malefic could also already own the next GCD. Spamming Helios Conjunction or Primal Rend could therefore lose to further rotation actions.

The existing occupancy check only blocked automation when the queued action's ID differed from the selected action. Identical spell IDs on different targets were not protected. Native queue replay was also being recorded as a fresh player press. That could misidentify an automatic queued action as manual input.

A concurrent change at `6c2e3aa4` added a command-action fallback for refused native presses. That change is retained for legacy instant mode when the new manual-priority option is disabled. The default priority path below handles native ownership directly, bypassing legacy interception flags and rotation eligibility/target heuristics.

## New behavior

`Prioritize manual button presses` defaults on and works independently of the legacy interception master switch. This covers PvE spells, weaponskills and abilities while BRR is active; Off, Target only and PvP do not retain requests.

1. Attempt the original press immediately, with its original action ID/type, target, extra parameter, execution mode and combo route. Return the native result.
2. Reserve the current action slot for the latest request. Discard an older unsent choice when a newer press arrives. Remove a queued BRR action only when its recorded native queue snapshot still matches. Do not remove an unrelated unknown queue.
3. If the game accepts the request into its queue, leave that queue alone. If it was too early, yield automation and retry the same native request as the current timing restriction clears. Macro-mode requests retain macro mode and their resolved targets; they are retried when ready rather than converted to normal-mode queueing.
4. Use the native action sequence to detect an actual submission. A successful-looking return or a queued action is not treated as an executed cast. Observe native submissions at `UseActionLocation`, including queued and ground-targeted execution, with sequence-change checks on direct/manual queue calls as a fallback.
5. Protect the manual cast from BRR's automatic cast-cancellation heuristics. Once the game acknowledges the action and casting has ended, let the rotation continue from the updated game state. No fixed pause is added after acknowledgement.

The scheduler yields both action selection and execution, including rotation preview side effects, then checks ownership again immediately before target switching/submission. The following automatic action is recomputed after manual ownership ends. Any existing native queue is protected, including the same action ID on a different target. Ground targeting also pauses automatic submissions while the player aims.

This intentionally adds a bounded **single latest-request reservation**. It replaces the insufficient native-only behavior; it does not use the legacy five-second intercept list, rotation healing thresholds, `CanUse`, AoE-count rules or job-specific priorities to decide the player's choice. The original press is always attempted first.

## Expiry and invalid input

- A request can wait for the current cast/GCD/animation lock, with a short allowance for handoff. It is bounded to the current timing window, not a future cooldown cycle; pending requests expire after at most ten seconds.
- Native resource/proc/level/target failures are not treated as timing-only restrictions. A cooldown beyond the current action window is discarded.
- If the adjusted action changes before an unsent request is used, discard it instead of silently using a different proc/morph.
- For an unresolved default target, a hard/soft target change cancels the old request. An explicit target, including a resolved mouseover macro target, is preserved.
- When a native queue is canceled, discard its request; do not resurrect it later. Death, job/territory changes, Off/Target only/PvP, disabled priority and the automatic pull gate clear retained ownership.
- Repeated rejected presses during an already submitted manual cast do not manufacture a second deferred cast. A repeat actually accepted by the game's native queue is respected.
- Native confirmation has a bounded timeout. Interrupted manual casts release ownership promptly; missing packets cannot freeze the rotation indefinitely.
- No game out-pointer or `ActionManager` pointer is stored between frames. Retained requests contain scalar values only.

The current cast, GCD, animation lock, MP, proc, target, range and other game restrictions still apply. This does not force-cancel a Malefic already casting or permit a Primal Rend without its native requirement. It prevents further automatic actions taking the slot your valid press is waiting for.

## Implementation

| File | Purpose |
|---|---|
| `RotationSolver/Basic/Helpers/ManualActionPriority.cs` | State policy, latest-request semantics, expiry, native-sequence confirmation and cast ownership. |
| `RotationSolver/Updaters/ActionQueueManager_ManualPriority.cs` | Native request preservation, timing validation/retries and queue ownership. |
| `RotationSolver/Updaters/ActionQueueManager.cs` | Distinguish solver submissions, actual button presses, nested native calls and native queue replay. |
| `RotationSolver/Basic/Helpers/ActionManagerEx.cs` | Notify when the native request sequence actually advances. |
| `RotationSolver/Updaters/ActionUpdater.cs` | Yield at scheduling, protect native queues/ground targeting and reset on death. |
| `RotationSolver/Commands/RSCommands_Actions.cs` | Recheck ownership before switching targets or submitting the selected action. |
| `RotationSolver/Updaters/CancelCastUpdater.cs` | Leave a manual cast's completion/cancellation to the player and game. |
| `RotationSolver/Commands/RSCommands_StateSpecialCommand.cs` | Clear reservations when leaving automation. |

Native API semantics were checked against [FFXIVClientStructs ActionManager](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/ActionManager.cs): `UseAction` attempts/queues actions, `UseActionLocation` submits an actual action, and `UseActionMode.Queue` replays an existing native queue rather than representing a new press.

## Validation

- Baseline branch Release build passed with zero warnings/errors.
- Patched Release build passed with zero warnings/errors against the available Dalamud references.
- The regression harness links the actual policy source. All 24 manual-priority scenarios and 16 existing countdown scenarios passed, covering AST/WAR timelines, same-action spam, target replacement, old/wrapped acknowledgements, native queue cancellation, confirmation timeout, canceled/manual casts, macro arguments, morph changes, ready-at-zero timing, long cooldowns and native resource/proc rejection. Both sets run in the same CI command.
- No live game session was available. Policy tests and compilation do not prove native hook behavior or real latency in FFXIV.

Run:

```sh
dotnet run --project Tests/CombatPolicyRegression/CombatPolicyRegression.csproj -c Release
dotnet build RotationSolver/RotationSolver.csproj -c Release
```

## Required game checks after rebuilding/reloading

| Check | Expected result |
|---|---|
| AST: press Helios Conjunction while an automatic Fall Malefic is casting | That existing cast may finish; Helios gets the next legal slot. No additional automatic Malefic takes it. Repeat with automatic healing disabled and the party at full HP. |
| WAR: press Primal Rend while GCD-locked and its proc is available | Rend gets the next legal slot, then the normal rotation continues. Repeat with movement/rotation-specific Rend settings restricting automatic use. |
| Both cases: single early press and repeated presses | A single valid request survives the current timing window; rejected spam after submission does not add another deferred use. |
| An automatic action is already in the native queue | The new manual request supersedes that known BRR queue. |
| Same heal ID, different target; mouseover macro | The manually resolved target is used; BRR does not replace it with its own target. |
| Manual heal above the auto-healing threshold | BRR does not cancel it as unnecessary. Walking or the game's own cancellation still works. |
| Primal Rend unavailable, insufficient MP, invalid target or long cooldown | Failed input does not leave a stale action or permanent pause. |
| Ground-targeted action | The reticle remains usable; BRR yields while aiming, and resumes after confirmation/cancellation. |
| Cancel a queue/cast; die; switch job/territory; toggle Off/on | No stale request executes after the context changes. |
| Disable legacy interception | Manual priority still works with its own option enabled. |

These checks target manual input ownership. This change does not independently revalidate the jobs' damage/mitigation rotations or alter burst-control macros.

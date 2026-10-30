# Reborn rotation review: zero-prepull and high-end use

Reviewed 2026-10-20 against branch `claude/dreamy-fermat-4yhu6z`, starting at commit `2b86b85e529c109c2145f4a5efcc7eb983d6e744`. This review supersedes findings against `5b1d9b6`: several ranged issues had already been fixed by Claude in between. Scope: the PvE **Reborn** rotations and their shared/base action checks for BRD, MCH, DNC, SGE, DRK, WAR, PCT, SAM, RPR and VPR. Extra/Churin rotations and PvP rotations are not independently reviewed here.

**Verdict: do not describe all ten jobs as validated Savage/Ultimate rotations yet.** Source review found definite bugs and remaining mitigation/recovery problems. This change fixes a bounded set of confirmed problems; it does not replace encounter planning or demonstrate an optimal damage timeline. Release compilation and countdown-policy tests pass. No live FFXIV execution, network-latency measurements, combat-log comparison or level-sync playtest was available.

## Required behavior and changes made

### No automatic preparation before the pull

With `NoCountdownActions` enabled (the default), automatic selection and execution wait for an observed countdown to finish, or for actual combat if no countdown is used. Before that, automatic dances, motifs, Eukrasia, Soulsow, casts, Reassemble, potions and other actions are blocked. Manual presses still go to the game.

The old check was `countDown > 0 && !InCombat`. A lingering combat flag after a wipe could bypass it. The new pull gate gives a positive countdown priority over combat state, checks both selection and execution, clears a stale next-action selection at execution, and resets its release after death, combat ending or a territory change. A completed timer permits non-damaging opening steps without requiring the opener's first action to enter combat. Without a countdown, initiate combat manually.

The gate also prevents automatic out-of-combat preparation before a countdown has even started. It uses a monotonic clock and an observed timer deadline. An early disappearance more than 200ms before that deadline is treated as cancellation. A disappearance in the last 200ms remains blocked until the estimated deadline; `TimeRemaining == 0` alone cannot distinguish a final-frame cancellation from a normal finish. A slow frame at the finish no longer cancels an otherwise completed pull solely because the last observed remaining time exceeded 200ms.

These checks prevent new plugin action submissions. They do not undo an action already accepted by the game, interrupt an existing cast, or disable auto-attacks a player previously enabled. Test countdown initiation while a native action is already queued separately. “Start at zero” means eligibility begins at zero, subject to framework updates and the game's own action restrictions.

Files: `RotationSolver/Basic/Helpers/AutomaticPullGate.cs`, `RotationSolver/Basic/Service.cs`, `RotationSolver/Basic/Rotations/CustomRotation_Invoke.cs`, `RotationSolver/Commands/RSCommands_Actions.cs`.

### Manual input uses the original game handler

The old instant path only tried the native call after plugin action matching, eligibility and movement checks. If the game returned false, it could still enqueue the action for up to five seconds. That did not meet the requested behavior.

With `InterceptInstantUse` enabled (default), the detour now immediately forwards the exact original arguments and returns the native result, including false. There is no plugin retry on that path. The default for the legacy interception master switch is also off. Native targeting, macro targets, cooldowns, animation locks and the game's native input queue remain authoritative. Existing configuration values are preserved; for this behavior use native input mode or leave interception off.

This removes a plugin input delay. It does **not** prove that every rotation recovers correctly from every manual intervention. In particular, SGE's automatic Eukrasia intent can still conflict with a manual Eukrasia-to-shield sequence; see SGE below. Native failed presses are not converted into later presses.

Files: `RotationSolver/Updaters/ActionQueueManager.cs`, `RotationSolver/Basic/Configuration/Configs.cs`.

### Burst controls remain optional

No new macros are required. `AutoBurst` and per-action toggles retain their existing purpose. Extra ranged party-alignment and predicted-downtime burst holds now require the new `AutomaticBurstHolds` option, which defaults off. That also gates MCH's predicted-downtime Queen hold. This avoids imposing the newer party/BMR hold policy on the ordinary rotation merely because a duty is an Ultimate.

Turning burst back on lets the job's existing priority rules resume. A ready oGCD still needs a legal weave; a dance still needs its GCD, steps and finish. “Use when ready” must not mean clipping a tool, dropping a proc, breaking a combo, or using every gauge spender outside raid buffs. Normal job resource management is distinct from manually holding an entire burst window.

The individual experimental heat-dump setting and legacy per-Ultimate configuration values have not been erased. Extra holds are opt-in; an encounter-specific plan still needs checking before those options are enabled.

### Other confirmed fixes

| Area | Problem at the reviewed head | Change |
|---|---|---|
| MCH Queen state | An `else` belonged to the opener-potion `if`, so Queen timing reset on every invocation that did not use a potion. | Reset in `UpdateInfo` before a pull/countdown, rather than on an ordinary no-potion frame. |
| RPR Enshroud | Base action check required **Soul** >= 50 and ignored a free Ideal Host use. A valid Shroud-funded/free Enshroud could be rejected. | Check **Shroud** >= 50 or Ideal Host; reject existing Enshrouded/Soul Reaver/Executioner. The official job guide identifies the resource as Shroud. |
| PCT Swiftcast | Rainbow Drip selected under Rainbow Bright could still request Swiftcast. | Do not spend Swiftcast on Rainbow Drip when Rainbow Bright already makes it instant. |
| Plugin artwork URL | Both manifest icon URLs pointed to `main`, which had the old artwork. | Pin them to the existing Reina artwork commit so the icon resolves independently of branch promotion. |

## Job-by-job findings

“Confirmed” below describes the source condition, not a measured loss from a combat log. “Test” marks a scenario whose severity/order depends on scheduler state, cooldowns or encounter timing.

### Machinist

The updated branch already fixes the previous Queen 80-to-100 plan entry, advances Queen state on actual robot activation, and provides a level-100 Wildfire fallback when Full Metal Field is absent. The Queen reset regression above prevented the new state tracking from functioning reliably and is fixed here.

The guide target for a level-100 Wildfire is six qualifying weaponskills, typically Full Metal Field plus five Blazing Shots; below that level the sequence differs. Keep Drill/Air Anchor/Chain Saw charge protection around Hypercharge. Do not infer success merely because Wildfire or Hypercharge was selected.

Remaining work:

- **Test:** full Wildfire windows after Barrel Stabilizer was manually used, disabled or delayed; verify six actual hits and correct second-Hypercharge/tool ordering.
- **Test:** Reassemble usage across two-minute windows. Check both wasted capped-charge time and whether the chosen tool actually receives the buff. The strict zero-prepull opener cannot assume Reassemble already exists on the first hit.
- **Test:** Queen deployment after death, same-battery consecutive summons, shortened phases and level 70 Rook use. A fixed full-uptime battery table is not an encounter-specific Ultimate plan.
- **Review:** `BmrDumpBeforeDowntime` remains an experimental separate option. It can change spending outside the normal burst plan; verify the predicted window before enabling it for a fight.

Evidence: `MCH_Reborn.EmergencyAbility`, `ToolChargeSoon`, `UseQueen`, `UpdateQueenStep`, `AdvanceStep`, and `AttackAbility`; `MachinistRotation.cs` action checks. Sources: MCH Balance basic/FAQ, Icy Veins rotation and official job guide, listed below.

### Bard

The newer branch already removed the burst-related early return, moved three-stack Pitch Perfect ahead of Empyreal Arrow, allowed Empyreal Arrow while AutoBurst is off, and added capped-Soul-Voice handling while burst is held. Those are meaningful improvements over the earlier review.

Remaining work:

- **Confirmed:** Battle Voice remains coupled to Radiant Finale/Raging Strikes readiness. After a death or a manually shifted cooldown, this can delay a ready raid buff. Add an explicit recovery policy with a bounded delay instead of assuming all cooldowns remain synchronized.
- **Test:** two-stack Pitch Perfect with a Repertoire tick due and Empyreal Arrow available. The three-stack priority fix does not by itself prove that all tick/Arrow overcaps are avoided.
- **Test:** Empyreal Arrow when Raging Strikes is ready but disabled, or alignment prerequisites cannot be satisfied. It should keep its own cadence while burst is unavailable.
- **Test:** first song, DoT application/snapshot, 43/43/34 song transitions, Finale coda count and all synced song sets after a zero-prepull start or resurrection.

The guide priorities support avoiding proc/charge loss and preserving song/DoT uptime; a generic waiting rule is not sufficient evidence of optimal party alignment. Evidence: `BRD_Reborn.AttackAbility`, `GeneralAbility`, `GeneralGCD`, `HoldTwoMinuteBurst`.

### Dancer

Technical Step no longer waits for another party buff before starting the dance. The newer guards also respect a disabled Technical Step/AutoBurst, so intentionally unavailable Technical should not block ordinary filler forever. The Last Dance safety check before a new Standard/Finishing Move and the Tillana Esprit check improve resource handling.

Remaining work:

- **Confirmed ordering:** `AttackGCD` considers Dance of the Dawn and Tillana before its Last Dance safety check. **Test** a nearly expired Last Dance alongside either available action; deadline protection should precede a GCD that would make it expire.
- **Test:** `StandardStepAboutToDance` and `TechnicalStepAboutToDance` suppress filler based on cooldown elapsed time, not solely whether that step can actually be selected now. In particular, Standard can be deliberately held for Technical within six seconds while a separate guard suppresses filler. Measure avoidable idle GCD time.
- **Test:** ready Technical after Manual/AutoBurst changes, disabled Standard, target-loss holds, and recovery from interrupted/manual dance steps. A short wait that preserves the next Technical GCD can be intentional; an indefinite or multi-GCD stall is not.
- **Zero-prepull tradeoff:** initial Standard preparation now starts after zero, so Technical Finish cannot land at the same time as the published prepull opener. Plan the party's opener around that deliberate constraint and test the full Standard-to-Technical sequence.

Evidence: `DNC_Reborn.GeneralGCD`, `AttackGCD`, `UseStandardStep`, `HandleTillana` and the step-about-to-dance properties. Sources include both Balance basic and advanced guides.

### Sage

The native-input fix forwards Eukrasia, shields and mitigation without plugin queuing. The shared pull gate blocks automatic OOC Eukrasia before the pull, regardless of `OOCEukrasia` being enabled. Raise/Swiftcast protection and Phlegma charge checks are present.

**High-priority remaining work:**

- **Confirmed:** `_EukrasiaActionAim` can retain an automatic DoT/shield intent. `ClearEukrasia` can remove Eukrasia with `StatusOff`; `GeneralGCD` can consume Eukrasia for its automatic aim. A manually accepted Eukrasia press therefore does not guarantee the following manually intended shield owns the next GCD. Add explicit manual ownership until the enhanced spell consumes/cancels the status, without delaying or queueing the player's presses. Test this with a DoT refresh simultaneously due.
- **Confirmed:** automatic Haima/Panhaima choices are restricted by low Addersgall conditions even though those cooldowns do not spend Addersgall. Taurochole has a sub-80% HP gate. These conditions can reject planned protection at full HP; separate proactive mitigation from reactive healing.
- **Confirmed:** Philosophia has a base action definition but no use in `SGE_Reborn`. Add a planned healing use rather than treating the level-100 kit as complete.
- **Test/optimization:** Phlegma charge preservation exists, but explicit raid-buff pooling is limited. Compare its actual two-minute placement with the DPS guide while preserving movement and charge-cap safety.

Do not sign off Sage's manual Eukrasia-to-shield recovery or automatic Savage healing plan until the ownership problem and scripted damage tests are resolved. Evidence: `SGE_Reborn.EmergencyAbility`, `ClearEukrasia`, `ChoiceEukrasia`, `GeneralGCD`, `DefenseAreaAbility`, `DefenseSingleAbility`; `SageRotation.cs`.

### Dark Knight

Early Darkside, Living Shadow/Delirium checks and MP-cap protection exist. The prepull TBN/Unmend/Provoke path is blocked by the shared policy. A melee first-hit opener is a deliberate variation from the guide's precast/ranged-pull timeline.

**Confirmed remaining problems:** `UseBlood` is defined but never used. `GeneralGCD` instead asks for Bloodspiller whenever its base check permits it, which includes ordinary 50-Blood availability. That does not implement the documented Blood-pooling intent. Implement and test a spender policy that handles raid buffs, incoming combo gauge, Delirium and impending downtime; do not simply wire in an untested old helper.

`DefenseAreaAbility` gates Dark Missionary, Reprisal and several support uses behind `!InTwoMIsBurst`. A raidwide during burst still needs mitigation. Remove damage-window suppression from explicitly requested/planned mitigation. TBN also needs an expected-damage/break plan, rather than assuming a low-HP target guarantees a break.

Evidence: `DRK_Reborn.GeneralGCD`, `UseBlood`, `CheckDarkSide`, `DefenseAreaAbility`; `DarkKnightRotation.ModifyBloodspillerPvE`. The Balance guide describes concentrating Bloodspiller in buffs and planning TBN around damage.

### Warrior

Core checks are stronger than a simple spender-on-availability loop: Infuriate checks combat and gauge <= 50, Nascent Chaos protection exists, and Surging Tempest governs the main burst spending path. The source alone does not establish a definite fundamental gauge bug comparable to RPR's Enshroud bug.

Remaining work is principally timing and mitigation validation. `GeneralGCD` spends Inner Release Fell Cleaves before Primal Rend/Ruination while stacks remain. Compare the complete sequence with raid-buff expiration and the party's chosen opener; the guides allow flexibility, so this ordering alone is not proof of an invalid rotation. Measure lost cooldown uses and gauge caps across repeated cycles at the actual skill speed.

Automatic Bloodwhetting/healing uses are HP-driven in places. Full-HP tankbusters and multi-hit sequences need an explicit plan for short mitigation, major cooldowns and Shake It Off. Test tank swaps and both single/AoE behavior at 70/80/90/100. Manual mitigation now follows the native handler, independent of the rotation's HP heuristic.

Evidence: `WAR_Reborn.AttackAbility`, `GeneralGCD`, `GeneralAbility`, defense methods; `WarriorRotation.ModifyInfuriatePvE`. No claim of high-end certification is made from a generic priority review.

### Pictomancer

The unnecessary Swiftcast-on-Rainbow-Bright case is fixed. Automatic OOC painting is blocked before zero. Because the standard guide assumes prepared canvases, an empty-canvas start after zero has a different opener and must be timed explicitly.

**Confirmed:** `BurstDefense` defaults true and suppresses Tempera Coat/Grassa/Addle during Starry Muse. That is unsuitable as a default for planned raid damage. Disable/remove that suppression for planned mitigation. `DefenseSingleAbility` also falls back to `base.DefenseAreaAbility` instead of its single-target counterpart.

**Test/optimization:** validate all Hyperphantasia stacks, Rainbow Bright, Star Prism and Hammer actions inside each Starry window, including movement and target loss. A high-priority instant or hammer chain is not automatically optimal if it pushes required casts beyond the window. Verify motif refresh during downtime and the zero-prepull three-canvas startup, not only a prepainted training-dummy scenario.

Evidence: `PCT_Reborn.EmergencyAbility`, defense methods, `AttackAbility`, `GeneralGCD`; `PictomancerRotation.cs`. Official action descriptions confirm that Rainbow Bright changes Rainbow Drip's cast behavior.

### Samurai

Sen/Meikyo and upgraded Iaijutsu paths exist, but the job requires testing at the guide's specific GCD tiers and after manual intervention. A priority tree is not evidence that the 120-second loop and Higanbana timings remain aligned.

**Confirmed:** automatic Shoha is suppressed while Ogi Namikiri is ready, including a candidate Ogi with three Meditation stacks. That can spend another stack-generating GCD while capped. Protect Shoha before a Meditation-generating GCD regardless of the Ogi-ready gate, while checking actual weave capacity.

`DefenseAreaAbility` suppresses Feint/Tengentsu/Third Eye while Zanshin is ready; planned mitigation must not wait on that damage proc. The low-HP fallback has the same damage-readiness condition.

Higanbana bypasses the TTK check for bosses/high-end duties. That avoids unreliable generic TTK estimates but can also apply a fresh DoT immediately before a phase ends. Use a verified phase/kill-time plan for this decision. Test one-Sen recovery, expired/disabled Meikyo, Tendo/Kaeshi continuity and Kenki/Meditation overflow after death.

Evidence: `SAM_Reborn.AttackAbility`, `GeneralGCD`, `DefenseAreaAbility`, `GeneralAbility`; `SamuraiRotation.ModifyShohaPvE`. The Balance explicitly distinguishes Higanbana value by usable tick time.

### Reaper

The Enshroud resource/free-use check is fixed here. Soul Reaver and Executioner must be spent without overwriting them; the rotation has dedicated checks for those sequences. The shared gate prevents prepull Soulsow/Harpe. In-combat Soulsow is not an issue in this implementation because its base check requires out of combat.

**Confirmed:** `EnshroudPooling` defaults false, which implements a simpler spend-at-50 approach rather than establishing the standard planned double-Enshroud window. For high-end optimization, select and validate a pooling/phase plan; do not equate this natural resource planning with the user's optional burst-hold macros.

Automatic Feint, Arcane Crest, Arm's Length and self-heals are gated by `NotInActiveCombo`, which excludes Enshrouded/Soul Reaver/Executioner. Those oGCD protections should remain available for requested raid damage during those states. Native manual presses bypass the plugin priority checks, but automatic mitigation still needs correction.

Test the free Ideal Host Enshroud with zero Shroud, ordinary Enshroud with Soul < 50, both Communios in a planned two-minute window, Sacrificium/Perfectio deadlines, movement, and level-80 behavior.

Evidence: `RPR_Reborn.AttackAbility`, `GeneralGCD`, defense methods; `ReaperRotation.ModifyEnshroudPvE`, `ModifySoulsowPvE`, `NotInActiveCombo`. Official Enshroud cost and Ideal Host behavior take precedence over a mistaken resource check.

### Viper

Generation/Legacy alternation and follow-up preservation checks are present. Serpent's Ire is tied to `IsBurst` and an enabled Reawaken; the user's optional action/AutoBurst macros can still control those choices. Reawaken must not break an unfinished Vicewinder/Vicepit combo.

Remaining work:

- **Confirmed default thresholds, test impact:** Reawaken requires more than ten seconds on both buffs with the default timer settings. That may cover one chain but does not establish the buff runway for two consecutive chains. Plan sufficient Hunter's Instinct/Swiftscaled duration for the full intended window and test before/after downtime.
- **Test:** a ready/free Reawaken after a long disengage when `LiveComboTime` has expired. The current extra combo-timer prerequisite can delay recovery until other actions reestablish it; check whether Ready to Reawaken could expire.
- **Confirmed:** Feint and Arm's Length depend on `NoAbilityReady`. Do not let dense damage follow-ups suppress planned mitigation/knockback protection.
- **Test:** low/high latency, lost Legacy/follow-up availability, disabled Ire/Reawaken, maximum Offerings/Coils, and target loss. The optional Uncoiled Fury ghosting behavior trades follow-up safety for continuing the GCD and should be evaluated separately.

The Balance distinguishes its simple priority from Savage/Ultimate optimization. Evidence: `VPR_Reborn.EmergencyAbility`, `AttackAbility`, `GeneralGCD`, defense methods and `NoAbilityReady`/timer checks; `ViperRotation.cs`.

## Artwork

The requested Reina/golden UCoB-style gun artwork was already present in the reviewed branch. Visually checked: `Images/icon.png` (512x512), `Images/Logo.png` (256x256), `Images/banner.jpg` (1600x681), and `RotationSolver/Changelog/banner.jpg` (1600x681). The project embeds the logo/changelog banner and copies `images/icon.png` beside the plugin DLL. README already uses the new banner. The manifest URL correction above fixes the remaining old-main-icon reference; no additional destructive crop or regeneration was needed.

## Validation and acceptance checks

Completed in this environment:

1. Baseline and patched `dotnet build RotationSolver/RotationSolver.csproj -c Release -p:Platform=x64`: succeeded, zero warnings/errors, using .NET 10 and downloaded Dalamud references.
2. `dotnet run --project Tests/CombatPolicyRegression/CombatPolicyRegression.csproj -c Release`: 13 lifecycle scenarios passed. Tests link the actual pull-gate source and cover initial preparation, lingering combat, zero release without combat, missed timer frames, cancellation, final-frame timing, new countdowns, execution rechecks, death, combat exit, wipe recovery and territory changes.
3. Diff whitespace check and JSON parsing; icon dimensions/visual inspection. These do not substitute for game execution.

Required live checks before calling the rotations high-end ready:

| Scenario | Acceptance evidence |
|---|---|
| Auto enabled before a timer, then countdown/wipe/retry | No new automatic action before zero, including dances, motifs, shields, potions and after a lingering combat flag. Record timestamps. |
| Timer canceled at 10s, 1s and the final frame; slow framework frame at zero | No early pull. Understand the final-frame cancellation ambiguity described above. |
| No timer | Solver waits for an actual manual/party pull, then begins. |
| Manual mitigation or mouseover shield | Original target/arguments; no plugin five-second retry; successful native execution timestamp. Also verify the rotation's following action. |
| SGE Eukrasia then shield while DoT is due | Shield consumes the manually requested Eukrasia; no automatic DoT or StatusOff takes it. Fixed in the follow-up below; needs this live test. |
| Burst toggled off, action disabled, then restored | Filler remains active; no stale queued burst; correct job-specific recovery when legal. |
| Repeated full-uptime cycles and phase transitions | Compare actions, buff windows, overcaps and drift with the selected guide/party plan, including death/resurrection. |
| Ultimate sync | Test at 70 (UCoB/UwU), 80 (TEA), 90 (DSR/TOP), and 100 content. Exclude unavailable actions and verify the altered low-level kit. |
| Planned raidwide/tankbuster during a damage burst | Requested mitigation executes before the hit even at full HP and during job damage procs. **Several automatic paths still need changes.** |

Recommended next implementation order: Sage manual-Eukrasia ownership; removal of burst/combo suppression from planned mitigation; DRK Blood-spending policy; BRD cooldown-desync recovery; SAM Meditation protection; DNC proc deadlines/stall traces; then encounter-specific optimization for all jobs. Each needs actual action-timeline evidence. No statement here guarantees a clear, parse percentile or optimal fight-specific burst timing.

## Guide sources checked

Retrieved 2026-10-20. The Icy Veins pages display Dawntrail 7.55; Balance guide revision dates vary. Repository `GameVersion` attributes alone do not establish guide compliance. Use official action descriptions to resolve current resource/status rules, and recheck guides after patches.

| Job | The Balance | Icy Veins |
|---|---|---|
| BRD | [Basic](https://www.thebalanceffxiv.com/jobs/ranged/bard/basic-guide/) | [Rotation](https://www.icy-veins.com/ffxiv/bard-pve-dps-rotation-openers-abilities) |
| MCH | [Basic](https://www.thebalanceffxiv.com/jobs/ranged/machinist/basic-guide/), [FAQ](https://www.thebalanceffxiv.com/jobs/ranged/machinist/faq/) | [Rotation](https://www.icy-veins.com/ffxiv/machinist-pve-dps-rotation-openers-abilities) |
| DNC | [Basic](https://www.thebalanceffxiv.com/jobs/ranged/dancer/basic-guide/), [Advanced](https://www.thebalanceffxiv.com/jobs/ranged/dancer/advanced-guide/) | [Rotation](https://www.icy-veins.com/ffxiv/dancer-pve-dps-rotation-openers-abilities) |
| SGE | [Basic](https://www.thebalanceffxiv.com/jobs/healers/sage/basic-guide/) | [DPS](https://www.icy-veins.com/ffxiv/sage-dps-rotation-for-healers), [Healing](https://www.icy-veins.com/ffxiv/sage-pve-healing-rotation-ability-priority) |
| DRK | [Basic](https://www.thebalanceffxiv.com/jobs/tanks/dark-knight/basic-guide/) | [Rotation](https://www.icy-veins.com/ffxiv/dark-knight-pve-tank-rotation-openers-abilities) |
| WAR | [Basic](https://www.thebalanceffxiv.com/jobs/tanks/warrior/basic-guide/) | [Rotation](https://www.icy-veins.com/ffxiv/warrior-pve-tank-rotation-openers-abilities) |
| PCT | [Basic](https://www.thebalanceffxiv.com/jobs/casters/pictomancer/basic-guide/) | [Rotation](https://www.icy-veins.com/ffxiv/pictomancer-pve-dps-rotation-openers-abilities) |
| SAM | [Basic](https://www.thebalanceffxiv.com/jobs/melee/samurai/basic-guide/) | [Rotation](https://www.icy-veins.com/ffxiv/samurai-pve-dps-rotation-openers-abilities) |
| RPR | [Basic](https://www.thebalanceffxiv.com/jobs/melee/reaper/basic-guide/) | [Rotation](https://www.icy-veins.com/ffxiv/reaper-pve-dps-rotation-openers-abilities) |
| VPR | [Basic](https://www.thebalanceffxiv.com/jobs/melee/viper/basic-guide/) | [Rotation](https://www.icy-veins.com/ffxiv/viper-pve-dps-rotation-openers-abilities) |

Official references: [Machinist](https://na.finalfantasyxiv.com/jobguide/machinist/), [Reaper](https://na.finalfantasyxiv.com/jobguide/reaper/), [Pictomancer](https://na.finalfantasyxiv.com/jobguide/pictomancer/).

## Follow-up fixes after this review

Applied on top of `8f28722`, verified by a Release build and the countdown regression tests (16 scenarios). Not yet validated in game.

| Review item | Change |
|---|---|
| No-countdown pull gate | Kept for Savage, Extreme and Ultimate. Elsewhere only a running countdown blocks automation, so dungeons, AutoDuty and out-of-combat raises/stances keep working. Countdown cancellation (StartOnCountdown) uses whether the timer actually reached zero. |
| SGE manual Eukrasia ownership | The input hook records the player's own presses (not BRR's). While a self-pressed Eukrasia is unspent (up to 10s), Sage selects no GCD and neither clears nor re-aims Eukrasia. **Resolved, needs the live test above.** |
| Mitigation suppressed by burst/procs | Removed for DRK (Dark Missionary, Reprisal, Oblation), PCT (`BurstDefense` default off; single-target fallback fixed), SAM (Zanshin gate), RPR (`NotInActiveCombo` on Feint/Arcane Crest/Arm's Length), VPR (`NoAbilityReady` on Feint/Arm's Length). DRK TBN keeps its burst gate (MP cost). |
| SAM Meditation overcap | Shoha is spent before Ogi Namikiri when Meditation is full. |
| BRD buff desync | A buff more than 10s from ready no longer holds back Battle Voice, Radiant Finale or Raging Strikes. |
| DNC Last Dance ordering / idle GCDs | Expiring Last Dance is checked before Dance of the Dawn and Tillana. Standard Step held for an upcoming Technical no longer suppresses filler. |

Still open: DRK Blood-spending policy, RPR Enshroud pooling, VPR Reawaken buff runway, SGE proactive Haima/Panhaima/Taurochole and Philosophia, Wildfire six-hit and level-sync validation.

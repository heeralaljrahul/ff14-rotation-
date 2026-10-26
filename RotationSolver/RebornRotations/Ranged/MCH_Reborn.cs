namespace RotationSolver.RebornRotations.Ranged;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.57", RotationType = RotationType.Reborn)]
[SourceCode(Path = "main/RebornRotations/Ranged/MCH_Reborn.cs")]

public sealed class MCH_Reborn : MachinistRotation
{
	#region Config Options
	[RotationConfig(CombatType.PvE, Name = "Use burst medicine in countdown (requires auto burst option on)")]
	private bool OpenerBurstMeds { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Bioblaster while moving")]
	private bool BioMove { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Only use Wildfire on Boss targets")]
	private bool WildfireBoss { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Restrict mitigations to not overlap")]
	private bool MitOverlap { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use AirAnchor at 1 second remaining on countdown")]
	private bool AirAnchorCountdown { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Restrict Tactician to only be allowed to be used when there are multiple hostile targets")]
	private bool MultiTact { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Prevent the use of defense abilities during burst")]
	private bool BurstDefense { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "BMR: Dump Heat before downtime (Experimental)")]
	public bool BmrDumpBeforeDowntime { get; set; } = true;
	#endregion

	#region Ultimate Settings
	[RotationConfig(CombatType.PvE, Name = "Use per-ultimate settings", Tooltip = "Inside an ultimate, use the settings on that ultimate's tab instead of the defaults.", UltimateSection = true)]
	public bool UseUltimateSettings { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "UCoB: Use potion in the opener", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.UCoB)]
	public bool UcobOpenerPotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "UCoB: Hold 2-minute burst when downtime is imminent (needs BossMod Reborn)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.UCoB)]
	public bool UcobHoldBurstForDowntime { get; set; } = true;

	[Range(0, 10, ConfigUnitType.Seconds, 0.5f)]
	[RotationConfig(CombatType.PvE, Name = "UCoB: Max seconds to hold 2-minute burst for party raid buffs (0 = never hold)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.UCoB)]
	public float UcobPartyAlignHold { get; set; } = 3;

	[RotationConfig(CombatType.PvE, Name = "UwU: Use potion in the opener", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.UwU)]
	public bool UwuOpenerPotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "UwU: Hold 2-minute burst when downtime is imminent (needs BossMod Reborn)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.UwU)]
	public bool UwuHoldBurstForDowntime { get; set; } = true;

	[Range(0, 10, ConfigUnitType.Seconds, 0.5f)]
	[RotationConfig(CombatType.PvE, Name = "UwU: Max seconds to hold 2-minute burst for party raid buffs (0 = never hold)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.UwU)]
	public float UwuPartyAlignHold { get; set; } = 3;

	[RotationConfig(CombatType.PvE, Name = "TEA: Use potion in the opener", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.TEA)]
	public bool TeaOpenerPotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "TEA: Hold 2-minute burst when downtime is imminent (needs BossMod Reborn)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.TEA)]
	public bool TeaHoldBurstForDowntime { get; set; } = true;

	[Range(0, 10, ConfigUnitType.Seconds, 0.5f)]
	[RotationConfig(CombatType.PvE, Name = "TEA: Max seconds to hold 2-minute burst for party raid buffs (0 = never hold)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.TEA)]
	public float TeaPartyAlignHold { get; set; } = 3;

	[RotationConfig(CombatType.PvE, Name = "DSR: Use potion in the opener", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.DSR)]
	public bool DsrOpenerPotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "DSR: Hold 2-minute burst when downtime is imminent (needs BossMod Reborn)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.DSR)]
	public bool DsrHoldBurstForDowntime { get; set; } = true;

	[Range(0, 10, ConfigUnitType.Seconds, 0.5f)]
	[RotationConfig(CombatType.PvE, Name = "DSR: Max seconds to hold 2-minute burst for party raid buffs (0 = never hold)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.DSR)]
	public float DsrPartyAlignHold { get; set; } = 3;

	[RotationConfig(CombatType.PvE, Name = "TOP: Use potion in the opener", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.TOP)]
	public bool TopOpenerPotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "TOP: Hold 2-minute burst when downtime is imminent (needs BossMod Reborn)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.TOP)]
	public bool TopHoldBurstForDowntime { get; set; } = true;

	[Range(0, 10, ConfigUnitType.Seconds, 0.5f)]
	[RotationConfig(CombatType.PvE, Name = "TOP: Max seconds to hold 2-minute burst for party raid buffs (0 = never hold)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.TOP)]
	public float TopPartyAlignHold { get; set; } = 3;

	[RotationConfig(CombatType.PvE, Name = "FRU: Use potion in the opener", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.FRU)]
	public bool FruOpenerPotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "FRU: Hold 2-minute burst when downtime is imminent (needs BossMod Reborn)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.FRU)]
	public bool FruHoldBurstForDowntime { get; set; } = true;

	[Range(0, 10, ConfigUnitType.Seconds, 0.5f)]
	[RotationConfig(CombatType.PvE, Name = "FRU: Max seconds to hold 2-minute burst for party raid buffs (0 = never hold)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.FRU)]
	public float FruPartyAlignHold { get; set; } = 3;

	[RotationConfig(CombatType.PvE, Name = "DMU: Use potion in the opener", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.DMU)]
	public bool DmuOpenerPotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "DMU: Hold 2-minute burst when downtime is imminent (needs BossMod Reborn)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.DMU)]
	public bool DmuHoldBurstForDowntime { get; set; } = true;

	[Range(0, 10, ConfigUnitType.Seconds, 0.5f)]
	[RotationConfig(CombatType.PvE, Name = "DMU: Max seconds to hold 2-minute burst for party raid buffs (0 = never hold)", Parent = nameof(UseUltimateSettings), Ultimate = UltimateRaid.DMU)]
	public float DmuPartyAlignHold { get; set; } = 3;

	private UltimateSettings Ultimate => !UseUltimateSettings ? UltimateSettings.Default : CurrentUltimate switch
	{
		UltimateRaid.UCoB => new(UcobOpenerPotion, UcobHoldBurstForDowntime, UcobPartyAlignHold),
		UltimateRaid.UwU => new(UwuOpenerPotion, UwuHoldBurstForDowntime, UwuPartyAlignHold),
		UltimateRaid.TEA => new(TeaOpenerPotion, TeaHoldBurstForDowntime, TeaPartyAlignHold),
		UltimateRaid.DSR => new(DsrOpenerPotion, DsrHoldBurstForDowntime, DsrPartyAlignHold),
		UltimateRaid.TOP => new(TopOpenerPotion, TopHoldBurstForDowntime, TopPartyAlignHold),
		UltimateRaid.FRU => new(FruOpenerPotion, FruHoldBurstForDowntime, FruPartyAlignHold),
		UltimateRaid.DMU => new(DmuOpenerPotion, DmuHoldBurstForDowntime, DmuPartyAlignHold),
		_ => UltimateSettings.Default,
	};
	#endregion

	#region Countdown logic
	protected override IAction? CountDownAction(float remainTime)
	{
		if (AirAnchorCountdown && remainTime < 1f && AirAnchorPvE.EnoughLevel && AirAnchorPvE.CanUse(out var act))
		{
			return act;
		}

		if (!AirAnchorCountdown && remainTime < 0.1f && AirAnchorPvE.EnoughLevel && AirAnchorPvE.CanUse(out act))
		{
			return act;
		}

		if (remainTime < 4.75f && ReassemblePvE.CanUse(out act))
		{
			return act;
		}

		// The Balance: an opener pot goes two seconds before the pull.
		if (IsBurst && UseOpenerPotion && remainTime <= 2f && UseBurstMedicine(out act))
		{
			return act;
		}

		return base.CountDownAction(remainTime);
	}
	#endregion

	private bool UseOpenerPotion => IsInUltimate && UseUltimateSettings ? Ultimate.OpenerPotion : OpenerBurstMeds;

	private readonly BurstAligner _burstAligner = new();

	/// <summary>
	/// True when the 2-minute burst (Barrel Stabilizer + Wildfire) should be held: either downtime would eat
	/// the Wildfire window, or the party's raid buffs haven't gone out yet.
	/// </summary>
	private bool HoldTwoMinuteBurst
	{
		get
		{
			if (!Service.Config.AutomaticBurstHolds)
			{
				return false;
			}

			// AutoBurst was turned back on (e.g. by a hold/release macro) and the burst hasn't gone out yet: go now.
			if (BurstReleasePending(WildfirePvE))
			{
				return false;
			}

			var burstReady = (BarrelStabilizerPvE.EnoughLevel && !BarrelStabilizerPvE.Cooldown.IsCoolingDown)
				|| (WildfirePvE.EnoughLevel && !WildfirePvE.Cooldown.IsCoolingDown);

			if (_burstAligner.ShouldHold(burstReady, Ultimate.PartyAlignHold))
			{
				return true;
			}

			// Wildfire lasts 10s; give it room for the full Hypercharge window plus a GCD of slack.
			return Ultimate.HoldBurstForDowntime && BMRDowntimeWithin(15f);
		}
	}

	/// <summary>
	/// True when Wildfire is coming soon (within 15s) and we should save Heat.
	/// </summary>
	private bool IsPreBurst => WildfirePvE.EnoughLevel
		&& WildfirePvE.Cooldown.IsCoolingDown
		&& !WildfirePvE.Cooldown.HasOneCharge
		&& WildfirePvE.Cooldown.RecastTimeRemain <= 15;

	protected override void UpdateInfo()
	{
		// Selection is blocked before a pull, so reset pull-local state here rather than in an ability branch.
		if (!InCombat || Service.CountDownTime > 0)
		{
			ResetQueenStep();
		}
		base.UpdateInfo();
	}

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (InCombat)
		{
			UpdateQueenStep();
			UpdateFoundStepPair();
		}
		else
		{
			ResetQueenStep();
		}

		// No pre-pull: the opener potion goes in the first weave after the pull instead.
		if (IsBurst && UseOpenerPotion && InPostPullPotionWindow() && UseBurstMedicine(out act))
		{
			return true;
		}

		if (HyperchargePvE.EnoughLevel)
		{
			if (!WildfirePvE.EnoughLevel)
			{
				if (HyperchargePvE.CanUse(out act, skipTTKCheck: true))
				{
					return true;
				}
			}
			if (!FullMetalFieldPvE.EnoughLevel && (HasWildfire || (WildfirePvE.Cooldown.IsCoolingDown && Heat == 100)))
			{
				if (HyperchargePvE.CanUse(out act, skipTTKCheck: true))
				{
					return true;
				}
			}
			// Hypercharge into Wildfire once Full Metal Field is spent (or never came, when Barrel Stabilizer drifted).
			if (HasWildfire && !HasFullMetalMachinist)
			{
				if (HyperchargePvE.CanUse(out act, skipTTKCheck: true))
				{
					return true;
				}
			}

			// === BMR: Dump Heat before downtime ===
			// If downtime is imminent (<=15s) and we're not in Wildfire, spend Heat aggressively
			// to avoid losing gauge value during the untargetable phase.
			// Skip if we're already in pre-burst window (Wildfire coming soon and downtime is far).
			// Only when the five Blazing Shots still fit before the boss leaves, and no tool is about to come up.
			if (BmrDumpBeforeDowntime && BMRDowntimeWithin(15f) && !BMRDowntimeWithin(8f)
				&& !HasWildfire && !IsOverheated && (Heat >= 50 || HasHypercharged)
				&& !IsPreBurst)
			{
				if (ToolChargeSoon(out act))
				{
					return true;
				}
			}
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	#region oGCD Logic
	[RotationDesc(ActionID.TacticianPvE, ActionID.DismantlePvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (BurstDefense && (IsOverheated || HasWildfire || HasFullMetalMachinist))
		{
			return base.DefenseAreaAbility(nextGCD, out act);
		}

		if (!MultiTact || (MultiTact && NumberOfAllHostilesInMaxRange > 1))
		{
			if (TacticianPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (!MitOverlap || (MitOverlap && !StatusHelper.PlayerHasStatus(true, StatusID.Tactician_1951)))
		{
			if (DismantlePvE.CanUse(out act))
			{
				return true;
			}
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	// Logic for using attack abilities outside of GCD, focusing on burst windows and cooldown management.
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (FullMetalFieldPvE.EnoughLevel && HasFullMetalMachinist && IsLastAction(false, WildfirePvE))
		{
			return base.AttackAbility(nextGCD, out act);
		}

		// Reassemble Logic
		// Check next GCD action and conditions for Reassemble.
		// Reassemble goes under raid buffs where possible (The Balance): outside the 2-minute window, only spend a
		// charge that would otherwise sit capped, so both charges are ready for the burst.
		var inTwoMinuteWindow = HasWildfire
			|| (WildfirePvE.EnoughLevel && WildfirePvE.Cooldown.WillHaveOneCharge(10))
			|| HasOtherPartyRaidBuff;
		var isReassembleUsable =
			//Reassemble current # of charges and double proc protection
			ReassemblePvE.Cooldown.CurrentCharges > 0 && !HasReassembled
			&& (inTwoMinuteWindow || !WildfirePvE.EnoughLevel || ReassemblePvE.Cooldown.CurrentCharges >= ReassemblePvE.Cooldown.MaxCharges) &&
			(nextGCD.IsTheSameTo(true, [ChainSawPvE, ExcavatorPvE])
			|| (!ChainSawPvE.EnoughLevel && nextGCD.IsTheSameTo(true, SpreadShotPvE) && ((IBaseAction)nextGCD).Target.AffectedTargets.Length >= (SpreadShotMasteryTrait.EnoughLevel ? 4 : 5))
			|| nextGCD.IsTheSameTo(false, [AirAnchorPvE])
			|| nextGCD.IsTheSameTo(true, DrillPvE)
			|| (!DrillPvE.EnoughLevel && nextGCD.IsTheSameTo(true, CleanShotPvE))
			|| (!CleanShotPvE.EnoughLevel && nextGCD.IsTheSameTo(false, HotShotPvE)));
		// Attempt to use Reassemble if it's ready
		if (isReassembleUsable)
		{
			if (ReassemblePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		// Start Ricochet/Gauss cooldowns rolling if they are not already
		if (!RicochetPvE.Cooldown.IsCoolingDown)
		{
			if (CheckmatePvE.EnoughLevel && CheckmatePvE.CanUse(out act))
			{
				return true;
			}
			if (!CheckmatePvE.EnoughLevel && RicochetPvE.CanUse(out act))
			{
				return true;
			}
		}
		if (!GaussRoundPvE.Cooldown.IsCoolingDown)
		{
			if (DoubleCheckPvE.EnoughLevel && DoubleCheckPvE.CanUse(out act))
			{
				return true;
			}
			if (!DoubleCheckPvE.EnoughLevel && GaussRoundPvE.CanUse(out act))
			{
				return true;
			}
		}

		var holdBurst = HoldTwoMinuteBurst;
		if (IsBurst && !holdBurst)
		{
			var bmrBlockBarrel = BMRDowntimeWithin(GCDTime(2)) && !BurstJustReleased;
			if (!bmrBlockBarrel && BarrelStabilizerPvE.CanUse(out act))
			{
				return true;
			}
		}

		var LowLevelHyperCheck = !AutoCrossbowPvE.EnoughLevel && SpreadShotPvE.CanUse(out _);
		var bmrBlockWildfire = BMRDowntimeWithin(10f) && !BurstJustReleased;
		if (IsBurst && !bmrBlockWildfire && !holdBurst)
		{
			if (FullMetalFieldPvE.EnoughLevel)
			{
				if (Heat >= 50 || HasHypercharged)
				{
					if (WeaponRemain < (GCDTime(1) / 2) && nextGCD.IsTheSameTo(false, FullMetalFieldPvE))
					{
						if (WildfirePvE.CanUse(out act))
						{
							if ((WildfirePvE.Target.Target.IsBossFromIcon() && WildfireBoss) || !WildfireBoss)
							{
								return true;
							}
						}
					}

					// Recovery when Barrel Stabilizer has drifted away from Wildfire, so no Full Metal Field is coming:
					// pair Wildfire with a Hypercharge window instead (Hypercharge follows right after), which still
					// lands six weaponskills. Only when no tool is about to come up (ToolChargeSoon).
					if (WeaponRemain < (GCDTime(1) / 2) && !HasFullMetalMachinist && !IsOverheated
						&& BarrelStabilizerPvE.Cooldown.IsCoolingDown && !BarrelStabilizerPvE.Cooldown.WillHaveOneCharge(10)
						&& ToolChargeSoon(out _))
					{
						if (WildfirePvE.CanUse(out act))
						{
							if ((WildfirePvE.Target.Target.IsBossFromIcon() && WildfireBoss) || !WildfireBoss)
							{
								return true;
							}
						}
					}
				}
			}
			if (!FullMetalFieldPvE.EnoughLevel)
			{
				if ((Heat >= 50 || HasHypercharged) && ToolChargeSoon(out _) && !LowLevelHyperCheck)
				{
					if (WeaponRemain < (GCDTime(1) / 2))
					{
						if (WildfirePvE.CanUse(out act))
						{
							if ((WildfirePvE.Target.Target.IsBossFromIcon() && WildfireBoss) || !WildfireBoss)
							{
								return true;
							}
						}
					}
				}
			}
		}

		if (UseQueen(out act, nextGCD))
		{
			return true;
		}

		// Use Hypercharge if wildfire will not be up in 30 seconds or if you hit 100 heat
		if (!LowLevelHyperCheck && !HasReassembled && (!WildfirePvE.Cooldown.WillHaveOneCharge(30) || (Heat == 100)))
		{
			if (!(LiveComboTime <= 9f && LiveComboTime > 0f) && ToolChargeSoon(out act))
			{
				return true;
			}
		}

		// Decide which oGCD to use based on which has more RecastTimeElapsed
		var whichToUse = RicochetPvE.EnoughLevel switch
		{
			true when RicochetPvE.Cooldown.RecastTimeElapsed > GaussRoundPvE.Cooldown.RecastTimeElapsed => "Ricochet",
			true when GaussRoundPvE.Cooldown.RecastTimeElapsed > RicochetPvE.Cooldown.RecastTimeElapsed => "GaussRound",
			true => "Ricochet", // Default to Ricochet if equal
			_ => "GaussRound"
		};

		// Pool Double Check/Checkmate for raid buffs; spend freely in Wildfire/Overheat (Blazing Shot reduces their
		// recast, so they'd overcap) or right before downtime. Outside that they are only used to avoid overcapping.
		var spendGaussCharges = HasWildfire || IsOverheated || HasOtherPartyRaidBuff || BMRDowntimeWithin(10f);

		if (!FullMetalFieldPvE.EnoughLevel || (FullMetalFieldPvE.EnoughLevel && !nextGCD.IsTheSameTo(false, FullMetalFieldPvE)))
		{
			switch (whichToUse)
			{
				case "Ricochet":
					if (CheckmatePvE.EnoughLevel && CheckmatePvE.CanUse(out act, usedUp: spendGaussCharges))
					{
						return true;
					}
					if (!CheckmatePvE.EnoughLevel && RicochetPvE.CanUse(out act, usedUp: spendGaussCharges))
					{
						return true;
					}
					break;
				case "GaussRound":
					if (DoubleCheckPvE.EnoughLevel && DoubleCheckPvE.CanUse(out act, usedUp: spendGaussCharges))
					{
						return true;
					}
					if (!DoubleCheckPvE.EnoughLevel && GaussRoundPvE.CanUse(out act, usedUp: spendGaussCharges))
					{
						return true;
					}
					break;
			}
		}

		return base.AttackAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	protected override bool GeneralGCD(out IAction? act)
	{
		// ensure combo is not broken, okay to drop during overheat
		if (IsLastComboAction(true, SlugShotPvE) && LiveComboTime >= GCDTime(1) && LiveComboTime <= GCDTime(2) && !IsOverheated)
		{
			// 3
			if (HeatedCleanShotPvE.EnoughLevel && HeatedCleanShotPvE.CanUse(out act))
			{
				return true;
			}
			if (!HeatedCleanShotPvE.EnoughLevel && CleanShotPvE.CanUse(out act))
			{
				return true;
			}
		}

		// ensure combo is not broken, okay to drop during overheat
		if (IsLastComboAction(true, SplitShotPvE) && LiveComboTime >= GCDTime(1) && LiveComboTime <= GCDTime(2) && !IsOverheated)
		{
			// 2
			if (HeatedSlugShotPvE.EnoughLevel && HeatedSlugShotPvE.CanUse(out act))
			{
				return true;
			}
			if (!HeatedSlugShotPvE.Info.EnoughLevelAndQuest() && SlugShotPvE.CanUse(out act))
			{
				return true;
			}
		}

		// Overheated AOE
		if (AutoCrossbowPvE.CanUse(out act))
		{
			return true;
		}

		// Overheated ST
		if (BlazingShotPvE.EnoughLevel && BlazingShotPvE.CanUse(out act))
		{
			return true;
		}
		if (!BlazingShotPvE.EnoughLevel && HeatBlastPvE.CanUse(out act))
		{
			return true;
		}

		if (IsLastAction(false, HyperchargePvE) && HeatBlastPvE.EnoughLevel)
		{
			return base.GeneralGCD(out act);
		}

		// Drill AOE
		if ((BioMove || (!IsMoving && !BioMove)) && BioblasterPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		// ST Big GCDs
		if (HotShotMasteryTrait.EnoughLevel && AirAnchorPvE.CanUse(out act))
		{
			return true;
		}

		// for opener: only use the first charge of Drill after AirAnchor when there are two
		if (DrillPvE.CanUse(out act, usedUp: false))
		{
			return true;
		}

		if (!HotShotMasteryTrait.EnoughLevel && HotShotPvE.CanUse(out act))
		{
			return true;
		}

		// use combo finisher asap
		if (ExcavatorPvE.CanUse(out act))
		{
			return true;
		}

		// ChainSaw is always used after Drill
		if (ChainSawPvE.CanUse(out act))
		{
			return true;
		}

		if (!AirAnchorPvE.CanUse(out _) && !ChainSawPvE.CanUse(out _) && !ExcavatorPvE.CanUse(out _) && !HasExcavatorReady
			&& !IsLastGCD(false, ChainSawPvE) && DrillPvE.Cooldown.CurrentCharges < 2 && (!WildfirePvE.Cooldown.IsCoolingDown || IsLastAction(false, WildfirePvE)))
		{
			if (FullMetalFieldPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (DrillPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (StatusHelper.PlayerWillStatusEnd(3, true, StatusID.FullMetalMachinist))
		{
			if (FullMetalFieldPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (StatusHelper.PlayerWillStatusEnd(3, true, StatusID.ExcavatorReady))
		{
			if (ExcavatorPvE.CanUse(out act))
			{
				return true;
			}
		}

		// 1 AOE
		if (!IsOverheated)
		{
			if (ScattergunPvE.EnoughLevel)
			{
				if (ScattergunPvE.CanUse(out act))
				{
					return true;
				}
			}
			if (!ScattergunPvE.EnoughLevel)
			{
				if (SpreadShotPvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		// 3 ST
		if (HeatedCleanShotPvE.EnoughLevel && HeatedCleanShotPvE.CanUse(out act))
		{
			return true;
		}
		if (!HeatedCleanShotPvE.EnoughLevel && CleanShotPvE.CanUse(out act))
		{
			return true;
		}
		// 2 ST
		if (HeatedSlugShotPvE.EnoughLevel && HeatedSlugShotPvE.CanUse(out act))
		{
			return true;
		}
		if (!HeatedSlugShotPvE.Info.EnoughLevelAndQuest() && SlugShotPvE.CanUse(out act))
		{
			return true;
		}
		// 1 ST
		if (HeatedSplitShotPvE.EnoughLevel && HeatedSplitShotPvE.CanUse(out act))
		{
			return true;
		}
		if (!HeatedSplitShotPvE.Info.EnoughLevelAndQuest() && SplitShotPvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion

	#region Tracking Properties
	public override void DisplayRotationStatus()
	{
		ImGui.Text($"QueenStep: {_currentStep}");
		ImGui.Text($"Step Pair Found: {foundStepPair}");
	}
	#endregion

	// Logic for Hypercharge
	private bool ToolChargeSoon(out IAction? act)
	{
		var REST_TIME = 8f;
		if
			//Cannot AOE
			(!SpreadShotPvE.CanUse(out _)
			&&
			// AirAnchor Enough Level % AirAnchor 
			((AirAnchorPvE.EnoughLevel && AirAnchorPvE.Cooldown.WillHaveOneCharge(REST_TIME))
			||
			// HotShot Charge Detection
			(!AirAnchorPvE.EnoughLevel && HotShotPvE.EnoughLevel && HotShotPvE.Cooldown.WillHaveOneCharge(REST_TIME))
			||
			// Drill Charge Detection
			(DrillPvE.EnoughLevel && DrillPvE.Cooldown.WillHaveXCharges(DrillPvE.Cooldown.MaxCharges, REST_TIME))
			||
			// Chainsaw Charge Detection
			(ChainSawPvE.EnoughLevel && ChainSawPvE.Cooldown.WillHaveOneCharge(REST_TIME))))
		{
			act = null;
			return false;
		}
		else
		{
			return HyperchargePvE.CanUse(out act, skipTTKCheck: true);
		}
	}

	private readonly (byte from, byte to, int step)[] _stepPairs =
	[
		(0, 60, 0),
		(60, 90, 1),
		(90, 100, 2),
		(100, 50, 3),
		(50, 60, 4),
		(60, 100, 5),
		(100, 50, 6),
		(50, 70, 7),
		(70, 100, 8),
		(100, 50, 9),
		(50, 80, 10),
		(80, 100, 11),
		(100, 50, 12),
		(50, 60, 13)
	];

	private int _currentStep = 0; // Track the current step
	private bool foundStepPair = false;

	/// <summary>
	/// Checks if the current battery transition matches the current step only.
	/// </summary>
	private void UpdateFoundStepPair()
	{
		// Only check the current step
		if (_currentStep < _stepPairs.Length)
		{
			(var from, var to, var _) = _stepPairs[_currentStep];
			foundStepPair = (LastSummonBatteryPower == from && Battery == to);
		}
		else
		{
			foundStepPair = false;
		}
	}

	// Steps 3-11 are one full cycle of the battery plan; later steps repeat it.
	private const int QueenCycleStart = 3;
	private const int QueenCycleLength = 9;

	private bool _wasRobotActive;

	public void UpdateQueenStep()
	{
		// Advance on every summon (the robot becoming active), including back-to-back summons at the same battery.
		if (IsRobotActive && !_wasRobotActive)
		{
			AdvanceStep();
		}
		_wasRobotActive = IsRobotActive;
	}

	private void ResetQueenStep()
	{
		_currentStep = 0;
		foundStepPair = false;
		_wasRobotActive = IsRobotActive;
	}

	private void AdvanceStep()
	{
		_currentStep++;
		if (_currentStep >= _stepPairs.Length)
		{
			_currentStep = QueenCycleStart + ((_currentStep - QueenCycleStart) % QueenCycleLength);
		}

		// Resync after deaths, downtime or fallback summons: jump to the next step that starts from the battery
		// the last Queen actually used.
		if (_stepPairs[_currentStep].from == LastSummonBatteryPower)
		{
			return;
		}

		for (var i = 0; i < QueenCycleLength; i++)
		{
			var index = QueenCycleStart + ((_currentStep - QueenCycleStart + i + QueenCycleLength) % QueenCycleLength);
			if (_stepPairs[index].from == LastSummonBatteryPower)
			{
				_currentStep = index;
				return;
			}
		}
	}
	private bool UseQueen(out IAction? act, IAction nextGCD)
	{
		act = null;
		if (!InCombat || IsRobotActive)
		{
			return false;
		}

		// Opener
		if (Battery == 60 && IsLastGCD(false, ExcavatorPvE) && CombatTime < 15)
		{
			if (AutomatonQueenPvE.EnoughLevel && AutomatonQueenPvE.CanUse(out act, skipTTKCheck: true))
			{
				return true;
			}

			if (!AutomatonQueenPvE.EnoughLevel && RookAutoturretPvE.CanUse(out act, skipTTKCheck: true))
			{
				return true;
			}
		}

		// Queen hits are lost while the boss is untargetable, so don't start one that would run into downtime.
		// Overcap protection below still applies.
		// Queen lasts 12s and Rook 9s regardless of battery; allow a couple of seconds to deploy.
		var queenIntoDowntime = Service.Config.AutomaticBurstHolds && Ultimate.HoldBurstForDowntime && !BurstJustReleased
			&& BMRDowntimeWithin(AutomatonQueenPvE.EnoughLevel ? 14f : 11f);

		// Burst: dump a high battery into the Wildfire window. Keeps 2-minute Queens aligned even after the
		// battery step table below desyncs (deaths, phase transitions, downtime).
		if (!queenIntoDowntime && Battery >= 90 && (HasWildfire || IsLastAction(false, WildfirePvE)))
		{
			if (AutomatonQueenPvE.EnoughLevel && AutomatonQueenPvE.CanUse(out act, skipTTKCheck: true))
			{
				return true;
			}

			if (!AutomatonQueenPvE.EnoughLevel && RookAutoturretPvE.CanUse(out act, skipTTKCheck: true))
			{
				return true;
			}
		}

		// Only allow battery usage if the current transition matches the expected step
		if (foundStepPair && !queenIntoDowntime)
		{
			if (AutomatonQueenPvE.EnoughLevel && AutomatonQueenPvE.CanUse(out act, skipTTKCheck: true))
			{
				return true;
			}

			if (!AutomatonQueenPvE.EnoughLevel && RookAutoturretPvE.CanUse(out act, skipTTKCheck: true))
			{
				return true;
			}
		}

		// overcap protection
		if ((nextGCD.IsTheSameTo(false, CleanShotPvE, HeatedCleanShotPvE) && Battery > 90)
			|| (nextGCD.IsTheSameTo(false, HotShotPvE, AirAnchorPvE, ChainSawPvE, ExcavatorPvE) && Battery > 80))
		{
			if (AutomatonQueenPvE.EnoughLevel && AutomatonQueenPvE.CanUse(out act, skipTTKCheck: true))
			{
				return true;
			}

			if (!AutomatonQueenPvE.EnoughLevel && RookAutoturretPvE.CanUse(out act, skipTTKCheck: true))
			{
				return true;
			}
		}
		return false;
	}
}
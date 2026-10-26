namespace RotationSolver.RebornRotations.Ranged;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.57", RotationType = RotationType.Reborn,
	Description = "Please make sure that the three song times add up to 120 seconds, Wanderers default first song for now.")]
[SourceCode(Path = "main/RebornRotations/Ranged/BRD_Reborn.cs")]

public sealed class BRD_Reborn : BardRotation
{
	#region Config Options

	[Range(1, 5, ConfigUnitType.Seconds, 0.1f)]
	[RotationConfig(CombatType.PvE, Name = "Buff Alignment Timer (Experimental, do not touch if you don't understand it)")]
	public float BuffAlignment { get; set; } = 1;

	[RotationConfig(CombatType.PvE, Name = "Only use DOTs on targets with Boss Icon")]
	public bool DOTBoss { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Skip DOT check for Barrage usage")]
	public bool DOTBarrage { get; set; } = false;

	[Range(80, 100, ConfigUnitType.None, 5)]
	[RotationConfig(CombatType.PvE, Name = "Soul Voice Threshold for Apex Arrow")]
	public float SoulVoiceConfig { get; set; } = 100;

	[Range(1, 45, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "Wanderer's Minuet Uptime")]
	public float WANDTime { get; set; } = 43;

	[Range(0, 45, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "Mage's Ballad Uptime")]
	public float MAGETime { get; set; } = 43;

	[Range(0, 45, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "Army's Paeon Uptime")]
	public float ARMYTime { get; set; } = 34;

	[RotationConfig(CombatType.PvE, Name = "First Song")]
	private SongTrack FirstSong { get; set; } = SongTrack.WanderersMinuet;

	[RotationConfig(CombatType.PvE, Name = "Second Song")]
	private SongTrack SecondSong { get; set; } = SongTrack.MagesBallad;

	[RotationConfig(CombatType.PvE, Name = "Third Song")]
	private SongTrack ThirdSong { get; set; } = SongTrack.ArmysPaeon;

	[RotationConfig(CombatType.PvE, Name = "Use Warden's Paean on other players")]
	public bool BRDEsuna { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Prevent the use of defense abilties during burst")]
	private bool BurstDefense { get; set; } = false;

	private float WANDRemainTime => 45 - WANDTime;
	private float MAGERemainTime => 45 - MAGETime;
	private float ARMYRemainTime => 45 - ARMYTime;

	private bool InBurstStatus => (!BattleVoicePvE.EnoughLevel && HasRagingStrikes)
		|| (BattleVoicePvE.EnoughLevel && !RadiantFinalePvE.EnoughLevel && HasRagingStrikes && HasBattleVoice)
		|| (MinstrelsCodaTrait.EnoughLevel && HasRagingStrikes && HasRadiantFinale && HasBattleVoice);

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
	// Defines logic for actions to take during the countdown before combat starts.
	protected override IAction? CountDownAction(float remainTime)
	{
		// tincture needs to be used on -0.7s exactly
		if ((!IsInUltimate || Ultimate.OpenerPotion) && remainTime <= 0.7f && UseBurstMedicine(out var act))
		{
			return act;
		}
		return base.CountDownAction(remainTime);
	}
	#endregion

	private readonly BurstAligner _burstAligner = new();

	/// <summary>
	/// True when Battle Voice (and so Radiant Finale and Raging Strikes, which chain off it) should be held:
	/// either downtime would eat most of the 20s buff window, or the party's raid buffs haven't gone out yet.
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
			if (BurstReleasePending(BattleVoicePvE))
			{
				return false;
			}

			var burstReady = BattleVoicePvE.EnoughLevel && !BattleVoicePvE.Cooldown.IsCoolingDown;
			if (_burstAligner.ShouldHold(burstReady, Ultimate.PartyAlignHold))
			{
				return true;
			}

			return burstReady && Ultimate.HoldBurstForDowntime && BMRDowntimeWithin(15f);
		}
	}

	#region oGCD Logic
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		// No pre-pull: the opener potion goes in the first weave after the pull instead.
		if ((!IsInUltimate || Ultimate.OpenerPotion) && InPostPullPotionWindow() && UseBurstMedicine(out act))
		{
			return true;
		}

		if (StatusHelper.PlayerHasStatus(false, StatusID.Doom))
		{
			if (TheWardensPaeanPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (nextGCD.IsTheSameTo(true, StraightShotPvE, VenomousBitePvE, WindbitePvE, IronJawsPvE))
		{
			return base.EmergencyAbility(nextGCD, out act);
		}
		else if (!RagingStrikesPvE.EnoughLevel || HasRagingStrikes)
		{
			if (((EmpyrealArrowPvE.Cooldown.IsCoolingDown && !EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1)) || !EmpyrealArrowPvE.EnoughLevel) && Repertoire != 3)
			{
				if (!StatusHelper.PlayerHasStatus(true, StatusID.HawksEye_3861) && BarragePvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.TheWardensPaeanPvE)]
	protected override bool DispelAbility(IAction nextGCD, out IAction? action)
	{
		if (BRDEsuna && TheWardensPaeanPvE.CanUse(out action))
		{
			return true;
		}
		return base.DispelAbility(nextGCD, out action);
	}

	[RotationDesc(ActionID.NaturesMinnePvE)]
	protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (NaturesMinnePvE.CanUse(out act))
		{
			return true;
		}
		return base.HealSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.TroubadourPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if ((!BurstDefense || (BurstDefense && !InBurstStatus)) && TroubadourPvE.CanUse(out act))
		{
			return true;
		}
		return base.DefenseAreaAbility(nextGCD, out act);
	}

	private SongTrack GetNextSongTrack(SongTrack currentSong)
	{
		if (currentSong == FirstSong)
		{
			return SecondSong;
		}

		return currentSong == SecondSong ? ThirdSong : FirstSong;
	}

	private static Song SongTrackToSong(SongTrack track)
	{
		return track switch
		{
			SongTrack.WanderersMinuet => Song.WanderersMinuet,
			SongTrack.MagesBallad => Song.MagesBallad,
			SongTrack.ArmysPaeon => Song.ArmysPaeon,
			_ => Song.None,
		};
	}

	private bool TrySongTrackAction(SongTrack track, out IAction? act)
	{
		act = null;
		return track switch
		{
			SongTrack.WanderersMinuet => TheWanderersMinuetPvE.CanUse(out act),
			SongTrack.MagesBallad => MagesBalladPvE.CanUse(out act),
			SongTrack.ArmysPaeon => ArmysPaeonPvE.CanUse(out act),
			_ => false,
		};
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		if (!InCombat)
		{
			return base.GeneralAbility(nextGCD, out act);
		}

		if (Song != Song.None)
		{
			var currentSongTrack = Song switch
			{
				Song.WanderersMinuet => SongTrack.WanderersMinuet,
				Song.MagesBallad => SongTrack.MagesBallad,
				Song.ArmysPaeon => SongTrack.ArmysPaeon,
				_ => SongTrack.WanderersMinuet,
			};

			var nextSongTrack = GetNextSongTrack(currentSongTrack);
			var nextSong = SongTrackToSong(nextSongTrack);

			var remainTime = currentSongTrack switch
			{
				SongTrack.WanderersMinuet => WANDRemainTime,
				SongTrack.MagesBallad => MAGERemainTime,
				SongTrack.ArmysPaeon => ARMYRemainTime,
				_ => 0,
			};

			if (TrySongTrackAction(nextSongTrack, out act) && SongEndAfter(remainTime))
			{
				if (nextSong == Song.WanderersMinuet)
				{
					if (Song != Song.None || StatusHelper.PlayerHasStatus(true, StatusID.ArmysEthos))
					{
						if (!IsLastAbility(ActionID.ArmysPaeonPvE) && !IsLastAbility(ActionID.MagesBalladPvE))
						{
							return true;
						}
					}
				}
				else if (nextSong == Song.MagesBallad)
				{
					if (Song == Song.WanderersMinuet && (Repertoire == 0 || !HasHostilesInMaxRange))
					{
						if (!IsLastAbility(ActionID.ArmysPaeonPvE) && !IsLastAbility(ActionID.TheWanderersMinuetPvE))
						{
							return true;
						}
					}
					else if (Song == Song.ArmysPaeon && SongEndAfterGCD(2) && TheWanderersMinuetPvE.Cooldown.IsCoolingDown)
					{
						if (!IsLastAbility(ActionID.ArmysPaeonPvE) && !IsLastAbility(ActionID.TheWanderersMinuetPvE))
						{
							return true;
						}
					}
				}
				else if (nextSong == Song.ArmysPaeon)
				{
					if (TheWanderersMinuetPvE.EnoughLevel)
					{
						if (SongEndAfter(remainTime))
						{
							if (!IsLastAbility(ActionID.MagesBalladPvE) && !IsLastAbility(ActionID.TheWanderersMinuetPvE))
							{
								return true;
							}
						}

						if (SongEndAfter(2) && MagesBalladPvE.Cooldown.IsCoolingDown && Song == Song.WanderersMinuet)
						{
							if (!IsLastAbility(ActionID.MagesBalladPvE) && !IsLastAbility(ActionID.TheWanderersMinuetPvE))
							{
								return true;
							}
						}
					}
					else if (SongEndAfter(2))
					{
						if (!IsLastAbility(ActionID.MagesBalladPvE) && !IsLastAbility(ActionID.TheWanderersMinuetPvE))
						{
							return true;
						}
					}
				}
			}
		}

		if (Song == Song.None)
		{
			if (TrySongTrackAction(FirstSong, out act))
			{
				return true;
			}

			if (TrySongTrackAction(SecondSong, out act))
			{
				return true;
			}

			if (TrySongTrackAction(ThirdSong, out act))
			{
				return true;
			}
		}

		return base.GeneralAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		// Recovery after deaths, manual presses or downtime: a buff that is more than 10s away no longer holds
		// the others back.
		var ragingStrikesDrifted = RagingStrikesPvE.EnoughLevel && RagingStrikesPvE.Cooldown.IsCoolingDown && !RagingStrikesPvE.Cooldown.WillHaveOneCharge(10);
		var radiantFinaleDrifted = RadiantFinalePvE.EnoughLevel && RadiantFinalePvE.Cooldown.IsCoolingDown && !RadiantFinalePvE.Cooldown.WillHaveOneCharge(10) && !HasRadiantFinale;
		var battleVoiceDrifted = BattleVoicePvE.EnoughLevel && BattleVoicePvE.Cooldown.IsCoolingDown && !BattleVoicePvE.Cooldown.WillHaveOneCharge(10) && !HasBattleVoice;

		if (IsBurst && Song != Song.None && MagesBalladPvE.EnoughLevel)
		{
			if (((!RadiantFinalePvE.EnoughLevel && !RagingStrikesPvE.Cooldown.IsCoolingDown)
					|| (RadiantFinalePvE.EnoughLevel && !RadiantFinalePvE.Cooldown.IsCoolingDown && RagingStrikesPvE.EnoughLevel && (!RagingStrikesPvE.Cooldown.IsCoolingDown || RagingStrikesPvE.Cooldown.WillHaveOneCharge(BuffAlignment) || ragingStrikesDrifted))
					|| radiantFinaleDrifted)
					&& ((CurrentTarget?.HasStatus(true, StatusID.Windbite, StatusID.Stormbite) == true && CurrentTarget?.HasStatus(true, StatusID.VenomousBite, StatusID.CausticBite) == true) || DOTBarrage)
					&& !HoldTwoMinuteBurst && BattleVoicePvE.CanUse(out act))
			{
				return true;
			}

			if (StatusHelper.PlayerHasStatus(true, StatusID.BattleVoice) && RadiantFinalePvE.CanUse(out act))
			{
				return true;
			}

			if (((RadiantFinalePvE.EnoughLevel && HasRadiantFinale && HasBattleVoice)
				|| (!RadiantFinalePvE.EnoughLevel && BattleVoicePvE.EnoughLevel && HasBattleVoice)
				|| (!RadiantFinalePvE.EnoughLevel && !BattleVoicePvE.EnoughLevel)
				|| (radiantFinaleDrifted && HasBattleVoice)
				|| battleVoiceDrifted)
				&& RagingStrikesPvE.CanUse(out act))
			{
				return true;
			}
		}
		else if (!MagesBalladPvE.EnoughLevel)
		{
			if (!StraightShotPvE.EnoughLevel && RagingStrikesPvE.CanUse(out act))
			{
				return true;
			}

			if (nextGCD.IsTheSameTo(true, StraightShotPvE) && RagingStrikesPvE.CanUse(out act))
			{
				return true;
			}
		}

		// Spend capped repertoire before Empyreal Arrow, whose repertoire proc would otherwise overcap.
		if (PitchPerfectPvE.CanUse(out act, skipAoeCheck: true, skipComboCheck: true) && Repertoire == 3)
		{
			return true;
		}

		// Empyreal Arrow waits for Raging Strikes only while a burst is actually coming; while burst is held
		// (AutoBurst off, or the hold logic), it keeps its 15s cadence.
		if ((RagingStrikesPvE.Cooldown.IsCoolingDown || !RagingStrikesPvE.Cooldown.WillHaveOneCharge(15) || !IsBurst || HoldTwoMinuteBurst)
			&& Song != Song.None && EmpyrealArrowPvE.CanUse(out act))
		{
			return true;
		}

		if (PitchPerfectPvE.CanUse(out act, skipAoeCheck: true, skipComboCheck: true))
		{
			if (SongEndAfter(3) && Repertoire > 0)
			{
				return true;
			}

			if (Repertoire == 2 && EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD() && RadiantFinalePvE.Cooldown.IsCoolingDown)
			{
				return true;
			}
		}

		if (SidewinderPvE.EnoughLevel)
		{
			if ((BattleVoicePvE.Cooldown.IsCoolingDown && !BattleVoicePvE.Cooldown.WillHaveOneCharge(10))
			&& (!RadiantFinalePvE.EnoughLevel || (RadiantFinalePvE.EnoughLevel && RadiantFinalePvE.Cooldown.IsCoolingDown && !RadiantFinalePvE.Cooldown.WillHaveOneCharge(10)))
			&& RagingStrikesPvE.Cooldown.IsCoolingDown)
			{
				if (SidewinderPvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		// Bloodletter Overcap protection
		if (BloodletterPvE.Cooldown.WillHaveXCharges(BloodletterMax, 3f))
		{
			if (RainOfDeathPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}

			if (HeartbreakShotPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}

			if (BloodletterPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		// Prevents Bloodletter bumpcapping when MAGE is the song due to Repetoire procs
		if (BloodletterPvE.Cooldown.WillHaveXCharges(BloodletterMax, 7.5f) && Song == Song.MagesBallad)
		{
			if (RainOfDeathPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}

			if (HeartbreakShotPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}

			if (BloodletterPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		if (BetterBloodletterLogic(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}
	#endregion

	#region GCD Logic
	protected override bool GeneralGCD(out IAction? act)
	{
		if (IronJawsPvE.CanUse(out act))
		{
			return true;
		}

		if (IronJawsPvE.CanUse(out act, skipStatusProvideCheck: true) && (IronJawsPvE.Target.Target?.WillStatusEnd(30, true, IronJawsPvE.Setting.TargetStatusProvide ?? []) ?? false))
		{
			if (StatusHelper.PlayerHasStatus(true, StatusID.BattleVoice, StatusID.RadiantFinale, StatusID.RagingStrikes) && StatusHelper.PlayerWillStatusEndGCD(1, 1, true, StatusID.BattleVoice, StatusID.RadiantFinale, StatusID.RagingStrikes))
			{
				return true;
			}
		}

		if (ResonantArrowPvE.CanUse(out act))
		{
			return true;
		}

		if (CanUseApexArrow(out act))
		{
			return true;
		}

		if (RadiantEncorePvE.CanUse(out act, skipComboCheck: true))
		{
			if (InBurstStatus)
			{
				return true;
			}
		}

		if (BlastArrowPvE.CanUse(out act))
		{
			if (!HasRagingStrikes)
			{
				return true;
			}

			if (HasRagingStrikes && BarragePvE.Cooldown.IsCoolingDown)
			{
				return true;
			}
		}

		//aoe
		if (ShadowbitePvE.CanUse(out act))
		{
			return true;
		}

		if (WideVolleyPvE.CanUse(out act))
		{
			return true;
		}

		if (LadonsbitePvE.CanUse(out act) && !HasHawksEye && !HasBarrage)
		{
			return true;
		}

		if (QuickNockPvE.CanUse(out act) && !HasHawksEye && !HasBarrage)
		{
			return true;
		}

		if (StormbitePvE.EnoughLevel)
		{
			if (StormbitePvE.CanUse(out act))
			{
				if ((DOTBoss && StormbitePvE.Target.Target.IsBossFromIcon()) || !DOTBoss)
				{
					if (StormbitePvE.Target.Target.HasStatus(true, StatusID.Stormbite) == false)
					{
						return true;
					}
				}
			}
		}
		if (CausticBitePvE.EnoughLevel)
		{
			if (CausticBitePvE.CanUse(out act))
			{
				if ((DOTBoss && CausticBitePvE.Target.Target.IsBossFromIcon()) || !DOTBoss)
				{
					if (CausticBitePvE.Target.Target.HasStatus(true, StatusID.CausticBite) == false)
					{
						return true;
					}
				}
			}
		}

		if (!StormbitePvE.EnoughLevel)
		{
			if (WindbitePvE.CanUse(out act))
			{
				if ((DOTBoss && WindbitePvE.Target.Target.IsBossFromIcon()) || !DOTBoss)
				{
					if (IronJawsPvE.EnoughLevel && WindbitePvE.Target.Target.HasStatus(true, StatusID.Windbite) == false)
					{
						return true;
					}
					if (!IronJawsPvE.EnoughLevel)
					{
						return true;
					}
				}
			}
		}
		if (!CausticBitePvE.EnoughLevel)
		{
			if (VenomousBitePvE.CanUse(out act))
			{
				if ((DOTBoss && VenomousBitePvE.Target.Target.IsBossFromIcon()) || !DOTBoss)
				{
					if (IronJawsPvE.EnoughLevel && VenomousBitePvE.Target.Target.HasStatus(true, StatusID.VenomousBite) == false)
					{
						return true;
					}
					if (!IronJawsPvE.EnoughLevel)
					{
						return true;
					}
				}
			}
		}

		if (RefulgentArrowPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		if (!RefulgentArrowPvE.Info.EnoughLevelAndQuest() && StraightShotPvE.CanUse(out act))
		{
			return true;
		}

		if (BurstShotPvE.CanUse(out act) && !HasHawksEye && !HasBarrage)
		{
			return true;
		}

		if (HeavyShotPvE.CanUse(out act) && !HasHawksEye && !HasBarrage)
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}
	#endregion

	#region Extra Methods
	private bool CanUseApexArrow(out IAction act)
	{
		if (!ApexArrowPvE.CanUse(out act))
		{
			return false;
		}

		if (QuickNockPvE.CanUse(out _) && SoulVoice >= SoulVoiceConfig)
		{
			return true;
		}

		if (LadonsbitePvE.CanUse(out _) && SoulVoice >= SoulVoiceConfig)
		{
			return true;
		}

		if (CurrentTarget?.WillStatusEndGCD(1, 1, true, StatusID.Windbite, StatusID.Stormbite, StatusID.VenomousBite, StatusID.CausticBite) ?? false)
		{
			return false;
		}

		// While burst is held, don't sit on a full gauge waiting for buffs that aren't coming.
		if (SoulVoice >= 100 && (!IsBurst || HoldTwoMinuteBurst || !BattleVoicePvE.IsEnabled))
		{
			return true;
		}

		if (Song == Song.WanderersMinuet && SoulVoice >= 80 && !HasRagingStrikes)
		{
			return false;
		}

		if (SoulVoice >= SoulVoiceConfig && BattleVoicePvE.Cooldown.WillHaveOneCharge(25))
		{
			return false;
		}

		if (SoulVoice >= 80 && HasRagingStrikes && StatusHelper.PlayerWillStatusEnd(10, false, StatusID.RagingStrikes))
		{
			return true;
		}

		if (SoulVoice >= SoulVoiceConfig && HasRagingStrikes && HasBattleVoice)
		{
			return true;
		}

		// The Balance: Apex around halfway through Mage's Ballad, by about 21-19s on the song timer.
		if (Song == Song.MagesBallad && SoulVoice >= 80 && SongEndAfter(21))
		{
			return true;
		}

		if (!HasRagingStrikes && SoulVoice >= SoulVoiceConfig)
		{
			return true;
		}

		return false;
	}

	private bool BetterBloodletterLogic(out IAction? act)
	{

		if (HeartbreakShotPvE.CanUse(out act, usedUp: true))
		{
			if (InBurstStatus)
			{
				return true;
			}
		}

		if (RainOfDeathPvE.CanUse(out act, usedUp: true))
		{
			if (InBurstStatus)
			{
				return true;
			}
		}

		if (BloodletterPvE.CanUse(out act, usedUp: true))
		{
			if (InBurstStatus)
			{
				return true;
			}
		}
		return false;
	}
	#endregion
}
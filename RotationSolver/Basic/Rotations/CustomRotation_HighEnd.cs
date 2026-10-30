namespace RotationSolver.Basic.Rotations;

/// <summary>
/// The ultimate raids rotations can tailor their behaviour to.
/// </summary>
public enum UltimateRaid : byte
{
	/// <summary>Not in an ultimate.</summary>
	[Description("None")]
	None,

	/// <summary>The Unending Coil of Bahamut (Ultimate).</summary>
	[Description("The Unending Coil of Bahamut")]
	UCoB,

	/// <summary>The Weapon's Refrain (Ultimate).</summary>
	[Description("The Weapon's Refrain")]
	UwU,

	/// <summary>The Epic of Alexander (Ultimate).</summary>
	[Description("The Epic of Alexander")]
	TEA,

	/// <summary>Dragonsong's Reprise (Ultimate).</summary>
	[Description("Dragonsong's Reprise")]
	DSR,

	/// <summary>The Omega Protocol (Ultimate).</summary>
	[Description("The Omega Protocol")]
	TOP,

	/// <summary>Futures Rewritten (Ultimate).</summary>
	[Description("Futures Rewritten")]
	FRU,

	/// <summary>Dancing Mad (Ultimate).</summary>
	[Description("Dancing Mad")]
	DMU,
}

/// <summary>
/// Per-ultimate tuning a rotation can expose through its own configs.
/// </summary>
/// <param name="OpenerPotion">Use a potion in the pre-pull/opener.</param>
/// <param name="HoldBurstForDowntime">Don't start a 2-minute burst when BossMod predicts downtime inside the burst window.</param>
/// <param name="PartyAlignHold">Seconds to hold a ready 2-minute burst waiting for another party member's raid buff. 0 disables.</param>
public readonly record struct UltimateSettings(bool OpenerPotion, bool HoldBurstForDowntime, float PartyAlignHold)
{
	/// <summary>
	/// Behaviour outside ultimates: potion in opener, hold for downtime, no party hold.
	/// </summary>
	public static UltimateSettings Default { get; } = new(true, true, 0);
}

/// <summary>
/// Holds a ready burst for a short time until another party member's raid buff goes out,
/// so 2-minute windows stay stacked after deaths, phase transitions or drift.
/// </summary>
public sealed class BurstAligner
{
	private DateTime _readySince = DateTime.MinValue;

	/// <summary>
	/// Seconds into combat during which bursts are never held, so the opener always goes out together.
	/// </summary>
	public const float OpenerGraceTime = 20;

	/// <summary>
	/// Returns true when the burst should be held this frame.
	/// </summary>
	/// <param name="burstReady">Whether the burst cooldown is ready to press.</param>
	/// <param name="maxHoldSeconds">The longest the burst may be held. 0 or less disables holding.</param>
	public bool ShouldHold(bool burstReady, float maxHoldSeconds)
	{
		if (!burstReady || maxHoldSeconds <= 0 || !DataCenter.InCombat)
		{
			_readySince = DateTime.MinValue;
			return false;
		}

		if (_readySince == DateTime.MinValue)
		{
			_readySince = DateTime.Now;
		}

		if (DataCenter.CombatTimeRaw < OpenerGraceTime
			|| !CustomRotation.PartyHasOtherRaidBuffJobs
			|| CustomRotation.HasOtherPartyRaidBuff)
		{
			return false;
		}

		return (DateTime.Now - _readySince).TotalSeconds < maxHoldSeconds;
	}
}

public partial class CustomRotation
{
	/// <summary>
	/// The ultimate raid the player is currently in, or <see cref="UltimateRaid.None"/>.
	/// </summary>
	public static UltimateRaid CurrentUltimate
	{
		get
		{
			if (DataCenter.IsInUCoB)
			{
				return UltimateRaid.UCoB;
			}

			if (DataCenter.IsInUwU)
			{
				return UltimateRaid.UwU;
			}

			if (DataCenter.IsInTEA)
			{
				return UltimateRaid.TEA;
			}

			if (DataCenter.IsInDSR)
			{
				return UltimateRaid.DSR;
			}

			if (DataCenter.IsInTOP)
			{
				return UltimateRaid.TOP;
			}

			if (DataCenter.IsInFRU)
			{
				return UltimateRaid.FRU;
			}

			return DataCenter.IsInDMU ? UltimateRaid.DMU : UltimateRaid.None;
		}
	}

	/// <summary>
	/// The longest a burst release stays pending if the burst never goes out (no target, downtime, etc.).
	/// </summary>
	public const float BurstReleaseMaxSeconds = 60f;

	private static bool _lastBurst = true;
	private static DateTime _burstReleasedAt = DateTime.MinValue;

	/// <summary>
	/// Per-frame high-end state: tracks AutoBurst being turned off and back on during combat and caches the
	/// party raid buff lookups. Called once per frame before the rotation runs.
	/// </summary>
	internal static void UpdateHighEndState()
	{
		var burst = IsBurst;
		if (!DataCenter.InCombat)
		{
			_burstReleasedAt = DateTime.MinValue;
		}
		else if (burst && !_lastBurst)
		{
			_burstReleasedAt = DateTime.Now;
		}

		_lastBurst = burst;

		StatusList();
		_partyHasOtherRaidBuffJobs = ComputePartyHasOtherRaidBuffJobs();
		_hasOtherPartyRaidBuff = ComputeHasOtherPartyRaidBuff();
	}

	/// <summary>
	/// AutoBurst was turned back on during combat (for example by a hold/release macro) and the burst hasn't gone
	/// out yet. Rotations should burst right away and skip alignment and downtime holds, then call
	/// <see cref="ConsumeBurstRelease"/> once their burst has started. Expires after <see cref="BurstReleaseMaxSeconds"/>.
	/// </summary>
	public static bool BurstJustReleased => Service.Config.BurstOnRelease
		&& _burstReleasedAt != DateTime.MinValue
		&& SecondsSinceBurstRelease < BurstReleaseMaxSeconds;

	/// <summary>
	/// Seconds since AutoBurst was last turned back on in combat, or <see cref="float.MaxValue"/> if no release is pending.
	/// </summary>
	public static float SecondsSinceBurstRelease => _burstReleasedAt == DateTime.MinValue
		? float.MaxValue
		: (float)(DateTime.Now - _burstReleasedAt).TotalSeconds;

	/// <summary>
	/// Marks a pending burst release as handled once the burst has actually started.
	/// </summary>
	public static void ConsumeBurstRelease()
	{
		_burstReleasedAt = DateTime.MinValue;
	}

	/// <summary>
	/// Consumes a pending burst release if <paramref name="burstAction"/> was used after the release.
	/// Returns whether a release is still pending.
	/// </summary>
	protected static bool BurstReleasePending(IBaseAction burstAction)
	{
		if (!BurstJustReleased)
		{
			return false;
		}

		if (burstAction.Cooldown.IsCoolingDown && burstAction.Cooldown.RecastTimeElapsed <= SecondsSinceBurstRelease)
		{
			ConsumeBurstRelease();
			return false;
		}

		return true;
	}

	/// <summary>
	/// With no pre-pull actions, the opener potion moves to the first weave after the pull. True during the first
	/// <paramref name="window"/> seconds of combat when pre-pull actions are disabled.
	/// </summary>
	protected static bool InPostPullPotionWindow(float window = 10f)
		=> Service.Config.NoCountdownActions && DataCenter.InCombat && CombatTime < window;

	/// <summary>
	/// Whether the player pressed <paramref name="id"/> themselves (not BRR) within the last <paramref name="seconds"/>.
	/// </summary>
	protected static bool ManualActionWithin(ActionID id, float seconds)
		=> DataCenter.LastManualActionId == (uint)id
		&& (DateTime.Now - DataCenter.LastManualActionTime).TotalSeconds <= seconds;

	/// <summary>
	/// Whether the player is currently in an ultimate raid.
	/// </summary>
	public static bool IsInUltimate => CurrentUltimate != UltimateRaid.None;

	private static string? PlayerJobAbbr => Player?.ClassJob.Value.Abbreviation.ToString();

	private static bool _partyHasOtherRaidBuffJobs;
	private static bool _hasOtherPartyRaidBuff;

	/// <summary>
	/// Whether the party contains a raid-buff job other than the player's own. Updated once per frame.
	/// </summary>
	public static bool PartyHasOtherRaidBuffJobs => _partyHasOtherRaidBuffJobs;

	/// <summary>
	/// Whether a raid buff from another party member's job is active on the player (buffs) or on the hostile
	/// target (debuffs). Updated once per frame. Buffs from another player of the same job are not counted.
	/// </summary>
	public static bool HasOtherPartyRaidBuff => _hasOtherPartyRaidBuff;

	private static bool ComputePartyHasOtherRaidBuffJobs()
	{
		var myJob = PlayerJobAbbr;
		foreach (var buff in Buffs)
		{
			if (buff.JobAbbr != myJob)
			{
				return true;
			}
		}

		return false;
	}

	private static bool ComputeHasOtherPartyRaidBuff()
	{
		var myJob = PlayerJobAbbr;
		foreach (var buff in Buffs)
		{
			if (buff.JobAbbr == myJob)
			{
				continue;
			}

			if (buff.Type == StatusType.Buff && StatusHelper.PlayerHasStatus(false, buff.Ids))
			{
				return true;
			}

			if (buff.Type == StatusType.Debuff && HostileTarget?.HasStatus(false, buff.Ids) == true)
			{
				return true;
			}
		}

		return false;
	}
}

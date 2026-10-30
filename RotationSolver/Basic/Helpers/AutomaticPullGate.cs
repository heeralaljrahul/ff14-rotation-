namespace RotationSolver.Basic.Helpers;

/// <summary>
/// Blocks automatic actions while a countdown runs. When <c>requirePull</c> is set (high-end duties), also blocks
/// them out of combat until a pull starts or an observed countdown finishes.
/// </summary>
internal sealed class AutomaticPullGate
{
	private bool _wasInCombat;
	private bool _waitingForZero;
	private bool _released;
	private double _endsAt;
	private uint _territory;

	/// <summary>Whether the last observed countdown ran to zero rather than being cancelled.</summary>
	internal bool Released => _released;

	internal bool CanAct(float countdown, bool inCombat, bool dead, uint territory, double now, bool requirePull = true)
	{
		if (territory != _territory || dead)
		{
			_waitingForZero = _released = false;
		}
		else if (_wasInCombat && !inCombat)
		{
			_released = false;
		}
		_territory = territory;
		_wasInCombat = inCombat;

		if (dead)
		{
			return false;
		}

		// A running countdown wins even over a stale combat flag after a wipe.
		if (countdown > 0)
		{
			_endsAt = now + countdown;
			_waitingForZero = true;
			_released = false;
			return false;
		}

		if (_waitingForZero)
		{
			if (now >= _endsAt)
			{
				_waitingForZero = false;
				_released = true;
			}
			else if (_endsAt - now > 0.2)
			{
				// The timer disappeared early: cancellation must not start a pull.
				_waitingForZero = false;
			}
			else
			{
				// Allow for the timer disappearing on its final frame, without acting early.
				return false;
			}
		}

		// The release persists through non-damaging opening steps until combat starts. Outside high-end duties,
		// out-of-combat actions (pulling in dungeons, raises, stances) stay allowed once no countdown is pending.
		return inCombat || _released || !requirePull;
	}
}

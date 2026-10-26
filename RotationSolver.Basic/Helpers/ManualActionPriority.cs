namespace RotationSolver.Basic.Helpers;

// Scalar values only: never retain the game's out-pointer or an ActionManager pointer between frames.
internal readonly record struct ManualActionRequest(uint ActionType, uint ActionId, uint AdjustedId,
	ulong TargetId, uint ExtraParam, uint Mode, uint ComboRouteId, ulong SelectedTargetId)
{
	internal bool Matches(uint actionId) => actionId == ActionId || actionId == AdjustedId;
}

/// <summary>Gives the latest manual press ownership of the current action slot, then returns it to automation.</summary>
internal sealed class ManualActionPriority
{
	internal ManualActionRequest? Pending { get; private set; }
	internal bool IsQueued { get; private set; }
	private double _pendingDeadline;
	private ManualActionRequest? _issued;
	private ushort _issuedSequence;
	private double _confirmationDeadline;
	private bool _observedCast;

	internal bool BlocksAutomation(double now) =>
		(Pending.HasValue && now < _pendingDeadline) || (_issued.HasValue && now < _confirmationDeadline);

	internal static bool CanWaitForTiming(bool sameAction, uint nativeStatus, double recast, double currentWindow) =>
		sameAction && nativeStatus == 0 && recast <= Math.Max(0.5, currentWindow) + 0.05;

	internal bool Begin(ManualActionRequest request, double now, double currentWindow)
	{
		ClearPending(); // A newer press always replaces an older unsent request, including a different target.
		if (_issued == request && now < _confirmationDeadline)
		{
			// Spamming an action already being sent/cast must not manufacture a second deferred use.
			return false;
		}

		Pending = request;
		_pendingDeadline = now + Math.Clamp(currentWindow + 0.75, 0.75, 10);
		return true;
	}

	internal void Queued(ManualActionRequest request, double now, double currentWindow)
	{
		Pending = request;
		IsQueued = true;
		_pendingDeadline = now + Math.Clamp(currentWindow + 0.75, 0.75, 10);
	}

	internal void Issued(ManualActionRequest request, ushort sequence, double castTime, double now)
	{
		if (Pending == request)
		{
			ClearPending();
		}
		_issued = request;
		_issuedSequence = sequence;
		_confirmationDeadline = now + Math.Clamp(castTime + 1, 1, 30);
		_observedCast = false;
	}

	internal void Update(double now, bool pendingStillQueued, uint castingActionId, ushort handledSequence)
	{
		if (now >= _pendingDeadline || (IsQueued && !pendingStillQueued))
		{
			ClearPending();
		}

		if (_issued is not { } issued)
		{
			return;
		}
		var castingThisAction = castingActionId != 0 && issued.Matches(castingActionId);
		if (now >= _confirmationDeadline || (handledSequence == _issuedSequence && !castingThisAction))
		{
			ClearIssued();
			return;
		}

		if (castingThisAction)
		{
			_observedCast = true;
		}
		else if (_observedCast)
		{
			// A canceled/finished manual cast must not keep the solver paused for its old full duration.
			_observedCast = false;
			_confirmationDeadline = Math.Min(_confirmationDeadline, now + 0.25);
		}
	}

	internal bool OwnsCast(uint actionId, double now) =>
		actionId != 0 && _issued is { } issued && issued.Matches(actionId) && now < _confirmationDeadline;

	internal void ClearPending()
	{
		Pending = null;
		IsQueued = false;
		_pendingDeadline = 0;
	}

	private void ClearIssued()
	{
		_issued = null;
		_observedCast = false;
		_confirmationDeadline = 0;
	}

	internal void Reset()
	{
		ClearPending();
		ClearIssued();
	}
}

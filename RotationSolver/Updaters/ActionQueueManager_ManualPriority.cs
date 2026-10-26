using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Commands;

namespace RotationSolver.Updaters;

public static partial class ActionQueueManager
{
	private static readonly ManualActionPriority _manualPriority = new();
	private static ManualActionRequest? _forwardingManual;
	private static ManualActionRequest? _groundTargetRequest;
	private static QueuedActionSnapshot? _automaticQueue;
	private static QueuedActionSnapshot? _manualQueue;
	private static uint _manualJob;
	private static uint _manualTerritory;
	private static double Now => Environment.TickCount64 / 1000d;
	private static ulong SelectedTargetId => Svc.Targets.SoftTarget?.GameObjectId ?? Svc.Targets.Target?.GameObjectId ?? 0;
	private static bool ManualPriorityEnabled => Service.Config.ManualInputPriority && Player.Available
		&& DataCenter.State && !DataCenter.IsTargetOnly && !DataCenter.IsPvP;

	private readonly record struct QueuedActionSnapshot(ActionType Type, uint Id, ulong Target, uint Param, uint ComboRoute);

	private static unsafe QueuedActionSnapshot? ReadQueue(ActionManager* manager) =>
		manager != null && manager->ActionQueued
			? new(manager->QueuedActionType, manager->QueuedActionId, (ulong)manager->QueuedTargetId,
				manager->QueuedExtraParam, manager->QueuedComboRouteId)
			: null;

	internal static void ResetManualPriority()
	{
		_manualPriority.Reset();
		_manualQueue = _automaticQueue = null;
		_groundTargetRequest = null;
	}

	internal static bool OwnsManualCast(uint actionId) =>
		ManualPriorityEnabled && _manualPriority.OwnsCast(actionId, Now);

	internal static unsafe bool BlocksAutomaticActions
	{
		get
		{
			var manager = ActionManager.Instance();
			return (ManualPriorityEnabled && _manualPriority.BlocksAutomation(Now))
				|| (manager != null && (manager->ActionQueued || manager->AreaTargetingActionId != 0));
		}
	}

	private static unsafe bool ForwardAutomaticAction(ActionManager* manager, uint type, uint id, ulong target,
		uint param, uint mode, uint comboRoute, bool* groundTarget)
	{
		if (BlocksAutomaticActions)
		{
			return false;
		}
		var result = _useActionHook!.Original(manager, type, id, target, param, mode, comboRoute, groundTarget);
		_automaticQueue = ReadQueue(manager);
		return result;
	}

	private static unsafe bool HandleManualPress(ActionManager* manager, uint type, uint id, ulong target,
		uint param, uint mode, uint comboRoute, bool* groundTarget)
	{
		var forwarded = false;
		var result = false;
		try
		{
			if (_manualJob != (uint)DataCenter.Job || _manualTerritory != Svc.ClientState.TerritoryType)
			{
				ResetManualPriority();
			}
			_manualJob = (uint)DataCenter.Job;
			_manualTerritory = Svc.ClientState.TerritoryType;
			var request = new ManualActionRequest(type, id, Service.GetAdjustedActionId(id), target,
				param, mode, comboRoute, SelectedTargetId);

			// Remove only a known solver queue or an older manual request replaced by this press.
			// A queued automatic Malefic must not fire while the player's Helios is waiting for the GCD.
			var queue = ReadQueue(manager);
			if (queue.HasValue && (queue == _automaticQueue
				|| (queue == _manualQueue && _manualPriority.Pending != request)))
			{
				manager->ActionQueued = false;
				_manualQueue = _automaticQueue = null;
			}

			_manualPriority.Begin(request, Now, CurrentWindow(manager));
			var beforeQueue = ReadQueue(manager);
			forwarded = true;
			result = ForwardManual(manager, request, groundTarget); // Always try the original press immediately.
			ObserveManualResult(manager, request, beforeQueue, result);
			return result;
		}
		catch (Exception ex)
		{
			ResetManualPriority();
			PluginLog.Warning($"[ManualInput] Failed to track player input: {ex.Message}");
			// Never send an already forwarded press twice if bookkeeping failed after the native call.
			return forwarded ? result : _useActionHook!.Original(manager, type, id, target, param, mode, comboRoute, groundTarget);
		}
	}

	private static unsafe bool ForwardManual(ActionManager* manager, ManualActionRequest request, bool* groundTarget)
	{
		var previous = _forwardingManual;
		_forwardingManual = request;
		try
		{
			var sequence = manager->LastUsedActionSequence;
			var result = _useActionHook!.Original(manager, request.ActionType, request.ActionId, request.TargetId,
				request.ExtraParam, request.Mode, request.ComboRouteId, groundTarget);
			if (manager->LastUsedActionSequence != sequence)
			{
				// Sequence change is authoritative even if the location-observer hook was unavailable.
				RecordManualIssued(request, manager->LastUsedActionSequence);
			}
			return result;
		}
		finally
		{
			_forwardingManual = previous;
		}
	}

	private static unsafe void ObserveManualResult(ActionManager* manager, ManualActionRequest request,
		QueuedActionSnapshot? beforeQueue, bool nativeResult)
	{
		var queue = ReadQueue(manager);
		if (queue is { } q && q.Type == (ActionType)request.ActionType && request.Matches(q.Id)
			&& (nativeResult || queue != beforeQueue || queue == _manualQueue))
		{
			_manualQueue = queue;
			_manualPriority.Queued(request, Now, CurrentWindow(manager));
			return;
		}
		if (manager->AreaTargetingActionId != 0 && request.Matches(manager->AreaTargetingActionId))
		{
			_groundTargetRequest = request;
			_manualPriority.ClearPending(); // The game owns the reticle; do not restart it with retries.
			return;
		}
		if (_manualPriority.Pending == request && (!CanRetryManual(manager, request)
			|| (CurrentWindow(manager) <= 0 && RecastRemaining(manager, request) <= 0)))
		{
			_manualPriority.ClearPending();
		}
	}

	private static unsafe double CurrentWindow(ActionManager* manager) => Math.Max(manager->AnimationLock,
		Math.Max(manager->CastTimeTotal - manager->CastTimeElapsed, DataCenter.DefaultGCDRemain));

	private static unsafe double RecastRemaining(ActionManager* manager, ManualActionRequest request)
	{
		var type = (ActionType)request.ActionType;
		var group = manager->GetRecastGroup((int)type, request.AdjustedId);
		var detail = group < 0 ? null : manager->GetRecastGroupDetail(group);
		var remaining = detail == null || !detail->IsActive || manager->GetCurrentCharges(request.AdjustedId) > 0
			? 0 : Math.Max(0, ActionManager.GetAdjustedRecastTime(type, request.AdjustedId) / 1000d - detail->Elapsed);
		var additional = manager->GetAdditionalRecastGroup(type, request.AdjustedId);
		var extra = additional < 0 || additional == group ? null : manager->GetRecastGroupDetail(additional);
		return extra == null || !extra->IsActive ? remaining : Math.Max(remaining, extra->Total - extra->Elapsed);
	}

	private static unsafe bool CanRetryManual(ActionManager* manager, ManualActionRequest request)
	{
		// Do not run rotation CanUse/HP/AoE/movement heuristics on the player's own choice.
		// Ignore only temporary timing restrictions; native resource/proc/level/target checks still apply.
		return ManualActionPriority.CanWaitForTiming(Service.GetAdjustedActionId(request.ActionId) == request.AdjustedId,
			manager->GetActionStatus((ActionType)request.ActionType, request.AdjustedId, request.TargetId,
				checkRecastActive: false, checkCastingActive: false), RecastRemaining(manager, request), CurrentWindow(manager));
	}

	private static void OnNativeActionIssued(ActionType type, uint id, ulong target, ushort sequence)
	{
		if (_automaticQueue is { } automatic && automatic.Type == type && automatic.Id == id)
		{
			_automaticQueue = null;
		}
		if (RSCommands.IsExecutingAction || !ManualPriorityEnabled || type != ActionType.Action)
		{
			return;
		}
		ManualActionRequest? request = null;
		if (_forwardingManual is { } forwarded && forwarded.Matches(id))
		{
			request = forwarded;
		}
		else if (_manualPriority.IsQueued && _manualPriority.Pending is { } pending && pending.Matches(id)
			&& _manualQueue is { } queue && queue.Target == target)
		{
			request = pending;
		}
		else if (_groundTargetRequest is { } ground && ground.Matches(id))
		{
			request = ground;
		}
		if (request is not { } manual)
		{
			return;
		}
		RecordManualIssued(manual, sequence);
	}

	private static unsafe void RecordManualIssued(ManualActionRequest request, ushort sequence)
	{
		_manualPriority.Issued(request, sequence,
			ActionManager.GetAdjustedCastTime((ActionType)request.ActionType, request.AdjustedId) / 1000d, Now);
		_manualQueue = null;
		_groundTargetRequest = null;
	}

	internal static unsafe bool UpdateManualPriority()
	{
		try
		{
			var manager = ActionManager.Instance();
			if (!ManualPriorityEnabled || manager == null || Player.Object?.CurrentHp == 0
				|| _manualJob != (uint)DataCenter.Job || _manualTerritory != Svc.ClientState.TerritoryType
				|| Service.BlockAutomaticActions)
			{
				ResetManualPriority();
				_manualJob = (uint)DataCenter.Job;
				_manualTerritory = Svc.ClientState.TerritoryType;
				return false;
			}

			_manualPriority.Update(Now, _manualQueue.HasValue && ReadQueue(manager) == _manualQueue,
				manager->CastTimeTotal > manager->CastTimeElapsed ? manager->CastActionId : 0,
				manager->LastHandledActionSequence);
			if (manager->AreaTargetingActionId == 0)
			{
				_groundTargetRequest = null;
			}
			if (_manualPriority.Pending is { } request && !_manualPriority.IsQueued)
			{
				var targetChanged = (request.TargetId == 0 || request.TargetId == InvalidGameObjectId)
					&& request.SelectedTargetId != SelectedTargetId;
				if (targetChanged || !CanRetryManual(manager, request))
				{
					_manualPriority.ClearPending();
				}
				else
				{
					var nativeWait = Math.Max(manager->AnimationLock,
						Math.Max(manager->CastTimeTotal - manager->CastTimeElapsed, RecastRemaining(manager, request)));
					var queueWindow = request.Mode == (uint)ActionManager.UseActionMode.Macro ? 0 : 0.5;
					if (nativeWait <= queueWindow)
					{
						var beforeQueue = ReadQueue(manager);
						bool ground = false;
						var result = ForwardManual(manager, request, &ground);
						ObserveManualResult(manager, request, beforeQueue, result);
					}
				}
			}
			return BlocksAutomaticActions;
		}
		catch (Exception ex)
		{
			ResetManualPriority();
			PluginLog.Warning($"[ManualInput] Failed to process manual priority: {ex.Message}");
			return true;
		}
	}
}

using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.GameHelpers;
using RebornMaterial;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;

namespace RotationSolver.UI.ExtraWindows;

/// <summary>
/// Per-ultimate rotation settings, one tab per ultimate, kept out of the main rotation options.
/// </summary>
internal class UltimateSettingsWindow : Window
{
	private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings;

	private static readonly Vector2 PreferredSize = new(640f, 520f);

	private static readonly UltimateRaid[] Ultimates =
	[
		UltimateRaid.UCoB,
		UltimateRaid.UwU,
		UltimateRaid.TEA,
		UltimateRaid.DSR,
		UltimateRaid.TOP,
		UltimateRaid.FRU,
		UltimateRaid.DMU,
	];

	private readonly List<UltimateRaid> _tabUltimates = [];
	private M3Tab[] _tabs = [];
	private Type? _tabsFor;
	private int _tab;

	private M3Style.Scope _theme;

	public UltimateSettingsWindow()
		: base("Ultimate Settings###brrUltimateSettingsWindow", BaseFlags)
	{
		SizeConstraints = new WindowSizeConstraints()
		{
			MinimumSize = new Vector2(480f, 360f),
			MaximumSize = new Vector2(1400f, 1400f),
		};
		RespectCloseHotkey = true;
	}

	public override bool DrawConditions()
	{
		return DataCenter.PlayerAvailable();
	}

	public override void OnOpen()
	{
		// Rebuild tabs and jump to the ultimate the player is in, if any.
		_tabsFor = null;
		base.OnOpen();
	}

	public override void PreDraw()
	{
		_theme = M3Style.Push();

		var viewport = ImGui.GetMainViewport();
		ImGui.SetNextWindowSize(Vector2.Min(PreferredSize * M3.Scale, viewport.Size * 0.9f), ImGuiCond.FirstUseEver);
		ImGui.SetNextWindowPos(viewport.Pos + (viewport.Size * 0.5f), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
		base.PreDraw();
	}

	public override void PostDraw()
	{
		base.PostDraw();
		_theme.Dispose();
		_theme = default;
	}

	public override void Draw()
	{
		var rotation = DataCenter.CurrentRotation;
		if (rotation == null || !Player.Available)
		{
			ImGui.TextWrapped("Switch to a combat job to see its ultimate settings.");
			return;
		}

		var set = rotation.Configs;
		var rotationType = rotation.GetType();

		IRotationConfig? masterToggle = null;
		foreach (var config in set.Configs)
		{
			var attr = MainWindow.GetRotationConfigAttribute(rotationType, config.Name);
			if (attr != null && attr.UltimateSection && attr.Ultimate == UltimateRaid.None)
			{
				masterToggle = config;
				break;
			}
		}

		RebuildTabs(rotationType, set);
		if (_tabUltimates.Count == 0)
		{
			_ = M3Widgets.Banner("##ultimate_none",
				$"{rotation.Name} has no ultimate-specific settings. They are available for Machinist, Bard and Dancer.",
				M3Severity.Info, FontAwesomeIcon.InfoCircle);
			return;
		}

		if (BunnyTheme.Enabled)
		{
			DrawArtHeader(rotation.Name);
		}

		using (ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(M3.Scheme.OnSurfaceVariant, 0.95f)))
		{
			ImGui.TextWrapped($"{rotation.Name}: settings used only inside ultimates. Outside ultimates the rotation's basic options apply.");
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));

		var current = CustomRotation.CurrentUltimate;
		if (current != UltimateRaid.None)
		{
			_ = M3Widgets.Banner("##ultimate_current",
				$"You are in {current.GetDescription()}. Its tab is active now.",
				M3Severity.Success, FontAwesomeIcon.Dragon);
			ImGui.Dummy(new Vector2(0f, M3.Space2));
		}

		if (masterToggle != null)
		{
			DrawRow(rotation, masterToggle, null);
			if (!bool.TryParse(masterToggle.Value, out var enabled) || !enabled)
			{
				ImGui.Dummy(new Vector2(0f, M3.Space2));
				_ = M3Widgets.Banner("##ultimate_off",
					"Per-ultimate settings are off, so the rotation's basic options are used in every ultimate.",
					M3Severity.Warning, FontAwesomeIcon.ToggleOff);
				return;
			}

			M3Widgets.Divider(6f);
		}

		var picked = M3Navigation.Tabs("##ultimate_tabs", _tabs, _tab, secondary: true);
		if (picked >= 0)
		{
			_tab = picked;
		}

		_tab = Math.Clamp(_tab, 0, _tabUltimates.Count - 1);
		var ultimate = _tabUltimates[_tab];

		ImGui.Dummy(new Vector2(0f, M3.Space2));
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			ImGui.TextUnformatted(ultimate.GetDescription());
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));

		var prefix = ultimate + ": ";
		foreach (var config in set.Configs)
		{
			var attr = MainWindow.GetRotationConfigAttribute(rotationType, config.Name);
			if (attr?.Ultimate != ultimate || !config.Type.HasFlag(CombatType.PvE))
			{
				continue;
			}

			var label = config.DisplayName.StartsWith(prefix, StringComparison.Ordinal)
				? config.DisplayName[prefix.Length..]
				: config.DisplayName;
			DrawRow(rotation, config, label);
		}

		ImGui.Dummy(new Vector2(0f, M3.Space3));
		using (ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(M3.Scheme.OnSurfaceVariant, 0.8f)))
		{
			ImGui.TextWrapped("The downtime hold uses BossMod Reborn's timeline and does nothing without it. The party hold never applies in the first 20 seconds of a pull.");
		}
	}

	private static void DrawArtHeader(string rotationName)
	{
		var width = ImGui.GetContentRegionAvail().X;
		var height = 118f * M3.Scale;
		ImGui.Dummy(new Vector2(width, height));
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();

		BunnyTheme.ArtHeader(drawList, min, max, UpdateNotesWindow.GetBanner(), 0.25f, 1f, M3.ShapeMedium);
		BunnyTheme.Twinkles(drawList, min, max);

		var pad = 20f * M3.Scale;
		var display = BunnyTheme.PushDisplay(true);
		float titleHeight;
		using (display)
		using (ImRaii.PushFont(M3.HeadlineSmall, display == null))
		{
			const string title = "Ultimate settings";
			titleHeight = ImGui.CalcTextSize(title).Y;
			var pos = new Vector2(min.X + pad, max.Y - titleHeight - (32f * M3.Scale));
			drawList.AddText(pos + new Vector2(1f), M3.U32(BunnyTheme.Gold, 0.3f), title);
			drawList.AddText(pos, M3.U32(BunnyTheme.Gold, 1f), title);
		}

		using (ImRaii.PushFont(M3.LabelSmall))
		{
			drawList.AddText(new Vector2(min.X + pad, max.Y - (26f * M3.Scale)), M3.U32(BunnyTheme.TextMuted, 1f), rotationName);
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));
	}

	private static void DrawRow(ICustomRotation rotation, IRotationConfig config, string? label)
	{
		var tooltip = MainWindow.GetRotationConfigTooltip(rotation, config);
		var supporting = Service.Config.UiInlineDescriptions ? tooltip : null;
		MainWindow.DrawRotationConfigRow(config, rotation, OtherCommandType.Rotations, supporting, tooltip, label);
	}

	private void RebuildTabs(Type rotationType, IRotationConfigSet set)
	{
		if (_tabsFor == rotationType)
		{
			return;
		}

		_tabsFor = rotationType;
		_tabUltimates.Clear();
		foreach (var ultimate in Ultimates)
		{
			foreach (var config in set.Configs)
			{
				if (MainWindow.GetRotationConfigAttribute(rotationType, config.Name)?.Ultimate == ultimate)
				{
					_tabUltimates.Add(ultimate);
					break;
				}
			}
		}

		var tabs = new M3Tab[_tabUltimates.Count];
		for (var i = 0; i < tabs.Length; i++)
		{
			tabs[i] = new(_tabUltimates[i].ToString(), FontAwesomeIcon.Dragon);
		}

		_tabs = tabs;
		var currentIndex = _tabUltimates.IndexOf(CustomRotation.CurrentUltimate);
		_tab = currentIndex >= 0 ? currentIndex : 0;
	}
}

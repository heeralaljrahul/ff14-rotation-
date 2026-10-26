using Dalamud.Common;
using Dalamud.Common.Game;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.Reflection;
using RebornMaterial;
using RotationSolver.Basic.Configuration;
using RotationSolver.Commands;
using RotationSolver.Data;
using RotationSolver.UI.ExtraWindows;

namespace RotationSolver.UI;

public partial class MainWindow : Window
{
	private static float Scale => M3.Scale;

	private MainWindowTab _activeTab;

	private List<IncompatiblePlugin> _crashPlugins = [];
	private List<IncompatiblePlugin> _enabledIncompatiblePlugins = [];
	private static DiagInfo? _cachedDiagInfo;
	private bool _showResetPopup = false;
	private M3Style.Scope _theme;

	// Keep this in this file: the page files read it while setting up, and this file is set up first.
	internal static SearchableCollection _allSearchable = new();

	private static bool? _cnLanguageClient;

	public static bool CNLanguageClient => _cnLanguageClient ??= GetClientLanguage() is "Chinese" or "ChineseSimplified";

	private static string GetClientLanguage()
	{
		return DalamudReflector.TryGetDalamudStartInfo(out var startInfo, Svc.PluginInterface)
			? startInfo.Language.ToString()
			: Svc.ClientState.ClientLanguage.ToString();
	}

	internal sealed class DiagInfo(DalamudStartInfo startInfo)
	{
		public string RSRVersion { get; } = typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "?.?.?";
		public GameVersion? GameVersion { get; } = startInfo.GameVersion;
		public string Platform { get; } = startInfo.Platform.ToString();
		public ClientLanguage Language { get; } = startInfo.Language;
	}

	public MainWindow()
	: base("###rsrConfigWindow", BaseFlags, false)
	{
		SizeCondition = ImGuiCond.FirstUseEver;
		Size = DefaultSize;
		SizeConstraints = DefaultSizeConstraints;
		RespectCloseHotkey = true;

		AllowPinning = false;
		AllowClickthrough = false;
	}

	public override void OnOpen()
	{
		// Clear any pin or click-through left over from when the window had a title bar.
		IsPinned = false;
		IsClickthrough = false;

		_enabledIncompatiblePlugins = [];
		_crashPlugins = [];

		foreach (var p in PluginCompatibility.IncompatiblePlugins)
		{
			if (p.IsInstalled && p.IsEnabled)
			{
				_enabledIncompatiblePlugins.Add(p);
			}
		}

		if (DalamudReflector.TryGetDalamudStartInfo(out var startinfo, Svc.PluginInterface))
		{
			_cachedDiagInfo = new DiagInfo(startinfo);
		}
		else
		{
			PluginLog.Error("Failed to get Dalamud start info.");
		}

		base.OnOpen();
	}

	public override void OnClose()
	{
		Service.Config.Save();
		_cachedDiagInfo = null;

		M3Motion.Reset();
		M3CardHost.Reset();
		M3Snackbar.Clear();
		_fold.Reset();

		base.OnClose();
	}

	internal void SetActiveTab(MainWindowTab tab)
	{
		_activeTab = tab;
		_searchResults = [];
	}

	// Push the theme before Begin, since Begin draws the window's background, padding and corners.
	public override void PreDraw()
	{
		_theme = M3Style.Push(M3Density.Tight);
		PrepareFold();
		base.PreDraw();
	}

	public override void PostDraw()
	{
		base.PostDraw();

		_fold.PopStyle();
		_theme.Dispose();
		_theme = default;
	}

	public override void Draw()
	{
		_fold.BeginDraw();
		_windowPos = ImGui.GetWindowPos();
		_windowSize = ImGui.GetWindowSize();

		var folded = _fold.Amount;
		if (folded < 1f)
		{
			using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * (1f - MathF.Min(1f, folded * 1.4f)));
			DrawWindowBackdrop(_fold.Rounding);

			var (openPos, openSize) = _fold.OpenRect();
			DrawWindowContent(openPos, openSize);
		}

		DrawResetDialog();
		DrawWindowBar();
	}

	private void DrawWindowContent(Vector2 openPos, Vector2 openSize)
	{
		var padding = _fold.OpenPadding;
		ImGui.SetCursorScreenPos(openPos + padding);
		using var content = ImRaii.Child("##rsr_window_content", Vector2.Max(Vector2.One, openSize - (padding * 2f)), false,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);
		if (!content)
		{
			return;
		}

		try
		{
			// Not a table: ImGui saves table column widths and they can't be clamped later. Capped at half the window.
			var available = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
			var spacing = ImGui.GetStyle().ItemSpacing.X;
			var sidebarWidth = MathF.Min(NavigationColumnMinWidth(), available * 0.5f);

			var origin = ImGui.GetCursorScreenPos();
			var paneHeight = MathF.Max(1f, ImGui.GetContentRegionAvail().Y);
			var dividerX = origin.X + sidebarWidth + (spacing * 0.5f);
			ImGui.GetWindowDrawList().AddLine(
				new Vector2(dividerX, origin.Y),
				new Vector2(dividerX, origin.Y + paneHeight),
				M3.U32(M3.Scheme.OutlineVariant, 0.45f), 1f * Scale);

			try
			{
				DrawNavigationColumn(sidebarWidth);
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Something wrong with sideBar: {ex.Message}");
			}

			ImGui.SameLine(0f, spacing);

			try
			{
				DrawBody();
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Something wrong with body: {ex.Message}");
			}

			M3Snackbar.Draw(new Vector2(origin.X + sidebarWidth + spacing, origin.Y),
				new Vector2(origin.X + available, origin.Y + paneHeight));
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Something wrong with config window: {ex.Message}");
		}
	}

	private static void DrawWindowBackdrop(float rounding)
	{
		var scheme = M3.Scheme;
		var drawList = ImGui.GetWindowDrawList();
		var windowPos = ImGui.GetWindowPos();
		var windowSize = ImGui.GetWindowSize();
		var top = M3.Alpha(scheme.SurfaceContainer, 0.65f);

		var min = windowPos;
		var max = new Vector2(windowPos.X + windowSize.X, windowPos.Y + MathF.Min(200f * Scale, windowSize.Y));
		rounding = MathF.Min(rounding, (max.Y - min.Y) * 0.5f);

		// Multi-color rects can't be rounded, so a solid strip draws the rounded top corners.
		drawList.PushClipRect(windowPos, windowPos + windowSize, false);
		if (rounding > 0f)
		{
			drawList.AddRectFilled(min, new Vector2(max.X, min.Y + rounding), M3.U32(top), rounding, ImDrawFlags.RoundCornersTop);
		}

		M3Draw.VerticalGradient(drawList, new Vector2(min.X, min.Y + rounding), max, top, M3.Alpha(scheme.Surface, 0f));

		if (BunnyTheme.Enabled)
		{
			// Thin gold edge with a soft glow, like the halo in the artwork.
			var inset = new Vector2(1f * Scale);
			drawList.AddRect(windowPos + inset, windowPos + windowSize - inset, M3.U32(BunnyTheme.Gold, 0.30f), rounding, ImDrawFlags.None, 1.5f * Scale);
			drawList.AddRect(windowPos + (inset * 3f), windowPos + windowSize - (inset * 3f), M3.U32(BunnyTheme.Gold, 0.07f), rounding, ImDrawFlags.None, 3f * Scale);
		}

		drawList.PopClipRect();
	}

	private void DrawResetDialog()
	{
		const string title = "Reset BRR Plugin Settings";

		if (_showResetPopup)
		{
			ImGui.OpenPopup(title);
			_showResetPopup = false;
		}

		ImGui.SetNextWindowSizeConstraints(new Vector2(380, 0) * Scale, new Vector2(520, 400) * Scale);

		using var popup = ImRaii.PopupModal(title, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
		if (!popup)
		{
			return;
		}

		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			ImGui.TextUnformatted(CNLanguageClient ? "重置所有插件设置？" : "Reset all plugin settings?");
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));

		using (ImRaii.PushColor(ImGuiCol.Text, M3.Scheme.OnSurfaceVariant))
		{
			ImGui.TextWrapped(CNLanguageClient
				? "如果你在使用旧版默认配置的 BRR 时遇到问题，通常推荐执行此操作。此操作无法撤销。"
				: "This is often recommended for users having issues while using an installation of BRR with an outdated default configuration. This cannot be undone.");
		}

		ImGui.Dummy(new Vector2(0f, M3.Space3));

		var cancelLabel = CNLanguageClient ? "取消" : "Cancel";
		var resetLabel = CNLanguageClient ? "重置" : "Reset everything";
		var resetWidth = M3Widgets.ButtonWidth(FontAwesomeIcon.TrashAlt, resetLabel);
		var cancelWidth = M3Widgets.ButtonWidth(FontAwesomeIcon.None, cancelLabel);

		// Right-aligned against the dialog's own padding, whatever the theme sets it to.
		ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(),
			ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - resetWidth - M3.Space2 - cancelWidth));

		if (M3Widgets.Button("##reset_cancel", cancelLabel, M3ButtonStyle.Text))
		{
			ImGui.CloseCurrentPopup();
		}

		ImGui.SameLine(0f, M3.Space2);

		if (M3Widgets.Button("##reset_confirm", resetLabel, M3ButtonStyle.Danger, FontAwesomeIcon.TrashAlt))
		{
			Service.Config = new Configs();
			Service.Config.Save();
			ImGui.CloseCurrentPopup();
		}
	}

	private void DrawTopAppBar()
	{
		if (BunnyTheme.Enabled)
		{
			DrawBunnyHeader();
			return;
		}

		DrawMaterialAppBar();
	}

	private bool IsHomeTab => _activeTab is MainWindowTab.Main or MainWindowTab.About;

	/// <summary>
	/// The Bunny header: the artwork faded into plum with twinkling sparkles, the page title in the display font,
	/// and on Home the autorotation state switch.
	/// </summary>
	private void DrawBunnyHeader()
	{
		var scheme = M3.Scheme;
		var searching = _searchResults is { Length: > 0 };
		var home = IsHomeTab && !searching;
		var title = searching
			? UiString.ConfigWindow_Search_Result.GetDescription()
			: home ? "Ready to burst, bunny" : GetActiveTabTitle();
		var subtitle = searching
			? string.Format("{0} matching settings", _searchResults!.Length)
			: home ? HomeSubtitle() : _activeTab.GetDescription();
		var eyebrow = home ? "Welcome back" : "BunnyRotationReborn";

		var width = MathF.Max(64f * Scale, ImGui.GetContentRegionAvail().X);
		var brand = Brand;
		var shown = _windowActions.Length;
		while (shown > 0 && M3Widgets.WindowActionsSize(shown, brand, 0f).X + (160f * Scale) > width)
		{
			shown--;
		}

		var barSize = M3Widgets.WindowActionsSize(shown, brand, 0f);
		var height = (home ? 206f : 112f) * Scale;

		ImGui.Dummy(new Vector2(width, height));
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();

		BunnyTheme.ArtHeader(drawList, min, max, UpdateNotesWindow.GetBanner(), home ? 0.3f : 0.22f, home ? 1f : 0.6f, M3.ShapeMedium);
		BunnyTheme.Twinkles(drawList, min, max);

		var pad = 22f * Scale;
		var textWidth = MathF.Max(32f * Scale, width - barSize.X - (pad * 2f));
		var y = min.Y + (home ? 26f * Scale : 16f * Scale);

		BunnyTheme.Eyebrow(drawList, new Vector2(min.X + pad, y), eyebrow, scheme.Primary);
		var eyebrowSize = BunnyTheme.EyebrowSize(eyebrow);
		var pillSize = M3Widgets.PillSize(BunnyTheme.VersionLabel, FontAwesomeIcon.Tag);
		ImGui.SetCursorScreenPos(new Vector2(min.X + pad + eyebrowSize.X + M3.Space2, y + ((eyebrowSize.Y - pillSize.Y) * 0.5f)));
		BunnyTheme.VersionPill("##bunny_header_version");
		y += MathF.Max(eyebrowSize.Y, pillSize.Y * 0.5f + (eyebrowSize.Y * 0.5f)) + (4f * Scale);

		var display = BunnyTheme.PushDisplay(home);
		using (display)
		using (ImRaii.PushFont(M3.HeadlineSmall, display == null))
		{
			var clipped = M3Navigation.Truncate(title, textWidth);
			var titleSize = ImGui.CalcTextSize(clipped);
			drawList.AddText(new Vector2(min.X + pad + 1f, y + 1f), M3.U32(BunnyTheme.Gold, 0.25f), clipped);
			drawList.AddText(new Vector2(min.X + pad, y), M3.U32(scheme.OnSurface, 0.98f), clipped);
			y += titleSize.Y + (4f * Scale);
		}

		if (!string.IsNullOrEmpty(subtitle))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			drawList.AddText(new Vector2(min.X + pad, y), M3.U32(scheme.OnSurfaceVariant, 0.95f), M3Navigation.Truncate(subtitle, textWidth));
		}

		_shownActions = shown;
		_fold.BarTop = 10f * Scale;

		if (home)
		{
			DrawStateSwitch(new Vector2(min.X + pad, max.Y - (52f * Scale)));
		}

		if (searching)
		{
			ImGui.SetCursorScreenPos(new Vector2(max.X - M3Widgets.IconButtonSize - pad, max.Y - M3Widgets.IconButtonSize - (12f * Scale)));
			if (M3Widgets.IconButton("##appbar_clear_search", FontAwesomeIcon.Eraser, "Clear search results"))
			{
				_searchText = string.Empty;
				_searchResults = [];
			}
		}

		ImGui.SetCursorScreenPos(new Vector2(min.X, max.Y));
		ImGui.Dummy(new Vector2(width, M3.Space2));
	}

	private static string HomeSubtitle()
	{
		var territory = DataCenter.Territory?.ContentFinderName;
		var rotation = DataCenter.CurrentRotation?.Name;
		var place = string.IsNullOrEmpty(territory) ? "Out in the world" : territory;
		return string.IsNullOrEmpty(rotation) ? place : $"{place} · {rotation}";
	}

	private static readonly (StateCommandType State, string Label)[] _headerStates =
	[
		(StateCommandType.Auto, "Auto"),
		(StateCommandType.Manual, "Manual"),
		(StateCommandType.TargetOnly, "Target only"),
		(StateCommandType.Off, "Off"),
	];

	internal static StateCommandType CurrentState => !DataCenter.State
		? StateCommandType.Off
		: DataCenter.IsTargetOnly ? StateCommandType.TargetOnly
		: DataCenter.IsManual ? StateCommandType.Manual
		: StateCommandType.Auto;

	/// <summary>Auto / Manual / Target only / Off, drawn as a glowing gold pill row on the header art.</summary>
	private static void DrawStateSwitch(Vector2 position)
	{
		var drawList = ImGui.GetWindowDrawList();
		var scale = Scale;
		var segmentHeight = 32f * scale;
		var inset = 4f * scale;
		var current = CurrentState;

		using var font = ImRaii.PushFont(M3.TitleMedium);
		var widths = new float[_headerStates.Length];
		var total = inset;
		for (var i = 0; i < _headerStates.Length; i++)
		{
			widths[i] = ImGui.CalcTextSize(_headerStates[i].Label).X + (28f * scale);
			total += widths[i] + inset;
		}

		var containerMax = position + new Vector2(total, segmentHeight + (inset * 2f));
		drawList.AddRectFilled(position, containerMax, M3.U32(BunnyTheme.PlumLow, 0.88f), 14f * scale);
		drawList.AddRect(position, containerMax, M3.U32(BunnyTheme.Gold, 0.32f), 14f * scale, ImDrawFlags.None, 1f * scale);

		var x = position.X + inset;
		for (var i = 0; i < _headerStates.Length; i++)
		{
			var (state, label) = _headerStates[i];
			var segMin = new Vector2(x, position.Y + inset);
			var segMax = segMin + new Vector2(widths[i], segmentHeight);
			ImGui.SetCursorScreenPos(segMin);
			if (ImGui.InvisibleButton($"##bunny_state_{state}", segMax - segMin))
			{
				RSCommands.DoStateCommandType(state);
			}

			var hovered = ImGui.IsItemHovered();
			var active = state == current;
			if (active)
			{
				BunnyTheme.GlowRect(drawList, segMin, segMax, 10f * scale);
				drawList.AddRectFilled(segMin, segMax, M3.U32(BunnyTheme.Gold, 1f), 10f * scale);
			}
			else if (hovered)
			{
				drawList.AddRectFilled(segMin, segMax, M3.U32(BunnyTheme.Gold, 0.10f), 10f * scale);
			}

			if (hovered)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			var textSize = ImGui.CalcTextSize(label);
			drawList.AddText(segMin + ((segMax - segMin - textSize) * 0.5f),
				active ? M3.U32(new Vector4(0.16f, 0.10f, 0.03f, 1f), 1f) : M3.U32(BunnyTheme.Text, 0.9f), label);
			x = segMax.X + inset;
		}
	}

	private void DrawMaterialAppBar()
	{

		var scheme = M3.Scheme;
		var searching = _searchResults is { Length: > 0 };
		var title = searching
			? UiString.ConfigWindow_Search_Result.GetDescription()
			: GetActiveTabTitle();
		var subtitle = searching
			? string.Format("{0} matching settings", _searchResults!.Length)
			: _activeTab.GetDescription();

		var width = MathF.Max(64f * Scale, ImGui.GetContentRegionAvail().X);
		var clearWidth = searching ? M3Widgets.IconButtonSize + (4f * Scale) : 0f;

		var brand = Brand;
		var shown = _windowActions.Length;
		while (shown > 0 && clearWidth + M3Widgets.WindowActionsSize(shown, brand, 0f).X + (96f * Scale) > width)
		{
			shown--;
		}

		var barSize = M3Widgets.WindowActionsSize(shown, brand, 0f);
		var actionsWidth = barSize.X + clearWidth + (12f * Scale);
		var textWidth = MathF.Max(32f * Scale, width - actionsWidth);

		var padTop = 4f * M3.PaddingScale;
		var padBottom = 4f * M3.PaddingScale;
		var lineGap = 2f * M3.PaddingScale;

		string clippedTitle;
		Vector2 titleSize;
		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			clippedTitle = M3Navigation.Truncate(title, textWidth);
			titleSize = ImGui.CalcTextSize(clippedTitle);
		}

		var clippedSubtitle = string.Empty;
		var subtitleSize = Vector2.Zero;
		if (!string.IsNullOrEmpty(subtitle))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			clippedSubtitle = M3Navigation.Truncate(subtitle, textWidth);
			subtitleSize = ImGui.CalcTextSize(clippedSubtitle);
		}

		var contentHeight = titleSize.Y + (subtitleSize.Y > 0f ? lineGap + subtitleSize.Y : 0f);
		// Tall enough for the action pill to clear the divider under the bar.
		var height = MathF.Max(barSize.Y + (4f * Scale), padTop + contentHeight + padBottom);

		ImGui.Dummy(new Vector2(width, height));

		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var textTop = min.Y + ((height - contentHeight) * 0.5f);

		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			drawList.AddText(new Vector2(min.X, textTop), M3.U32(scheme.OnSurface, 0.98f), clippedTitle);
		}

		if (subtitleSize.Y > 0f)
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			drawList.AddText(new Vector2(min.X, textTop + titleSize.Y + lineGap),
				M3.U32(scheme.OnSurfaceVariant, 0.88f), clippedSubtitle);
		}

		_shownActions = shown;
		_fold.BarTop = (height - barSize.Y) * 0.5f;

		if (searching)
		{
			ImGui.SetCursorScreenPos(new Vector2(max.X - barSize.X - clearWidth, min.Y + ((height - M3Widgets.IconButtonSize) * 0.5f)));
			if (M3Widgets.IconButton("##appbar_clear_search", FontAwesomeIcon.Eraser, "Clear search results"))
			{
				_searchText = string.Empty;
				_searchResults = [];
			}
		}

		ImGui.SetCursorScreenPos(new Vector2(min.X, max.Y));
		drawList.AddLine(new Vector2(min.X, max.Y), new Vector2(max.X, max.Y), M3.U32(scheme.OutlineVariant, 0.5f), 1f * Scale);
		ImGui.Dummy(new Vector2(width, M3.Space1));
	}

	private string GetActiveTabTitle()
	{
		if (_activeTab == MainWindowTab.Job && Player.Object != null)
		{
			return CNLanguageClient
				? Player.ClassJob.ValueNullable?.Name.ExtractText() ?? Player.Job.ToString()
				: Player.Job.ToString();
		}

		if (_activeTab == MainWindowTab.DutyRotation)
		{
			return GetDutyRotationTabName();
		}

		return CNLanguageClient ? _activeTab.CNString() : _activeTab.ToString();
	}

	private void DrawBody()
	{
		using var child = ImRaii.Child("BunnyRotationReborn Body", -Vector2.One, false,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
		if (!child)
		{
			return;
		}

		DrawTopAppBar();

		using var page = ImRaii.Child("BunnyRotationReborn Page", -Vector2.One);
		if (!page)
		{
			return;
		}

		if (_searchResults is { Length: > 0 })
		{
			DrawSearchResults();
			return;
		}

		_ = DrawNotices();

		switch (_activeTab)
		{
			case MainWindowTab.Main:
			case MainWindowTab.About:
				DrawAbout();
				break;

			case MainWindowTab.Job:
			case MainWindowTab.Rotation:
				DrawRotation();
				break;

			case MainWindowTab.DutyRotation:
				DrawDutyRotationBody();
				break;

			case MainWindowTab.AutoDuty:
				DrawAutoduty();
				break;

			case MainWindowTab.Actions:
				DrawActions();
				break;

			case MainWindowTab.List:
				DrawList();
				break;

			case MainWindowTab.Basic:
				DrawBasic();
				break;

			case MainWindowTab.UI:
				DrawUI();
				break;

			case MainWindowTab.Auto:
				DrawAuto();
				break;

			case MainWindowTab.Target:
				DrawTarget();
				break;

			case MainWindowTab.Duty:
				DrawDutySpecific();
				break;

			case MainWindowTab.Extra:
				DrawExtra();
				break;

			case MainWindowTab.Debug:
				DrawDebug();
				break;

			default:
				ImGui.TextUnformatted("Unknown tab selected.");
				break;
		}
	}

	private static void OpenLinkSafely(string url)
	{
		try
		{
			Util.OpenLink(url);
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to open {url}: {ex.Message}");
		}
	}

	private static void DrawPageIntro(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		using (ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(M3.Scheme.OnSurfaceVariant, 0.92f)))
		{
			ImGui.TextWrapped(text);
		}

		ImGui.Dummy(new Vector2(0f, M3.Space1));
	}

	private static CollapsingHeaderGroup BuildHeaderGroup(
		Dictionary<Func<string>, Action> headers,
		params (UiString Title, FontAwesomeIcon Icon)[] icons)
	{
		var group = new CollapsingHeaderGroup(headers);
		foreach (var (title, icon) in icons)
		{
			group.SetHeaderIcon(title.GetDescription(), icon);
		}

		return group;
	}
}

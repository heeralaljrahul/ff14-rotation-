using Dalamud.Interface.Utility.Raii;
using RebornMaterial;
using RotationSolver.Basic.Configuration;
using RotationSolver.Data;
using RotationSolver.UI.ExtraWindows;
using RotationSolver.Updaters;

namespace RotationSolver.UI;

/// <summary>
/// The Bunny theme's Home page: autorotation, next action, this duty, quick macros and support cards.
/// </summary>
public partial class MainWindow
{
	private static readonly string[] _homeMacros =
	[
		"/bsr Auto",
		"/bsr Manual",
		"/bsr Off",
		"/bsr Settings AutoBurst False",
		"/bsr Settings AutoBurst True",
	];

	private static void DrawHomeDashboard()
	{
		var width = ImGui.GetContentRegionAvail().X;
		var wide = width >= 720f * Scale;

		using (ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(M3.Space1, M3.Space1)))
		{
			if (wide)
			{
				using (var table = ImRaii.Table("##bunny_home_top", 3, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings))
				{
					if (table)
					{
						ImGui.TableNextRow();
						_ = ImGui.TableNextColumn();
						DrawHomeAutorotationCard();
						_ = ImGui.TableNextColumn();
						DrawHomeNextActionCard();
						_ = ImGui.TableNextColumn();
						DrawHomeDutyCard();
					}
				}

				using (var table = ImRaii.Table("##bunny_home_bottom", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings))
				{
					if (table)
					{
						ImGui.TableSetupColumn("macros", ImGuiTableColumnFlags.WidthStretch, 2f);
						ImGui.TableSetupColumn("support", ImGuiTableColumnFlags.WidthStretch, 1f);
						ImGui.TableNextRow();
						_ = ImGui.TableNextColumn();
						DrawHomeMacrosCard();
						_ = ImGui.TableNextColumn();
						DrawHomeSupportCard();
					}
				}
			}
			else
			{
				DrawHomeAutorotationCard();
				DrawHomeNextActionCard();
				DrawHomeDutyCard();
				DrawHomeMacrosCard();
				DrawHomeSupportCard();
			}
		}

		ImGui.Dummy(new Vector2(0f, M3.Space1));
		_ = M3Widgets.Banner("##about_warning", UiString.ConfigWindow_About_Warning.GetDescription(),
			M3Severity.Warning, FontAwesomeIcon.ExclamationTriangle);
		ImGui.Dummy(new Vector2(0f, M3.Space2));
	}

	private static void DrawHomeAutorotationCard()
	{
		using var card = M3Card.Begin("bunny_home_autorotation", "Autorotation", FontAwesomeIcon.Robot, BunnyTheme.Gold, M3CardStyle.Outlined);

		var state = CurrentState;
		var label = state switch
		{
			StateCommandType.Auto => "AUTO",
			StateCommandType.Manual => "MANUAL",
			StateCommandType.TargetOnly => "TARGET ONLY",
			_ => "OFF",
		};

		var display = BunnyTheme.PushDisplay(true);
		using (display)
		using (ImRaii.PushFont(M3.HeadlineSmall, display == null))
		using (ImRaii.PushColor(ImGuiCol.Text, state == StateCommandType.Off ? BunnyTheme.TextMuted : BunnyTheme.Gold))
		{
			ImGui.TextUnformatted(label);
		}

		if (state == StateCommandType.Auto)
		{
			_ = M3Widgets.Pill("##bunny_home_target", $"Target: {DataCenter.TargetingType.GetDescription()}",
				M3.Scheme.Primary, FontAwesomeIcon.Crosshairs);
		}

		ImGui.Dummy(new Vector2(0f, M3.Space1));
		HomeSwitch("AutoBurst", "Your hold/release macros toggle this.", Service.Config.AutoBurst);
		HomeSwitch("No pre-pull", "Nothing happens until the countdown reaches 0.", Service.Config.NoCountdownActions);
		HomeSwitch("Instant raise", "Swiftcast and raise in high-end duties.", Service.Config.InstantRaiseHighEnd);
	}

	private static void HomeSwitch(string label, string description, ConditionBoolean setting)
	{
		var value = setting.Value;
		if (M3Widgets.RowSwitch(label, ref value, Service.Config.UiInlineDescriptions ? description : null))
		{
			setting.Value = value;
		}
	}

	private static void DrawHomeNextActionCard()
	{
		using var card = M3Card.Begin("bunny_home_next", "Next action", FontAwesomeIcon.Bolt, BunnyTheme.Gold, M3CardStyle.Outlined);

		var next = ActionUpdater.NextAction;
		if (next == null)
		{
			using (ImRaii.PushColor(ImGuiCol.Text, BunnyTheme.TextMuted))
			{
				ImGui.TextWrapped("Nothing queued yet. Turn on Auto and pick a target to start.");
			}

			return;
		}

		var size = 56f * Scale;
		var iconMin = ImGui.GetCursorScreenPos();
		if (next.GetTexture(out var texture))
		{
			_ = M3ActionIcon.Draw("##bunny_home_next_icon", texture, size, tooltip: next.Name);
			BunnyTheme.GlowRect(ImGui.GetWindowDrawList(), iconMin, iconMin + new Vector2(size), M3.ShapeSmall);
			ImGui.SameLine(0f, M3.Space2);
		}

		using (ImRaii.Group())
		{
			using (ImRaii.PushFont(M3.TitleMedium))
			{
				ImGui.TextUnformatted(next.Name);
			}

			var isGcd = next is IBaseAction baseAction && baseAction.Info.IsRealGCD;
			using (ImRaii.PushFont(M3.LabelSmall))
			using (ImRaii.PushColor(ImGuiCol.Text, BunnyTheme.TextMuted))
			{
				ImGui.TextUnformatted(isGcd ? "GCD" : "oGCD");
			}
		}

		ImGui.Dummy(new Vector2(0f, M3.Space1));
		NextActionWindow.DrawGcdProgress(ImGui.GetContentRegionAvail().X, showTime: true);

		var gcd = ActionUpdater.NextGCDAction;
		if (gcd != null && gcd != next)
		{
			ImGui.Dummy(new Vector2(0f, M3.Space1));
			_ = M3Widgets.Pill("##bunny_home_then", $"Then: {gcd.Name}", M3.Scheme.Secondary, FontAwesomeIcon.ArrowRight);
		}
	}

	private static void DrawHomeDutyCard()
	{
		using var card = M3Card.Begin("bunny_home_duty", "This duty", FontAwesomeIcon.Dragon, BunnyTheme.Gold, M3CardStyle.Outlined);

		var territory = DataCenter.Territory;
		var ultimate = CustomRotation.CurrentUltimate;
		var name = territory?.ContentFinderName;

		if (ultimate != UltimateRaid.None)
		{
			_ = M3Widgets.Pill("##bunny_home_ult", ultimate.ToString(), BunnyTheme.Gold, FontAwesomeIcon.Crown);
			ImGui.SameLine(0f, M3.Space1);
		}
		else if (territory?.IsHighEndDuty == true)
		{
			_ = M3Widgets.Pill("##bunny_home_highend", "High-end", BunnyTheme.Gold, FontAwesomeIcon.Fire);
			ImGui.SameLine(0f, M3.Space1);
		}

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			ImGui.TextWrapped(string.IsNullOrEmpty(name) ? "Out in the world" : name);
		}

		ImGui.Dummy(new Vector2(0f, M3.Space1));
		HomeStatusLine("Burst holds", Service.Config.AutomaticBurstHolds ? "On" : "Off");
		HomeStatusLine("Per-ultimate settings", ultimate != UltimateRaid.None ? "Active" : "Not in an ultimate");
		HomeStatusLine("Pre-pull", Service.Config.NoCountdownActions ? "Blocked" : "Allowed");

		ImGui.Dummy(new Vector2(0f, M3.Space1));
		if (M3Widgets.Button("##bunny_home_ultimate", "Ultimate settings", M3ButtonStyle.Outlined, FontAwesomeIcon.ExternalLinkAlt))
		{
			RotationSolverPlugin.OpenUltimateSettings();
		}
	}

	private static void HomeStatusLine(string label, string value)
	{
		var lineStart = ImGui.GetCursorPosX();
		var width = ImGui.GetContentRegionAvail().X;
		using (ImRaii.PushColor(ImGuiCol.Text, BunnyTheme.TextMuted))
		{
			ImGui.TextUnformatted(label);
		}

		var labelEnd = lineStart + ImGui.GetItemRectSize().X + M3.Space1;
		var valueWidth = ImGui.CalcTextSize(value).X;
		ImGui.SameLine(MathF.Max(labelEnd, lineStart + width - valueWidth));
		using (ImRaii.PushColor(ImGuiCol.Text, BunnyTheme.Gold))
		{
			ImGui.TextUnformatted(value);
		}
	}

	private static void DrawHomeMacrosCard()
	{
		using var card = M3Card.Begin("bunny_home_macros", "Quick macros", FontAwesomeIcon.Terminal, BunnyTheme.Gold, M3CardStyle.Outlined,
			subtitle: "Click one to copy it, then paste it into a macro.");

		var right = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X;
		for (var i = 0; i < _homeMacros.Length; i++)
		{
			var macro = _homeMacros[i];
			if (i > 0)
			{
				var lastMax = ImGui.GetItemRectMax().X;
				if (lastMax + M3.Space1 + M3Widgets.ChipWidth(macro, FontAwesomeIcon.Copy) <= right)
				{
					ImGui.SameLine(0f, M3.Space1);
				}
			}

			if (M3Widgets.Chip($"##bunny_home_macro_{i}", macro, false, FontAwesomeIcon.Copy, "Copy to clipboard"))
			{
				ImGui.SetClipboardText(macro);
				M3Snackbar.Show($"Copied {macro}");
			}
		}
	}

	private static void DrawHomeSupportCard()
	{
		using var card = M3Card.Begin("bunny_home_support", "Made with love", FontAwesomeIcon.Heart, BunnyTheme.BunnyPink, M3CardStyle.Outlined);

		using (ImRaii.PushColor(ImGuiCol.Text, BunnyTheme.TextMuted))
		{
			ImGui.TextWrapped("Built on RotationSolverReborn. Please support the official release and its developers. AI was used in the development of this fork.");
		}

		ImGui.Dummy(new Vector2(0f, M3.Space1));
		if (M3Widgets.Button("##bunny_home_kofi", "Ko-fi", M3ButtonStyle.Filled, FontAwesomeIcon.MugHot))
		{
			OpenLinkSafely(Service.UPSTREAM_KOFI_URL);
		}

		ImGui.SameLine(0f, M3.Space1);
		if (M3Widgets.Button("##bunny_home_discord", "Discord", M3ButtonStyle.Outlined, FontAwesomeIcon.Comments))
		{
			OpenLinkSafely(Service.UPSTREAM_DISCORD_URL);
		}

		if (M3Widgets.Button("##bunny_home_tutorial", CNLanguageClient ? "打开初次启动向导" : "Tutorial", M3ButtonStyle.Text, FontAwesomeIcon.GraduationCap))
		{
			RotationSolverPlugin.OpenFirstStartTutorial();
		}

		ImGui.SameLine(0f, M3.Space1);
		if (M3Widgets.Button("##bunny_home_changelog", CNLanguageClient ? "更新说明" : "What's new", M3ButtonStyle.Text, FontAwesomeIcon.Newspaper))
		{
			RotationSolverPlugin.OpenChangelog();
		}

		var clickingCount = OtherConfiguration.RotationSolverRecord.ClickingCount;
		var countStr = UiString.ConfigWindow_About_ClickingCount.GetDescription();
		if (clickingCount > 0 && !string.IsNullOrEmpty(countStr))
		{
			ImGui.Dummy(new Vector2(0f, M3.Space1));
			_ = M3Widgets.Pill("##bunny_home_clicks", string.Format(countStr, clickingCount), M3.Scheme.Tertiary, FontAwesomeIcon.MousePointer);
		}
	}
}

using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.Logging;
using RebornMaterial;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.UI;

/// <summary>
/// The BunnyRotationReborn look: dark plum surfaces with gold glow, from the bunny artwork.
/// RebornMaterial derives its scheme from one seed colour, so the palette is applied by patching the
/// generated scheme's colour roles each time the library builds a new one.
/// </summary>
internal static class BunnyTheme
{
	#region Palette
	internal static readonly Vector4 Plum = Hex(0x120B10);
	internal static readonly Vector4 PlumDeep = Hex(0x0E080C);
	internal static readonly Vector4 PlumLow = Hex(0x170F15);
	internal static readonly Vector4 PlumCard = Hex(0x1C1219);
	internal static readonly Vector4 PlumHigh = Hex(0x241820);
	internal static readonly Vector4 PlumHighest = Hex(0x2C1E26);
	internal static readonly Vector4 Gold = Hex(0xF2C46D);
	internal static readonly Vector4 GoldBright = Hex(0xFBDD7C);
	internal static readonly Vector4 GoldDeep = Hex(0xE4AD61);
	internal static readonly Vector4 BunnyPink = Hex(0xD49AB5);
	internal static readonly Vector4 MoonlitCyan = Hex(0x5FAAC6);
	internal static readonly Vector4 Text = Hex(0xF3E9EE);
	internal static readonly Vector4 TextMuted = Hex(0xC7B5BF);

	private static Vector4 Hex(uint rgb) => new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
	#endregion

	internal static bool Enabled => Service.Config.UiBunnyTheme;

	/// <summary>The running plugin version, e.g. "v7.5.7.4".</summary>
	internal static string VersionLabel { get; } = $"v{typeof(BunnyTheme).Assembly.GetName().Version?.ToString() ?? "?.?.?"}";

	/// <summary>A gold version pill at the cursor; clicking it opens the update notes.</summary>
	internal static void VersionPill(string id)
	{
		if (M3Widgets.Pill(id, VersionLabel, Gold, FontAwesomeIcon.Tag, "The version you are running. Click for what's new.", interactive: true))
		{
			RotationSolverPlugin.OpenChangelog();
		}
	}

	#region Scheme patch
	private static M3Scheme? _patched;
	private static bool _patchFailed;

	/// <summary>Call once per frame after <see cref="M3.BeginFrame"/>.</summary>
	internal static void BeginFrame()
	{
		if (_patchFailed)
		{
			return;
		}

		try
		{
			var scheme = M3.Scheme;
			if (!Enabled)
			{
				if (_patched != null)
				{
					RestoreScheme();
				}

				return;
			}

			if (!ReferenceEquals(scheme, _patched))
			{
				Patch(scheme);
				_patched = scheme;
			}
		}
		catch (Exception ex)
		{
			_patchFailed = true;
			PluginLog.Warning($"[BunnyTheme] Could not apply the Bunny palette, using the default scheme: {ex.Message}");
		}
	}

	private static void Patch(M3Scheme scheme)
	{
		Set(scheme, nameof(M3Scheme.Surface), Plum);
		Set(scheme, nameof(M3Scheme.SurfaceContainerLowest), PlumDeep);
		Set(scheme, nameof(M3Scheme.SurfaceContainerLow), PlumLow);
		Set(scheme, nameof(M3Scheme.SurfaceContainer), PlumCard);
		Set(scheme, nameof(M3Scheme.SurfaceContainerHigh), PlumHigh);
		Set(scheme, nameof(M3Scheme.SurfaceContainerHighest), PlumHighest);
		Set(scheme, nameof(M3Scheme.OnSurface), Text);
		Set(scheme, nameof(M3Scheme.OnSurfaceVariant), TextMuted);
		Set(scheme, nameof(M3Scheme.Outline), Hex(0x8A6E4A));
		Set(scheme, nameof(M3Scheme.OutlineVariant), Hex(0x3D2B2E));
		Set(scheme, nameof(M3Scheme.InverseSurface), Text);
		Set(scheme, nameof(M3Scheme.InverseOnSurface), PlumHigh);

		Set(scheme, nameof(M3Scheme.Secondary), BunnyPink);
		Set(scheme, nameof(M3Scheme.SecondaryContainer), Hex(0x4A2D3D));
		Set(scheme, nameof(M3Scheme.OnSecondaryContainer), Hex(0xF6D9E6));
		Set(scheme, nameof(M3Scheme.Tertiary), MoonlitCyan);
		Set(scheme, nameof(M3Scheme.TertiaryContainer), Hex(0x1F3E4A));
		Set(scheme, nameof(M3Scheme.OnTertiaryContainer), Hex(0xCFE8F1));
		Set(scheme, nameof(M3Scheme.Info), MoonlitCyan);

		// The gold primary roles only replace the generated ones for the default Bunny Gold accent, so a picked
		// accent colour still drives buttons and highlights.
		if (Vector4.Distance(Service.Config.UiAccentColor, Configs.DefaultAccentColor) < 0.01f)
		{
			Set(scheme, nameof(M3Scheme.Primary), Gold);
			Set(scheme, nameof(M3Scheme.OnPrimary), Hex(0x2A1A08));
			Set(scheme, nameof(M3Scheme.PrimaryContainer), Hex(0x5A4220));
			Set(scheme, nameof(M3Scheme.OnPrimaryContainer), GoldBright);
			Set(scheme, nameof(M3Scheme.PrimaryFixedDim), GoldDeep);
			Set(scheme, nameof(M3Scheme.InversePrimary), Hex(0x8A6420));
		}
	}

	private static void Set(M3Scheme scheme, string role, Vector4 value)
	{
		var field = typeof(M3Scheme).GetField($"<{role}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(nameof(M3Scheme), role);
		field.SetValue(scheme, value);
	}

	private static void RestoreScheme()
	{
		var field = typeof(M3).GetField("_scheme", BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new MissingFieldException(nameof(M3), "_scheme");
		field.SetValue(null, M3Scheme.FromSeed(Service.Config.UiAccentColor));
		_patched = null;
	}
	#endregion

	#region Fonts
	private static IFontHandle? _displayLarge;
	private static IFontHandle? _displaySmall;

	internal static void Initialize()
	{
		try
		{
			var directory = Svc.PluginInterface.AssemblyLocation.DirectoryName;
			if (directory == null)
			{
				return;
			}

			var path = Path.Combine(directory, "Fonts", "Cinzel-Bold.ttf");
			if (!File.Exists(path))
			{
				PluginLog.Warning($"[BunnyTheme] Display font not found at {path}; headings use the default font.");
				return;
			}

			var atlas = Svc.PluginInterface.UiBuilder.FontAtlas;
			_displayLarge = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddFontFromFile(path, new SafeFontConfig { SizePx = 30f })));
			_displaySmall = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddFontFromFile(path, new SafeFontConfig { SizePx = 19f })));
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"[BunnyTheme] Could not load the display font: {ex.Message}");
		}
	}

	internal static void Dispose()
	{
		_displayLarge?.Dispose();
		_displaySmall?.Dispose();
		_displayLarge = _displaySmall = null;
	}

	/// <summary>Pushes the Cinzel display font, or nothing when the theme is off or the font isn't ready.</summary>
	internal static IDisposable? PushDisplay(bool large)
	{
		var handle = large ? _displayLarge : _displaySmall;
		return Enabled && handle is { Available: true } ? handle.Push() : null;
	}
	#endregion

	#region Drawing
	internal static uint U32(Vector4 color, float alpha) => M3.U32(color, alpha);

	/// <summary>A four-point star sparkle with a soft glow, like the sparks around the bunny's gun.</summary>
	internal static void Sparkle(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, float alpha = 1f)
	{
		if (alpha <= 0.01f)
		{
			return;
		}

		drawList.AddCircleFilled(center, radius * 1.1f, U32(color, 0.12f * alpha), 16);
		Span<Vector2> points = stackalloc Vector2[8];
		for (var i = 0; i < 8; i++)
		{
			var angle = (i * MathF.PI / 4f) - (MathF.PI / 2f);
			var length = i % 2 == 0 ? radius : radius * 0.28f;
			points[i] = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * length;
		}

		var col = U32(color, alpha);
		for (var i = 0; i < 8; i += 2)
		{
			drawList.AddTriangleFilled(center, points[(i + 7) % 8], points[i], col);
			drawList.AddTriangleFilled(center, points[i], points[i + 1], col);
		}
	}

	private static readonly Vector3[] _sparkles =
	[
		new(0.34f, 0.22f, 7f), new(0.42f, 0.55f, 4.5f), new(0.28f, 0.72f, 3.5f),
		new(0.58f, 0.18f, 3.5f), new(0.50f, 0.38f, 5f), new(0.66f, 0.62f, 4f),
	];

	/// <summary>Twinkling sparkles scattered over a header.</summary>
	internal static void Twinkles(ImDrawListPtr drawList, Vector2 min, Vector2 max)
	{
		var time = (float)ImGui.GetTime();
		var size = max - min;
		for (var i = 0; i < _sparkles.Length; i++)
		{
			var s = _sparkles[i];
			var alpha = 0.55f + (0.45f * MathF.Sin((time * 1.6f) + (i * 1.7f)));
			var color = i % 3 == 0 ? GoldBright : (i % 3 == 1 ? Gold : GoldDeep);
			Sparkle(drawList, min + new Vector2(size.X * s.X, size.Y * s.Y), s.Z * M3.Scale, color, alpha);
		}
	}

	/// <summary>A gold ring with a soft outer glow, like the halo behind the bunny.</summary>
	internal static void GlowRing(ImDrawListPtr drawList, Vector2 center, float radius)
	{
		var scale = M3.Scale;
		for (var i = 4; i >= 1; i--)
		{
			drawList.AddCircle(center, radius + (i * 2.5f * scale), U32(Gold, 0.05f * (5 - i)), 64, 2.5f * scale);
		}

		drawList.AddCircle(center, radius, U32(Gold, 0.95f), 64, 2f * scale);
	}

	/// <summary>Soft gold glow around a rounded rectangle, for selected or highlighted elements.</summary>
	internal static void GlowRect(ImDrawListPtr drawList, Vector2 min, Vector2 max, float rounding, float strength = 1f)
	{
		var scale = M3.Scale;
		for (var i = 3; i >= 1; i--)
		{
			var grow = new Vector2(i * 2f * scale);
			drawList.AddRect(min - grow, max + grow, U32(Gold, 0.06f * strength * (4 - i)), rounding + (i * 2f * scale), ImDrawFlags.None, 2f * scale);
		}
	}

	/// <summary>
	/// The artwork filling <paramref name="min"/>..<paramref name="max"/> (cropped to cover), faded into plum from the
	/// left and the bottom so text on it stays readable.
	/// </summary>
	internal static void ArtHeader(ImDrawListPtr drawList, Vector2 min, Vector2 max, IDalamudTextureWrap? art, float focusY,
		float imageAlpha = 1f, float rounding = 0f)
	{
		drawList.AddRectFilled(min, max, U32(Plum, 1f), rounding);
		var size = max - min;
		if (art != null && art.Width > 0 && art.Height > 0 && size.X > 1f && size.Y > 1f)
		{
			var scale = MathF.Max(size.X / art.Width, size.Y / art.Height);
			var uvSize = new Vector2(size.X / (art.Width * scale), size.Y / (art.Height * scale));
			var uv0 = new Vector2((1f - uvSize.X) * 0.5f, Math.Clamp(focusY - (uvSize.Y * 0.5f), 0f, 1f - uvSize.Y));
			drawList.AddImageRounded(art.Handle, min, max, uv0, uv0 + uvSize, U32(Vector4.One, imageAlpha), rounding);
		}

		var left = U32(Plum, 0.94f);
		var mid = U32(Plum, 0.45f);
		var clear = U32(Plum, 0.05f);
		var split = min.X + (size.X * 0.5f);
		drawList.AddRectFilledMultiColor(min, new Vector2(split, max.Y), left, mid, mid, left);
		drawList.AddRectFilledMultiColor(new Vector2(split, min.Y), max, mid, clear, clear, mid);
		drawList.AddRectFilledMultiColor(new Vector2(min.X, max.Y - (size.Y * 0.45f)), max,
			U32(Plum, 0f), U32(Plum, 0f), U32(Plum, 1f), U32(Plum, 1f));
	}

	/// <summary>A small letter-spaced label, like the gold eyebrow text over headings.</summary>
	internal static void Eyebrow(ImDrawListPtr drawList, Vector2 position, string text, Vector4 color)
	{
		using var font = ImRaii.PushFont(M3.LabelSmall);
		var x = position.X;
		var spacing = 1.6f * M3.Scale;
		var col = U32(color, 1f);
		foreach (var ch in text.ToUpperInvariant())
		{
			var s = ch.ToString();
			drawList.AddText(new Vector2(x, position.Y), col, s);
			x += ImGui.CalcTextSize(s).X + spacing;
		}
	}

	internal static Vector2 EyebrowSize(string text)
	{
		using var font = ImRaii.PushFont(M3.LabelSmall);
		var size = ImGui.CalcTextSize(text.ToUpperInvariant());
		return new Vector2(size.X + (text.Length * 1.6f * M3.Scale), size.Y);
	}
	#endregion
}

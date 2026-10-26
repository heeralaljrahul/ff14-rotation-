using static RotationSolver.Basic.Rotations.Duties.DutyRotation;

namespace RotationSolver.Basic.Attributes;

/// <summary>
///
/// </summary>
/// <param name="type"></param>
[AttributeUsage(AttributeTargets.Property)]
public class RotationConfigAttribute(CombatType type) : Attribute
{
	/// <summary>
	/// The type of this config.
	/// </summary>
	public CombatType Type => type;

	/// <summary>
	/// The display name for this config.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the parent of this config item.
	/// </summary>
	public string Parent { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the value that the parent property must have for this config item to be enabled/visible.
	/// </summary>
	public object? ParentValue { get; set; }

	/// <summary>
	/// Phantom Job for this config.
	/// </summary>
	public PhantomJob PhantomJob { get; set; } = PhantomJob.None;

	/// <summary>
	/// Tooltip for this config.
	/// </summary>
	public string Tooltip { get; set; } = string.Empty;

	/// <summary>
	/// Shows this config in the Ultimate settings window instead of the main rotation options.
	/// Implied when <see cref="Ultimate"/> is set.
	/// </summary>
	public bool UltimateSection { get; set; }

	/// <summary>
	/// The ultimate this config belongs to. Shown on that ultimate's tab in the Ultimate settings window.
	/// </summary>
	public UltimateRaid Ultimate { get; set; } = UltimateRaid.None;

	/// <summary>
	/// Whether this config belongs in the Ultimate settings window.
	/// </summary>
	public bool InUltimateSection => UltimateSection || Ultimate != UltimateRaid.None;
}
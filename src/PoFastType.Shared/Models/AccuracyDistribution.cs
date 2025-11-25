namespace PoFastType.Shared.Models;

/// <summary>
/// Represents an accuracy distribution category for charts.
/// </summary>
public class AccuracyDistribution
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

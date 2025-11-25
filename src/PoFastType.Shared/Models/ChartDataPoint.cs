namespace PoFastType.Shared.Models;

/// <summary>
/// Represents a data point for WPM progress charts.
/// </summary>
public class ChartDataPoint
{
    public int TestNumber { get; set; }
    public double NetWPM { get; set; }
    public double GrossWPM { get; set; }
}

namespace GraphApp.Core.Models;

/// <summary>
/// Đại diện cho một cạnh (edge) trong đồ thị.
/// </summary>
public class Edge
{
    public int Id { get; set; }

    /// <summary>Id của đỉnh nguồn</summary>
    public int Source { get; set; }

    /// <summary>Id của đỉnh đích</summary>
    public int Target { get; set; }

    /// <summary>Trọng số của cạnh (mặc định = 1)</summary>
    public double Weight { get; set; } = 1.0;
}

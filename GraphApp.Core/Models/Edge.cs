namespace GraphApp.Core.Models;

/// <summary>
/// Đại diện cho một cạnh (edge) trong đồ thị.
/// Với đồ thị vô hướng: Source và Target có thể hoán đổi nhau.
/// Với đồ thị có hướng: cạnh đi từ Source → Target.
/// </summary>
public class Edge
{
    /// <summary>Định danh duy nhất của cạnh trong đồ thị.</summary>
    public int Id { get; set; }

    /// <summary>Id của đỉnh nguồn.</summary>
    public int Source { get; set; }

    /// <summary>Id của đỉnh đích.</summary>
    public int Target { get; set; }

    /// <summary>Trọng số của cạnh. Mặc định = 1.</summary>
    public double Weight { get; set; } = 1.0;
}

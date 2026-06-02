namespace GraphApp.Core.Models;

/// <summary>
/// Đại diện cho một đỉnh (vertex) trong đồ thị.
/// </summary>
public class Node
{
    /// <summary>Định danh duy nhất của đỉnh trong đồ thị.</summary>
    public int Id { get; set; }

    /// <summary>Nhãn hiển thị trên canvas (VD: "A", "B", "1", "2").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Tọa độ của đỉnh trên canvas (đơn vị: pixel).</summary>
    public System.Drawing.PointF Position { get; set; }
}

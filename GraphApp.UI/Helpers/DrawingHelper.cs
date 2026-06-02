using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GraphApp.UI.Helpers;

/// <summary>
/// Các hàm tiện ích cho việc vẽ đồ thị bằng GDI+:
/// vẽ mũi tên, tính toán geometry, ...
/// </summary>
public static class DrawingHelper
{
    /// <summary>
    /// Vẽ mũi tên ở cuối đoạn thẳng từ <paramref name="from"/> đến <paramref name="to"/>.
    /// </summary>
    public static void DrawArrow(
        Graphics g,
        Pen pen,
        PointF from,
        PointF to,
        float arrowSize = 12f)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float len = (float)Math.Sqrt(dx * dx + dy * dy);
        if (len < 1f) return;

        float ux = dx / len;
        float uy = dy / len;

        // Điểm mũi tên lùi lại một chút để không bị chồng lên node
        PointF tip  = new(to.X - ux * 22, to.Y - uy * 22);
        PointF left = new(tip.X - arrowSize * ux + arrowSize * 0.5f * uy,
                          tip.Y - arrowSize * uy - arrowSize * 0.5f * ux);
        PointF right= new(tip.X - arrowSize * ux - arrowSize * 0.5f * uy,
                          tip.Y - arrowSize * uy + arrowSize * 0.5f * ux);

        using var brush = new SolidBrush(pen.Color);
        g.FillPolygon(brush, new[] { tip, left, right });
        g.DrawLine(pen, from, tip);
    }

    /// <summary>
    /// Tính khoảng cách từ điểm <paramref name="p"/> đến đoạn thẳng [a, b].
    /// </summary>
    public static float DistanceToSegment(PointF p, PointF a, PointF b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        float lenSq = dx * dx + dy * dy;

        if (lenSq < 1e-6f)
            return Distance(p, a);

        float t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq, 0, 1);
        PointF proj = new(a.X + t * dx, a.Y + t * dy);
        return Distance(p, proj);
    }

    private static float Distance(PointF a, PointF b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Tính điểm giao giữa đoạn thẳng và biên hình tròn bán kính <paramref name="radius"/>.
    /// Dùng để căn chỉnh điểm cuối của cạnh không chui vào trong node.
    /// </summary>
    public static PointF EdgeEndpoint(PointF center, PointF other, float radius)
    {
        float dx = other.X - center.X;
        float dy = other.Y - center.Y;
        float len = (float)Math.Sqrt(dx * dx + dy * dy);
        if (len < 1f) return center;
        return new PointF(center.X + dx / len * radius, center.Y + dy / len * radius);
    }
}

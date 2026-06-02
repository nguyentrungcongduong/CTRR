using System.Drawing;
using System.Drawing.Drawing2D;

namespace GraphApp.UI.Helpers;

/// <summary>
/// Các hàm tiện ích cho việc vẽ đồ thị bằng GDI+:
/// vẽ mũi tên, tính toán geometry cho cạnh và đỉnh.
/// </summary>
public static class DrawingHelper
{
    /// <summary>
    /// Vẽ cạnh có hướng: đường thẳng + mũi tên tại điểm cuối.
    /// <paramref name="from"/> và <paramref name="to"/> đã là điểm trên biên hình tròn.
    /// </summary>
    public static void DrawArrow(
        Graphics g,
        Pen      pen,
        PointF   from,
        PointF   to,
        float    arrowSize = 13f)
    {
        float dx  = to.X - from.X;
        float dy  = to.Y - from.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1f) return;

        float ux = dx / len;
        float uy = dy / len;

        // Đường thân cạnh (dừng lại trước mũi tên)
        PointF lineEnd = new(to.X - ux * arrowSize * 0.7f, to.Y - uy * arrowSize * 0.7f);
        g.DrawLine(pen, from, lineEnd);

        // Mũi tên: tam giác tại điểm to
        PointF tip   = to;
        PointF left  = new(
            tip.X - arrowSize * ux + arrowSize * 0.45f * uy,
            tip.Y - arrowSize * uy - arrowSize * 0.45f * ux);
        PointF right = new(
            tip.X - arrowSize * ux - arrowSize * 0.45f * uy,
            tip.Y - arrowSize * uy + arrowSize * 0.45f * ux);

        using var fillBrush = new SolidBrush(pen.Color);
        g.FillPolygon(fillBrush, new[] { tip, left, right });
    }

    /// <summary>
    /// Tính khoảng cách vuông góc từ điểm <paramref name="p"/>
    /// đến đoạn thẳng [<paramref name="a"/>, <paramref name="b"/>].
    /// </summary>
    public static float DistanceToSegment(PointF p, PointF a, PointF b)
    {
        float dx    = b.X - a.X;
        float dy    = b.Y - a.Y;
        float lenSq = dx * dx + dy * dy;

        if (lenSq < 1e-6f) return Distance(p, a);

        float t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq, 0f, 1f);
        PointF proj = new(a.X + t * dx, a.Y + t * dy);
        return Distance(p, proj);
    }

    /// <summary>
    /// Tính điểm trên biên hình tròn tâm <paramref name="center"/> bán kính <paramref name="radius"/>,
    /// theo hướng từ <paramref name="center"/> sang <paramref name="other"/>.
    /// Dùng để xác định điểm bắt đầu/kết thúc của cạnh không chui vào trong node.
    /// </summary>
    public static PointF EdgeEndpoint(PointF center, PointF other, float radius)
    {
        float dx  = other.X - center.X;
        float dy  = other.Y - center.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1f) return center;
        return new PointF(center.X + dx / len * radius, center.Y + dy / len * radius);
    }

    /// <summary>Khoảng cách Euclidean giữa 2 điểm.</summary>
    public static float Distance(PointF a, PointF b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}

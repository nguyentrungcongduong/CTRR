using System.Drawing;
using System.Windows.Forms;
using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.UI.Controls;

/// <summary>
/// UserControl vẽ đồ thị bằng GDI+.
/// Xử lý các sự kiện chuột để thêm/xóa đỉnh/cạnh và kéo thả.
/// </summary>
public class GraphCanvas : UserControl
{
    // ─── Constants ─────────────────────────────────────────────────────
    private const float NodeRadius    = 22f;
    private const float EdgeClickTol  = 6f;   // px — ngưỡng click trúng cạnh

    // ─── Colors (theo quy ước ARCHITECTURE.md) ─────────────────────────
    private static readonly Color ColorDefault   = ColorTranslator.FromHtml("#4A90D9");
    private static readonly Color ColorVisited   = ColorTranslator.FromHtml("#27AE60");
    private static readonly Color ColorActive    = ColorTranslator.FromHtml("#E74C3C");
    private static readonly Color ColorInQueue   = ColorTranslator.FromHtml("#F39C12");
    private static readonly Color ColorMstEdge   = ColorTranslator.FromHtml("#8E44AD");
    private static readonly Color ColorAugmented = ColorTranslator.FromHtml("#E67E22");

    // ─── State ─────────────────────────────────────────────────────────
    private Graph _graph = new();
    private AlgorithmStep? _currentStep;
    private CanvasMode _mode = CanvasMode.Select;

    private Node? _dragNode;
    private PointF _dragOffset;
    private int _edgeFirstNodeId = -1;   // id đỉnh đầu tiên khi thêm cạnh

    // ─── Public API ────────────────────────────────────────────────────

    public event Action<Graph>? GraphChanged;

    public CanvasMode Mode
    {
        get => _mode;
        set { _mode = value; _edgeFirstNodeId = -1; Invalidate(); }
    }

    /// <summary>Bind đồ thị mới vào canvas và vẽ lại.</summary>
    public void RefreshGraph(Graph graph)
    {
        _graph       = graph;
        _currentStep = null;
        Invalidate();
    }

    /// <summary>Áp dụng một bước animation lên canvas.</summary>
    public void ApplyStep(AlgorithmStep step)
    {
        _currentStep = step;
        Invalidate();
    }

    public Graph GetGraph() => _graph;

    // ─── Paint ─────────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // TODO: TASK-03 — implement đầy đủ ở Phase 1
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        DrawEdges(g);
        DrawNodes(g);
    }

    private void DrawEdges(Graphics g)
    {
        // TODO: TASK-03
    }

    private void DrawNodes(Graphics g)
    {
        // TODO: TASK-03
    }

    // ─── Mouse Events ──────────────────────────────────────────────────

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        // TODO: TASK-04
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        // TODO: TASK-04
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        // TODO: TASK-04
    }

    // ─── Hit Testing ───────────────────────────────────────────────────

    private Node? HitTestNode(PointF point)
    {
        foreach (var node in _graph.Nodes)
        {
            var dx = node.Position.X - point.X;
            var dy = node.Position.Y - point.Y;
            if (Math.Sqrt(dx * dx + dy * dy) <= NodeRadius)
                return node;
        }
        return null;
    }

    private Edge? HitTestEdge(PointF point)
    {
        // TODO: TASK-04 — khoảng cách điểm đến đoạn thẳng < EdgeClickTol
        return null;
    }

    // ─── Constructor ───────────────────────────────────────────────────

    public GraphCanvas()
    {
        DoubleBuffered    = true;
        BackColor         = Color.White;
        BorderStyle       = BorderStyle.FixedSingle;
    }
}

/// <summary>Chế độ tương tác của canvas.</summary>
public enum CanvasMode
{
    Select,
    AddNode,
    AddEdge,
    Delete
}

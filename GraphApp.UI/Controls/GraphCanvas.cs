using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;
using GraphApp.UI.Helpers;

namespace GraphApp.UI.Controls;

/// <summary>
/// UserControl vẽ đồ thị bằng GDI+.
/// Xử lý các sự kiện chuột để thêm/xóa đỉnh/cạnh và kéo thả (TASK-04).
/// </summary>
public class GraphCanvas : UserControl
{
    // ─── Constants ─────────────────────────────────────────────────────
    public  const  float NodeRadius   = 22f;
    private const  float EdgeClickTol = 6f;

    // ─── Color palette (theo ARCHITECTURE.md) ──────────────────────────
    private static readonly Color CNodeDefault   = ColorTranslator.FromHtml("#4A90D9");
    private static readonly Color CNodeVisited   = ColorTranslator.FromHtml("#27AE60");
    private static readonly Color CNodeActive    = ColorTranslator.FromHtml("#E74C3C");
    private static readonly Color CNodeQueue     = ColorTranslator.FromHtml("#F39C12");
    private static readonly Color CEdgeDefault   = Color.FromArgb(160, 160, 165);
    private static readonly Color CEdgeMst       = ColorTranslator.FromHtml("#8E44AD");
    private static readonly Color CEdgeAugmented = ColorTranslator.FromHtml("#E67E22");
    private static readonly Color CEdgeRejected  = Color.FromArgb(230, 190, 190);
    private static readonly Color CEdgeConsidered= Color.FromArgb(235, 205, 0);
    private static readonly Color CBackground    = Color.FromArgb(245, 246, 250);
    private static readonly Color CGrid          = Color.FromArgb(235, 235, 240);

    // ─── Fonts ─────────────────────────────────────────────────────────
    private readonly Font _nodeFont   = new("Segoe UI", 10f, FontStyle.Bold);
    private readonly Font _weightFont = new("Segoe UI", 7.5f);
    private readonly Font _secFont    = new("Segoe UI", 7.5f);

    // ─── State ─────────────────────────────────────────────────────────
    private Graph          _graph       = new();
    private AlgorithmStep? _currentStep;
    private CanvasMode     _mode        = CanvasMode.Select;

    // Drag/AddEdge state (sẽ dùng ở TASK-04)
    private Node?  _dragNode        = null;
    private PointF _dragOffset      = PointF.Empty;
    private int    _edgeFirstNodeId = -1;

    // ─── Public Events & Properties ────────────────────────────────────

    public event Action<Graph>? GraphChanged;
    public event Action<Node>?  NodeSelected;

    public CanvasMode Mode
    {
        get => _mode;
        set { _mode = value; _edgeFirstNodeId = -1; Cursor = ModeToCursor(value); Invalidate(); }
    }

    // ─── Public API ────────────────────────────────────────────────────

    /// <summary>Bind đồ thị mới vào canvas và vẽ lại.</summary>
    public void RefreshGraph(Graph graph)
    {
        _graph       = graph;
        _currentStep = null;
        Invalidate();
    }

    /// <summary>Áp dụng một bước animation — đổi màu và Invalidate.</summary>
    public void ApplyStep(AlgorithmStep step)
    {
        _currentStep = step;
        Invalidate();
    }

    /// <summary>Xóa trạng thái animation, trả về màu mặc định.</summary>
    public void ClearStep()
    {
        _currentStep = null;
        Invalidate();
    }

    public Graph GetGraph() => _graph;

    // ─── OnPaint ───────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode       = SmoothingMode.AntiAlias;
        g.TextRenderingHint   = TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode   = InterpolationMode.HighQualityBicubic;

        DrawBackground(g);
        DrawEdges(g);
        DrawNodes(g);
        DrawModeHint(g);
    }

    // ─── Draw Background ───────────────────────────────────────────────

    private void DrawBackground(Graphics g)
    {
        g.Clear(CBackground);

        // Grid chấm nhẹ
        using var gridPen = new Pen(CGrid, 1f);
        const int spacing = 30;
        for (int x = 0; x < Width;  x += spacing)
            for (int y = 0; y < Height; y += spacing)
                g.FillEllipse(gridPen.Brush, x - 1, y - 1, 2, 2);
    }

    // ─── Draw Edges ────────────────────────────────────────────────────

    private void DrawEdges(Graphics g)
    {
        foreach (var edge in _graph.Edges)
        {
            var src = _graph.GetNode(edge.Source);
            var tgt = _graph.GetNode(edge.Target);
            if (src == null || tgt == null) continue;

            // ── Chọn màu & độ dày ──
            Color edgeColor = CEdgeDefault;
            float lineWidth = 1.8f;

            if (_currentStep != null)
            {
                if (_currentStep.HighlightEdges.Contains(edge.Id))
                {
                    // Phân biệt Augmented (Ford-Fulkerson) vs MST/Path
                    edgeColor = _currentStep.StepType == "augment_flow"
                        ? CEdgeAugmented : CEdgeMst;
                    lineWidth = 3.5f;
                }
                else if (_currentStep.RejectedEdges.Contains(edge.Id))
                {
                    edgeColor = CEdgeRejected;
                    lineWidth = 1.5f;
                }
                else if (_currentStep.ConsideredEdges.Contains(edge.Id))
                {
                    edgeColor = CEdgeConsidered;
                    lineWidth = 2.5f;
                }
            }

            using var pen = new Pen(edgeColor, lineWidth);
            pen.LineJoin    = LineJoin.Round;

            PointF from = src.Position;
            PointF to   = tgt.Position;

            // Tính điểm bắt đầu/kết thúc trên biên hình tròn
            PointF pStart = DrawingHelper.EdgeEndpoint(from, to,   NodeRadius);
            PointF pEnd   = DrawingHelper.EdgeEndpoint(to,   from, NodeRadius);

            if (_graph.Directed)
                DrawingHelper.DrawArrow(g, pen, pStart, pEnd);
            else
                g.DrawLine(pen, pStart, pEnd);

            // Label trọng số / edge label
            DrawEdgeLabel(g, edge, from, to);
        }

        // Highlight đỉnh đầu tiên khi đang AddEdge mode
        if (_edgeFirstNodeId >= 0)
        {
            var firstNode = _graph.GetNode(_edgeFirstNodeId);
            if (firstNode != null)
            {
                float x = firstNode.Position.X - NodeRadius - 4;
                float y = firstNode.Position.Y - NodeRadius - 4;
                float d = (NodeRadius + 4) * 2;
                using var hlPen = new Pen(Color.FromArgb(200, CNodeActive), 2.5f);
                hlPen.DashStyle = DashStyle.Dash;
                g.DrawEllipse(hlPen, x, y, d, d);
            }
        }
    }

    private void DrawEdgeLabel(Graphics g, Edge edge, PointF from, PointF to)
    {
        // Vị trí giữa cạnh, lệch nhẹ để không che cạnh
        float mx = (from.X + to.X) / 2f;
        float my = (from.Y + to.Y) / 2f;

        // Ưu tiên EdgeLabel từ step (Ford-Fulkerson) > Weight
        string text = string.Empty;

        if (_currentStep?.EdgeLabels.TryGetValue(edge.Id, out var stepLabel) == true)
            text = stepLabel;
        else if (edge.Weight != 1.0)
            text = edge.Weight % 1 == 0
                ? ((int)edge.Weight).ToString()
                : edge.Weight.ToString("G4");

        if (string.IsNullOrEmpty(text)) return;

        var sz = g.MeasureString(text, _weightFont);
        float rx = mx - sz.Width  / 2f - 3;
        float ry = my - sz.Height / 2f - 1;

        // Nền trắng bo góc nhẹ
        using var bgBrush = new SolidBrush(Color.FromArgb(230, 255, 255, 255));
        g.FillRectangle(bgBrush, rx, ry, sz.Width + 6, sz.Height + 2);

        using var txtBrush = new SolidBrush(Color.FromArgb(80, 80, 90));
        g.DrawString(text, _weightFont, txtBrush, rx + 3, ry + 1);
    }

    // ─── Draw Nodes ────────────────────────────────────────────────────

    private void DrawNodes(Graphics g)
    {
        foreach (var node in _graph.Nodes)
        {
            Color fill = GetNodeColor(node.Id);
            float x = node.Position.X - NodeRadius;
            float y = node.Position.Y - NodeRadius;
            float d = NodeRadius * 2;

            // Shadow
            using var shadow = new SolidBrush(Color.FromArgb(35, 0, 0, 0));
            g.FillEllipse(shadow, x + 3, y + 3, d, d);

            // Gradient fill
            using var gradBrush = new LinearGradientBrush(
                new RectangleF(x, y, d, d),
                LightenColor(fill, 0.25f),
                DarkenColor(fill, 0.15f),
                LinearGradientMode.ForwardDiagonal);
            g.FillEllipse(gradBrush, x, y, d, d);

            // Border trắng
            using var borderPen = new Pen(Color.White, 2.5f);
            g.DrawEllipse(borderPen, x, y, d, d);

            // Outer ring nếu đang drag node này
            if (_dragNode?.Id == node.Id)
            {
                using var dragPen = new Pen(Color.FromArgb(180, fill), 2f);
                g.DrawEllipse(dragPen, x - 4, y - 4, d + 8, d + 8);
            }

            // Node Label (text trắng, canh giữa)
            var lsz = g.MeasureString(node.Label, _nodeFont);
            using var lblBrush = new SolidBrush(Color.White);
            g.DrawString(node.Label, _nodeFont, lblBrush,
                node.Position.X - lsz.Width  / 2f,
                node.Position.Y - lsz.Height / 2f);

            // Secondary label từ AlgorithmStep.NodeLabels (bên dưới node)
            if (_currentStep?.NodeLabels.TryGetValue(node.Id, out var secLabel) == true
                && !string.IsNullOrEmpty(secLabel))
            {
                var ssz = g.MeasureString(secLabel, _secFont);
                float sx = node.Position.X - ssz.Width  / 2f;
                float sy = node.Position.Y + NodeRadius + 4f;

                using var secBg = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
                g.FillRectangle(secBg, sx - 2, sy - 1, ssz.Width + 4, ssz.Height + 1);

                using var secBrush = new SolidBrush(Color.FromArgb(60, 60, 70));
                g.DrawString(secLabel, _secFont, secBrush, sx, sy);
            }
        }
    }

    private Color GetNodeColor(int nodeId)
    {
        if (_currentStep == null) return CNodeDefault;
        if (_currentStep.ActiveNodes.Contains(nodeId))  return CNodeActive;
        if (_currentStep.QueueOrStack.Contains(nodeId)) return CNodeQueue;
        if (_currentStep.VisitedNodes.Contains(nodeId)) return CNodeVisited;
        return CNodeDefault;
    }

    // ─── Draw Mode Hint ────────────────────────────────────────────────

    private void DrawModeHint(Graphics g)
    {
        string hint = _mode switch
        {
            CanvasMode.AddNode => "Click để thêm đỉnh",
            CanvasMode.AddEdge => _edgeFirstNodeId < 0
                ? "Click đỉnh nguồn"
                : "Click đỉnh đích để tạo cạnh",
            CanvasMode.Delete  => "Click đỉnh/cạnh để xóa",
            _                  => string.Empty
        };

        if (string.IsNullOrEmpty(hint)) return;

        using var hintFont   = new Font("Segoe UI", 9f, FontStyle.Italic);
        using var hintBrush  = new SolidBrush(Color.FromArgb(140, 100, 100, 120));
        var sz = g.MeasureString(hint, hintFont);
        g.DrawString(hint, hintFont, hintBrush, (Width - sz.Width) / 2f, Height - sz.Height - 10);
    }

    // ─── Mouse Events (TASK-04) ────────────────────────────────────────

    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); /* TODO: TASK-04 */ }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); /* TODO: TASK-04 */ }
    protected override void OnMouseUp  (MouseEventArgs e) { base.OnMouseUp(e);   /* TODO: TASK-04 */ }

    // ─── Hit Testing ───────────────────────────────────────────────────

    public Node? HitTestNode(PointF p)
    {
        // Duyệt ngược để ưu tiên node vẽ sau (trên cùng)
        for (int i = _graph.Nodes.Count - 1; i >= 0; i--)
        {
            var n  = _graph.Nodes[i];
            float dx = n.Position.X - p.X;
            float dy = n.Position.Y - p.Y;
            if (dx * dx + dy * dy <= NodeRadius * NodeRadius)
                return n;
        }
        return null;
    }

    public Edge? HitTestEdge(PointF p)
    {
        foreach (var edge in _graph.Edges)
        {
            var src = _graph.GetNode(edge.Source);
            var tgt = _graph.GetNode(edge.Target);
            if (src == null || tgt == null) continue;

            if (DrawingHelper.DistanceToSegment(p, src.Position, tgt.Position) <= EdgeClickTol)
                return edge;
        }
        return null;
    }

    // ─── Color Helpers ─────────────────────────────────────────────────

    private static Color LightenColor(Color c, float amount)
    {
        float r = Math.Min(1f, c.R / 255f + amount);
        float g = Math.Min(1f, c.G / 255f + amount);
        float b = Math.Min(1f, c.B / 255f + amount);
        return Color.FromArgb(c.A, (int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    private static Color DarkenColor(Color c, float amount)
    {
        float r = Math.Max(0f, c.R / 255f - amount);
        float g = Math.Max(0f, c.G / 255f - amount);
        float b = Math.Max(0f, c.B / 255f - amount);
        return Color.FromArgb(c.A, (int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    private static Cursor ModeToCursor(CanvasMode mode) => mode switch
    {
        CanvasMode.AddNode => Cursors.Cross,
        CanvasMode.Delete  => Cursors.No,
        _                  => Cursors.Default
    };

    // ─── Constructor & Dispose ─────────────────────────────────────────

    public GraphCanvas()
    {
        DoubleBuffered = true;
        BackColor      = CBackground;
        BorderStyle    = BorderStyle.None;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _nodeFont.Dispose();
            _weightFont.Dispose();
            _secFont.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>Chế độ tương tác của canvas.</summary>
public enum CanvasMode { Select, AddNode, AddEdge, Delete }

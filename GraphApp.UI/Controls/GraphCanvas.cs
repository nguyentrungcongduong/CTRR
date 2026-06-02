using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;
using GraphApp.UI.Forms;
using GraphApp.UI.Helpers;

namespace GraphApp.UI.Controls;

/// <summary>
/// UserControl vẽ đồ thị bằng GDI+.
/// Xử lý mouse để thêm/xóa đỉnh/cạnh và kéo thả node.
/// </summary>
public class GraphCanvas : UserControl
{
    // ─── Constants ─────────────────────────────────────────────────────
    public  const float NodeRadius   = 22f;
    private const float EdgeClickTol = 6f;

    // ─── Color palette (theo ARCHITECTURE.md) ──────────────────────────
    private static readonly Color CNodeDefault    = ColorTranslator.FromHtml("#4A90D9");
    private static readonly Color CNodeVisited    = ColorTranslator.FromHtml("#27AE60");
    private static readonly Color CNodeActive     = ColorTranslator.FromHtml("#E74C3C");
    private static readonly Color CNodeQueue      = ColorTranslator.FromHtml("#F39C12");
    private static readonly Color CEdgeDefault    = Color.FromArgb(160, 160, 165);
    private static readonly Color CEdgeMst        = ColorTranslator.FromHtml("#8E44AD");
    private static readonly Color CEdgeAugmented  = ColorTranslator.FromHtml("#E67E22");
    private static readonly Color CEdgeRejected   = Color.FromArgb(230, 190, 190);
    private static readonly Color CEdgeConsidered = Color.FromArgb(235, 205, 0);
    private static readonly Color CBackground     = Color.FromArgb(245, 246, 250);
    private static readonly Color CGrid           = Color.FromArgb(232, 232, 238);

    // ─── Fonts ─────────────────────────────────────────────────────────
    private readonly Font _nodeFont   = new("Segoe UI", 10f, FontStyle.Bold);
    private readonly Font _weightFont = new("Segoe UI", 7.5f);
    private readonly Font _secFont    = new("Segoe UI", 7.5f);

    // ─── State ─────────────────────────────────────────────────────────
    private Graph          _graph       = new();
    private AlgorithmStep? _currentStep;
    private CanvasMode     _mode        = CanvasMode.Select;

    // Drag state (Select mode)
    private Node?  _dragNode   = null;
    private PointF _dragOffset = PointF.Empty;

    // AddEdge state
    private int _edgeFirstNodeId = -1;

    // ─── Events ────────────────────────────────────────────────────────
    /// <summary>Kích hoạt sau mỗi thao tác thay đổi đồ thị (thêm/xóa node/edge).</summary>
    public event Action<Graph>? GraphChanged;

    // ─── Properties ────────────────────────────────────────────────────
    public CanvasMode Mode
    {
        get => _mode;
        set
        {
            _mode            = value;
            _edgeFirstNodeId = -1;
            _dragNode        = null;
            Cursor           = ModeToCursor(value);
            Invalidate();
        }
    }

    // ─── Public API ────────────────────────────────────────────────────

    /// <summary>Bind đồ thị mới, xóa animation state.</summary>
    public void RefreshGraph(Graph graph)
    {
        _graph       = graph;
        _currentStep = null;
        Invalidate();
    }

    /// <summary>Áp dụng bước animation: đổi màu node/edge.</summary>
    public void ApplyStep(AlgorithmStep step)
    {
        _currentStep = step;
        Invalidate();
    }

    /// <summary>Xóa animation, trả về màu mặc định.</summary>
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
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        DrawBackground(g);
        DrawEdges(g);
        DrawNodes(g);
        DrawModeHint(g);
    }

    // ─── Background ────────────────────────────────────────────────────

    private void DrawBackground(Graphics g)
    {
        g.Clear(CBackground);
        const int spacing = 30;
        using var dotBrush = new SolidBrush(CGrid);
        for (int x = spacing; x < Width;  x += spacing)
        for (int y = spacing; y < Height; y += spacing)
            g.FillEllipse(dotBrush, x - 1, y - 1, 2.5f, 2.5f);
    }

    // ─── Draw Edges ────────────────────────────────────────────────────

    private void DrawEdges(Graphics g)
    {
        foreach (var edge in _graph.Edges)
        {
            var src = _graph.GetNode(edge.Source);
            var tgt = _graph.GetNode(edge.Target);
            if (src == null || tgt == null) continue;

            (Color color, float width) = GetEdgeStyle(edge.Id);
            using var pen = new Pen(color, width) { LineJoin = LineJoin.Round };

            PointF pStart = DrawingHelper.EdgeEndpoint(src.Position, tgt.Position, NodeRadius);
            PointF pEnd   = DrawingHelper.EdgeEndpoint(tgt.Position, src.Position, NodeRadius);

            if (_graph.Directed)
                DrawingHelper.DrawArrow(g, pen, pStart, pEnd);
            else
                g.DrawLine(pen, pStart, pEnd);

            DrawEdgeLabel(g, edge, src.Position, tgt.Position);
        }

        // Highlight node đầu tiên khi đang chọn AddEdge
        if (_edgeFirstNodeId >= 0)
        {
            var fn = _graph.GetNode(_edgeFirstNodeId);
            if (fn != null)
            {
                float x = fn.Position.X - NodeRadius - 5;
                float y = fn.Position.Y - NodeRadius - 5;
                float d = (NodeRadius + 5) * 2;
                using var hlPen = new Pen(Color.FromArgb(200, CNodeActive), 2.5f)
                    { DashStyle = DashStyle.Dash };
                g.DrawEllipse(hlPen, x, y, d, d);
            }
        }
    }

    private (Color color, float width) GetEdgeStyle(int edgeId)
    {
        if (_currentStep == null) return (CEdgeDefault, 1.8f);
        if (_currentStep.HighlightEdges.Contains(edgeId))
        {
            bool augmented = _currentStep.StepType == "augment_flow";
            return (augmented ? CEdgeAugmented : CEdgeMst, 3.5f);
        }
        if (_currentStep.RejectedEdges.Contains(edgeId))   return (CEdgeRejected,   1.5f);
        if (_currentStep.ConsideredEdges.Contains(edgeId)) return (CEdgeConsidered, 2.5f);
        return (CEdgeDefault, 1.8f);
    }

    private void DrawEdgeLabel(Graphics g, Edge edge, PointF from, PointF to)
    {
        float mx = (from.X + to.X) / 2f;
        float my = (from.Y + to.Y) / 2f;

        string text = string.Empty;
        if (_currentStep?.EdgeLabels.TryGetValue(edge.Id, out var sl) == true)
            text = sl;
        else if (edge.Weight != 1.0)
            text = edge.Weight % 1 == 0
                ? ((int)edge.Weight).ToString()
                : edge.Weight.ToString("G4");

        if (string.IsNullOrEmpty(text)) return;

        var sz = g.MeasureString(text, _weightFont);
        float rx = mx - sz.Width  / 2f - 3;
        float ry = my - sz.Height / 2f - 1;

        using var bgBrush  = new SolidBrush(Color.FromArgb(225, 255, 255, 255));
        using var txtBrush = new SolidBrush(Color.FromArgb(80, 80, 90));
        g.FillRectangle(bgBrush, rx, ry, sz.Width + 6, sz.Height + 2);
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
            using var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            g.FillEllipse(shadow, x + 3, y + 3, d, d);

            // Gradient fill
            using var grad = new LinearGradientBrush(
                new RectangleF(x - 1, y - 1, d + 2, d + 2),
                LightenColor(fill, 0.25f),
                DarkenColor(fill, 0.15f),
                LinearGradientMode.ForwardDiagonal);
            g.FillEllipse(grad, x, y, d, d);

            // White border
            using var border = new Pen(Color.White, 2.5f);
            g.DrawEllipse(border, x, y, d, d);

            // Ring khi đang kéo
            if (_dragNode?.Id == node.Id)
            {
                using var ring = new Pen(Color.FromArgb(160, fill), 2f);
                g.DrawEllipse(ring, x - 4, y - 4, d + 8, d + 8);
            }

            // Label node
            var lsz = g.MeasureString(node.Label, _nodeFont);
            using var lbl = new SolidBrush(Color.White);
            g.DrawString(node.Label, _nodeFont, lbl,
                node.Position.X - lsz.Width  / 2f,
                node.Position.Y - lsz.Height / 2f);

            // Secondary label (AlgorithmStep.NodeLabels)
            if (_currentStep?.NodeLabels.TryGetValue(node.Id, out var sec) == true
                && !string.IsNullOrEmpty(sec))
            {
                var ssz = g.MeasureString(sec, _secFont);
                float sx = node.Position.X - ssz.Width / 2f;
                float sy = node.Position.Y + NodeRadius + 4f;
                using var secBg  = new SolidBrush(Color.FromArgb(210, 255, 255, 255));
                using var secTxt = new SolidBrush(Color.FromArgb(60, 60, 70));
                g.FillRectangle(secBg, sx - 2, sy - 1, ssz.Width + 4, ssz.Height + 1);
                g.DrawString(sec, _secFont, secTxt, sx, sy);
            }
        }
    }

    private Color GetNodeColor(int id)
    {
        if (_currentStep == null) return CNodeDefault;
        if (_currentStep.ActiveNodes.Contains(id))  return CNodeActive;
        if (_currentStep.QueueOrStack.Contains(id)) return CNodeQueue;
        if (_currentStep.VisitedNodes.Contains(id)) return CNodeVisited;
        return CNodeDefault;
    }

    // ─── Mode Hint ─────────────────────────────────────────────────────

    private void DrawModeHint(Graphics g)
    {
        string hint = _mode switch
        {
            CanvasMode.AddNode => "✚  Click vào canvas để thêm đỉnh",
            CanvasMode.AddEdge => _edgeFirstNodeId < 0
                ? "→  Click đỉnh nguồn"
                : "→  Đã chọn đỉnh nguồn — Click đỉnh đích",
            CanvasMode.Delete  => "✕  Click đỉnh hoặc cạnh để xóa",
            _                  => string.Empty
        };
        if (string.IsNullOrEmpty(hint)) return;

        using var f = new Font("Segoe UI", 9f, FontStyle.Italic);
        using var b = new SolidBrush(Color.FromArgb(130, 100, 100, 120));
        var sz = g.MeasureString(hint, f);
        g.DrawString(hint, f, b, (Width - sz.Width) / 2f, Height - sz.Height - 12);
    }

    // ─── Mouse Events (TASK-04) ────────────────────────────────────────

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        var clickPt = new PointF(e.X, e.Y);

        switch (_mode)
        {
            case CanvasMode.AddNode:
                HandleAddNode(clickPt);
                break;

            case CanvasMode.AddEdge:
                HandleAddEdge(clickPt);
                break;

            case CanvasMode.Delete:
                HandleDelete(clickPt);
                break;

            case CanvasMode.Select:
                HandleSelectDown(clickPt);
                break;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_mode != CanvasMode.Select || _dragNode == null) return;

        // Cập nhật vị trí node khi kéo
        _dragNode.Position = new PointF(
            e.X - _dragOffset.X,
            e.Y - _dragOffset.Y);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_dragNode == null) return;

        // Kết thúc drag: fire event
        _dragNode = null;
        GraphChanged?.Invoke(_graph);
        Invalidate();
    }

    // ─── Mode Handlers ─────────────────────────────────────────────────

    private void HandleAddNode(PointF pt)
    {
        // Không thêm nếu click trúng node đã có
        if (HitTestNode(pt) != null) return;

        _graph.AddNode(pt.X, pt.Y);
        GraphChanged?.Invoke(_graph);
        Invalidate();
    }

    private void HandleAddEdge(PointF pt)
    {
        var hitNode = HitTestNode(pt);
        if (hitNode == null) return;

        if (_edgeFirstNodeId < 0)
        {
            // Chọn node đầu tiên
            _edgeFirstNodeId = hitNode.Id;
            Invalidate();
        }
        else
        {
            // Chọn node thứ hai
            int secondId = hitNode.Id;

            if (secondId == _edgeFirstNodeId)
            {
                // Click cùng node → hủy
                _edgeFirstNodeId = -1;
                Invalidate();
                return;
            }

            // Kiểm tra cạnh đã tồn tại chưa
            if (_graph.EdgeExists(_edgeFirstNodeId, secondId))
            {
                MessageBox.Show("Cạnh này đã tồn tại!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _edgeFirstNodeId = -1;
                Invalidate();
                return;
            }

            // Mở dialog nhập trọng số
            using var dlg = new EdgeWeightDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _graph.AddEdge(_edgeFirstNodeId, secondId, dlg.Weight);
                GraphChanged?.Invoke(_graph);
            }

            _edgeFirstNodeId = -1;
            Invalidate();
        }
    }

    private void HandleDelete(PointF pt)
    {
        // Ưu tiên xóa node trước
        var node = HitTestNode(pt);
        if (node != null)
        {
            var result = MessageBox.Show(
                $"Xóa đỉnh \"{node.Label}\" và tất cả cạnh liên quan?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _graph.RemoveNode(node.Id);
                GraphChanged?.Invoke(_graph);
                Invalidate();
            }
            return;
        }

        // Nếu không trúng node thì thử xóa edge
        var edge = HitTestEdge(pt);
        if (edge != null)
        {
            var srcNode = _graph.GetNode(edge.Source);
            var tgtNode = _graph.GetNode(edge.Target);
            string label = $"{srcNode?.Label} — {tgtNode?.Label} (w={edge.Weight})";

            var result = MessageBox.Show(
                $"Xóa cạnh {label}?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _graph.RemoveEdge(edge.Id);
                GraphChanged?.Invoke(_graph);
                Invalidate();
            }
        }
    }

    private void HandleSelectDown(PointF pt)
    {
        var node = HitTestNode(pt);
        if (node == null) return;

        _dragNode   = node;
        _dragOffset = new PointF(pt.X - node.Position.X, pt.Y - node.Position.Y);
    }

    // ─── Hit Testing ───────────────────────────────────────────────────

    /// <summary>Trả về node tại điểm p (ưu tiên node vẽ sau = trên cùng).</summary>
    public Node? HitTestNode(PointF p)
    {
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

    /// <summary>Trả về edge tại điểm p (khoảng cách ≤ EdgeClickTol).</summary>
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

    // ─── Color helpers ─────────────────────────────────────────────────

    private static Color LightenColor(Color c, float amt) => Color.FromArgb(c.A,
        (int)Math.Min(255, c.R + 255 * amt),
        (int)Math.Min(255, c.G + 255 * amt),
        (int)Math.Min(255, c.B + 255 * amt));

    private static Color DarkenColor(Color c, float amt) => Color.FromArgb(c.A,
        (int)Math.Max(0, c.R - 255 * amt),
        (int)Math.Max(0, c.G - 255 * amt),
        (int)Math.Max(0, c.B - 255 * amt));

    private static Cursor ModeToCursor(CanvasMode m) => m switch
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
        if (disposing) { _nodeFont.Dispose(); _weightFont.Dispose(); _secFont.Dispose(); }
        base.Dispose(disposing);
    }
}

/// <summary>Chế độ tương tác của canvas.</summary>
public enum CanvasMode { Select, AddNode, AddEdge, Delete }

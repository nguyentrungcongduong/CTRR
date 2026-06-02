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

    // ─── State ─────────────────────────────────────────────────
    private Graph          _graph       = new();
    private AlgorithmStep? _currentStep;
    private CanvasMode     _mode        = CanvasMode.Select;

    // ── Zoom + Pan ─────────────────────────────────────
    private const float MinZoom   = 0.15f;
    private const float MaxZoom   = 5.00f;
    private float  _zoom          = 1.0f;
    private PointF _panOffset     = PointF.Empty;

    // Middle-click pan state
    private bool   _isPanning       = false;
    private PointF _panStart        = PointF.Empty;
    private PointF _panOffsetStart  = PointF.Empty;

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

    // ── Zoom / Pan API ────────────────────────────────────

    /// <summary>
    /// Zoom + pan để toàn bộ đồ thị vừa khớp canvas (có margin).
    /// </summary>
    public void FitToScreen()
    {
        if (_graph.Nodes.Count == 0) { ResetView(); return; }

        float minX = _graph.Nodes.Min(n => n.Position.X) - NodeRadius * 2;
        float maxX = _graph.Nodes.Max(n => n.Position.X) + NodeRadius * 2;
        float minY = _graph.Nodes.Min(n => n.Position.Y) - NodeRadius * 2;
        float maxY = _graph.Nodes.Max(n => n.Position.Y) + NodeRadius * 2;

        float gw = maxX - minX;
        float gh = maxY - minY;
        if (gw < 1) gw = 1;
        if (gh < 1) gh = 1;

        float margin = 50f;
        float scaleX = (Width  - 2 * margin) / gw;
        float scaleY = (Height - 2 * margin) / gh;
        _zoom = Math.Clamp(Math.Min(scaleX, scaleY), MinZoom, MaxZoom);

        float cx = (minX + maxX) / 2f;
        float cy = (minY + maxY) / 2f;
        _panOffset = new PointF(Width / 2f - cx * _zoom, Height / 2f - cy * _zoom);

        Invalidate();
    }

    /// <summary>Reset zoom = 1, pan = 0.</summary>
    public void ResetView()
    {
        _zoom      = 1.0f;
        _panOffset = PointF.Empty;
        Invalidate();
    }

    /// <summary>
    /// Xuất canvas hiện tại ra Bitmap (không có UI hints, chỉ đồ thị).
    /// scaleFactor = 2 → xuất 2× độ phân giải màn hình.
    /// </summary>
    public Bitmap ExportToBitmap(int scaleFactor = 2)
    {
        int w = Math.Max(1, Width  * scaleFactor);
        int h = Math.Max(1, Height * scaleFactor);
        var bmp = new Bitmap(w, h);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.ScaleTransform(scaleFactor, scaleFactor);

        // Background (screen-space, nhân scaleFactor)
        DrawBackground(g);

        // Đồ thị (world-space)
        g.TranslateTransform(_panOffset.X, _panOffset.Y);
        g.ScaleTransform(_zoom, _zoom);
        DrawEdges(g);
        DrawNodes(g);

        return bmp;
    }

    // ─── OnPaint ───────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        // ── Nền (screen space) ──
        DrawBackground(g);

        // ── World-space: áp dụng zoom + pan ──
        g.TranslateTransform(_panOffset.X, _panOffset.Y);
        g.ScaleTransform(_zoom, _zoom);

        DrawEdges(g);
        DrawNodes(g);

        // ── Screen-space hints (sau khi reset transform) ──
        g.ResetTransform();
        DrawModeHint(g);
        DrawZoomIndicator(g);
    }

    // ─── Background ────────────────────────────────────────────────────

    private void DrawBackground(Graphics g)
    {
        g.Clear(CBackground);

        // Grid điểm chấm — offset theo pan (không scale theo zoom để giữ đơn giản)
        const int spacing = 30;
        float ox = _panOffset.X % spacing;
        float oy = _panOffset.Y % spacing;
        if (ox < 0) ox += spacing;
        if (oy < 0) oy += spacing;

        using var dotBrush = new SolidBrush(CGrid);
        for (float x = ox; x < Width;  x += spacing)
        for (float y = oy; y < Height; y += spacing)
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

        // Offset label perpendicular to edge để tránh chồng lên cạnh
        float dx = to.X - from.X, dy = to.Y - from.Y;
        float len = (float)Math.Sqrt(dx * dx + dy * dy);
        if (len > 0) { float nx = -dy / len * 12f; float ny = dx / len * 12f; mx += nx; my += ny; }

        bool isFlowLabel = false;
        string text = string.Empty;

        if (_currentStep?.EdgeLabels.TryGetValue(edge.Id, out var sl) == true)
        {
            text = sl;
            isFlowLabel = text.Contains('/');   // "flow/capacity" format
        }
        else if (edge.Weight != 1.0)
        {
            text = edge.Weight % 1 == 0
                ? ((int)edge.Weight).ToString()
                : edge.Weight.ToString("G4");
        }

        if (string.IsNullOrEmpty(text)) return;

        var sz = g.MeasureString(text, _weightFont);
        float rx = mx - sz.Width  / 2f - 4;
        float ry = my - sz.Height / 2f - 2;

        // Flow labels: fondo arancione se flow > 0, altrimenti bianco
        Color bgColor  = Color.FromArgb(230, 255, 255, 255);
        Color txtColor = Color.FromArgb(70, 70, 80);

        if (isFlowLabel)
        {
            var parts = text.Split('/');
            bool hasFlow = parts.Length == 2 && double.TryParse(parts[0], out double f) && f > 0;
            bgColor  = hasFlow
                ? Color.FromArgb(230, 255, 165, 50)   // arancione per flow attivo
                : Color.FromArgb(220, 240, 240, 255);  // blu chiaro per flow = 0
            txtColor = hasFlow ? Color.FromArgb(120, 50, 0) : Color.FromArgb(50, 50, 120);
        }

        using var bgBrush  = new SolidBrush(bgColor);
        using var txtBrush = new SolidBrush(txtColor);
        g.FillRectangle(bgBrush, rx, ry, sz.Width + 8, sz.Height + 4);
        g.DrawString(text, _weightFont, txtBrush, rx + 4, ry + 2);
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

    // ─── Coordinate Conversion ──────────────────────────────────────

    /// <summary>Chuyển điểm từ screen (pixel) → world (tọa độ đồ thị).</summary>
    private PointF ScreenToWorld(PointF screen) =>
        new((screen.X - _panOffset.X) / _zoom,
            (screen.Y - _panOffset.Y) / _zoom);

    // ─── Mouse Events ────────────────────────────────────────────

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        // ── Middle-click: start pan ──
        if (e.Button == MouseButtons.Middle)
        {
            _isPanning      = true;
            _panStart       = new PointF(e.X, e.Y);
            _panOffsetStart = _panOffset;
            Cursor          = Cursors.SizeAll;
            return;
        }

        if (e.Button != MouseButtons.Left) return;

        var worldPt = ScreenToWorld(new PointF(e.X, e.Y));

        switch (_mode)
        {
            case CanvasMode.AddNode:  HandleAddNode(worldPt);   break;
            case CanvasMode.AddEdge:  HandleAddEdge(worldPt);   break;
            case CanvasMode.Delete:   HandleDelete(worldPt);    break;
            case CanvasMode.Select:   HandleSelectDown(worldPt); break;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        // ── Pan (middle button) ──
        if (_isPanning)
        {
            _panOffset = new PointF(
                _panOffsetStart.X + e.X - _panStart.X,
                _panOffsetStart.Y + e.Y - _panStart.Y);
            Invalidate();
            return;
        }

        // ── Drag node (left button, Select mode) ──
        if (_mode != CanvasMode.Select || _dragNode == null) return;
        var world = ScreenToWorld(new PointF(e.X, e.Y));
        _dragNode.Position = new PointF(world.X - _dragOffset.X, world.Y - _dragOffset.Y);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        // ── End pan ──
        if (e.Button == MouseButtons.Middle)
        {
            _isPanning = false;
            Cursor     = ModeToCursor(_mode);
            return;
        }

        if (_dragNode == null) return;
        _dragNode = null;
        GraphChanged?.Invoke(_graph);
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        float factor  = e.Delta > 0 ? 1.15f : 1f / 1.15f;
        float newZoom = Math.Clamp(_zoom * factor, MinZoom, MaxZoom);
        if (Math.Abs(newZoom - _zoom) < 0.001f) return;

        // Zoom quanh con trỏ chuột: giữ world point dưới cursor cố định
        float wx = (e.X - _panOffset.X) / _zoom;
        float wy = (e.Y - _panOffset.Y) / _zoom;
        _zoom      = newZoom;
        _panOffset = new PointF(e.X - wx * _zoom, e.Y - wy * _zoom);

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

    private void HandleSelectDown(PointF worldPt)
    {
        var node = HitTestNode(worldPt);
        if (node == null) return;

        _dragNode   = node;
        _dragOffset = new PointF(worldPt.X - node.Position.X, worldPt.Y - node.Position.Y);
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

    // ─── Zoom Indicator ───────────────────────────────────────────

    private void DrawZoomIndicator(Graphics g)
    {
        if (Math.Abs(_zoom - 1.0f) < 0.02f) return;   // nối bật khi zoom ≠ 100%

        string txt = $"🔍 {_zoom * 100:F0}%";
        using var f = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        var sz = g.MeasureString(txt, f);
        float rx = Width  - sz.Width  - 14;
        float ry = Height - sz.Height - 14;

        using var bg  = new SolidBrush(Color.FromArgb(180, 40, 44, 52));
        using var fg  = new SolidBrush(Color.FromArgb(220, 200, 220, 255));
        g.FillRectangle(bg, rx - 4, ry - 2, sz.Width + 12, sz.Height + 6);
        g.DrawString(txt, f, fg, rx + 2, ry + 1);
    }

    // ─── Constructor & Dispose ────────────────────────────────────

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

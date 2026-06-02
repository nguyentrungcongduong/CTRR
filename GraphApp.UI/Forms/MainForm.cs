using System.Drawing;
using System.Windows.Forms;
using GraphApp.Core.Models;
using GraphApp.UI.Controls;

namespace GraphApp.UI.Forms;

/// <summary>
/// Form chính của ứng dụng GraphApp.
/// Layout: ToolStrip (trên) | Canvas (giữa, full) | StatusStrip (dưới).
/// </summary>
public partial class MainForm : Form
{
    // ─── Controls ──────────────────────────────────────────────────────
    private readonly GraphCanvas    _canvas;
    private readonly ToolStrip      _toolbar;
    private readonly StatusStrip    _statusBar;

    // Toolbar buttons (mode)
    private readonly ToolStripButton _btnSelect;
    private readonly ToolStripButton _btnAddNode;
    private readonly ToolStripButton _btnAddEdge;
    private readonly ToolStripButton _btnDelete;

    // Toolbar buttons (actions)
    private readonly ToolStripButton      _btnDirected;
    private readonly ToolStripButton      _btnClear;
    private readonly ToolStripButton      _btnNewGraph;

    // Status labels
    private readonly ToolStripStatusLabel _lblNodes;
    private readonly ToolStripStatusLabel _lblEdges;
    private readonly ToolStripStatusLabel _lblMode;
    private readonly ToolStripStatusLabel _lblDirected;

    public MainForm()
    {
        InitializeComponent();

        // ── Khởi tạo controls ───────────────────────────────────────
        _canvas    = BuildCanvas();
        _toolbar   = BuildToolbar(out _btnSelect, out _btnAddNode, out _btnAddEdge,
                                  out _btnDelete, out _btnDirected, out _btnClear, out _btnNewGraph);
        _statusBar = BuildStatusBar(out _lblNodes, out _lblEdges, out _lblMode, out _lblDirected);

        Controls.Add(_canvas);
        Controls.Add(_toolbar);
        Controls.Add(_statusBar);

        // ── Cài đặt Form ────────────────────────────────────────────
        Text          = "GraphApp — Ứng dụng Đồ thị";
        MinimumSize   = new Size(900, 650);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState   = FormWindowState.Maximized;
        BackColor     = Color.FromArgb(245, 246, 250);

        // ── Đồ thị mẫu ──────────────────────────────────────────────
        LoadSampleGraph();
        UpdateStatus();
    }

    // ─── Build Canvas ──────────────────────────────────────────────────

    private GraphCanvas BuildCanvas()
    {
        var canvas = new GraphCanvas { Dock = DockStyle.Fill };
        canvas.GraphChanged += _ => UpdateStatus();
        return canvas;
    }

    // ─── Build Toolbar ─────────────────────────────────────────────────

    private ToolStrip BuildToolbar(
        out ToolStripButton btnSelect,
        out ToolStripButton btnAddNode,
        out ToolStripButton btnAddEdge,
        out ToolStripButton btnDelete,
        out ToolStripButton btnDirected,
        out ToolStripButton btnClear,
        out ToolStripButton btnNewGraph)
    {
        var ts = new ToolStrip
        {
            Dock         = DockStyle.Top,
            GripStyle    = ToolStripGripStyle.Hidden,
            BackColor    = Color.FromArgb(40, 44, 52),
            ForeColor    = Color.White,
            RenderMode   = ToolStripRenderMode.Professional,
            ImageScalingSize = new Size(20, 20),
            Padding      = new Padding(6, 4, 6, 4),
            Height       = 44
        };

        // Chế độ tương tác
        btnSelect  = MakeModeButton("↖  Chọn",      "Chọn / kéo thả đỉnh (Select)",    CanvasMode.Select);
        btnAddNode = MakeModeButton("⊕  Thêm đỉnh", "Click để thêm đỉnh (AddNode)",    CanvasMode.AddNode);
        btnAddEdge = MakeModeButton("→  Thêm cạnh", "Click 2 đỉnh để thêm cạnh",       CanvasMode.AddEdge);
        btnDelete  = MakeModeButton("✕  Xóa",       "Click đỉnh/cạnh để xóa (Delete)", CanvasMode.Delete);

        // Nút hành động
        btnDirected = MakeActionButton("⇄  Có hướng", "Bật/tắt đồ thị có hướng");
        btnDirected.CheckOnClick = true;
        btnDirected.CheckedChanged += (_, _) =>
        {
            var g = _canvas.GetGraph();
            if (g.Edges.Count > 0)
            {
                string msg = btnDirected.Checked
                    ? "Chuyển sang đồ thị có hướng. Giữ nguyên các cạnh hiện tại?"
                    : "Chuyển sang đồ thị vô hướng. Giữ nguyên các cạnh hiện tại?";
                MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            g.Directed = btnDirected.Checked;
            btnDirected.ForeColor = btnDirected.Checked ? Color.FromArgb(255, 180, 50) : Color.White;
            _canvas.Invalidate();
            UpdateStatus();
        };

        btnClear = MakeActionButton("🗑  Xóa tất cả", "Xóa toàn bộ đồ thị");
        btnClear.Click += (_, _) =>
        {
            if (MessageBox.Show("Xóa toàn bộ đồ thị?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _canvas.GetGraph().Clear();
                _canvas.ClearStep();
                UpdateStatus();
            }
        };

        btnNewGraph = MakeActionButton("★  Đồ thị mẫu", "Tải lại đồ thị mẫu");
        btnNewGraph.Click += (_, _) => LoadSampleGraph();

        ts.Items.Add(new ToolStripLabel("  CHẾ ĐỘ:  ")
            { ForeColor = Color.FromArgb(180, 180, 190), Font = new Font("Segoe UI", 8f) });
        ts.Items.Add(btnSelect);
        ts.Items.Add(btnAddNode);
        ts.Items.Add(btnAddEdge);
        ts.Items.Add(btnDelete);
        ts.Items.Add(new ToolStripSeparator());
        ts.Items.Add(btnDirected);
        ts.Items.Add(new ToolStripSeparator());
        ts.Items.Add(btnNewGraph);
        ts.Items.Add(btnClear);

        // Chọn Select mặc định
        SelectModeButton(btnSelect);
        return ts;
    }

    private ToolStripButton MakeModeButton(string text, string tooltip, CanvasMode mode)
    {
        var btn = new ToolStripButton(text)
        {
            ToolTipText  = tooltip,
            CheckOnClick = false,
            AutoSize     = true,
            ForeColor    = Color.White,
            Font         = new Font("Segoe UI", 9f),
            Padding      = new Padding(8, 2, 8, 2),
            Margin       = new Padding(2, 0, 2, 0),
        };
        btn.Click += (_, _) =>
        {
            _canvas.Mode = mode;
            SelectModeButton(btn);
            UpdateStatus();
        };
        return btn;
    }

    private static ToolStripButton MakeActionButton(string text, string tooltip)
    {
        return new ToolStripButton(text)
        {
            ToolTipText = tooltip,
            AutoSize    = true,
            ForeColor   = Color.White,
            Font        = new Font("Segoe UI", 9f),
            Padding     = new Padding(8, 2, 8, 2),
            Margin      = new Padding(2, 0, 2, 0),
        };
    }

    private void SelectModeButton(ToolStripButton active)
    {
        ToolStripButton[] modeButtons = [_btnSelect, _btnAddNode, _btnAddEdge, _btnDelete];
        foreach (var b in modeButtons)
        {
            b.Checked    = b == active;
            b.BackColor  = b == active
                ? Color.FromArgb(70, 130, 210)
                : Color.Transparent;
        }
    }

    // ─── Build StatusBar ───────────────────────────────────────────────

    private static StatusStrip BuildStatusBar(
        out ToolStripStatusLabel lblNodes,
        out ToolStripStatusLabel lblEdges,
        out ToolStripStatusLabel lblMode,
        out ToolStripStatusLabel lblDirected)
    {
        var ss = new StatusStrip
        {
            BackColor  = Color.FromArgb(40, 44, 52),
            ForeColor  = Color.White,
            SizingGrip = false
        };

        lblNodes    = new ToolStripStatusLabel("Đỉnh: 0")    { ForeColor = Color.FromArgb(180, 220, 255) };
        lblEdges    = new ToolStripStatusLabel("Cạnh: 0")    { ForeColor = Color.FromArgb(180, 220, 255) };
        lblDirected = new ToolStripStatusLabel("Vô hướng")   { ForeColor = Color.FromArgb(200, 200, 200) };
        lblMode     = new ToolStripStatusLabel("Chế độ: Chọn") { ForeColor = Color.FromArgb(180, 255, 180), Spring = true, TextAlign = ContentAlignment.MiddleRight };

        ss.Items.Add(new ToolStripStatusLabel("  "));
        ss.Items.Add(lblNodes);
        ss.Items.Add(new ToolStripStatusLabel(" | ") { ForeColor = Color.FromArgb(80, 80, 90) });
        ss.Items.Add(lblEdges);
        ss.Items.Add(new ToolStripStatusLabel(" | ") { ForeColor = Color.FromArgb(80, 80, 90) });
        ss.Items.Add(lblDirected);
        ss.Items.Add(lblMode);
        ss.Items.Add(new ToolStripStatusLabel("  "));

        return ss;
    }

    // ─── Update Status ─────────────────────────────────────────────────

    private void UpdateStatus()
    {
        var g = _canvas.GetGraph();
        _lblNodes.Text    = $"Đỉnh: {g.Nodes.Count}";
        _lblEdges.Text    = $"Cạnh: {g.Edges.Count}";
        _lblDirected.Text = g.Directed ? "Có hướng" : "Vô hướng";
        _lblDirected.ForeColor = g.Directed
            ? Color.FromArgb(255, 200, 80)
            : Color.FromArgb(200, 200, 200);

        string modeName = _canvas.Mode switch
        {
            CanvasMode.AddNode => "Thêm đỉnh",
            CanvasMode.AddEdge => "Thêm cạnh",
            CanvasMode.Delete  => "Xóa",
            _                  => "Chọn / Kéo thả",
        };
        _lblMode.Text = $"Chế độ: {modeName}   ";
    }

    // ─── Sample Graph ──────────────────────────────────────────────────

    private void LoadSampleGraph()
    {
        var g = new Graph { Directed = false };

        int a = g.AddNode(200, 150);   // A
        int b = g.AddNode(420, 100);   // B
        int c = g.AddNode(640, 150);   // C
        int d = g.AddNode(200, 360);   // D
        int e = g.AddNode(420, 320);   // E
        int f = g.AddNode(640, 360);   // F

        g.AddEdge(a, b, 4);
        g.AddEdge(a, d, 2);
        g.AddEdge(b, c, 5);
        g.AddEdge(b, e, 3);
        g.AddEdge(c, f, 1);
        g.AddEdge(d, e, 6);
        g.AddEdge(e, f, 7);

        _btnDirected.Checked   = false;
        _btnDirected.ForeColor = Color.White;
        _canvas.RefreshGraph(g);
        UpdateStatus();
    }
}

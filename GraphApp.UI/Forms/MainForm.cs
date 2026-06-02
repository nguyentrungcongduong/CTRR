using System.Drawing;
using System.Windows.Forms;
using GraphApp.Core.Algorithms.Traversal;
using GraphApp.Core.Models;
using GraphApp.UI.Controls;

namespace GraphApp.UI.Forms;

/// <summary>
/// Form chính của ứng dụng GraphApp.
/// Layout (top→bottom): Toolbar | Canvas | AnimPanel | StatusBar
/// </summary>
public partial class MainForm : Form
{
    // ─── Core Controls ─────────────────────────────────────────────────
    private readonly GraphCanvas    _canvas;
    private readonly ToolStrip      _toolbar;
    private readonly StatusStrip    _statusBar;
    private readonly Panel          _animPanel;
    private readonly AnimationEngine _engine = new();

    // Toolbar mode buttons
    private readonly ToolStripButton _btnSelect;
    private readonly ToolStripButton _btnAddNode;
    private readonly ToolStripButton _btnAddEdge;
    private readonly ToolStripButton _btnDelete;
    private readonly ToolStripButton _btnDirected;

    // Status labels
    private readonly ToolStripStatusLabel _lblNodes;
    private readonly ToolStripStatusLabel _lblEdges;
    private readonly ToolStripStatusLabel _lblMode;
    private readonly ToolStripStatusLabel _lblDirected;

    // AnimPanel controls
    private ComboBox        _cmbAlgorithm  = null!;
    private ComboBox        _cmbStartNode  = null!;
    private Button          _btnRun        = null!;
    private Label           _lblStep       = null!;
    private Button          _btnFirst      = null!;
    private Button          _btnPrev       = null!;
    private Button          _btnNext       = null!;
    private Button          _btnLast       = null!;
    private Button          _btnPlayPause  = null!;
    private TrackBar        _trackSpeed    = null!;
    private Label           _lblDesc       = null!;
    private Label           _lblSpeedVal   = null!;

    // ─── Algorithm registry ────────────────────────────────────────────
    private readonly Dictionary<string, Func<Graph, int, List<Core.Algorithms.Base.AlgorithmStep>>>
        _algorithms = new()
        {
            ["BFS – Duyệt chiều rộng"] = (g, s) => BFS.Run(g, s),
            ["DFS – Duyệt chiều sâu"]  = (g, s) => DFS.Run(g, s),
        };

    // ─── Constructor ───────────────────────────────────────────────────
    public MainForm()
    {
        InitializeComponent();

        _canvas    = BuildCanvas();
        _toolbar   = BuildToolbar(out _btnSelect, out _btnAddNode, out _btnAddEdge,
                                  out _btnDelete, out _btnDirected);
        _animPanel = BuildAnimPanel();
        _statusBar = BuildStatusBar(out _lblNodes, out _lblEdges, out _lblMode, out _lblDirected);

        // Thứ tự Add quyết định vị trí Dock:
        // Thêm trước = z-order thấp = bị đẩy vào trong
        Controls.Add(_canvas);      // Fill — thêm trước → fill phần còn lại
        Controls.Add(_toolbar);     // Top
        Controls.Add(_animPanel);   // Bottom — thêm trước statusBar → ngay trên statusBar
        Controls.Add(_statusBar);   // Bottom — thêm sau → dán sát đáy

        // Engine events
        _engine.OnStepChanged += OnEngineStepChanged;
        _engine.OnFinished    += OnEngineFinished;

        // Form settings
        Text          = "GraphApp — Ứng dụng Đồ thị";
        MinimumSize   = new Size(900, 700);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState   = FormWindowState.Maximized;
        BackColor     = Color.FromArgb(245, 246, 250);

        LoadSampleGraph();
        UpdateStatus();
    }

    // ─── Build Canvas ──────────────────────────────────────────────────

    private GraphCanvas BuildCanvas()
    {
        var c = new GraphCanvas { Dock = DockStyle.Fill };
        c.GraphChanged += _ => { RefreshStartNodeCombo(); UpdateStatus(); };
        return c;
    }

    // ─── Build Toolbar ─────────────────────────────────────────────────

    private ToolStrip BuildToolbar(
        out ToolStripButton btnSelect,
        out ToolStripButton btnAddNode,
        out ToolStripButton btnAddEdge,
        out ToolStripButton btnDelete,
        out ToolStripButton btnDirected)
    {
        var ts = new ToolStrip
        {
            Dock             = DockStyle.Top,
            GripStyle        = ToolStripGripStyle.Hidden,
            BackColor        = Color.FromArgb(40, 44, 52),
            ForeColor        = Color.White,
            RenderMode       = ToolStripRenderMode.Professional,
            ImageScalingSize = new Size(20, 20),
            Padding          = new Padding(6, 4, 6, 4),
            Height           = 44
        };

        btnSelect  = MakeModeBtn("↖  Chọn",      "Select / kéo thả", CanvasMode.Select);
        btnAddNode = MakeModeBtn("⊕  Thêm đỉnh", "Thêm đỉnh",        CanvasMode.AddNode);
        btnAddEdge = MakeModeBtn("→  Thêm cạnh", "Thêm cạnh",        CanvasMode.AddEdge);
        btnDelete  = MakeModeBtn("✕  Xóa",       "Xóa đỉnh/cạnh",   CanvasMode.Delete);

        btnDirected = MakeActionBtn("⇄  Có hướng", "Bật/tắt đồ thị có hướng");
        btnDirected.CheckOnClick = true;
        var dirBtn = btnDirected;
        dirBtn.CheckedChanged += (_, _) =>
        {
            var g = _canvas.GetGraph();
            g.Directed         = dirBtn.Checked;
            dirBtn.ForeColor   = dirBtn.Checked ? Color.FromArgb(255, 180, 50) : Color.White;
            _canvas.Invalidate();
            UpdateStatus();
        };

        var btnClear = MakeActionBtn("🗑  Xóa tất cả", "Xóa toàn bộ đồ thị");
        btnClear.Click += (_, _) =>
        {
            if (MessageBox.Show("Xóa toàn bộ đồ thị?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _engine.Pause();
                _canvas.GetGraph().Clear();
                _canvas.ClearStep();
                RefreshStartNodeCombo();
                UpdateStatus();
                ResetAnimUI();
            }
        };

        var btnSample = MakeActionBtn("★  Đồ thị mẫu", "Tải lại đồ thị mẫu");
        btnSample.Click += (_, _) => LoadSampleGraph();

        ts.Items.Add(new ToolStripLabel("  CHẾ ĐỘ:  ")
            { ForeColor = Color.FromArgb(170, 170, 185), Font = new Font("Segoe UI", 8f) });
        ts.Items.Add(btnSelect);
        ts.Items.Add(btnAddNode);
        ts.Items.Add(btnAddEdge);
        ts.Items.Add(btnDelete);
        ts.Items.Add(new ToolStripSeparator());
        ts.Items.Add(btnDirected);
        ts.Items.Add(new ToolStripSeparator());
        ts.Items.Add(btnSample);
        ts.Items.Add(btnClear);

        SelectModeBtn(btnSelect);
        return ts;
    }

    // ─── Build AnimPanel ───────────────────────────────────────────────

    private Panel BuildAnimPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 130,
            BackColor = Color.FromArgb(30, 33, 40),
            Padding   = new Padding(10, 6, 10, 6)
        };

        // ── Hàng 1: Chọn thuật toán + chạy + step counter ────────────
        var row1 = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            Height        = 38,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0, 4, 0, 2)
        };

        row1.Controls.Add(MakeLabel("Thuật toán:"));

        _cmbAlgorithm = new ComboBox
        {
            Width         = 210,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("Segoe UI", 9f),
            Margin        = new Padding(0, 0, 8, 0)
        };
        _cmbAlgorithm.Items.AddRange(_algorithms.Keys.ToArray<object>());
        _cmbAlgorithm.SelectedIndex = 0;
        row1.Controls.Add(_cmbAlgorithm);

        row1.Controls.Add(MakeLabel("Đỉnh bắt đầu:"));

        _cmbStartNode = new ComboBox
        {
            Width         = 80,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("Segoe UI", 9f),
            Margin        = new Padding(0, 0, 8, 0)
        };
        row1.Controls.Add(_cmbStartNode);

        _btnRun = new Button
        {
            Text      = "▶  Chạy",
            Width     = 90,
            Height    = 28,
            BackColor = Color.FromArgb(46, 160, 67),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 16, 0)
        };
        _btnRun.FlatAppearance.BorderSize = 0;
        _btnRun.Click += OnRunClicked;
        row1.Controls.Add(_btnRun);

        // Separator
        row1.Controls.Add(new Label
            { Text = "│", ForeColor = Color.FromArgb(80, 80, 90),
              Width = 10, TextAlign = ContentAlignment.MiddleCenter });

        _lblStep = new Label
        {
            Text      = "Bước: —",
            ForeColor = Color.FromArgb(200, 200, 210),
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            AutoSize  = true,
            Margin    = new Padding(8, 4, 0, 0)
        };
        row1.Controls.Add(_lblStep);

        panel.Controls.Add(row1);

        // ── Hàng 2: Nút điều khiển + tốc độ ─────────────────────────
        var row2 = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            Height        = 38,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0, 2, 0, 2)
        };

        _btnFirst     = MakeAnimBtn("|◀", "Về đầu");
        _btnPrev      = MakeAnimBtn("◀",  "Bước trước");
        _btnNext      = MakeAnimBtn("▶",  "Bước sau");
        _btnLast      = MakeAnimBtn("▶|", "Về cuối");
        _btnPlayPause = MakeAnimBtn("⏵ Phát", "Phát/Tạm dừng", 90);

        _btnFirst.Click     += (_, _) => { _engine.Pause(); _engine.GoToStart(); };
        _btnPrev.Click      += (_, _) => { _engine.Pause(); _engine.Prev(); };
        _btnNext.Click      += (_, _) => { _engine.Pause(); _engine.Next(); };
        _btnLast.Click      += (_, _) => { _engine.Pause(); _engine.GoToEnd(); };
        _btnPlayPause.Click += OnPlayPauseClicked;

        row2.Controls.Add(_btnFirst);
        row2.Controls.Add(_btnPrev);
        row2.Controls.Add(_btnNext);
        row2.Controls.Add(_btnLast);
        row2.Controls.Add(_btnPlayPause);

        // Speed
        row2.Controls.Add(new Label
            { Text = "  Tốc độ:", ForeColor = Color.FromArgb(160, 160, 175),
              Font = new Font("Segoe UI", 8f), AutoSize = true,
              Margin = new Padding(8, 6, 0, 0) });

        _trackSpeed = new TrackBar
        {
            Minimum    = 1,
            Maximum    = 10,
            Value      = 5,
            TickStyle  = TickStyle.None,
            Width      = 100,
            Height     = 28,
            Margin     = new Padding(0, 4, 0, 0)
        };
        _trackSpeed.Scroll += (_, _) =>
        {
            int ms = SpeedToMs(_trackSpeed.Value);
            _engine.SetSpeed(ms);
            _lblSpeedVal.Text = $"{ms}ms";
        };
        row2.Controls.Add(_trackSpeed);

        _lblSpeedVal = new Label
        {
            Text      = "800ms",
            ForeColor = Color.FromArgb(160, 160, 175),
            Font      = new Font("Segoe UI", 8f),
            AutoSize  = true,
            Margin    = new Padding(2, 6, 0, 0)
        };
        row2.Controls.Add(_lblSpeedVal);

        panel.Controls.Add(row2);

        // ── Hàng 3: Description ───────────────────────────────────────
        _lblDesc = new Label
        {
            Dock      = DockStyle.Fill,
            Text      = "Chọn thuật toán và bấm ▶ Chạy để bắt đầu.",
            ForeColor = Color.FromArgb(190, 190, 200),
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(4, 0, 0, 0),
            AutoEllipsis = true
        };
        panel.Controls.Add(_lblDesc);

        // Tắt các nút điều khiển ban đầu
        SetAnimButtonsEnabled(false);
        return panel;
    }

    // ─── Engine Event Handlers ─────────────────────────────────────────

    private void OnEngineStepChanged(Core.Algorithms.Base.AlgorithmStep step, int index, int total)
    {
        // Cập nhật canvas
        _canvas.ApplyStep(step);

        // Cập nhật UI
        _lblStep.Text = $"Bước: {index}/{total}";
        _lblDesc.Text = step.Description.Replace("\n", "  │  ");
        _lblDesc.ForeColor = step.StepType == "done"
            ? Color.FromArgb(100, 220, 130)
            : step.StepType == "error"
                ? Color.FromArgb(240, 100, 100)
                : Color.FromArgb(200, 200, 215);

        // Cập nhật trạng thái nút
        _btnFirst.Enabled = _btnPrev.Enabled = !_engine.IsAtStart;
        _btnLast.Enabled  = _btnNext.Enabled = !_engine.IsAtEnd;
        _btnPlayPause.Text = _engine.IsPlaying ? "⏸ Dừng" : "⏵ Phát";
    }

    private void OnEngineFinished()
    {
        _btnPlayPause.Text = "⏵ Phát";
        _lblDesc.ForeColor = Color.FromArgb(100, 220, 130);
    }

    // ─── Run Algorithm ─────────────────────────────────────────────────

    private void OnRunClicked(object? sender, EventArgs e)
    {
        if (_cmbAlgorithm.SelectedItem is not string algoName) return;
        if (!_algorithms.TryGetValue(algoName, out var runner)) return;

        // Lấy start node
        int startId = -1;
        if (_cmbStartNode.SelectedItem is Node selectedNode)
            startId = selectedNode.Id;
        else if (_canvas.GetGraph().Nodes.Count > 0)
            startId = _canvas.GetGraph().Nodes[0].Id;

        if (startId < 0)
        {
            MessageBox.Show("Vui lòng thêm ít nhất một đỉnh vào đồ thị.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Chạy thuật toán
        _engine.Pause();
        var graph = _canvas.GetGraph().Clone();   // clone để không ảnh hưởng đồ thị gốc
        var steps = runner(graph, startId);

        _engine.Load(steps);
        SetAnimButtonsEnabled(true);

        // Tự động phát
        int speedMs = SpeedToMs(_trackSpeed.Value);
        _engine.Play(speedMs);
        _btnPlayPause.Text = "⏸ Dừng";
    }

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        if (!_engine.HasSteps) return;

        if (_engine.IsPlaying)
        {
            _engine.Pause();
            _btnPlayPause.Text = "⏵ Phát";
        }
        else
        {
            _engine.Play(SpeedToMs(_trackSpeed.Value));
            _btnPlayPause.Text = "⏸ Dừng";
        }
    }

    // ─── Status Bar ────────────────────────────────────────────────────

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

        lblNodes    = new ToolStripStatusLabel("Đỉnh: 0")     { ForeColor = Color.FromArgb(180, 220, 255) };
        lblEdges    = new ToolStripStatusLabel("Cạnh: 0")     { ForeColor = Color.FromArgb(180, 220, 255) };
        lblDirected = new ToolStripStatusLabel("Vô hướng")    { ForeColor = Color.FromArgb(200, 200, 200) };
        lblMode     = new ToolStripStatusLabel("Chế độ: Chọn")
            { ForeColor = Color.FromArgb(180, 255, 180), Spring = true, TextAlign = ContentAlignment.MiddleRight };

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

    private void UpdateStatus()
    {
        var g = _canvas.GetGraph();
        _lblNodes.Text    = $"Đỉnh: {g.Nodes.Count}";
        _lblEdges.Text    = $"Cạnh: {g.Edges.Count}";
        _lblDirected.Text = g.Directed ? "Có hướng" : "Vô hướng";
        _lblDirected.ForeColor = g.Directed
            ? Color.FromArgb(255, 200, 80) : Color.FromArgb(200, 200, 200);

        string modeName = _canvas.Mode switch
        {
            CanvasMode.AddNode => "Thêm đỉnh",
            CanvasMode.AddEdge => "Thêm cạnh",
            CanvasMode.Delete  => "Xóa",
            _                  => "Chọn / Kéo thả"
        };
        _lblMode.Text = $"Chế độ: {modeName}   ";
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    private void RefreshStartNodeCombo()
    {
        _cmbStartNode.Items.Clear();
        foreach (var n in _canvas.GetGraph().Nodes)
            _cmbStartNode.Items.Add(n);
        if (_cmbStartNode.Items.Count > 0)
            _cmbStartNode.SelectedIndex = 0;
    }

    private void SetAnimButtonsEnabled(bool enabled)
    {
        _btnFirst.Enabled = _btnPrev.Enabled =
        _btnNext.Enabled  = _btnLast.Enabled =
        _btnPlayPause.Enabled = enabled;
    }

    private void ResetAnimUI()
    {
        _lblStep.Text = "Bước: —";
        _lblDesc.Text = "Chọn thuật toán và bấm ▶ Chạy để bắt đầu.";
        _lblDesc.ForeColor = Color.FromArgb(190, 190, 200);
        SetAnimButtonsEnabled(false);
    }

    private static int SpeedToMs(int trackValue) =>
        // trackValue 1→10: 1=slow(2000ms), 10=fast(100ms)
        (int)(2000.0 / trackValue * 0.9 + 100);

    private void SelectModeBtn(ToolStripButton active)
    {
        ToolStripButton[] modeButtons = [_btnSelect, _btnAddNode, _btnAddEdge, _btnDelete];
        foreach (var b in modeButtons)
        {
            b.Checked   = b == active;
            b.BackColor = b == active ? Color.FromArgb(70, 130, 210) : Color.Transparent;
        }
    }

    private ToolStripButton MakeModeBtn(string text, string tooltip, CanvasMode mode)
    {
        var btn = new ToolStripButton(text)
        {
            ToolTipText = tooltip, AutoSize = true,
            ForeColor   = Color.White, Font = new Font("Segoe UI", 9f),
            Padding     = new Padding(8, 2, 8, 2), Margin = new Padding(2, 0, 2, 0)
        };
        btn.Click += (_, _) => { _canvas.Mode = mode; SelectModeBtn(btn); UpdateStatus(); };
        return btn;
    }

    private static ToolStripButton MakeActionBtn(string text, string tooltip) =>
        new(text) { ToolTipText = tooltip, AutoSize = true, ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9f), Padding = new Padding(8, 2, 8, 2),
                    Margin = new Padding(2, 0, 2, 0) };

    private static Label MakeLabel(string text) => new()
    {
        Text      = text, ForeColor = Color.FromArgb(160, 160, 175),
        Font      = new Font("Segoe UI", 8.5f), AutoSize = true,
        Margin    = new Padding(0, 6, 4, 0)
    };

    private static Button MakeAnimBtn(string text, string tooltip, int width = 42) => new()
    {
        Text      = text, ToolTipText = tooltip,
        Width     = width, Height = 28,
        BackColor = Color.FromArgb(55, 60, 75), ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f),
        Cursor    = Cursors.Hand, Margin = new Padding(2, 0, 2, 0)
    };

    // ─── Sample Graph ──────────────────────────────────────────────────

    private void LoadSampleGraph()
    {
        _engine.Pause();
        var g = new Graph { Directed = false };
        int a = g.AddNode(200, 150); int b = g.AddNode(420, 100);
        int c = g.AddNode(640, 150); int d = g.AddNode(200, 360);
        int e = g.AddNode(420, 320); int f = g.AddNode(640, 360);
        g.AddEdge(a, b, 4); g.AddEdge(a, d, 2); g.AddEdge(b, c, 5);
        g.AddEdge(b, e, 3); g.AddEdge(c, f, 1); g.AddEdge(d, e, 6);
        g.AddEdge(e, f, 7);

        _btnDirected.Checked   = false;
        _btnDirected.ForeColor = Color.White;
        _canvas.RefreshGraph(g);
        RefreshStartNodeCombo();
        ResetAnimUI();
        UpdateStatus();
    }
}

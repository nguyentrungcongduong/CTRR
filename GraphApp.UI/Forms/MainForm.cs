using System.Drawing;
using System.Windows.Forms;
using GraphApp.Core.Algorithms.Advanced;
using GraphApp.Core.Algorithms.Properties;
using GraphApp.Core.Algorithms.ShortestPath;
using GraphApp.Core.Algorithms.Traversal;
using GraphApp.Core.Models;
using GraphApp.Core.Persistence;
using GraphApp.UI.Controls;

namespace GraphApp.UI.Forms;

/// <summary>
/// Form chính của ứng dụng GraphApp.
/// Layout (top→bottom): Toolbar | Canvas | AnimPanel | StatusBar
/// </summary>
public partial class MainForm : Form
{
    // ─── Core Controls ─────────────────────────────────────────────────
    private readonly GraphCanvas        _canvas;
    private readonly ToolStrip          _toolbar;
    private readonly StatusStrip        _statusBar;
    private readonly Panel              _animPanel;
    private readonly RepresentationPanel _repPanel;
    private readonly AnimationEngine    _engine = new();

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

    // Flag tránh vòng lặp khi hoàn tác toggle directed
    private bool _suppressDirectedChange;

    // ─── Algorithm registry ────────────────────────────────────────────
    private readonly Dictionary<string, Func<Graph, int, List<Core.Algorithms.Base.AlgorithmStep>>>
        _algorithms = new()
        {
            ["BFS – Duyệt chiều rộng"]              = (g, s) => BFS.Run(g, s),
            ["DFS – Duyệt chiều sâu"]                = (g, s) => DFS.Run(g, s),
            ["Dijkstra – Đường đi ngắn nhất"]        = (g, s) => Dijkstra.Run(g, s),
            ["Kiểm tra 2 phía (Bipartite)"]          = (g, _) => BipartiteChecker.Run(g),
            ["Prim – Cây khung nhỏ nhất (MST)"]      = (g, s) => Prim.Run(g, s),
            ["Kruskal – Cây khung nhỏ nhất"]         = (g, _) => Kruskal.Run(g),
            ["Ford-Fulkerson – Luồng cực đại"]       = (g, s) =>
                FordFulkerson.Run(g, s, g.Nodes.LastOrDefault()?.Id ?? s),
            ["Fleury – Đường/Chu trình Euler"]       = (g, s) => Fleury.Run(g, s),
            ["Hierholzer – Chu trình Euler (O(E))"]  = (g, s) => Hierholzer.Run(g, s),
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
        _repPanel  = BuildRepresentationPanel();

        // Thứ tự Add quyết định vị trí Dock:
        Controls.Add(_canvas);      // Fill
        Controls.Add(_repPanel);    // Right — thêm trước toolbar để toolbar ưu tiên
        Controls.Add(_toolbar);     // Top
        Controls.Add(_animPanel);   // Bottom
        Controls.Add(_statusBar);   // Bottom (outermost)

        // Engine events
        _engine.OnStepChanged += OnEngineStepChanged;
        _engine.OnFinished    += OnEngineFinished;

        // RepresentationPanel events
        _repPanel.GraphApplied += OnRepPanelGraphApplied;

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
        c.GraphChanged += _ =>
        {
            RefreshStartNodeCombo();
            UpdateStatus();
            // Sync RepresentationPanel
            if (_repPanel.Visible)
                _repPanel.RefreshFromGraph(_canvas.GetGraph());
        };
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

        btnDirected = MakeActionBtn("⇄  Có hướng", "Bật/tắt đồ thị có hướng (directed / undirected)");
        btnDirected.CheckOnClick = true;
        var dirBtn = btnDirected;
        dirBtn.CheckedChanged += (_, _) =>
        {
            if (_suppressDirectedChange) return;

            var g = _canvas.GetGraph();

            // Cảnh báo nếu đang có cạnh
            if (g.Edges.Count > 0)
            {
                string newMode = dirBtn.Checked ? "CÓ HƯỚNG" : "VÔ HƯỚNG";
                string oldMode = dirBtn.Checked ? "vô hướng"  : "có hướng";
                var ans = MessageBox.Show(
                    $"Chuyển đồ thị từ {oldMode} → {newMode}.\n\n" +
                    $"Các cạnh hiện tại ({g.Edges.Count} cạnh) sẽ được GIỮ NGUYÊN.\n" +
                    "Bạn có muốn tiếp tục không?",
                    "Xác nhận chuyển đổi",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (ans == DialogResult.No)
                {
                    // Hoàn tác toggle mà không kích hoạt lại event
                    _suppressDirectedChange = true;
                    dirBtn.Checked          = !dirBtn.Checked;
                    _suppressDirectedChange = false;
                    return;
                }
            }

            g.Directed       = dirBtn.Checked;
            dirBtn.Text      = dirBtn.Checked ? "⇄  Có hướng ✓" : "⇄  Có hướng";
            dirBtn.ForeColor = dirBtn.Checked ? Color.FromArgb(255, 200, 60) : Color.White;
            _canvas.Invalidate();
            UpdateStatus();

            if (_repPanel.Visible)
                _repPanel.RefreshFromGraph(_canvas.GetGraph());
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
        ts.Items.Add(new ToolStripSeparator());

        // ── Save / Load ────────────────────────────────────────────────
        var btnSave = MakeActionBtn("💾  Lưu", "Lưu đồ thị ra file .graph.json");
        btnSave.Click += (_, _) =>
        {
            using var dlg = new SaveFileDialog
            {
                Title            = "Lưu đồ thị",
                Filter           = GraphSerializer.FileFilter,
                DefaultExt       = "graph.json",
                AddExtension     = true,
                FileName         = "graph"
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    GraphSerializer.Save(_canvas.GetGraph(), dlg.FileName);
                    _lblMode.Text = $"✓ Đã lưu: {Path.GetFileName(dlg.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể lưu file:\n{ex.Message}",
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        };

        var btnLoad = MakeActionBtn("📂  Mở", "Mở file đồ thị .graph.json");
        btnLoad.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Mở đồ thị",
                Filter = GraphSerializer.FileFilter
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var loaded = GraphSerializer.Load(dlg.FileName);
                    if (loaded == null)
                    {
                        MessageBox.Show("File không hợp lệ hoặc rỗng.",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    _engine.Pause();
                    _canvas.RefreshGraph(loaded);
                    _btnDirected.Checked = loaded.Directed;
                    RefreshStartNodeCombo();
                    ResetAnimUI();
                    UpdateStatus();
                    if (_repPanel.Visible) _repPanel.RefreshFromGraph(_canvas.GetGraph());
                    _lblMode.Text = $"✓ Đã mở: {Path.GetFileName(dlg.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở file:\n{ex.Message}",
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        };

        ts.Items.Add(btnSave);
        ts.Items.Add(btnLoad);
        ts.Items.Add(new ToolStripSeparator());

        // Toggle RepresentationPanel
        var btnRep = MakeActionBtn("📊  Biểu Diễn", "Hiện/ẩn panel biểu diễn đồ thị");
        btnRep.CheckOnClick = true;
        var repBtn = btnRep;
        repBtn.CheckedChanged += (_, _) =>
        {
            _repPanel.Visible = repBtn.Checked;
            if (repBtn.Checked) _repPanel.RefreshFromGraph(_canvas.GetGraph());
        };
        ts.Items.Add(btnRep);

        // InputMatrixForm button
        var btnMatrix = MakeActionBtn("📝  Nhập Ma Trận", "Nhập đồ thị từ ma trận kề");
        btnMatrix.Click += (_, _) =>
        {
            using var form = new InputMatrixForm();
            if (form.ShowDialog(this) == DialogResult.OK && form.ResultGraph != null)
            {
                _engine.Pause();
                _canvas.RefreshGraph(form.ResultGraph);
                _btnDirected.Checked = form.ResultGraph.Directed;
                RefreshStartNodeCombo();
                ResetAnimUI();
                UpdateStatus();
                if (_repPanel.Visible)
                    _repPanel.RefreshFromGraph(_canvas.GetGraph());
            }
        };
        ts.Items.Add(btnMatrix);

        SelectModeBtn(btnSelect);
        return ts;
    }

    // ─── Build RepresentationPanel ─────────────────────────────────────

    private RepresentationPanel BuildRepresentationPanel()
    {
        var panel = new RepresentationPanel { Visible = false };
        return panel;
    }

    private void OnRepPanelGraphApplied(Graph newGraph)
    {
        _engine.Pause();
        _canvas.RefreshGraph(newGraph);
        _btnDirected.Checked = newGraph.Directed;
        RefreshStartNodeCombo();
        ResetAnimUI();
        UpdateStatus();
        // Sync lại tất cả 3 tab
        _repPanel.RefreshFromGraph(_canvas.GetGraph());
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

    private static Button MakeAnimBtn(string text, string tooltip, int width = 42)
    {
        var btn = new Button
        {
            Text      = text,
            Width     = width, Height = 28,
            BackColor = Color.FromArgb(55, 60, 75), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f),
            Cursor    = Cursors.Hand, Margin = new Padding(2, 0, 2, 0)
        };
        // Button không có ToolTipText — dùng ToolTip component
        new ToolTip().SetToolTip(btn, tooltip);
        return btn;
    }

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

using System.Drawing;
using System.Drawing.Imaging;
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

    // Undo / Redo toolbar buttons (cần ref để enable/disable)
    private ToolStripButton _btnUndo = null!;
    private ToolStripButton _btnRedo = null!;

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

    // ─── Undo / Redo history ─────────────────────────────────────────
    private const int MaxHistory = 50;
    private readonly List<string> _history = new();
    private int  _historyIndex  = -1;
    private bool _isRestoring   = false;

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

        // Font toàn app — cascade xuống tất cả controls
        Font          = new Font("Segoe UI", 10f, FontStyle.Regular);
        AutoScaleMode = AutoScaleMode.Dpi;

        LoadSampleGraph();
        UpdateStatus();
    }

    // ─── Build Canvas ──────────────────────────────────────────────────

    private GraphCanvas BuildCanvas()
    {
        var c = new GraphCanvas { Dock = DockStyle.Fill };
        c.GraphChanged += graph =>
        {
            // Undo/Redo: đẩy trạng thái mới vào history
            if (!_isRestoring) PushHistory(graph);

            RefreshStartNodeCombo();
            UpdateStatus();
            if (_btnRun != null)
                _btnRun.Enabled = _canvas.GetGraph().Nodes.Count > 0;
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

        // ── Dropdown "Đồ thị mẫu" với 5 mẫu ──────────────────────
        var ddSample = new ToolStripDropDownButton("📚  Đồ thị mẫu")
        {
            ForeColor   = Color.White,
            Font        = new Font("Segoe UI", 9f),
            Padding     = new Padding(8, 2, 8, 2),
            Margin      = new Padding(2, 0, 2, 0),
            ShowDropDownArrow = true,
            ToolTipText = "Tải một đồ thị mẫu có sẵn"
        };

        void AddSampleItem(string icon, string label, string desc, Action loader)
        {
            var item = new ToolStripMenuItem($"{icon}  {label}")
            {
                ToolTipText = desc,
                Font = new Font("Segoe UI", 9f)
            };
            item.Click += (_, _) => loader();
            ddSample.DropDownItems.Add(item);
        }

        AddSampleItem("🔵", "Vô hướng 6 đỉnh",
            "BFS / DFS / Prim / Kruskal",
            () => LoadSample(BuildUndirected6()));

        AddSampleItem("🟡", "Có hướng có trọng số",
            "Dijkstra / Ford-Fulkerson",
            () => LoadSample(BuildDirectedWeighted()));

        AddSampleItem("🟢", "2 Phía (Bipartite)",
            "Kiểm tra Bipartite",
            () => LoadSample(BuildBipartite()));

        AddSampleItem("🟣", "Đường Euler",
            "Fleury / Hierholzer — có đường Euler (2 bậc lẻ)",
            () => LoadSample(BuildEulerPath()));

        AddSampleItem("⚪", "Chu trình Euler",
            "Fleury / Hierholzer — chu trình Euler (tất cả bậc chẵn)",
            () => LoadSample(BuildEulerCircuit()));

        ts.Items.Add(new ToolStripLabel("  CHẾĐỘ:  ")
            { ForeColor = Color.FromArgb(170, 170, 185), Font = new Font("Segoe UI", 8f) });
        ts.Items.Add(btnSelect);
        ts.Items.Add(btnAddNode);
        ts.Items.Add(btnAddEdge);
        ts.Items.Add(btnDelete);
        ts.Items.Add(new ToolStripSeparator());
        ts.Items.Add(btnDirected);
        ts.Items.Add(new ToolStripSeparator());

        // Undo / Redo
        _btnUndo = MakeActionBtn("↩  Hoàn tác", "Hoàn tác thao tác vừa rồi [Ctrl+Z]");
        _btnUndo.Click   += (_, _) => DoUndo();
        _btnUndo.Enabled  = false;
        ts.Items.Add(_btnUndo);

        _btnRedo = MakeActionBtn("↪  Làm lại", "Làm lại thao tác đã hoàn tác [Ctrl+Y]");
        _btnRedo.Click   += (_, _) => DoRedo();
        _btnRedo.Enabled  = false;
        ts.Items.Add(_btnRedo);


        ts.Items.Add(ddSample);
        ts.Items.Add(btnClear);

        // ── Zoom controls ─────────────────────────────────────
        ts.Items.Add(new ToolStripSeparator());

        var btnFit = MakeActionBtn("🔲  Vừa màn hình", "Zoom và pan để hiển thị toàn bộ đồ thị [F]");
        btnFit.Click += (_, _) => _canvas.FitToScreen();
        ts.Items.Add(btnFit);

        var btnZoomReset = MakeActionBtn("🔍 100%", "Reset zoom về 100% [Ctrl+0]");
        btnZoomReset.Click += (_, _) => _canvas.ResetView();
        ts.Items.Add(btnZoomReset);
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

        // Export PNG
        var btnExport = MakeActionBtn("📷  Xuất PNG", "Xuất đồ thị ra file PNG (2× độ phân giải) [Ctrl+E]");
        btnExport.Click += (_, _) => ExportPng();
        ts.Items.Add(btnExport);
        ts.Items.Add(new ToolStripSeparator());

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
            Height    = 148,
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
            Width         = 220,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("Segoe UI", 9f),
            Margin        = new Padding(0, 0, 8, 0)
        };
        _cmbAlgorithm.Items.AddRange(_algorithms.Keys.ToArray<object>());
        _cmbAlgorithm.SelectedIndex = 0;
        // Khi đổi thuật toán: reset animation + hiện gợi ý
        _cmbAlgorithm.SelectedIndexChanged += (_, _) =>
        {
            _engine.Pause();
            _canvas.ClearStep();
            SetAnimButtonsEnabled(false);
            _lblStep.Text  = "Bước: —";
            string algo    = _cmbAlgorithm.SelectedItem?.ToString() ?? "";
            _lblDesc.Text  = GetAlgorithmHint(algo);
            _lblDesc.ForeColor = Color.FromArgb(150, 200, 255);
        };
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
        new ToolTip().SetToolTip(_btnRun, "Chạy thuật toán đã chọn [Enter]");
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

        _btnFirst     = MakeAnimBtn("|◀", "Về bước đầu [Home]");
        _btnPrev      = MakeAnimBtn("◀",  "Bước trước [←]");
        _btnNext      = MakeAnimBtn("▶",  "Bước sau [→]");
        _btnLast      = MakeAnimBtn("▶|", "Về bước cuối [End]");
        _btnPlayPause = MakeAnimBtn("⏵ Phát", "Phát / Tạm dừng [Space]", 90);

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

        // Speed: Chậm ← slider → Nhanh
        row2.Controls.Add(new Label
            { Text = "  🐢 Chậm", ForeColor = Color.FromArgb(140, 140, 160),
              Font = new Font("Segoe UI", 7.5f), AutoSize = true,
              Margin = new Padding(12, 7, 0, 0) });

        _trackSpeed = new TrackBar
        {
            Minimum    = 1,
            Maximum    = 10,
            Value      = 5,
            TickStyle  = TickStyle.None,
            Width      = 110,
            Height     = 28,
            Margin     = new Padding(2, 4, 2, 0)
        };
        _trackSpeed.Scroll += (_, _) =>
        {
            int ms = SpeedToMs(_trackSpeed.Value);
            _engine.SetSpeed(ms);
            _lblSpeedVal.Text = SpeedLabel(_trackSpeed.Value);
        };
        row2.Controls.Add(_trackSpeed);

        row2.Controls.Add(new Label
            { Text = "Nhanh 🐇", ForeColor = Color.FromArgb(140, 140, 160),
              Font = new Font("Segoe UI", 7.5f), AutoSize = true,
              Margin = new Padding(0, 7, 8, 0) });

        _lblSpeedVal = new Label
        {
            Text      = SpeedLabel(5),
            ForeColor = Color.FromArgb(180, 200, 255),
            Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
            AutoSize  = true,
            Margin    = new Padding(2, 6, 0, 0)
        };
        row2.Controls.Add(_lblSpeedVal);

        panel.Controls.Add(row2);

        // ── Hàng 3: Description (multiline) ─────────────────────────
        _lblDesc = new Label
        {
            Dock      = DockStyle.Fill,
            Text      = GetAlgorithmHint(_cmbAlgorithm.SelectedItem?.ToString() ?? ""),
            ForeColor = Color.FromArgb(150, 200, 255),
            Font      = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 2, 0, 2),
            AutoSize  = false
        };
        panel.Controls.Add(_lblDesc);

        // Tắt nút điều khiển ban đầu; Run disabled nếu graph rỗng
        SetAnimButtonsEnabled(false);
        return panel;
    }

    // ─── Engine Event Handlers ─────────────────────────────────────────

    private void OnEngineStepChanged(Core.Algorithms.Base.AlgorithmStep step, int index, int total)
    {
        _canvas.ApplyStep(step);

        // Step counter nổi bật
        _lblStep.Text = $"  🔢 Bước {index} / {total}";

        // Description: hiển thị như multiline
        _lblDesc.Text = step.Description;
        _lblDesc.ForeColor = step.StepType switch
        {
            "done"         => Color.FromArgb(100, 220, 130),
            "error"        => Color.FromArgb(240, 100, 100),
            "check_bridge" => Color.FromArgb(255, 220, 80),
            "find_path"
            or "augment_flow" => Color.FromArgb(255, 180, 80),
            _              => Color.FromArgb(200, 210, 230)
        };

        // Enable/disable navigation
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
        var statusFont = new Font("Segoe UI", 9.5f);

        var ss = new StatusStrip
        {
            BackColor  = Color.FromArgb(40, 44, 52),
            ForeColor  = Color.White,
            SizingGrip = false,
            Font       = statusFont
        };

        lblNodes    = new ToolStripStatusLabel("Đỉnh: 0")
            { ForeColor = Color.FromArgb(180, 220, 255), Font = statusFont };
        lblEdges    = new ToolStripStatusLabel("Cạnh: 0")
            { ForeColor = Color.FromArgb(180, 220, 255), Font = statusFont };
        lblDirected = new ToolStripStatusLabel("Vô hướng")
            { ForeColor = Color.FromArgb(200, 200, 200), Font = statusFont };
        lblMode     = new ToolStripStatusLabel("Chế độ: Chọn")
            { ForeColor = Color.FromArgb(180, 255, 180), Spring = true,
              TextAlign = ContentAlignment.MiddleRight, Font = statusFont };

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

        // Tính bậc trung bình
        double avgDeg = g.Nodes.Count > 0
            ? g.Edges.Count * (g.Directed ? 1.0 : 2.0) / g.Nodes.Count : 0;

        _lblNodes.Text    = $"📍 {g.Nodes.Count} đỉnh";
        _lblEdges.Text    = $"🔗 {g.Edges.Count} cạnh";
        _lblDirected.Text = g.Directed ? "⇄ Có hướng" : "— Vô hướng";
        _lblDirected.ForeColor = g.Directed
            ? Color.FromArgb(255, 200, 80) : Color.FromArgb(180, 200, 220);

        string modeName = _canvas.Mode switch
        {
            CanvasMode.AddNode => "⊕ Thêm đỉnh",
            CanvasMode.AddEdge => "→ Thêm cạnh",
            CanvasMode.Delete  => "✕ Xóa",
            _                  => "↖ Chọn/Kéo"
        };
        _lblMode.Text = $"{modeName}   ";
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

    private static string SpeedLabel(int v) => v switch
    {
        <= 2  => "⏱ Rất chậm",
        <= 4  => "⏱ Chậm",
        <= 6  => "⏱ Bình thường",
        <= 8  => "⚡ Nhanh",
        _     => "⚡ Rất nhanh"
    };

    private static string GetAlgorithmHint(string algo) => algo switch
    {
        var s when s.Contains("BFS")         => "💡 BFS: Duyệt theo chiều rộng (Queue). Chọn đỉnh bắt đầu.",
        var s when s.Contains("DFS")         => "💡 DFS: Duyệt theo chiều sâu (Stack). Chọn đỉnh bắt đầu.",
        var s when s.Contains("Dijkstra")    => "💡 Dijkstra: Đường ngắn nhất (đồ thị có trọng số ≥ 0). Chọn đỉnh nguồn.",
        var s when s.Contains("Bipartite")   => "💡 Bipartite: Kiểm tra 2 phía bằng BFS tô màu. Không cần chọn đỉnh.",
        var s when s.Contains("Prim")        => "💡 Prim: MST cho đồ thị VÔ HƯỚNG có trọng số. Chọn đỉnh bắt đầu.",
        var s when s.Contains("Kruskal")     => "💡 Kruskal: MST bằng Union-Find. Không cần chọn đỉnh bắt đầu.",
        var s when s.Contains("Ford")        => "💡 Ford-Fulkerson: Max Flow trên đồ thị CÓ HƯỚNG. Nguồn = đỉnh chọn, Đích = đỉnh cuối.",
        var s when s.Contains("Fleury")      => "💡 Fleury: Đường/Chu trình Euler (có bridge detection). Chọn đỉnh bắt đầu.",
        var s when s.Contains("Hierholzer")  => "💡 Hierholzer: Đường/Chu trình Euler O(E) bằng stack. Chọn đỉnh bắt đầu.",
        _                                    => "💡 Chọn thuật toán và bấm ▶ Chạy."
    };

    // ─── Sample Graphs ─────────────────────────────────────────────

    /// <summary>Tải đồ thị mẫu lên canvas và reset UI.</summary>
    private void LoadSample(Graph g)
    {
        _engine.Pause();
        _suppressDirectedChange = true;
        _btnDirected.Checked    = g.Directed;
        _btnDirected.Text       = g.Directed ? "⇄  Có hướng ✓" : "⇄  Có hướng";
        _btnDirected.ForeColor  = g.Directed ? Color.FromArgb(255, 200, 60) : Color.White;
        _suppressDirectedChange = false;

        _canvas.RefreshGraph(g);
        RefreshStartNodeCombo();
        ResetAnimUI();
        UpdateStatus();
        if (_repPanel.Visible) _repPanel.RefreshFromGraph(g);

        // Đẩy trạng thái ban đầu vào history (clear history trước)
        _history.Clear();
        _historyIndex = -1;
        PushHistory(g);
    }

    // ── 1. Vô hướng 6 đỉnh — test BFS/DFS/Prim/Kruskal ────────────────
    private static Graph BuildUndirected6()
    {
        //        B(4)  C(5)
        //       /    \ /  \
        //      A(2)   E(3)  F
        //       \    / \  /
        //        D(6)   --(7)--
        var g = new Graph { Directed = false };
        int a = g.AddNode(200, 160);   // A
        int b = g.AddNode(400, 90);    // B
        int c = g.AddNode(630, 90);    // C
        int d = g.AddNode(200, 370);   // D
        int e = g.AddNode(420, 320);   // E
        int f = g.AddNode(630, 370);   // F

        g.AddEdge(a, b, 4);
        g.AddEdge(a, d, 2);
        g.AddEdge(b, c, 5);
        g.AddEdge(b, e, 3);
        g.AddEdge(c, f, 1);
        g.AddEdge(d, e, 6);
        g.AddEdge(e, f, 7);
        return g;
    }

    // ── 2. Có hướng có trọng số — test Dijkstra/Ford-Fulkerson ────────
    private static Graph BuildDirectedWeighted()
    {
        //  S ←10→ A −8→ T
        //  S →₇ B →₇ T
        //  A →₃ B      (cross)
        var g = new Graph { Directed = true };
        int s = g.AddNode(120, 240);  // Source
        int a = g.AddNode(340, 120);  // A
        int b = g.AddNode(340, 370);  // B
        int c = g.AddNode(560, 240);  // C
        int t = g.AddNode(760, 240);  // Sink

        // Flow network edges (weight = capacity)
        g.AddEdge(s, a, 10);
        g.AddEdge(s, b, 7);
        g.AddEdge(a, c, 8);
        g.AddEdge(a, b, 3);
        g.AddEdge(b, c, 5);
        g.AddEdge(c, t, 12);
        g.AddEdge(b, t, 6);
        return g;
    }

    // ── 3. 2 Phía (Bipartite) — test Bipartite checker ──────────────
    private static Graph BuildBipartite()
    {
        // Nhóm A: 3 đỉnh bên trái
        // Nhóm B: 3 đỉnh bên phải
        // Chỉ có cạnh A↔B (không có cạnh trong cùng nhóm)
        var g = new Graph { Directed = false };
        int a1 = g.AddNode(160, 140);
        int a2 = g.AddNode(160, 280);
        int a3 = g.AddNode(160, 420);
        int b1 = g.AddNode(560, 140);
        int b2 = g.AddNode(560, 280);
        int b3 = g.AddNode(560, 420);

        g.AddEdge(a1, b1, 1); g.AddEdge(a1, b2, 1);
        g.AddEdge(a2, b1, 1); g.AddEdge(a2, b3, 1);
        g.AddEdge(a3, b2, 1); g.AddEdge(a3, b3, 1);
        return g;
    }

    // ── 4. Đường Euler — 2 đỉnh bậc lẻ ─────────────────────────
    private static Graph BuildEulerPath()
    {
        // Königsberg-style: bậc các đỉnh: A=3 B=3 C=2 D=2 E=2
        // => 2 đỉnh bậc lẻ (A, B) => đường Euler A..B
        var g = new Graph { Directed = false };
        int a = g.AddNode(160, 240);   // bậc 3
        int b = g.AddNode(620, 240);   // bậc 3
        int c = g.AddNode(390, 120);   // bậc 2
        int d = g.AddNode(390, 360);   // bậc 2
        int e = g.AddNode(390, 240);   // bậc 4

        g.AddEdge(a, c, 1);
        g.AddEdge(a, d, 1);
        g.AddEdge(a, e, 1);
        g.AddEdge(c, b, 1);
        g.AddEdge(d, b, 1);
        g.AddEdge(e, b, 1);
        g.AddEdge(c, d, 1);
        return g;
    }

    // ── 5. Chu trình Euler — tất cả bậc chẵn ─────────────────────
    private static Graph BuildEulerCircuit()
    {
        // Petersen-style: ngũ giác ngoài + sô giác trong
        // Tất cả đỉnh bậc 4 => chu trình Euler
        var g = new Graph { Directed = false };

        // Vòng ngoài (pentagon)
        int p1 = g.AddNode(390, 80);
        int p2 = g.AddNode(620, 250);
        int p3 = g.AddNode(530, 470);
        int p4 = g.AddNode(250, 470);
        int p5 = g.AddNode(160, 250);

        // Vòng trong (pentagram)
        int q1 = g.AddNode(390, 200);
        int q2 = g.AddNode(490, 320);
        int q3 = g.AddNode(440, 430);
        int q4 = g.AddNode(340, 430);
        int q5 = g.AddNode(290, 320);

        // Cạnh ngoài
        g.AddEdge(p1, p2, 1); g.AddEdge(p2, p3, 1);
        g.AddEdge(p3, p4, 1); g.AddEdge(p4, p5, 1);
        g.AddEdge(p5, p1, 1);

        // Cạnh trong
        g.AddEdge(q1, q3, 1); g.AddEdge(q3, q5, 1);
        g.AddEdge(q5, q2, 1); g.AddEdge(q2, q4, 1);
        g.AddEdge(q4, q1, 1);

        // Cạnh nối ngoài-trong
        g.AddEdge(p1, q1, 1); g.AddEdge(p2, q2, 1);
        g.AddEdge(p3, q3, 1); g.AddEdge(p4, q4, 1);
        g.AddEdge(p5, q5, 1);

        return g;
    }

    // Tương thích ngược với code cũ gọi LoadSampleGraph()
    private void LoadSampleGraph() => LoadSample(BuildUndirected6());

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
            ForeColor   = Color.White, Font = new Font("Segoe UI", 9.5f),
            Padding     = new Padding(10, 3, 10, 3), Margin = new Padding(2, 0, 2, 0)
        };
        btn.Click += (_, _) => { _canvas.Mode = mode; SelectModeBtn(btn); UpdateStatus(); };
        return btn;
    }

    private static ToolStripButton MakeActionBtn(string text, string tooltip) =>
        new(text) { ToolTipText = tooltip, AutoSize = true, ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f), Padding = new Padding(10, 3, 10, 3),
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

    // ─── Keyboard Shortcuts ────────────────────────────────────────────

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Bỏ qua khi đang nhập liệu vào TextBox / ComboBox
        if (ActiveControl is TextBox or ComboBox or RichTextBox)
            return base.ProcessCmdKey(ref msg, keyData);

        switch (keyData)
        {
            case Keys.Space:
                if (_engine.HasSteps) { OnPlayPauseClicked(null, EventArgs.Empty); return true; }
                break;
            case Keys.Left:
                if (_btnPrev.Enabled) { _engine.Pause(); _engine.Prev(); return true; }
                break;
            case Keys.Right:
                if (_btnNext.Enabled) { _engine.Pause(); _engine.Next(); return true; }
                break;
            case Keys.Home:
                if (_btnFirst.Enabled) { _engine.Pause(); _engine.GoToStart(); return true; }
                break;
            case Keys.End:
                if (_btnLast.Enabled) { _engine.Pause(); _engine.GoToEnd(); return true; }
                break;
            case Keys.Enter:
                if (_btnRun.Enabled && !_engine.HasSteps)
                { OnRunClicked(null, EventArgs.Empty); return true; }
                break;
            case Keys.F:
                _canvas.FitToScreen();
                return true;
            case Keys.Control | Keys.D0:
            case Keys.Control | Keys.NumPad0:
                _canvas.ResetView();
                return true;

            // Undo / Redo / Export
            case Keys.Control | Keys.Z:
                DoUndo(); return true;
            case Keys.Control | Keys.Y:
                DoRedo(); return true;
            case Keys.Control | Keys.E:
                ExportPng(); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ─── Undo / Redo ───────────────────────────────────────────────────

    private void PushHistory(Graph g)
    {
        // Xóa future history khi có action mới
        if (_historyIndex < _history.Count - 1)
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);

        _history.Add(GraphSerializer.SerializeToString(g));
        _historyIndex = _history.Count - 1;

        // Giới hạn bộ nhớ
        if (_history.Count > MaxHistory)
        {
            _history.RemoveAt(0);
            _historyIndex = _history.Count - 1;
        }
        UpdateUndoRedoButtons();
    }

    private void DoUndo()
    {
        if (_historyIndex <= 0) return;
        _historyIndex--;
        RestoreHistoryState(_history[_historyIndex]);
    }

    private void DoRedo()
    {
        if (_historyIndex >= _history.Count - 1) return;
        _historyIndex++;
        RestoreHistoryState(_history[_historyIndex]);
    }

    private void RestoreHistoryState(string json)
    {
        var g = GraphSerializer.DeserializeFromString(json);
        if (g == null) return;

        _isRestoring = true;
        try
        {
            _suppressDirectedChange = true;
            _btnDirected.Checked    = g.Directed;
            _btnDirected.Text       = g.Directed ? "⇄  Có hướng ✓" : "⇄  Có hướng";
            _btnDirected.ForeColor  = g.Directed ? Color.FromArgb(255, 200, 60) : Color.White;
            _suppressDirectedChange = false;

            _engine.Pause();
            _canvas.RefreshGraph(g);
            _canvas.ClearStep();
            RefreshStartNodeCombo();
            ResetAnimUI();
            UpdateStatus();
            if (_repPanel.Visible) _repPanel.RefreshFromGraph(g);
        }
        finally { _isRestoring = false; }

        UpdateUndoRedoButtons();
    }

    private void UpdateUndoRedoButtons()
    {
        if (_btnUndo != null) _btnUndo.Enabled = _historyIndex > 0;
        if (_btnRedo != null) _btnRedo.Enabled = _historyIndex < _history.Count - 1;
    }

    // ─── Export PNG ────────────────────────────────────────────────────

    private void ExportPng()
    {
        using var dlg = new SaveFileDialog
        {
            Title      = "Xuất đồ thị ra PNG",
            Filter     = "PNG Image (*.png)|*.png|All files (*.*)|*.*",
            DefaultExt = "png",
            FileName   = "graph_export"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            using var bmp = _canvas.ExportToBitmap(scaleFactor: 2);
            bmp.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
            _lblMode.Text = $"✓ Đã xuất: {Path.GetFileName(dlg.FileName)} ({bmp.Width}×{bmp.Height}px)   ";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể xuất PNG:\n{ex.Message}",
                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

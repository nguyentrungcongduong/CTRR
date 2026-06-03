using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GraphApp.Core.Converters;
using GraphApp.Core.Models;

namespace GraphApp.UI.Controls;

/// <summary>
/// Panel bên phải: hiển thị đồ thị dưới 3 dạng biểu diễn.
/// Tab 1 — Ma Trận Kề  : DataGridView (có thể chỉnh sửa)
/// Tab 2 — Danh Sách Kề: RichTextBox (có thể chỉnh sửa)
/// Tab 3 — Danh Sách Cạnh: DataGridView 3 cột (double-click để sửa)
/// Nút "Áp dụng" → parse ngược → gọi event GraphApplied(newGraph).
/// </summary>
public class RepresentationPanel : Panel
{
    // ─── Event ─────────────────────────────────────────────────────────
    /// <summary>Kích hoạt khi người dùng nhấn Áp Dụng trên bất kỳ tab nào.</summary>
    public event Action<Graph>? GraphApplied;

    // ─── State ─────────────────────────────────────────────────────────
    private Graph?   _graph;
    private bool     _syncing;      // tránh vòng lặp refresh

    // ─── Controls ──────────────────────────────────────────────────────
    private readonly TabControl   _tabs;

    // Tab 1: Ma Trận Kề
    private readonly DataGridView _dgv;
    private string[] _matrixLabels = [];

    // Tab 2: Danh Sách Kề
    private readonly RichTextBox  _rtb;

    // Tab 3: Danh Sách Cạnh
    private readonly DataGridView _dgvEdge;   // thành DataGridView — double-click để sửa

    // ─── Constructor ───────────────────────────────────────────────────
    public RepresentationPanel()
    {
        Dock      = DockStyle.Right;
        Width     = 390;
        BackColor = Color.FromArgb(28, 31, 38);
        Padding   = new Padding(0);

        // Header
        var header = new Label
        {
            Text      = "  📊  Biểu Diễn Đồ Thị",
            Dock      = DockStyle.Top,
            Height    = 34,
            BackColor = Color.FromArgb(40, 44, 55),
            ForeColor = Color.FromArgb(200, 210, 255),
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0)
        };

        // Build tabs
        _tabs = new TabControl
        {
            Dock       = DockStyle.Fill,
            Font       = new Font("Segoe UI", 9f),
            Appearance = TabAppearance.Normal,
            Padding    = new System.Drawing.Point(10, 4)
        };

        var (tab1, dgv)      = BuildMatrixTab();
        var (tab2, rtb)      = BuildAdjListTab();
        var (tab3, edgeDgv)  = BuildEdgeListTab();

        _dgv     = dgv;
        _rtb     = rtb;
        _dgvEdge = edgeDgv;

        _tabs.TabPages.Add(tab1);
        _tabs.TabPages.Add(tab2);
        _tabs.TabPages.Add(tab3);

        Controls.Add(_tabs);
        Controls.Add(header);   // Add after tabs so header appears on top (docking)
    }

    // ─── Public API ────────────────────────────────────────────────────

    /// <summary>Cập nhật toàn bộ 3 tab từ graph mới.</summary>
    public void RefreshFromGraph(Graph graph)
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            _graph = graph;
            RefreshMatrix();
            RefreshAdjList();
            RefreshEdgeList();
        }
        finally { _syncing = false; }
    }

    // ─── Tab 1: Ma Trận Kề ─────────────────────────────────────────────

    private (TabPage, DataGridView) BuildMatrixTab()
    {
        var tab = new TabPage("Ma Trận Kề")
        {
            BackColor = Color.FromArgb(34, 37, 46),
            Padding   = new Padding(6)
        };

        var dgv = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            BackgroundColor       = Color.FromArgb(28, 31, 38),
            GridColor             = Color.FromArgb(55, 60, 75),
            ForeColor             = Color.FromArgb(210, 215, 230),
            // DefaultCellStyle: base fallback
            DefaultCellStyle      = { BackColor = Color.FromArgb(34, 37, 46),
                                      ForeColor = Color.FromArgb(210, 215, 230),
                                      SelectionBackColor = Color.FromArgb(60, 100, 160),
                                      SelectionForeColor = Color.White,
                                      Font = new Font("Consolas", 9f) },
            // RowsDefaultCellStyle: override visual-styles renderer (quan trọng cho dark mode)
            RowsDefaultCellStyle           = { BackColor = Color.FromArgb(34, 37, 46),
                                               ForeColor = Color.FromArgb(210, 215, 230),
                                               SelectionBackColor = Color.FromArgb(60, 100, 160),
                                               SelectionForeColor = Color.White },
            AlternatingRowsDefaultCellStyle = { BackColor = Color.FromArgb(38, 42, 54),
                                                ForeColor = Color.FromArgb(210, 215, 230),
                                                SelectionBackColor = Color.FromArgb(65, 105, 165),
                                                SelectionForeColor = Color.White },
            ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(45, 50, 65),
                                              ForeColor = Color.FromArgb(180, 200, 255),
                                              Font = new Font("Segoe UI", 9f, FontStyle.Bold) },
            RowHeadersVisible     = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.AllCells,
            BorderStyle           = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight   = 28
        };


        var applyBtn = MakeApplyBtn();
        applyBtn.Click += (_, _) => ApplyMatrix();

        tab.Controls.Add(dgv);
        tab.Controls.Add(applyBtn);
        return (tab, dgv);
    }

    private void RefreshMatrix()
    {
        if (_graph == null) return;
        var (matrix, labels) = GraphConverter.ToAdjMatrix(_graph);
        _matrixLabels = labels;
        int n = labels.Length;

        _dgv.Columns.Clear();
        _dgv.Rows.Clear();

        // Cột đầu: nhãn hàng (read-only)
        var headerCol = new DataGridViewTextBoxColumn
        {
            Name = "_hdr", HeaderText = "",
            Width = 48, ReadOnly = true,
            DefaultCellStyle = { BackColor  = Color.FromArgb(45, 50, 65),
                                  ForeColor  = Color.FromArgb(180, 200, 255),
                                  Font       = new Font("Segoe UI", 9f, FontStyle.Bold),
                                  Alignment  = DataGridViewContentAlignment.MiddleCenter }
        };
        _dgv.Columns.Add(headerCol);

        for (int c = 0; c < n; c++)
        {
            _dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = $"c{c}", HeaderText = labels[c], Width = 46,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
        }

        for (int r = 0; r < n; r++)
        {
            var vals = new object[n + 1];
            vals[0] = labels[r];
            for (int c = 0; c < n; c++)
                vals[c + 1] = matrix[r, c] == 0 ? "0" : FormatW(matrix[r, c]);
            _dgv.Rows.Add(vals);
            _dgv.Rows[r].Cells[0].ReadOnly = true;

            // Đường chéo: read-only (self-loop thường = 0)
            _dgv.Rows[r].Cells[r + 1].Style.BackColor = Color.FromArgb(50, 55, 70);
            _dgv.Rows[r].Cells[r + 1].ReadOnly = true;
        }
    }

    private void ApplyMatrix()
    {
        if (_graph == null || _matrixLabels.Length == 0) return;
        int n = _matrixLabels.Length;
        var matrix = new double[n, n];

        for (int r = 0; r < Math.Min(_dgv.Rows.Count, n); r++)
            for (int c = 0; c < n; c++)
            {
                var raw = _dgv.Rows[r].Cells[c + 1].Value?.ToString() ?? "0";
                if (double.TryParse(raw, out double w)) matrix[r, c] = w;
            }

        var newGraph = GraphConverter.FromAdjMatrix(matrix, _matrixLabels,
            _graph.Directed);
        GraphApplied?.Invoke(newGraph);
    }

    // ─── Tab 2: Danh Sách Kề ──────────────────────────────────────────

    private (TabPage, RichTextBox) BuildAdjListTab()
    {
        var tab = new TabPage("Danh Sách Kề")
        {
            BackColor = Color.FromArgb(34, 37, 46),
            Padding   = new Padding(6)
        };

        var hint = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 22,
            Text      = "  Cú pháp: A: B(4) → C(2)  |  trọng số = 1 nếu bỏ qua",
            Font      = new Font("Segoe UI", 7.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(120, 130, 150),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var rtb = new RichTextBox
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(28, 31, 38),
            ForeColor = Color.FromArgb(210, 220, 245),
            Font      = new Font("Consolas", 9.5f),
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            WordWrap    = false
        };

        var applyBtn = MakeApplyBtn();
        applyBtn.Click += (_, _) => ApplyAdjList();

        tab.Controls.Add(rtb);
        tab.Controls.Add(hint);
        tab.Controls.Add(applyBtn);
        return (tab, rtb);
    }

    private void RefreshAdjList()
    {
        if (_graph == null) return;
        var adjList = GraphConverter.ToAdjList(_graph);
        _rtb.Text = GraphConverter.AdjListToString(adjList);
    }

    private void ApplyAdjList()
    {
        if (_graph == null) return;
        var adjList = ParseAdjListText(_rtb.Text);
        if (adjList == null)
        {
            MessageBox.Show("Không thể phân tích danh sách kề.\nKiểm tra lại định dạng: A: B(4) → C(2)",
                "Lỗi định dạng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        GraphApplied?.Invoke(GraphConverter.FromAdjList(adjList, _graph.Directed));
    }

    /// <summary>Parse "A: B(4) → C(2)\nB: ..." thành Dictionary.</summary>
    private static Dictionary<string, List<(string, double)>>?
        ParseAdjListText(string text)
    {
        var result = new Dictionary<string, List<(string, double)>>();
        var lines  = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            int colonIdx = line.IndexOf(':');
            if (colonIdx < 0) return null;

            string key  = line[..colonIdx].Trim();
            string rest = line[(colonIdx + 1)..].Trim();
            var neighbors = new List<(string, double)>();

            if (!rest.StartsWith("(") && !string.IsNullOrWhiteSpace(rest))
            {
                // "B(4) → C(2)" hay "B → C"
                var parts = rest.Split(new[] { " → ", "->", "→" },
                    StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    var m = Regex.Match(part.Trim(), @"^(.+?)(?:\((\d+(?:[.,]\d+)?)\))?$");
                    if (!m.Success) return null;

                    string nb = m.Groups[1].Value.Trim();
                    double w  = m.Groups[2].Success
                        ? double.Parse(m.Groups[2].Value.Replace(',', '.'),
                            System.Globalization.CultureInfo.InvariantCulture)
                        : 1.0;
                    neighbors.Add((nb, w));
                }
            }

            result[key] = neighbors;
        }

        return result.Count == 0 ? null : result;
    }

    // ─── Tab 3: Danh Sách Cạnh ────────────────────────────────────────

    private (TabPage, DataGridView) BuildEdgeListTab()
    {
        var tab = new TabPage("Danh Sách Cạnh")
        {
            BackColor = Color.FromArgb(34, 37, 46),
            Padding   = new Padding(6)
        };

        // DataGridView — 3 cột sửa được toàn bộ
        var dgv = new DataGridView
        {
            Dock      = DockStyle.Fill,
            BackgroundColor       = Color.FromArgb(28, 31, 38),
            GridColor             = Color.FromArgb(55, 60, 75),
            ForeColor             = Color.FromArgb(210, 215, 230),
            DefaultCellStyle      = { BackColor = Color.FromArgb(34, 37, 46),
                                      ForeColor = Color.FromArgb(210, 220, 245),
                                      SelectionBackColor = Color.FromArgb(60, 100, 160),
                                      SelectionForeColor = Color.White,
                                      Font = new Font("Consolas", 9.5f) },
            RowsDefaultCellStyle  = { BackColor = Color.FromArgb(34, 37, 46),
                                      ForeColor = Color.FromArgb(210, 220, 245),
                                      SelectionBackColor = Color.FromArgb(60, 100, 160),
                                      SelectionForeColor = Color.White },
            AlternatingRowsDefaultCellStyle = { BackColor = Color.FromArgb(38, 42, 54),
                                                ForeColor = Color.FromArgb(210, 220, 245),
                                                SelectionBackColor = Color.FromArgb(65, 105, 165),
                                                SelectionForeColor = Color.White },
            ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(45, 50, 65),
                                              ForeColor = Color.FromArgb(180, 200, 255),
                                              Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                                              Alignment = DataGridViewContentAlignment.MiddleCenter },
            RowHeadersVisible     = false,
            AllowUserToAddRows    = false,   // dùng nút riêng
            AllowUserToDeleteRows = false,
            BorderStyle           = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight   = 28,
            EditMode              = DataGridViewEditMode.EditOnKeystrokeOrF2,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect
        };
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name="src", HeaderText="Nguồn", Width=100 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name="tgt", HeaderText="Đích",   Width=100 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name="w",   HeaderText="Trọng số", Width=90  });
        dgv.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgv.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgv.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        // + Thêm cạnh — thêm hàng trống, tự focus vào edit
        var addRow = new Button
        {
            Text      = "+ Thêm cạnh",
            Dock      = DockStyle.Bottom,
            Height    = 26,
            BackColor = Color.FromArgb(45, 50, 65),
            ForeColor = Color.FromArgb(160, 200, 255),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.5f)
        };
        addRow.FlatAppearance.BorderSize = 0;
        addRow.Click += (_, _) =>
        {
            int idx = dgv.Rows.Add("A", "B", "1");
            dgv.ClearSelection();
            dgv.Rows[idx].Selected = true;
            dgv.CurrentCell = dgv.Rows[idx].Cells[0];
            dgv.BeginEdit(true);    // bắt đầu sửa ngay
        };

        // − Xóa cạnh — xóa hàng đang chọn
        var delRow = new Button
        {
            Text      = "− Xóa cạnh",
            Dock      = DockStyle.Bottom,
            Height    = 26,
            BackColor = Color.FromArgb(45, 50, 65),
            ForeColor = Color.FromArgb(255, 160, 160),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.5f)
        };
        delRow.FlatAppearance.BorderSize = 0;
        delRow.Click += (_, _) =>
        {
            var toDelete = dgv.SelectedRows.Cast<DataGridViewRow>().ToList();
            foreach (var row in toDelete)
                if (!row.IsNewRow) dgv.Rows.Remove(row);
        };

        var applyBtn = MakeApplyBtn();
        applyBtn.Click += (_, _) => ApplyEdgeList();

        tab.Controls.Add(dgv);
        tab.Controls.Add(addRow);
        tab.Controls.Add(delRow);
        tab.Controls.Add(applyBtn);
        return (tab, dgv);
    }

    private void RefreshEdgeList()
    {
        if (_graph == null) return;
        _dgvEdge.Rows.Clear();
        foreach (var (src, tgt, w) in GraphConverter.ToEdgeList(_graph))
            _dgvEdge.Rows.Add(src, tgt, FormatW(w));
    }

    private void ApplyEdgeList()
    {
        if (_graph == null) return;

        // Commit ô đang edit trước khi parse
        if (_dgvEdge.IsCurrentCellInEditMode)
            _dgvEdge.EndEdit();

        // Parse tất cả hàng từ DataGridView
        var rows = new List<(string Src, string Tgt, double W)>();
        foreach (DataGridViewRow row in _dgvEdge.Rows)
        {
            if (row.IsNewRow) continue;
            string src = row.Cells["src"].Value?.ToString()?.Trim() ?? "";
            string tgt = row.Cells["tgt"].Value?.ToString()?.Trim() ?? "";
            string wStr = row.Cells["w"].Value?.ToString() ?? "1";
            double w = double.TryParse(wStr, out double d) ? d : 1.0;
            if (!string.IsNullOrWhiteSpace(src) && !string.IsNullOrWhiteSpace(tgt) && src != tgt)
                rows.Add((src, tgt, w));
        }

        // Clone graph hiện tại — GIỮ VỊ TRÍ NODE
        var newGraph = _graph.Clone();
        var labelToId = newGraph.Nodes.ToDictionary(n => n.Label, n => n.Id);
        newGraph.Edges.Clear();

        var addedEdges = new HashSet<(int, int)>();
        foreach (var (srcLabel, tgtLabel, weight) in rows)
        {
            if (!labelToId.TryGetValue(srcLabel, out int srcId))
            {
                srcId = newGraph.AddNode(300, 300, srcLabel);
                labelToId[srcLabel] = srcId;
            }
            if (!labelToId.TryGetValue(tgtLabel, out int tgtId))
            {
                tgtId = newGraph.AddNode(450, 300, tgtLabel);
                labelToId[tgtLabel] = tgtId;
            }
            if (!newGraph.Directed)
            {
                int lo = Math.Min(srcId, tgtId), hi = Math.Max(srcId, tgtId);
                if (!addedEdges.Add((lo, hi))) continue;
            }
            newGraph.AddEdge(srcId, tgtId, weight);
        }

        GraphApplied?.Invoke(newGraph);
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    private static Button MakeApplyBtn() => new()
    {
        Text      = "🔄  Áp Dụng vào Canvas",
        Dock      = DockStyle.Bottom,
        Height    = 32,
        BackColor = Color.FromArgb(46, 140, 67),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
        Cursor    = Cursors.Hand
    };

    private static string FormatW(double w) =>
        w == Math.Floor(w) ? ((int)w).ToString() : w.ToString("F2");
}

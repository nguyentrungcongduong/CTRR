using System.Drawing;
using System.Windows.Forms;
using GraphApp.Core.Converters;
using GraphApp.Core.Models;

namespace GraphApp.UI.Forms;

/// <summary>
/// Form nhập đồ thị từ ma trận kề.
/// Bước 1: Nhập số đỉnh + tên các đỉnh + loại đồ thị.
/// Bước 2: Điền ma trận kề vào DataGridView.
/// Kết quả: trả về Graph qua property ResultGraph.
/// </summary>
public class InputMatrixForm : Form
{
    // ─── Result ────────────────────────────────────────────────────────
    public Graph? ResultGraph { get; private set; }

    // ─── Step tracking ─────────────────────────────────────────────────
    private int _step = 1;

    // ─── Controls Step 1 ───────────────────────────────────────────────
    private readonly NumericUpDown _numNodes;
    private readonly TextBox       _txtLabels;
    private readonly CheckBox      _chkDirected;
    private readonly Panel         _step1Panel;

    // ─── Controls Step 2 ───────────────────────────────────────────────
    private readonly DataGridView  _dgv;
    private readonly Panel         _step2Panel;
    private string[] _labels = [];

    // ─── Nav buttons ───────────────────────────────────────────────────
    private readonly Button _btnNext;
    private readonly Button _btnBack;
    private readonly Button _btnOk;
    private readonly Label  _lblTitle;

    // ─── Constructor ───────────────────────────────────────────────────
    public InputMatrixForm()
    {
        Text            = "Nhập đồ thị từ ma trận kề";
        Size            = new Size(560, 480);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        BackColor       = Color.FromArgb(38, 42, 52);
        ForeColor       = Color.White;

        // ── Title ──────────────────────────────────────────────────────
        _lblTitle = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 44,
            Text      = "  Bước 1/2 — Khai báo đỉnh",
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 210, 255),
            BackColor = Color.FromArgb(28, 32, 42),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(12, 0, 0, 0)
        };

        // ── Step 1 Panel ───────────────────────────────────────────────
        _step1Panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        var tableLayout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 4,
            Padding     = new Padding(20, 16, 20, 8),
            BackColor   = Color.Transparent
        };
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        // Row 0: Số đỉnh
        tableLayout.Controls.Add(MakeLabel("Số đỉnh (2–20):"), 0, 0);
        _numNodes = new NumericUpDown
        {
            Minimum   = 2, Maximum = 20, Value = 4,
            BackColor = Color.FromArgb(48, 52, 65),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 10f),
            Anchor    = AnchorStyles.Left
        };
        _numNodes.ValueChanged += OnNodeCountChanged;
        tableLayout.Controls.Add(_numNodes, 1, 0);

        // Row 1: Nhãn đỉnh
        tableLayout.Controls.Add(MakeLabel("Nhãn đỉnh\n(cách nhau bằng dấu phẩy):"), 0, 1);
        _txtLabels = new TextBox
        {
            Text      = "A,B,C,D",
            BackColor = Color.FromArgb(48, 52, 65),
            ForeColor = Color.White,
            Font      = new Font("Consolas", 10f),
            Anchor    = AnchorStyles.Left | AnchorStyles.Right
        };
        _txtLabels.TextChanged += OnLabelsChanged;
        tableLayout.Controls.Add(_txtLabels, 1, 1);

        // Row 2: Loại đồ thị
        tableLayout.Controls.Add(MakeLabel("Loại đồ thị:"), 0, 2);
        _chkDirected = new CheckBox
        {
            Text      = "Có hướng (Directed)",
            ForeColor = Color.FromArgb(180, 200, 240),
            Font      = new Font("Segoe UI", 9.5f),
            Anchor    = AnchorStyles.Left
        };
        tableLayout.Controls.Add(_chkDirected, 1, 2);

        // Row 3: Hint
        var hint = new Label
        {
            Text      = "💡 Trọng số = 0 nghĩa là không có cạnh.",
            ForeColor = Color.FromArgb(120, 140, 170),
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            Anchor    = AnchorStyles.Left
        };
        tableLayout.Controls.Add(hint, 1, 3);

        _step1Panel.Controls.Add(tableLayout);

        // ── Step 2 Panel ───────────────────────────────────────────────
        _step2Panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false };

        var matrixHint = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 26,
            Text      = "  Nhập trọng số vào ô (0 = không có cạnh):",
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(140, 160, 200),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _dgv = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            BackgroundColor       = Color.FromArgb(28, 31, 40),
            GridColor             = Color.FromArgb(60, 65, 80),
            ForeColor             = Color.FromArgb(210, 220, 245),
            DefaultCellStyle      = { BackColor = Color.FromArgb(38, 42, 52),
                                      ForeColor = Color.FromArgb(210, 220, 245),
                                      SelectionBackColor = Color.FromArgb(60, 100, 160),
                                      Font = new Font("Consolas", 10f),
                                      Alignment = DataGridViewContentAlignment.MiddleCenter },
            ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(50, 55, 70),
                                              ForeColor = Color.FromArgb(180, 200, 255),
                                              Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                                              Alignment = DataGridViewContentAlignment.MiddleCenter },
            RowHeadersVisible     = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            BorderStyle           = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight   = 30
        };

        _step2Panel.Controls.Add(_dgv);
        _step2Panel.Controls.Add(matrixHint);

        // ── Navigation buttons ─────────────────────────────────────────
        var btnPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Bottom,
            Height        = 48,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor     = Color.FromArgb(28, 32, 42),
            Padding       = new Padding(8, 8, 8, 8)
        };

        _btnOk = new Button
        {
            Text      = "✅  Tạo đồ thị",
            Width     = 130, Height = 32,
            BackColor = Color.FromArgb(46, 140, 67),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Visible   = false, Cursor = Cursors.Hand
        };
        _btnOk.FlatAppearance.BorderSize = 0;
        _btnOk.Click += OnOkClicked;

        _btnNext = new Button
        {
            Text      = "Tiếp theo ▶",
            Width     = 110, Height = 32,
            BackColor = Color.FromArgb(50, 100, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f),
            Cursor    = Cursors.Hand
        };
        _btnNext.FlatAppearance.BorderSize = 0;
        _btnNext.Click += OnNextClicked;

        _btnBack = new Button
        {
            Text      = "◀ Quay lại",
            Width     = 100, Height = 32,
            BackColor = Color.FromArgb(60, 65, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f),
            Visible   = false, Cursor = Cursors.Hand
        };
        _btnBack.FlatAppearance.BorderSize = 0;
        _btnBack.Click += OnBackClicked;

        var btnCancel = new Button
        {
            Text      = "Hủy",
            Width     = 70, Height = 32,
            BackColor = Color.FromArgb(80, 40, 40),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 9.5f),
            Cursor    = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(_btnOk);
        btnPanel.Controls.Add(_btnNext);
        btnPanel.Controls.Add(_btnBack);

        Controls.Add(_step1Panel);
        Controls.Add(_step2Panel);
        Controls.Add(btnPanel);
        Controls.Add(_lblTitle);
    }

    // ─── Event Handlers ────────────────────────────────────────────────

    private void OnNodeCountChanged(object? sender, EventArgs e)
    {
        // Auto-generate labels
        int n = (int)_numNodes.Value;
        var current = _txtLabels.Text.Split(',').Select(s => s.Trim()).ToArray();
        var newLabels = new string[n];
        for (int i = 0; i < n; i++)
            newLabels[i] = i < current.Length && !string.IsNullOrWhiteSpace(current[i])
                ? current[i]
                : ((char)('A' + i)).ToString();
        _txtLabels.Text = string.Join(",", newLabels);
    }

    private void OnLabelsChanged(object? sender, EventArgs e)
    {
        var lbls = _txtLabels.Text.Split(',').Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s)).ToArray();
        if (lbls.Length >= 2)
            _numNodes.Value = Math.Min(lbls.Length, 20);
    }

    private void OnNextClicked(object? sender, EventArgs e)
    {
        _labels = _txtLabels.Text.Split(',')
            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();

        if (_labels.Length < 2)
        {
            MessageBox.Show("Cần ít nhất 2 đỉnh.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        BuildMatrix(_labels);

        _step = 2;
        _step1Panel.Visible = false;
        _step2Panel.Visible = true;
        _btnNext.Visible    = false;
        _btnBack.Visible    = true;
        _btnOk.Visible      = true;
        _lblTitle.Text      = "  Bước 2/2 — Nhập ma trận kề";
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        _step = 1;
        _step2Panel.Visible = false;
        _step1Panel.Visible = true;
        _btnBack.Visible    = false;
        _btnOk.Visible      = false;
        _btnNext.Visible    = true;
        _lblTitle.Text      = "  Bước 1/2 — Khai báo đỉnh";
    }

    private void OnOkClicked(object? sender, EventArgs e)
    {
        int n = _labels.Length;
        var matrix = new double[n, n];

        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
            {
                var raw = _dgv.Rows[r].Cells[c + 1].Value?.ToString() ?? "0";
                if (double.TryParse(raw, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double w))
                    matrix[r, c] = w;
            }

        ResultGraph    = GraphConverter.FromAdjMatrix(matrix, _labels, _chkDirected.Checked);
        DialogResult   = DialogResult.OK;
        Close();
    }

    // ─── Build Matrix Grid ─────────────────────────────────────────────

    private void BuildMatrix(string[] labels)
    {
        int n = labels.Length;
        _dgv.Columns.Clear();
        _dgv.Rows.Clear();

        // Cột đầu: nhãn hàng
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "_hdr", HeaderText = "",
            Width = 48, ReadOnly = true,
            DefaultCellStyle = { BackColor  = Color.FromArgb(50, 55, 70),
                                  ForeColor  = Color.FromArgb(180, 200, 255),
                                  Font       = new Font("Segoe UI", 9f, FontStyle.Bold),
                                  Alignment  = DataGridViewContentAlignment.MiddleCenter }
        });

        for (int c = 0; c < n; c++)
            _dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = $"c{c}", HeaderText = labels[c], Width = 46,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

        for (int r = 0; r < n; r++)
        {
            var vals = new object[n + 1];
            vals[0] = labels[r];
            for (int c = 0; c < n; c++) vals[c + 1] = "0";
            _dgv.Rows.Add(vals);
            _dgv.Rows[r].Cells[0].ReadOnly = true;
            // Diagonal
            _dgv.Rows[r].Cells[r + 1].Style.BackColor = Color.FromArgb(50, 55, 70);
            _dgv.Rows[r].Cells[r + 1].ReadOnly = true;
        }
    }

    private static Label MakeLabel(string text) => new()
    {
        Text      = text,
        ForeColor = Color.FromArgb(180, 195, 225),
        Font      = new Font("Segoe UI", 9.5f),
        AutoSize  = false,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding   = new Padding(0, 6, 8, 6)
    };
}

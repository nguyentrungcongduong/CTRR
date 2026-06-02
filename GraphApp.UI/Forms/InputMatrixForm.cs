using System.Windows.Forms;

namespace GraphApp.UI.Forms;

/// <summary>
/// Form nhập ma trận kề từ đầu: số đỉnh → tên đỉnh → nhập ma trận.
/// </summary>
public class InputMatrixForm : Form
{
    private readonly DataGridView _grid;
    private readonly NumericUpDown _numNodes;
    private readonly TextBox _txtLabels;
    private readonly Button _btnGenerate;
    private readonly Button _btnApply;
    private readonly Button _btnCancel;

    public double[,]? ResultMatrix { get; private set; }
    public string[]?  ResultLabels { get; private set; }
    public bool       Directed     { get; private set; }

    public InputMatrixForm()
    {
        Text            = "Nhập ma trận kề";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition   = FormStartPosition.CenterParent;
        MinimumSize     = new System.Drawing.Size(500, 400);
        ClientSize      = new System.Drawing.Size(600, 450);

        // TODO: TASK-11 — build full UI
        _grid        = new DataGridView { Dock = DockStyle.Fill };
        _numNodes    = new NumericUpDown();
        _txtLabels   = new TextBox();
        _btnGenerate = new Button { Text = "Tạo bảng" };
        _btnApply    = new Button { Text = "Áp dụng", DialogResult = DialogResult.OK };
        _btnCancel   = new Button { Text = "Hủy",     DialogResult = DialogResult.Cancel };

        AcceptButton = _btnApply;
        CancelButton = _btnCancel;
        Controls.Add(_grid);
    }
}

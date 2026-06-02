using System.Windows.Forms;

namespace GraphApp.UI.Forms;

/// <summary>
/// Dialog nhập trọng số khi người dùng thêm cạnh bằng chuột.
/// </summary>
public class EdgeWeightDialog : Form
{
    private readonly NumericUpDown _numWeight;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    public double Weight => (double)_numWeight.Value;

    public EdgeWeightDialog(double defaultWeight = 1.0)
    {
        Text            = "Nhập trọng số cạnh";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ClientSize      = new System.Drawing.Size(260, 100);

        var label = new Label
        {
            Text     = "Trọng số:",
            Location = new System.Drawing.Point(12, 15),
            AutoSize = true
        };

        _numWeight = new NumericUpDown
        {
            Location      = new System.Drawing.Point(90, 12),
            Width         = 140,
            DecimalPlaces = 2,
            Minimum       = 0,
            Maximum       = 100000,
            Value         = (decimal)defaultWeight
        };

        _btnOk = new Button
        {
            Text         = "OK",
            DialogResult = DialogResult.OK,
            Location     = new System.Drawing.Point(90, 55),
            Width        = 80
        };

        _btnCancel = new Button
        {
            Text         = "Hủy",
            DialogResult = DialogResult.Cancel,
            Location     = new System.Drawing.Point(180, 55),
            Width        = 60
        };

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
        Controls.AddRange(new Control[] { label, _numWeight, _btnOk, _btnCancel });
    }
}

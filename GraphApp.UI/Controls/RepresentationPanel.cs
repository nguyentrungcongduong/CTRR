using System.Windows.Forms;
using GraphApp.Core.Models;

namespace GraphApp.UI.Controls;

/// <summary>
/// Panel hiển thị 3 dạng biểu diễn đồ thị qua TabControl:
/// Tab 1: Ma trận kề (DataGridView)
/// Tab 2: Danh sách kề (RichTextBox)
/// Tab 3: Danh sách cạnh (ListView)
/// </summary>
public class RepresentationPanel : UserControl
{
    private readonly TabControl _tabControl;
    private readonly DataGridView _matrixGrid;
    private readonly RichTextBox _adjListBox;
    private readonly ListView _edgeListView;

    public event Action<Graph>? GraphImported;

    public RepresentationPanel()
    {
        _tabControl   = new TabControl { Dock = DockStyle.Fill };
        _matrixGrid   = new DataGridView { Dock = DockStyle.Fill };
        _adjListBox   = new RichTextBox { Dock = DockStyle.Fill, Font = new System.Drawing.Font("Consolas", 10) };
        _edgeListView = new ListView { Dock = DockStyle.Fill, View = View.Details };

        _edgeListView.Columns.Add("Nguồn", 80);
        _edgeListView.Columns.Add("Đích",  80);
        _edgeListView.Columns.Add("Trọng số", 80);

        var tabMatrix   = new TabPage("Ma trận kề");
        var tabAdjList  = new TabPage("Danh sách kề");
        var tabEdgeList = new TabPage("Danh sách cạnh");

        tabMatrix.Controls.Add(_matrixGrid);
        tabAdjList.Controls.Add(_adjListBox);
        tabEdgeList.Controls.Add(_edgeListView);

        _tabControl.TabPages.Add(tabMatrix);
        _tabControl.TabPages.Add(tabAdjList);
        _tabControl.TabPages.Add(tabEdgeList);

        Controls.Add(_tabControl);
    }

    /// <summary>Cập nhật tất cả 3 tab khi đồ thị thay đổi.</summary>
    public void UpdateAll(Graph graph)
    {
        // TODO: TASK-11 — implement ở Phase 2
    }
}

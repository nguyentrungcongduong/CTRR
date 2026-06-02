using System.Windows.Forms;
using GraphApp.Core.Models;
using GraphApp.UI.Controls;

namespace GraphApp.UI.Forms;

/// <summary>
/// Form chính của ứng dụng GraphApp.
/// Layout: Canvas (trái, lớn) | Panel info (phải).
/// Toolbar trên đỉnh.
/// </summary>
public partial class MainForm : Form
{
    private readonly GraphCanvas _canvas;

    public MainForm()
    {
        InitializeComponent();

        // ── Canvas ──────────────────────────────────────────────────
        _canvas = new GraphCanvas { Dock = DockStyle.Fill };
        Controls.Add(_canvas);

        // ── Đồ thị mẫu để kiểm tra TASK-03 ─────────────────────────
        LoadSampleGraph();

        Text          = "GraphApp — Ứng dụng Đồ thị";
        MinimumSize   = new System.Drawing.Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState   = FormWindowState.Maximized;
    }

    // ────────────────────────────────────────────────────────────────
    // Sample graph — 6 đỉnh, 7 cạnh, vô hướng có trọng số
    // Dùng để kiểm tra render tĩnh (TASK-03)
    // ────────────────────────────────────────────────────────────────
    private void LoadSampleGraph()
    {
        var g = new Graph { Directed = false };

        // Thêm đỉnh tại các vị trí trên canvas
        int a = g.AddNode(200, 150);   // A
        int b = g.AddNode(400, 100);   // B
        int c = g.AddNode(600, 150);   // C
        int d = g.AddNode(200, 350);   // D
        int e = g.AddNode(400, 320);   // E
        int f = g.AddNode(600, 350);   // F

        // Thêm cạnh có trọng số
        g.AddEdge(a, b, 4);
        g.AddEdge(a, d, 2);
        g.AddEdge(b, c, 5);
        g.AddEdge(b, e, 3);
        g.AddEdge(c, f, 1);
        g.AddEdge(d, e, 6);
        g.AddEdge(e, f, 7);

        _canvas.RefreshGraph(g);
    }
}

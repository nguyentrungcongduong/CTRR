using System.Windows.Forms;
using GraphApp.Core.Models;
using GraphApp.UI.Controls;

namespace GraphApp.UI.Forms;

/// <summary>
/// Form chính của ứng dụng GraphApp.
/// Chứa: Toolbar, GraphCanvas, RepresentationPanel, AnimationEngine controls, StatusBar.
/// </summary>
public partial class MainForm : Form
{
    private readonly GraphCanvas _canvas;
    private readonly AnimationEngine _animEngine;
    private readonly RepresentationPanel _repPanel;

    public MainForm()
    {
        InitializeComponent();

        _canvas     = new GraphCanvas { Dock = DockStyle.Fill };
        _animEngine = new AnimationEngine();
        _repPanel   = new RepresentationPanel { Dock = DockStyle.Fill };

        // TODO: TASK-03/04/07 — kết nối các controls vào layout
        // Placeholder: chỉ thêm canvas tạm
        Controls.Add(_canvas);

        Text            = "GraphApp — Ứng dụng Đồ thị";
        MinimumSize     = new System.Drawing.Size(1024, 720);
        StartPosition   = FormStartPosition.CenterScreen;
        WindowState     = FormWindowState.Maximized;
    }
}

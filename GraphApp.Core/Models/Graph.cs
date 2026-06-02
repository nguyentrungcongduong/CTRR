namespace GraphApp.Core.Models;

/// <summary>
/// Model chính của đồ thị: chứa danh sách đỉnh và danh sách cạnh.
/// Hỗ trợ cả đồ thị có hướng và vô hướng.
/// </summary>
public class Graph
{
    private int _nextNodeId = 1;
    private int _nextEdgeId = 1;

    public bool Directed { get; set; } = false;

    public List<Node> Nodes { get; private set; } = new();
    public List<Edge> Edges { get; private set; } = new();

    // ─── Node Operations ───────────────────────────────────────────────

    /// <summary>Thêm đỉnh mới tại tọa độ (x, y). Trả về Id của đỉnh vừa tạo.</summary>
    public int AddNode(float x, float y, string? label = null)
    {
        var node = new Node
        {
            Id       = _nextNodeId++,
            Label    = label ?? (_nextNodeId - 1).ToString(),
            Position = new System.Drawing.PointF(x, y)
        };
        Nodes.Add(node);
        return node.Id;
    }

    /// <summary>Xóa đỉnh theo Id và tất cả cạnh liên quan.</summary>
    public void RemoveNode(int id)
    {
        Nodes.RemoveAll(n => n.Id == id);
        Edges.RemoveAll(e => e.Source == id || e.Target == id);
    }

    // ─── Edge Operations ───────────────────────────────────────────────

    /// <summary>Thêm cạnh. Trả về Id của cạnh vừa tạo.</summary>
    public int AddEdge(int source, int target, double weight = 1.0)
    {
        var edge = new Edge
        {
            Id     = _nextEdgeId++,
            Source = source,
            Target = target,
            Weight = weight
        };
        Edges.Add(edge);
        return edge.Id;
    }

    /// <summary>Xóa cạnh theo Id.</summary>
    public void RemoveEdge(int id) => Edges.RemoveAll(e => e.Id == id);

    // ─── Query ─────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về danh sách các đỉnh kề với <paramref name="nodeId"/>.
    /// Mỗi phần tử: (nodeId kề, edgeId, weight)
    /// </summary>
    public List<(int Node, int EdgeId, double Weight)> Neighbors(int nodeId)
    {
        var result = new List<(int, int, double)>();

        foreach (var e in Edges)
        {
            if (e.Source == nodeId)
                result.Add((e.Target, e.Id, e.Weight));
            else if (!Directed && e.Target == nodeId)
                result.Add((e.Source, e.Id, e.Weight));
        }

        return result;
    }

    /// <summary>Lấy Node theo Id. Trả về null nếu không tồn tại.</summary>
    public Node? GetNode(int id) => Nodes.FirstOrDefault(n => n.Id == id);

    /// <summary>Lấy Edge theo Id. Trả về null nếu không tồn tại.</summary>
    public Edge? GetEdge(int id) => Edges.FirstOrDefault(e => e.Id == id);

    // ─── Utility ───────────────────────────────────────────────────────

    /// <summary>Deep copy đồ thị (dùng cho animation).</summary>
    public Graph Clone()
    {
        var clone = new Graph
        {
            Directed   = this.Directed,
            _nextNodeId = this._nextNodeId,
            _nextEdgeId = this._nextEdgeId
        };

        foreach (var n in Nodes)
            clone.Nodes.Add(new Node { Id = n.Id, Label = n.Label, Position = n.Position });

        foreach (var e in Edges)
            clone.Edges.Add(new Edge { Id = e.Id, Source = e.Source, Target = e.Target, Weight = e.Weight });

        return clone;
    }

    /// <summary>Xóa toàn bộ đỉnh và cạnh, reset bộ đếm Id.</summary>
    public void Clear()
    {
        Nodes.Clear();
        Edges.Clear();
        _nextNodeId = 1;
        _nextEdgeId = 1;
    }
}

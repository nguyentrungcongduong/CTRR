namespace GraphApp.Core.Models;

/// <summary>
/// Model chính của đồ thị — chứa danh sách đỉnh và danh sách cạnh.
/// Hỗ trợ cả đồ thị có hướng (directed) và vô hướng (undirected).
/// </summary>
public class Graph
{
    // Bộ đếm nội bộ để sinh Id tự tăng — không reset khi xóa từng đỉnh/cạnh,
    // chỉ reset khi gọi Clear() để tránh Id bị trùng lặp trong 1 phiên làm việc.
    private int _nextNodeId = 1;
    private int _nextEdgeId = 1;

    // ─── Properties ────────────────────────────────────────────────────

    /// <summary>
    /// True = đồ thị có hướng (directed), False = vô hướng (undirected).
    /// Ảnh hưởng đến: vẽ mũi tên, Neighbors(), các thuật toán Euler, Ford-Fulkerson.
    /// </summary>
    public bool Directed { get; set; } = false;

    /// <summary>Danh sách tất cả đỉnh trong đồ thị.</summary>
    public List<Node> Nodes { get; private set; } = new();

    /// <summary>Danh sách tất cả cạnh trong đồ thị.</summary>
    public List<Edge> Edges { get; private set; } = new();

    // ─── Node Operations ───────────────────────────────────────────────

    /// <summary>
    /// Thêm đỉnh mới tại tọa độ (x, y).
    /// Nếu không truyền label, tự động tạo nhãn dạng chữ cái (A, B, C... AA, AB...).
    /// </summary>
    /// <returns>Id của đỉnh vừa tạo.</returns>
    public int AddNode(float x, float y, string? label = null)
    {
        int id = _nextNodeId++;
        var node = new Node
        {
            Id       = id,
            Label    = label ?? GenerateLabel(id),
            Position = new System.Drawing.PointF(x, y)
        };
        Nodes.Add(node);
        return id;
    }

    /// <summary>
    /// Xóa đỉnh theo Id và xóa toàn bộ cạnh liên quan đến đỉnh đó.
    /// </summary>
    public void RemoveNode(int id)
    {
        Nodes.RemoveAll(n => n.Id == id);
        Edges.RemoveAll(e => e.Source == id || e.Target == id);
    }

    // ─── Edge Operations ───────────────────────────────────────────────

    /// <summary>
    /// Thêm cạnh từ <paramref name="source"/> đến <paramref name="target"/>.
    /// Với đồ thị vô hướng, một cạnh đại diện cho cả 2 chiều.
    /// </summary>
    /// <returns>Id của cạnh vừa tạo. Trả về -1 nếu source hoặc target không tồn tại.</returns>
    public int AddEdge(int source, int target, double weight = 1.0)
    {
        // Kiểm tra đỉnh tồn tại
        if (GetNode(source) == null || GetNode(target) == null)
            return -1;

        // Kiểm tra tự vòng (self-loop)
        if (source == target)
            return -1;

        // Kiểm tra cạnh đã tồn tại (với đồ thị vô hướng: kiểm tra cả 2 chiều)
        if (EdgeExists(source, target))
            return -1;

        int id = _nextEdgeId++;
        Edges.Add(new Edge { Id = id, Source = source, Target = target, Weight = weight });
        return id;
    }

    /// <summary>Xóa cạnh theo Id.</summary>
    public void RemoveEdge(int id) => Edges.RemoveAll(e => e.Id == id);

    // ─── Query ─────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về danh sách các đỉnh kề với <paramref name="nodeId"/>.
    /// Với đồ thị vô hướng: trả về cả 2 đầu của cạnh.
    /// Với đồ thị có hướng: chỉ trả về đỉnh mà cạnh đi ra từ nodeId.
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

    /// <summary>
    /// Lấy cạnh giữa 2 đỉnh (nếu có).
    /// Với đồ thị vô hướng: kiểm tra cả 2 chiều.
    /// </summary>
    public Edge? GetEdgeBetween(int source, int target)
    {
        return Edges.FirstOrDefault(e =>
            (e.Source == source && e.Target == target) ||
            (!Directed && e.Source == target && e.Target == source));
    }

    /// <summary>Kiểm tra cạnh giữa 2 đỉnh có tồn tại không.</summary>
    public bool EdgeExists(int source, int target) => GetEdgeBetween(source, target) != null;

    /// <summary>Kiểm tra đỉnh có tồn tại không.</summary>
    public bool NodeExists(int id) => Nodes.Any(n => n.Id == id);

    // ─── Statistics ────────────────────────────────────────────────────

    /// <summary>Bậc (degree) của đỉnh: số cạnh liên thuộc.</summary>
    public int Degree(int nodeId)
    {
        if (Directed)
        {
            // Directed: phân biệt in-degree và out-degree
            return Edges.Count(e => e.Source == nodeId || e.Target == nodeId);
        }
        return Edges.Count(e => e.Source == nodeId || e.Target == nodeId);
    }

    /// <summary>Out-degree (đồ thị có hướng): số cạnh đi ra từ đỉnh.</summary>
    public int OutDegree(int nodeId) => Edges.Count(e => e.Source == nodeId);

    /// <summary>In-degree (đồ thị có hướng): số cạnh đi vào đỉnh.</summary>
    public int InDegree(int nodeId) => Edges.Count(e => e.Target == nodeId);

    // ─── Utility ───────────────────────────────────────────────────────

    /// <summary>
    /// Deep copy đồ thị — dùng cho animation (không làm thay đổi đồ thị gốc).
    /// </summary>
    public Graph Clone()
    {
        var clone = new Graph { Directed = this.Directed };
        clone._nextNodeId = this._nextNodeId;
        clone._nextEdgeId = this._nextEdgeId;

        foreach (var n in Nodes)
            clone.Nodes.Add(new Node { Id = n.Id, Label = n.Label, Position = n.Position });

        foreach (var e in Edges)
            clone.Edges.Add(new Edge { Id = e.Id, Source = e.Source, Target = e.Target, Weight = e.Weight });

        return clone;
    }

    /// <summary>Xóa toàn bộ đỉnh và cạnh, reset bộ đếm Id về 1.</summary>
    public void Clear()
    {
        Nodes.Clear();
        Edges.Clear();
        _nextNodeId = 1;
        _nextEdgeId = 1;
    }

    // ─── Private helpers ───────────────────────────────────────────────

    /// <summary>
    /// Sinh nhãn tự động từ Id:
    /// Id 1→A, 2→B, ... 26→Z, 27→AA, 28→AB, ...
    /// </summary>
    private static string GenerateLabel(int id)
    {
        // Chuyển id (1-based) sang chữ cái kiểu Excel: A, B, ..., Z, AA, AB...
        string result = string.Empty;
        while (id > 0)
        {
            id--;
            result = (char)('A' + id % 26) + result;
            id /= 26;
        }
        return result;
    }
}

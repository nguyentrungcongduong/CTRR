using GraphApp.Core.Models;

namespace GraphApp.Core.Converters;

/// <summary>
/// Chuyển đổi qua lại giữa 3 dạng biểu diễn đồ thị:
/// Ma trận kề ↔ Danh sách kề ↔ Danh sách cạnh.
/// Hỗ trợ cả đồ thị có hướng và vô hướng.
/// Quy ước: trọng số 0 trong ma trận = không có cạnh.
/// </summary>
public static class GraphConverter
{
    // ─── Graph → Representations ──────────────────────────────────────

    /// <summary>
    /// Chuyển Graph sang ma trận kề n×n.
    /// matrix[i,j] = trọng số cạnh i→j, 0 nếu không có cạnh.
    /// labels[i] = nhãn đỉnh thứ i (theo thứ tự Nodes).
    /// </summary>
    public static (double[,] Matrix, string[] Labels) ToAdjMatrix(Graph graph)
    {
        var nodes  = graph.Nodes;
        int n      = nodes.Count;
        var labels = new string[n];
        var matrix = new double[n, n];

        // Lập bảng index: nodeId → chỉ số trong ma trận
        var idx = new Dictionary<int, int>(n);
        for (int i = 0; i < n; i++)
        {
            labels[i]        = nodes[i].Label;
            idx[nodes[i].Id] = i;
        }

        // Điền trọng số
        foreach (var edge in graph.Edges)
        {
            if (!idx.TryGetValue(edge.Source, out int r)) continue;
            if (!idx.TryGetValue(edge.Target, out int c)) continue;

            matrix[r, c] = edge.Weight;

            // Vô hướng: điền cả chiều ngược
            if (!graph.Directed)
                matrix[c, r] = edge.Weight;
        }

        return (matrix, labels);
    }

    /// <summary>
    /// Chuyển Graph sang danh sách kề.
    /// Key = nhãn đỉnh, Value = [(nhãn láng giềng, trọng số)].
    /// Mỗi đỉnh đều có entry kể cả khi không có láng giềng.
    /// </summary>
    public static Dictionary<string, List<(string Neighbor, double Weight)>>
        ToAdjList(Graph graph)
    {
        var result = new Dictionary<string, List<(string, double)>>();

        // Tạo entry rỗng cho mọi đỉnh (kể cả đỉnh cô lập)
        foreach (var node in graph.Nodes)
            result[node.Label] = new List<(string, double)>();

        // Điền các cạnh
        foreach (var edge in graph.Edges)
        {
            var src = graph.GetNode(edge.Source);
            var tgt = graph.GetNode(edge.Target);
            if (src == null || tgt == null) continue;

            result[src.Label].Add((tgt.Label, edge.Weight));

            // Vô hướng: thêm chiều ngược
            if (!graph.Directed)
                result[tgt.Label].Add((src.Label, edge.Weight));
        }

        return result;
    }

    /// <summary>
    /// Chuyển Graph sang danh sách cạnh.
    /// Mỗi tuple: (nhãn nguồn, nhãn đích, trọng số).
    /// Đồ thị vô hướng: mỗi cạnh xuất hiện 1 lần (Source → Target).
    /// </summary>
    public static List<(string Source, string Target, double Weight)>
        ToEdgeList(Graph graph)
    {
        var result = new List<(string, string, double)>();

        foreach (var edge in graph.Edges)
        {
            var src = graph.GetNode(edge.Source);
            var tgt = graph.GetNode(edge.Target);
            if (src == null || tgt == null) continue;

            result.Add((src.Label, tgt.Label, edge.Weight));
        }

        return result;
    }

    // ─── Representations → Graph ──────────────────────────────────────

    /// <summary>
    /// Tạo Graph từ ma trận kề n×n.
    /// labels[i] = nhãn đỉnh i. matrix[i,j] ≠ 0 → có cạnh i→j với trọng số đó.
    /// Đỉnh được đặt tự động theo lưới để hiển thị đẹp trên canvas.
    /// </summary>
    public static Graph FromAdjMatrix(double[,] matrix, string[] labels,
        bool directed = false)
    {
        int n = labels.Length;
        if (matrix.GetLength(0) != n || matrix.GetLength(1) != n)
            throw new ArgumentException("Kích thước ma trận phải bằng số nhãn.");

        var graph = new Graph { Directed = directed };

        // Thêm đỉnh — bố cục lưới tự động
        var nodeIds = new int[n];
        float cx = 200f, cy = 150f, spacing = 140f;
        int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(n)));

        for (int i = 0; i < n; i++)
        {
            float x = cx + (i % cols) * spacing;
            float y = cy + (i / cols) * spacing;
            nodeIds[i] = graph.AddNode((int)x, (int)y, labels[i]);
        }

        // Thêm cạnh
        for (int r = 0; r < n; r++)
        {
            // Đồ thị vô hướng: chỉ xét c > r để tránh thêm 2 lần
            int cStart = directed ? 0 : r + 1;
            for (int c = cStart; c < n; c++)
            {
                double w = matrix[r, c];
                if (w != 0) graph.AddEdge(nodeIds[r], nodeIds[c], w);
            }
        }

        return graph;
    }

    /// <summary>
    /// Tạo Graph từ danh sách kề.
    /// Tự động tạo đỉnh cho mọi nhãn xuất hiện (kể cả trong danh sách láng giềng).
    /// </summary>
    public static Graph FromAdjList(
        Dictionary<string, List<(string Neighbor, double Weight)>> adjList,
        bool directed = false)
    {
        var graph   = new Graph { Directed = directed };
        var nodeIds = new Dictionary<string, int>();

        // Thu thập tất cả nhãn (key + neighbors)
        var allLabels = new HashSet<string>(adjList.Keys);
        foreach (var neighbors in adjList.Values)
            foreach (var (nb, _) in neighbors)
                allLabels.Add(nb);

        // Tạo đỉnh theo lưới
        int idx   = 0;
        int cols  = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(allLabels.Count)));
        float cx  = 200f, cy = 150f, spacing = 140f;

        foreach (var label in allLabels.OrderBy(l => l))
        {
            float x     = cx + (idx % cols) * spacing;
            float y     = cy + (idx / cols) * spacing;
            nodeIds[label] = graph.AddNode((int)x, (int)y, label);
            idx++;
        }

        // Thêm cạnh
        var addedEdges = new HashSet<(int, int)>();   // tránh trùng cho vô hướng

        foreach (var (srcLabel, neighbors) in adjList)
        {
            if (!nodeIds.TryGetValue(srcLabel, out int srcId)) continue;

            foreach (var (tgtLabel, weight) in neighbors)
            {
                if (!nodeIds.TryGetValue(tgtLabel, out int tgtId)) continue;

                if (!directed)
                {
                    // Tránh thêm cạnh (A,B) và (B,A) cho vô hướng
                    int lo = Math.Min(srcId, tgtId), hi = Math.Max(srcId, tgtId);
                    if (!addedEdges.Add((lo, hi))) continue;
                }

                graph.AddEdge(srcId, tgtId, weight);
            }
        }

        return graph;
    }

    /// <summary>
    /// Tạo Graph từ danh sách cạnh.
    /// Tự động tạo đỉnh cho mọi nhãn xuất hiện.
    /// </summary>
    public static Graph FromEdgeList(
        List<(string Source, string Target, double Weight)> edgeList,
        bool directed = false)
    {
        var graph   = new Graph { Directed = directed };
        var nodeIds = new Dictionary<string, int>();

        // Thu thập tất cả nhãn
        var allLabels = new HashSet<string>();
        foreach (var (src, tgt, _) in edgeList)
        {
            allLabels.Add(src);
            allLabels.Add(tgt);
        }

        // Tạo đỉnh theo lưới
        int idx  = 0;
        int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(allLabels.Count)));
        float cx = 200f, cy = 150f, spacing = 140f;

        foreach (var label in allLabels.OrderBy(l => l))
        {
            float x        = cx + (idx % cols) * spacing;
            float y        = cy + (idx / cols) * spacing;
            nodeIds[label] = graph.AddNode((int)x, (int)y, label);
            idx++;
        }

        // Thêm cạnh
        var addedEdges = new HashSet<(int, int)>();

        foreach (var (srcLabel, tgtLabel, weight) in edgeList)
        {
            if (!nodeIds.TryGetValue(srcLabel, out int srcId)) continue;
            if (!nodeIds.TryGetValue(tgtLabel, out int tgtId)) continue;

            if (!directed)
            {
                int lo = Math.Min(srcId, tgtId), hi = Math.Max(srcId, tgtId);
                if (!addedEdges.Add((lo, hi))) continue;
            }

            graph.AddEdge(srcId, tgtId, weight);
        }

        return graph;
    }

    // ─── Utility: Text serialization ──────────────────────────────────

    /// <summary>
    /// Hiển thị ma trận kề dưới dạng string (cho UI text box).
    /// </summary>
    public static string AdjMatrixToString(double[,] matrix, string[] labels)
    {
        int n = labels.Length;
        int colW = labels.Max(l => l.Length) + 2;

        var sb = new System.Text.StringBuilder();

        // Header
        sb.Append("".PadLeft(colW));
        foreach (var lbl in labels) sb.Append(lbl.PadLeft(colW));
        sb.AppendLine();

        // Rows
        for (int r = 0; r < n; r++)
        {
            sb.Append(labels[r].PadLeft(colW));
            for (int c = 0; c < n; c++)
            {
                string cell = matrix[r, c] == 0 ? "0" : FormatW(matrix[r, c]);
                sb.Append(cell.PadLeft(colW));
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Hiển thị danh sách kề dưới dạng string.
    /// </summary>
    public static string AdjListToString(
        Dictionary<string, List<(string Neighbor, double Weight)>> adjList)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var (node, neighbors) in adjList.OrderBy(kv => kv.Key))
        {
            sb.Append($"{node}: ");
            if (neighbors.Count == 0)
                sb.AppendLine("(không có láng giềng)");
            else
                sb.AppendLine(string.Join(" → ",
                    neighbors.Select(n => n.Weight == 1
                        ? n.Neighbor
                        : $"{n.Neighbor}({FormatW(n.Weight)})")));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Hiển thị danh sách cạnh dưới dạng string.
    /// </summary>
    public static string EdgeListToString(
        List<(string Source, string Target, double Weight)> edgeList)
    {
        if (edgeList.Count == 0) return "(Không có cạnh)";
        return string.Join("\n",
            edgeList.Select((e, i) =>
                $"  {i + 1,3}. {e.Source} → {e.Target}  (w = {FormatW(e.Weight)})"));
    }

    private static string FormatW(double w) =>
        w == Math.Floor(w) ? ((int)w).ToString() : w.ToString("F2");
}

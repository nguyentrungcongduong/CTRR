using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.ShortestPath;

/// <summary>
/// Thuật toán Dijkstra tìm đường đi ngắn nhất từ đỉnh nguồn.
/// Dùng PriorityQueue&lt;int, double&gt; (.NET 6+) — min-heap theo khoảng cách.
/// Chỉ hoạt động đúng với trọng số không âm.
/// </summary>
public class Dijkstra : IGraphAlgorithm
{
    public string Name => "Dijkstra – Đường đi ngắn nhất";

    // ─── IGraphAlgorithm ───────────────────────────────────────────────
    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        int startId = parameters != null && parameters.TryGetValue("startId", out var s)
            ? Convert.ToInt32(s) : (graph.Nodes.FirstOrDefault()?.Id ?? -1);
        int? endId = parameters != null && parameters.TryGetValue("endId", out var e)
            ? (int?)Convert.ToInt32(e) : null;
        return Run(graph, startId, endId);
    }

    // ─── Static entry point ────────────────────────────────────────────
    /// <summary>
    /// Chạy Dijkstra từ <paramref name="startId"/>.
    /// Nếu truyền <paramref name="endId"/>, dừng sớm khi tìm thấy đỉnh đích.
    /// </summary>
    public static List<AlgorithmStep> Run(Graph graph, int startId, int? endId = null)
    {
        var steps = new List<AlgorithmStep>();

        // ── Kiểm tra đầu vào ──────────────────────────────────────────
        var startNode = graph.GetNode(startId);
        if (startNode == null)
        {
            steps.Add(AlgorithmStep.Create($"Lỗi: Đỉnh Id={startId} không tồn tại.", "error"));
            return steps;
        }

        // Kiểm tra trọng số âm
        if (graph.Edges.Any(e => e.Weight < 0))
        {
            steps.Add(AlgorithmStep.Create(
                "Lỗi: Dijkstra không hoạt động với trọng số âm. Dùng Bellman-Ford thay thế.", "error"));
            return steps;
        }

        // ── Khởi tạo ─────────────────────────────────────────────────
        var dist     = new Dictionary<int, double>();   // khoảng cách ngắn nhất hiện tại
        var prev     = new Dictionary<int, int>();       // đỉnh trước trong đường đi ngắn nhất
        var prevEdge = new Dictionary<int, int>();       // cạnh dùng để đến mỗi đỉnh
        var settled  = new HashSet<int>();               // đã xử lý xong (xanh lá)
        var inPQ     = new HashSet<int>();               // đang trong PQ (cam)

        // PriorityQueue<node, priority> — min-heap theo priority (distance)
        var pq = new PriorityQueue<int, double>();

        foreach (var n in graph.Nodes)
            dist[n.Id] = double.PositiveInfinity;
        dist[startId] = 0.0;
        pq.Enqueue(startId, 0.0);
        inPQ.Add(startId);

        // ── Step 0: Khởi tạo ─────────────────────────────────────────
        steps.Add(new AlgorithmStep
        {
            Description    = $"Khởi tạo Dijkstra từ đỉnh {startNode.Label}.\n" +
                             $"  dist[{startNode.Label}] = 0, tất cả đỉnh còn lại = ∞.\n" +
                             $"  Thêm {startNode.Label} vào hàng đợi ưu tiên (PQ).",
            StepType       = "init",
            VisitedNodes   = new HashSet<int>(settled),
            ActiveNodes    = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(inPQ),
            HighlightEdges = new HashSet<int>(),
            NodeLabels     = BuildDistLabels(dist, graph)
        });

        // ── Vòng lặp Dijkstra ─────────────────────────────────────────
        while (pq.Count > 0)
        {
            pq.TryDequeue(out int current, out double currentDist);
            var currentNode = graph.GetNode(current)!;

            // Bỏ qua nếu đã xử lý (có thể bị push nhiều lần với giá trị cũ)
            if (settled.Contains(current))
            {
                steps.Add(new AlgorithmStep
                {
                    Description    = $"Bỏ qua đỉnh {currentNode.Label} (đã xử lý với dist={FormatDist(currentDist)}).",
                    StepType       = "skip_settled",
                    VisitedNodes   = new HashSet<int>(settled),
                    ActiveNodes    = new HashSet<int>(),
                    QueueOrStack   = new HashSet<int>(inPQ),
                    HighlightEdges = new HashSet<int>(),
                    NodeLabels     = BuildDistLabels(dist, graph)
                });
                continue;
            }

            inPQ.Remove(current);
            settled.Add(current);

            // Step: Extract min
            string targetInfo = endId.HasValue && current == endId.Value
                ? $" ← ĐÃ TÌM THẤY ĐÍCH!" : string.Empty;
            steps.Add(new AlgorithmStep
            {
                Description    = $"Chọn đỉnh có khoảng cách nhỏ nhất: {currentNode.Label} (dist={FormatDist(currentDist)}){targetInfo}\n" +
                                 $"  Đánh dấu {currentNode.Label} đã xử lý xong.",
                StepType       = "extract_min",
                VisitedNodes   = new HashSet<int>(settled),
                ActiveNodes    = new HashSet<int> { current },
                QueueOrStack   = new HashSet<int>(inPQ),
                HighlightEdges = new HashSet<int>(),
                NodeLabels     = BuildDistLabels(dist, graph)
            });

            // Dừng sớm nếu đã đến đỉnh đích
            if (endId.HasValue && current == endId.Value)
                break;

            // ── Xét từng láng giềng ──────────────────────────────────
            var neighbors = graph.Neighbors(current);
            foreach (var (neighborId, edgeId, weight) in neighbors)
            {
                if (settled.Contains(neighborId)) continue;   // đã xử lý, bỏ qua

                var neighborNode = graph.GetNode(neighborId)!;
                double newDist   = currentDist + weight;
                double oldDist   = dist[neighborId];

                if (newDist < oldDist)
                {
                    // Tìm thấy đường ngắn hơn → cập nhật
                    dist[neighborId]     = newDist;
                    prev[neighborId]     = current;
                    prevEdge[neighborId] = edgeId;
                    pq.Enqueue(neighborId, newDist);
                    inPQ.Add(neighborId);

                    string improve = double.IsPositiveInfinity(oldDist)
                        ? $"∞ → {FormatDist(newDist)}"
                        : $"{FormatDist(oldDist)} → {FormatDist(newDist)}";

                    steps.Add(new AlgorithmStep
                    {
                        Description    = $"  Xét cạnh {currentNode.Label}→{neighborNode.Label} (w={weight}):\n" +
                                         $"  dist[{neighborNode.Label}] = {FormatDist(currentDist)} + {weight} = {FormatDist(newDist)} < {FormatDist(oldDist)}\n" +
                                         $"  → CẬP NHẬT dist[{neighborNode.Label}]: {improve}",
                        StepType       = "update_dist",
                        VisitedNodes   = new HashSet<int>(settled),
                        ActiveNodes    = new HashSet<int> { current },
                        QueueOrStack   = new HashSet<int>(inPQ),
                        HighlightEdges = new HashSet<int>(),
                        NodeLabels     = BuildDistLabels(dist, graph)
                    });
                }
                else
                {
                    // Không cải thiện
                    steps.Add(new AlgorithmStep
                    {
                        Description    = $"  Xét cạnh {currentNode.Label}→{neighborNode.Label} (w={weight}):\n" +
                                         $"  dist[{neighborNode.Label}] = {FormatDist(currentDist)} + {weight} = {FormatDist(newDist)} ≥ {FormatDist(oldDist)}\n" +
                                         $"  → Không cải thiện, bỏ qua.",
                        StepType       = "no_improvement",
                        VisitedNodes   = new HashSet<int>(settled),
                        ActiveNodes    = new HashSet<int> { current },
                        QueueOrStack   = new HashSet<int>(inPQ),
                        HighlightEdges = new HashSet<int>(),
                        NodeLabels     = BuildDistLabels(dist, graph)
                    });
                }
            }
        }

        // ── Tái tạo đường đi ngắn nhất ───────────────────────────────
        var pathEdges = new HashSet<int>();
        var pathNodes = new HashSet<int>();
        string pathStr;

        if (endId.HasValue)
        {
            // Truy ngược từ endId → startId
            if (!settled.Contains(endId.Value) || double.IsPositiveInfinity(dist[endId.Value]))
            {
                var endNode = graph.GetNode(endId.Value);
                pathStr = $"Không có đường đi từ {startNode.Label} đến {endNode?.Label}.";
            }
            else
            {
                var pathList = new List<int>();
                int cur = endId.Value;
                while (prev.ContainsKey(cur))
                {
                    pathEdges.Add(prevEdge[cur]);
                    pathList.Add(cur);
                    pathNodes.Add(cur);
                    cur = prev[cur];
                }
                pathList.Add(startId);
                pathNodes.Add(startId);
                pathList.Reverse();

                var endNode = graph.GetNode(endId.Value)!;
                pathStr = $"Đường đi ngắn nhất {startNode.Label}→{endNode.Label}: " +
                          string.Join(" → ", pathList.Select(id => graph.GetNode(id)?.Label ?? id.ToString())) +
                          $"\nTổng khoảng cách: {FormatDist(dist[endId.Value])}";
            }
        }
        else
        {
            // Tái tạo tất cả các đường từ startId
            foreach (var (nodeId, edgeId) in prevEdge)
                pathEdges.Add(edgeId);
            foreach (var (nodeId, _) in dist)
                if (!double.IsPositiveInfinity(dist[nodeId]))
                    pathNodes.Add(nodeId);

            pathStr = $"Dijkstra hoàn tất!\n" +
                      string.Join("\n", graph.Nodes
                          .OrderBy(n => dist[n.Id])
                          .Select(n => $"  {startNode.Label} → {n.Label}: {FormatDist(dist[n.Id])}"));
        }

        // Step: Reconstruct
        steps.Add(new AlgorithmStep
        {
            Description    = $"Tái tạo đường đi ngắn nhất (đường màu tím):",
            StepType       = "reconstruct",
            VisitedNodes   = new HashSet<int>(settled),
            ActiveNodes    = pathNodes,
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(pathEdges),
            NodeLabels     = BuildDistLabels(dist, graph)
        });

        // Step cuối
        steps.Add(new AlgorithmStep
        {
            Description    = pathStr,
            StepType       = "done",
            VisitedNodes   = new HashSet<int>(settled),
            ActiveNodes    = pathNodes,
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(pathEdges),
            NodeLabels     = BuildDistLabels(dist, graph)
        });

        return steps;
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Sinh NodeLabels hiển thị khoảng cách ngắn nhất hiện tại lên mỗi đỉnh.
    /// Dùng "∞" cho khoảng cách vô cùng.
    /// </summary>
    private static Dictionary<int, string> BuildDistLabels(
        Dictionary<int, double> dist, Graph graph)
    {
        var labels = new Dictionary<int, string>();
        foreach (var node in graph.Nodes)
        {
            if (!dist.TryGetValue(node.Id, out double d))
                continue;
            labels[node.Id] = double.IsPositiveInfinity(d) ? "∞" : FormatDist(d);
        }
        return labels;
    }

    /// <summary>Định dạng khoảng cách: số nguyên nếu có thể, không thì 2 chữ số thập phân.</summary>
    private static string FormatDist(double d)
    {
        if (double.IsPositiveInfinity(d)) return "∞";
        return d == Math.Floor(d) ? ((int)d).ToString() : d.ToString("F2");
    }
}

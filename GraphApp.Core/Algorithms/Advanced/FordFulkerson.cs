using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Advanced;

/// <summary>
/// Thuật toán Ford-Fulkerson tìm luồng cực đại (Max Flow).
/// Dùng BFS để tìm đường tăng luồng (Edmonds-Karp variant — O(VE²)).
/// Xây dựng residual graph: forward edge (u→v) = capacity - flow,
/// backward edge (v→u) = flow (cho phép hoàn trả luồng).
/// Chỉ chạy trên đồ thị CÓ HƯỚNG.
/// </summary>
public class FordFulkerson : IGraphAlgorithm
{
    public string Name => "Ford-Fulkerson – Luồng cực đại (Max Flow)";

    // ─── IGraphAlgorithm ───────────────────────────────────────────────
    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        int srcId  = parameters != null && parameters.TryGetValue("sourceId", out var s)
            ? Convert.ToInt32(s) : (graph.Nodes.FirstOrDefault()?.Id ?? -1);
        int sinkId = parameters != null && parameters.TryGetValue("sinkId", out var t)
            ? Convert.ToInt32(t) : (graph.Nodes.LastOrDefault()?.Id ?? -1);
        return Run(graph, srcId, sinkId);
    }

    // ─── Static entry point ────────────────────────────────────────────
    public static List<AlgorithmStep> Run(Graph graph, int sourceId, int sinkId)
    {
        var steps = new List<AlgorithmStep>();

        // ── Kiểm tra đầu vào ─────────────────────────────────────────
        if (!graph.Directed)
        {
            steps.Add(AlgorithmStep.Create(
                "❌ Lỗi: Ford-Fulkerson chỉ chạy trên đồ thị CÓ HƯỚNG.\n" +
                "   Hãy bật chế độ 'Có hướng' trước khi chạy.", "error"));
            return steps;
        }

        if (graph.Nodes.Count == 0 || graph.Edges.Count == 0)
        {
            steps.Add(AlgorithmStep.Create("Đồ thị rỗng hoặc không có cạnh.", "error"));
            return steps;
        }

        var srcNode  = graph.GetNode(sourceId);
        var sinkNode = graph.GetNode(sinkId);
        if (srcNode == null || sinkNode == null)
        {
            steps.Add(AlgorithmStep.Create("Lỗi: Đỉnh nguồn hoặc đỉnh đích không tồn tại.", "error"));
            return steps;
        }

        if (sourceId == sinkId)
        {
            steps.Add(AlgorithmStep.Create("Lỗi: Nguồn và đích không được trùng nhau.", "error"));
            return steps;
        }

        // ── Xây dựng residual graph ───────────────────────────────────
        // residual[(u,v)] = khả năng còn lại trên cạnh u→v
        var residual  = new Dictionary<(int, int), double>();
        // edgeFlow[edgeId] = luồng hiện tại trên cạnh gốc
        var edgeFlow  = new Dictionary<int, double>();
        // capacity: original capacity of each edge
        var edgeCap   = new Dictionary<int, double>();
        // adjacency: all nodes reachable in residual graph
        var adjSet    = new HashSet<(int, int)>();  // (u,v) pairs

        foreach (var e in graph.Edges)
        {
            edgeFlow[e.Id] = 0;
            edgeCap[e.Id]  = e.Weight > 0 ? e.Weight : 1; // treat weight as capacity

            // Forward edge
            double cap = edgeCap[e.Id];
            residual[(e.Source, e.Target)] =
                residual.GetValueOrDefault((e.Source, e.Target)) + cap;
            adjSet.Add((e.Source, e.Target));

            // Backward edge (starts at 0)
            if (!residual.ContainsKey((e.Target, e.Source)))
                residual[(e.Target, e.Source)] = 0;
            adjSet.Add((e.Target, e.Source));
        }

        // Build adjacency list for BFS
        var adj = new Dictionary<int, List<int>>();
        foreach (var n in graph.Nodes) adj[n.Id] = new List<int>();
        foreach (var (u, v) in adjSet)
        {
            if (!adj[u].Contains(v)) adj[u].Add(v);
        }

        double maxFlow = 0;

        // ── Step 0: Khởi tạo ─────────────────────────────────────────
        steps.Add(new AlgorithmStep
        {
            Description    = $"Khởi tạo Ford-Fulkerson (Edmonds-Karp / BFS augmenting path).\n" +
                             $"  Nguồn: {srcNode.Label}   Đích: {sinkNode.Label}\n" +
                             $"  Luồng ban đầu: 0 trên tất cả cạnh.\n" +
                             $"  Trọng số cạnh = khả năng thông qua (capacity).",
            StepType       = "init",
            VisitedNodes   = new HashSet<int> { sourceId },
            ActiveNodes    = new HashSet<int> { sinkId },
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(),
            NodeLabels     = BuildFlowNodeLabels(graph, sourceId, sinkId, maxFlow, edgeFlow, edgeCap),
            EdgeLabels     = BuildEdgeFlowLabels(graph, edgeFlow, edgeCap)
        });

        int iteration = 0;

        // ── Vòng lặp Ford-Fulkerson ───────────────────────────────────
        while (true)
        {
            iteration++;

            // BFS tìm đường tăng luồng
            var parent = new Dictionary<int, int>();   // node → predecessor
            var found  = BfsAugmenting(sourceId, sinkId, adj, residual, parent);

            if (!found)
            {
                steps.Add(new AlgorithmStep
                {
                    Description    = $"Lần BFS thứ {iteration}: Không tìm được đường tăng luồng\n" +
                                     $"từ {srcNode.Label} đến {sinkNode.Label}.\n" +
                                     $"→ Thuật toán kết thúc. Max Flow = {FormatW(maxFlow)}",
                    StepType       = "no_path",
                    VisitedNodes   = new HashSet<int> { sourceId },
                    ActiveNodes    = new HashSet<int> { sinkId },
                    QueueOrStack   = new HashSet<int>(),
                    HighlightEdges = new HashSet<int>(),
                    NodeLabels     = BuildFlowNodeLabels(graph, sourceId, sinkId, maxFlow, edgeFlow, edgeCap),
                    EdgeLabels     = BuildEdgeFlowLabels(graph, edgeFlow, edgeCap)
                });
                break;
            }

            // Truy ngược đường tăng luồng
            var path = ReconstructPath(sourceId, sinkId, parent);

            // Tìm bottleneck
            double bottleneck = double.PositiveInfinity;
            for (int i = 0; i < path.Count - 1; i++)
                bottleneck = Math.Min(bottleneck, residual[(path[i], path[i + 1])]);

            // Tìm edge IDs trên path (chỉ forward edges)
            var pathEdgeIds = new HashSet<int>();
            for (int i = 0; i < path.Count - 1; i++)
            {
                int u = path[i], v = path[i + 1];
                var fwdEdge = graph.Edges.FirstOrDefault(e => e.Source == u && e.Target == v);
                if (fwdEdge != null) pathEdgeIds.Add(fwdEdge.Id);
            }

            string pathStr = string.Join(" → ",
                path.Select(id => graph.GetNode(id)?.Label ?? id.ToString()));

            // Step: tìm đường tăng luồng
            steps.Add(new AlgorithmStep
            {
                Description    = $"Lần {iteration}: Tìm thấy đường tăng luồng:\n" +
                                 $"  {pathStr}\n" +
                                 $"  Bottleneck (luồng tăng thêm) = {FormatW(bottleneck)}",
                StepType       = "find_path",
                VisitedNodes   = new HashSet<int> { sourceId },
                ActiveNodes    = new HashSet<int>(path),
                QueueOrStack   = new HashSet<int> { sinkId },
                HighlightEdges = new HashSet<int>(pathEdgeIds),
                NodeLabels     = BuildFlowNodeLabels(graph, sourceId, sinkId, maxFlow, edgeFlow, edgeCap),
                EdgeLabels     = BuildEdgeFlowLabels(graph, edgeFlow, edgeCap)
            });

            // Cập nhật flow và residual
            for (int i = 0; i < path.Count - 1; i++)
            {
                int u = path[i], v = path[i + 1];

                // Cập nhật residual
                residual[(u, v)] -= bottleneck;
                residual[(v, u)] =
                    residual.GetValueOrDefault((v, u)) + bottleneck;

                // Cập nhật edgeFlow (chỉ forward edges gốc)
                var fwdEdge = graph.Edges.FirstOrDefault(e => e.Source == u && e.Target == v);
                if (fwdEdge != null)
                    edgeFlow[fwdEdge.Id] += bottleneck;
                else
                {
                    // Backward edge: giảm flow trên cạnh gốc ngược lại
                    var bkwEdge = graph.Edges.FirstOrDefault(e => e.Source == v && e.Target == u);
                    if (bkwEdge != null)
                        edgeFlow[bkwEdge.Id] -= bottleneck;
                }
            }

            maxFlow += bottleneck;

            // Step: tăng luồng
            steps.Add(new AlgorithmStep
            {
                Description    = $"Tăng luồng {FormatW(bottleneck)} dọc theo đường:\n" +
                                 $"  {pathStr}\n" +
                                 $"  Cập nhật residual graph (forward -, backward +).\n" +
                                 $"  Tổng luồng hiện tại: {FormatW(maxFlow)}",
                StepType       = "augment_flow",
                VisitedNodes   = new HashSet<int> { sourceId },
                ActiveNodes    = new HashSet<int>(path),
                QueueOrStack   = new HashSet<int> { sinkId },
                HighlightEdges = new HashSet<int>(pathEdgeIds),
                NodeLabels     = BuildFlowNodeLabels(graph, sourceId, sinkId, maxFlow, edgeFlow, edgeCap),
                EdgeLabels     = BuildEdgeFlowLabels(graph, edgeFlow, edgeCap)
            });
        }

        // ── Step cuối: Max Flow ───────────────────────────────────────
        // Tìm saturated edges (flow == capacity)
        var saturatedEdges = graph.Edges
            .Where(e => edgeFlow[e.Id] >= edgeCap[e.Id])
            .Select(e => e.Id)
            .ToHashSet();

        steps.Add(new AlgorithmStep
        {
            Description    = $"✅ LUỒNG CỰC ĐẠI (MAX FLOW) = {FormatW(maxFlow)}\n\n" +
                             $"  Từ {srcNode.Label} → {sinkNode.Label}\n" +
                             $"  Sau {iteration - 1} lần tăng luồng.\n\n" +
                             $"  Chi tiết luồng trên các cạnh:\n" +
                             string.Join("\n", graph.Edges
                                 .Where(e => edgeFlow[e.Id] > 0)
                                 .OrderByDescending(e => edgeFlow[e.Id])
                                 .Select(e => $"    {graph.GetNode(e.Source)?.Label}→" +
                                              $"{graph.GetNode(e.Target)?.Label}: " +
                                              $"{FormatW(edgeFlow[e.Id])}/{FormatW(edgeCap[e.Id])}")),
            StepType       = "done",
            VisitedNodes   = new HashSet<int> { sourceId },
            ActiveNodes    = new HashSet<int> { sinkId },
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = saturatedEdges,
            NodeLabels     = BuildFlowNodeLabels(graph, sourceId, sinkId, maxFlow, edgeFlow, edgeCap),
            EdgeLabels     = BuildEdgeFlowLabels(graph, edgeFlow, edgeCap)
        });

        return steps;
    }

    // ─── BFS tìm augmenting path ───────────────────────────────────────

    private static bool BfsAugmenting(
        int source, int sink,
        Dictionary<int, List<int>> adj,
        Dictionary<(int, int), double> residual,
        Dictionary<int, int> parent)
    {
        parent.Clear();
        parent[source] = -1;
        var queue = new Queue<int>();
        queue.Enqueue(source);

        while (queue.Count > 0)
        {
            int u = queue.Dequeue();
            if (u == sink) return true;

            foreach (int v in adj.GetValueOrDefault(u) ?? [])
            {
                if (!parent.ContainsKey(v) &&
                    residual.GetValueOrDefault((u, v)) > 1e-9)
                {
                    parent[v] = u;
                    queue.Enqueue(v);
                }
            }
        }
        return parent.ContainsKey(sink);
    }

    private static List<int> ReconstructPath(int source, int sink, Dictionary<int, int> parent)
    {
        var path = new List<int>();
        for (int cur = sink; cur != -1; cur = parent[cur])
            path.Add(cur);
        path.Reverse();
        return path;
    }

    // ─── Label builders ────────────────────────────────────────────────

    /// <summary>NodeLabels: "SRC" cho nguồn, "SINK" cho đích, maxFlow ở step cuối.</summary>
    private static Dictionary<int, string> BuildFlowNodeLabels(
        Graph graph, int srcId, int sinkId, double maxFlow,
        Dictionary<int, double> edgeFlow, Dictionary<int, double> edgeCap)
    {
        var labels = new Dictionary<int, string>();
        foreach (var n in graph.Nodes)
        {
            if (n.Id == srcId)
                labels[n.Id] = maxFlow > 0 ? $"SRC ({FormatW(maxFlow)})" : "SRC";
            else if (n.Id == sinkId)
                labels[n.Id] = maxFlow > 0 ? $"SINK ({FormatW(maxFlow)})" : "SINK";
        }
        return labels;
    }

    /// <summary>EdgeLabels: "flow/capacity" cho mỗi cạnh gốc.</summary>
    private static Dictionary<int, string> BuildEdgeFlowLabels(
        Graph graph,
        Dictionary<int, double> edgeFlow,
        Dictionary<int, double> edgeCap)
    {
        var labels = new Dictionary<int, string>();
        foreach (var e in graph.Edges)
        {
            double f = edgeFlow.GetValueOrDefault(e.Id);
            double c = edgeCap.GetValueOrDefault(e.Id, e.Weight);
            labels[e.Id] = $"{FormatW(f)}/{FormatW(c)}";
        }
        return labels;
    }

    private static string FormatW(double w) =>
        w == Math.Floor(w) ? ((int)w).ToString() : w.ToString("F1");
}

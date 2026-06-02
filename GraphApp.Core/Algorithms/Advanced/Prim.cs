using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Advanced;

/// <summary>
/// Thuật toán Prim tìm cây khung nhỏ nhất (MST) cho đồ thị VÔ HƯỚNG có trọng số.
/// Dùng PriorityQueue&lt;int, double&gt; (min-heap theo key value).
/// Độ phức tạp: O((V + E) log V).
/// </summary>
public class Prim : IGraphAlgorithm
{
    public string Name => "Prim – Cây khung nhỏ nhất (MST)";

    // ─── IGraphAlgorithm ───────────────────────────────────────────────
    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        int startId = parameters != null && parameters.TryGetValue("startId", out var s)
            ? Convert.ToInt32(s) : (graph.Nodes.FirstOrDefault()?.Id ?? -1);
        return Run(graph, startId);
    }

    // ─── Static entry point ────────────────────────────────────────────
    public static List<AlgorithmStep> Run(Graph graph, int startId)
    {
        var steps = new List<AlgorithmStep>();

        // ── Kiểm tra đầu vào ─────────────────────────────────────────
        if (graph.Directed)
        {
            steps.Add(AlgorithmStep.Create(
                "❌ Lỗi: Thuật toán Prim chỉ chạy trên đồ thị VÔ HƯỚNG.\n" +
                "   Hãy tắt chế độ 'Có hướng' trước khi chạy.", "error"));
            return steps;
        }

        if (graph.Nodes.Count == 0)
        {
            steps.Add(AlgorithmStep.Create("Đồ thị rỗng — không có gì để xử lý.", "error"));
            return steps;
        }

        var startNode = graph.GetNode(startId);
        if (startNode == null)
        {
            steps.Add(AlgorithmStep.Create($"Lỗi: Đỉnh Id={startId} không tồn tại.", "error"));
            return steps;
        }

        // ── Khởi tạo ─────────────────────────────────────────────────
        var key      = new Dictionary<int, double>();  // key[v] = trọng số nhỏ nhất để kết nối v vào MST
        var prev     = new Dictionary<int, int>();     // đỉnh cha trong MST
        var prevEdge = new Dictionary<int, int>();     // cạnh dùng để kết nối
        var inMST    = new HashSet<int>();             // đỉnh đã vào MST (xanh lá)
        var mstEdges = new HashSet<int>();             // cạnh MST (tím)
        var reachable = new HashSet<int>();            // đỉnh có key hữu hạn nhưng chưa vào MST (cam)

        foreach (var n in graph.Nodes)
            key[n.Id] = double.PositiveInfinity;
        key[startId] = 0.0;
        reachable.Add(startId);

        var pq = new PriorityQueue<int, double>();
        pq.Enqueue(startId, 0.0);

        // ── Step 0: Khởi tạo ─────────────────────────────────────────
        steps.Add(new AlgorithmStep
        {
            Description    = $"Khởi tạo Prim từ đỉnh {startNode.Label}.\n" +
                             $"  key[{startNode.Label}] = 0, tất cả đỉnh còn lại = ∞.\n" +
                             $"  Thêm {startNode.Label} vào hàng đợi ưu tiên.",
            StepType       = "init",
            VisitedNodes   = new HashSet<int>(),
            ActiveNodes    = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(reachable),
            HighlightEdges = new HashSet<int>(),
            NodeLabels     = BuildKeyLabels(key, graph)
        });

        // ── Vòng lặp Prim ────────────────────────────────────────────
        while (pq.Count > 0)
        {
            pq.TryDequeue(out int u, out double keyU);
            var uNode = graph.GetNode(u)!;

            // Bỏ qua nếu đã vào MST (push nhiều lần)
            if (inMST.Contains(u))
            {
                steps.Add(new AlgorithmStep
                {
                    Description    = $"Bỏ qua {uNode.Label} (đã trong MST).",
                    StepType       = "skip_in_mst",
                    VisitedNodes   = new HashSet<int>(inMST),
                    ActiveNodes    = new HashSet<int>(),
                    QueueOrStack   = new HashSet<int>(reachable),
                    HighlightEdges = new HashSet<int>(mstEdges),
                    NodeLabels     = BuildKeyLabels(key, graph)
                });
                continue;
            }

            inMST.Add(u);
            reachable.Remove(u);

            // Step: chọn đỉnh min + thêm vào MST
            string addDesc;
            if (prev.ContainsKey(u))
            {
                mstEdges.Add(prevEdge[u]);
                var pNode = graph.GetNode(prev[u])!;
                addDesc = $"Chọn đỉnh {uNode.Label} (key={FormatW(keyU)}) — nhỏ nhất hiện tại.\n" +
                          $"  Thêm cạnh {pNode.Label}—{uNode.Label} (w={FormatW(keyU)}) vào MST. ✅";
            }
            else
            {
                addDesc = $"Bắt đầu từ đỉnh {uNode.Label} (key=0) — đỉnh khởi đầu MST.";
            }

            steps.Add(new AlgorithmStep
            {
                Description    = addDesc,
                StepType       = "add_to_mst",
                VisitedNodes   = new HashSet<int>(inMST),
                ActiveNodes    = new HashSet<int> { u },
                QueueOrStack   = new HashSet<int>(reachable),
                HighlightEdges = new HashSet<int>(mstEdges),
                NodeLabels     = BuildKeyLabels(key, graph)
            });

            // ── Cập nhật key cho láng giềng ──────────────────────────
            foreach (var (vId, edgeId, weight) in graph.Neighbors(u))
            {
                var vNode = graph.GetNode(vId)!;

                if (inMST.Contains(vId))
                {
                    // Đã trong MST — bỏ qua (không step để tránh quá nhiều bước)
                    continue;
                }

                if (weight < key[vId])
                {
                    double oldKey  = key[vId];
                    key[vId]       = weight;
                    prev[vId]      = u;
                    prevEdge[vId]  = edgeId;
                    reachable.Add(vId);
                    pq.Enqueue(vId, weight);

                    string improve = double.IsPositiveInfinity(oldKey)
                        ? $"∞ → {FormatW(weight)}"
                        : $"{FormatW(oldKey)} → {FormatW(weight)}";

                    steps.Add(new AlgorithmStep
                    {
                        Description    = $"  Xét cạnh {uNode.Label}—{vNode.Label} (w={FormatW(weight)}):\n" +
                                         $"  key[{vNode.Label}] = {improve}  ✅ CẬP NHẬT",
                        StepType       = "update_key",
                        VisitedNodes   = new HashSet<int>(inMST),
                        ActiveNodes    = new HashSet<int> { u },
                        QueueOrStack   = new HashSet<int>(reachable),
                        HighlightEdges = new HashSet<int>(mstEdges),
                        NodeLabels     = BuildKeyLabels(key, graph)
                    });
                }
                else
                {
                    steps.Add(new AlgorithmStep
                    {
                        Description    = $"  Xét cạnh {uNode.Label}—{vNode.Label} (w={FormatW(weight)}):\n" +
                                         $"  key[{vNode.Label}] = {FormatW(key[vId])} ≤ {FormatW(weight)} — không cải thiện.",
                        StepType       = "no_improvement",
                        VisitedNodes   = new HashSet<int>(inMST),
                        ActiveNodes    = new HashSet<int> { u },
                        QueueOrStack   = new HashSet<int>(reachable),
                        HighlightEdges = new HashSet<int>(mstEdges),
                        NodeLabels     = BuildKeyLabels(key, graph)
                    });
                }
            }
        }

        // ── Kết quả ───────────────────────────────────────────────────
        bool connected   = inMST.Count == graph.Nodes.Count;
        double totalWeight = mstEdges
            .Select(eid => graph.Edges.FirstOrDefault(e => e.Id == eid)?.Weight ?? 0)
            .Sum();

        string doneDesc;
        if (connected)
        {
            doneDesc = $"✅ HOÀN TẤT! Cây khung nhỏ nhất (MST) đã xây dựng xong.\n" +
                       $"  Số cạnh MST: {mstEdges.Count}\n" +
                       $"  Tổng trọng số: {FormatW(totalWeight)}\n" +
                       $"  Các cạnh MST: " +
                       string.Join(", ", mstEdges.Select(eid =>
                       {
                           var e = graph.Edges.FirstOrDefault(x => x.Id == eid);
                           if (e == null) return "?";
                           var s = graph.GetNode(e.Source)?.Label ?? "?";
                           var t = graph.GetNode(e.Target)?.Label ?? "?";
                           return $"{s}—{t}({FormatW(e.Weight)})";
                       }));
        }
        else
        {
            int missing = graph.Nodes.Count - inMST.Count;
            doneDesc = $"⚠️ Đồ thị KHÔNG LIÊN THÔNG!\n" +
                       $"  {missing} đỉnh không thể đến từ {startNode.Label}.\n" +
                       $"  Prim chỉ xây MST cho thành phần liên thông chứa {startNode.Label}.\n" +
                       $"  Tổng trọng số MST một phần: {FormatW(totalWeight)}";
        }

        steps.Add(new AlgorithmStep
        {
            Description    = doneDesc,
            StepType       = "done",
            VisitedNodes   = new HashSet<int>(inMST),
            ActiveNodes    = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(mstEdges),
            NodeLabels     = BuildKeyLabels(key, graph)
        });

        return steps;
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    private static Dictionary<int, string> BuildKeyLabels(
        Dictionary<int, double> key, Graph graph)
    {
        var labels = new Dictionary<int, string>();
        foreach (var node in graph.Nodes)
        {
            if (!key.TryGetValue(node.Id, out double k)) continue;
            labels[node.Id] = double.IsPositiveInfinity(k) ? "∞" : FormatW(k);
        }
        return labels;
    }

    private static string FormatW(double w) =>
        w == Math.Floor(w) ? ((int)w).ToString() : w.ToString("F2");
}

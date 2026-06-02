using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Advanced;

/// <summary>
/// Thuật toán Kruskal tìm cây khung nhỏ nhất (MST) cho đồ thị VÔ HƯỚNG.
/// Dùng Union-Find (Disjoint Set Union) với Path Compression + Union by Rank.
/// Sắp xếp cạnh tăng dần theo trọng số, thêm cạnh nếu không tạo chu trình.
/// Độ phức tạp: O(E log E) — chủ yếu do sắp xếp.
/// </summary>
public class Kruskal : IGraphAlgorithm
{
    public string Name => "Kruskal – Cây khung nhỏ nhất (MST)";

    // ─── IGraphAlgorithm ───────────────────────────────────────────────
    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
        => Run(graph);

    // ─── Static entry point ────────────────────────────────────────────
    public static List<AlgorithmStep> Run(Graph graph)
    {
        var steps = new List<AlgorithmStep>();

        // ── Kiểm tra đầu vào ─────────────────────────────────────────
        if (graph.Directed)
        {
            steps.Add(AlgorithmStep.Create(
                "❌ Lỗi: Thuật toán Kruskal chỉ chạy trên đồ thị VÔ HƯỚNG.\n" +
                "   Hãy tắt chế độ 'Có hướng' trước khi chạy.", "error"));
            return steps;
        }

        if (graph.Nodes.Count == 0)
        {
            steps.Add(AlgorithmStep.Create("Đồ thị rỗng — không có gì để xử lý.", "error"));
            return steps;
        }

        // ── Union-Find (DSU) ──────────────────────────────────────────
        var parent = new Dictionary<int, int>();
        var rank   = new Dictionary<int, int>();

        foreach (var n in graph.Nodes)
        {
            parent[n.Id] = n.Id;
            rank[n.Id]   = 0;
        }

        int Find(int x)
        {
            // Path compression
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        bool Union(int x, int y)
        {
            int px = Find(x), py = Find(y);
            if (px == py) return false;   // đã cùng thành phần → sẽ tạo chu trình
            // Union by rank
            if (rank[px] < rank[py]) (px, py) = (py, px);
            parent[py] = px;
            if (rank[px] == rank[py]) rank[px]++;
            return true;
        }

        // ── Sắp xếp cạnh tăng dần ────────────────────────────────────
        var sortedEdges = graph.Edges
            .OrderBy(e => e.Weight)
            .ThenBy(e => e.Id)
            .ToList();

        // ── Tracking ──────────────────────────────────────────────────
        var mstEdges    = new HashSet<int>();   // cạnh đã vào MST (tím)
        var mstNodes    = new HashSet<int>();   // đỉnh đã vào MST (xanh)
        var rejEdges    = new HashSet<int>();   // cạnh bị từ chối (tạo chu trình)

        // ── Step 0: Khởi tạo ─────────────────────────────────────────
        steps.Add(new AlgorithmStep
        {
            Description    = $"Khởi tạo Kruskal — Union-Find với {graph.Nodes.Count} thành phần.\n" +
                             $"Sắp xếp {sortedEdges.Count} cạnh tăng dần theo trọng số:\n  " +
                             string.Join(", ", sortedEdges.Take(8).Select(e =>
                             {
                                 var s = graph.GetNode(e.Source)?.Label ?? "?";
                                 var t = graph.GetNode(e.Target)?.Label ?? "?";
                                 return $"{s}—{t}({FormatW(e.Weight)})";
                             })) + (sortedEdges.Count > 8 ? "..." : ""),
            StepType       = "init",
            VisitedNodes   = new HashSet<int>(),
            ActiveNodes    = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(),
            NodeLabels     = BuildCompLabels(parent, graph)
        });

        // ── Duyệt từng cạnh theo thứ tự đã sắp ──────────────────────
        foreach (var edge in sortedEdges)
        {
            var srcNode = graph.GetNode(edge.Source)!;
            var tgtNode = graph.GetNode(edge.Target)!;
            int compSrc = Find(edge.Source);
            int compTgt = Find(edge.Target);

            bool sameCom = (compSrc == compTgt);

            // Step: xét cạnh này
            steps.Add(new AlgorithmStep
            {
                Description    = $"Xét cạnh {srcNode.Label}—{tgtNode.Label} (w={FormatW(edge.Weight)}):\n" +
                                 $"  comp({srcNode.Label})={graph.GetNode(compSrc)?.Label}  " +
                                 $"comp({tgtNode.Label})={graph.GetNode(compTgt)?.Label}" +
                                 (sameCom
                                     ? "\n  → Cùng thành phần → BỎ QUA (sẽ tạo chu trình)."
                                     : "\n  → Khác thành phần → THÊM VÀO MST."),
                StepType       = "consider_edge",
                VisitedNodes   = new HashSet<int>(mstNodes),
                ActiveNodes    = new HashSet<int> { edge.Source, edge.Target },
                QueueOrStack   = new HashSet<int>(),
                HighlightEdges = new HashSet<int>(mstEdges),
                NodeLabels     = BuildCompLabels(parent, graph)
            });

            if (!sameCom)
            {
                // Thêm cạnh vào MST
                Union(edge.Source, edge.Target);
                mstEdges.Add(edge.Id);
                mstNodes.Add(edge.Source);
                mstNodes.Add(edge.Target);

                steps.Add(new AlgorithmStep
                {
                    Description    = $"  ✅ THÊM cạnh {srcNode.Label}—{tgtNode.Label} (w={FormatW(edge.Weight)}) vào MST.\n" +
                                     $"  Hợp nhất thành phần: comp({srcNode.Label}) ∪ comp({tgtNode.Label}).\n" +
                                     $"  MST hiện có {mstEdges.Count} cạnh.",
                    StepType       = "add_to_mst",
                    VisitedNodes   = new HashSet<int>(mstNodes),
                    ActiveNodes    = new HashSet<int> { edge.Source, edge.Target },
                    QueueOrStack   = new HashSet<int>(),
                    HighlightEdges = new HashSet<int>(mstEdges),
                    NodeLabels     = BuildCompLabels(parent, graph)
                });

                // Dừng sớm khi đủ V-1 cạnh (MST hoàn chỉnh)
                if (mstEdges.Count == graph.Nodes.Count - 1)
                {
                    steps.Add(new AlgorithmStep
                    {
                        Description    = $"🏁 Đã có {mstEdges.Count} = V-1 cạnh → MST hoàn chỉnh! Dừng sớm.",
                        StepType       = "early_stop",
                        VisitedNodes   = new HashSet<int>(mstNodes),
                        ActiveNodes    = new HashSet<int>(),
                        QueueOrStack   = new HashSet<int>(),
                        HighlightEdges = new HashSet<int>(mstEdges),
                        NodeLabels     = BuildCompLabels(parent, graph)
                    });
                    break;
                }
            }
            else
            {
                // Từ chối cạnh
                rejEdges.Add(edge.Id);
                steps.Add(new AlgorithmStep
                {
                    Description    = $"  ❌ BỎ QUA cạnh {srcNode.Label}—{tgtNode.Label} (w={FormatW(edge.Weight)})\n" +
                                     $"  → Cùng thành phần, thêm vào sẽ tạo chu trình.",
                    StepType       = "reject_edge",
                    VisitedNodes   = new HashSet<int>(mstNodes),
                    ActiveNodes    = new HashSet<int>(),
                    QueueOrStack   = new HashSet<int>(),
                    HighlightEdges = new HashSet<int>(mstEdges),
                    NodeLabels     = BuildCompLabels(parent, graph)
                });
            }
        }

        // ── Kết quả ───────────────────────────────────────────────────
        double totalWeight = mstEdges
            .Select(eid => graph.Edges.FirstOrDefault(e => e.Id == eid)?.Weight ?? 0)
            .Sum();

        int components = graph.Nodes
            .Select(n => Find(n.Id))
            .Distinct().Count();

        string doneDesc;
        if (components == 1)
        {
            doneDesc = $"✅ HOÀN TẤT! Cây khung nhỏ nhất (Kruskal):\n" +
                       $"  Số cạnh MST : {mstEdges.Count} (= V-1 = {graph.Nodes.Count - 1})\n" +
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
            doneDesc = $"⚠️ Đồ thị có {components} thành phần liên thông — MST không liên thông.\n" +
                       $"  Số cạnh MST: {mstEdges.Count}\n" +
                       $"  Tổng trọng số: {FormatW(totalWeight)}\n" +
                       $"  (Kruskal tạo Spanning Forest thay vì Spanning Tree)";
        }

        steps.Add(new AlgorithmStep
        {
            Description    = doneDesc,
            StepType       = "done",
            VisitedNodes   = new HashSet<int>(mstNodes),
            ActiveNodes    = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(mstEdges),
            NodeLabels     = BuildCompLabels(parent, graph)
        });

        return steps;
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// NodeLabels hiển thị thành phần liên thông hiện tại của mỗi đỉnh.
    /// "comp X" — X là nhãn của đỉnh gốc (root) trong Union-Find.
    /// </summary>
    private static Dictionary<int, string> BuildCompLabels(
        Dictionary<int, int> parent, Graph graph)
    {
        // Cần path-compression-safe find (không sửa parent vì đang build labels)
        int SafeFind(int x)
        {
            while (parent[x] != x) x = parent[x];
            return x;
        }

        var labels = new Dictionary<int, string>();
        foreach (var node in graph.Nodes)
        {
            int root = SafeFind(node.Id);
            string rootLabel = graph.GetNode(root)?.Label ?? root.ToString();
            labels[node.Id] = root == node.Id ? $"root" : $"→{rootLabel}";
        }
        return labels;
    }

    private static string FormatW(double w) =>
        w == Math.Floor(w) ? ((int)w).ToString() : w.ToString("F2");
}

using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Advanced;

/// <summary>
/// Thuật toán Fleury tìm đường Euler / chu trình Euler.
/// Quy tắc: tại mỗi bước, chọn cạnh KHÔNG phải cầu (bridge) nếu còn lựa chọn.
/// Bridge detection: DFS naive — tạm xóa cạnh, kiểm tra khả năng đạt đến đích.
/// Hỗ trợ cả đồ thị có hướng và vô hướng.
/// </summary>
public class Fleury : IGraphAlgorithm
{
    public string Name => "Fleury – Đường/Chu trình Euler";

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

        if (graph.Nodes.Count == 0)
        {
            steps.Add(AlgorithmStep.Create("Đồ thị rỗng.", "error"));
            return steps;
        }

        // ── Kiểm tra điều kiện Euler ─────────────────────────────────
        var (canRun, eulerType, bestStart, conditionDesc) =
            CheckEulerCondition(graph, startId);

        steps.Add(new AlgorithmStep
        {
            Description    = conditionDesc,
            StepType       = canRun ? "init" : "error",
            VisitedNodes   = new HashSet<int>(),
            ActiveNodes    = new HashSet<int> { bestStart },
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(),
            NodeLabels     = BuildDegreeLabels(graph)
        });

        if (!canRun) return steps;

        // ── Chọn đỉnh bắt đầu ────────────────────────────────────────
        var startNode = graph.GetNode(bestStart)!;
        steps.Add(new AlgorithmStep
        {
            Description    = $"Chọn đỉnh bắt đầu: {startNode.Label}\n" +
                             (eulerType == EulerType.Circuit
                                 ? "  (Tất cả bậc chẵn — bắt đầu từ đỉnh bất kỳ)"
                                 : "  (Đỉnh có bậc lẻ — bắt đầu bắt buộc từ đây)"),
            StepType       = "choose_start",
            VisitedNodes   = new HashSet<int>(),
            ActiveNodes    = new HashSet<int> { bestStart },
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(),
            NodeLabels     = BuildDegreeLabels(graph)
        });

        // ── Fleury main loop ──────────────────────────────────────────
        var usedEdges  = new HashSet<int>();         // cạnh đã đi qua
        var eulerPath  = new List<int> { bestStart };
        var eulerEdges = new List<int>();             // thứ tự cạnh Euler
        var visitedSet = new HashSet<int> { bestStart };
        int current    = bestStart;

        while (true)
        {
            var available = GetAvailableEdges(graph, current, usedEdges);
            if (available.Count == 0) break;

            // Chỉ 1 lựa chọn → không cần check bridge
            if (available.Count == 1)
            {
                var edge = available[0];
                int next = Other(edge, current);

                steps.Add(new AlgorithmStep
                {
                    Description    = $"Chỉ còn 1 cạnh từ {graph.GetNode(current)!.Label}: " +
                                     $"{EdgeLabel(graph, edge)} → đi ngay (không cần check cầu).",
                    StepType       = "move",
                    VisitedNodes   = new HashSet<int>(visitedSet),
                    ActiveNodes    = new HashSet<int> { current },
                    QueueOrStack   = new HashSet<int> { next },
                    ConsideredEdges= new HashSet<int> { edge.Id },
                    HighlightEdges = new HashSet<int>(eulerEdges),
                    NodeLabels     = BuildPathLabels(eulerPath, graph)
                });

                usedEdges.Add(edge.Id);
                eulerEdges.Add(edge.Id);
                current = next;
                visitedSet.Add(current);
                eulerPath.Add(current);
                continue;
            }

            // Nhiều lựa chọn → kiểm tra bridge từng cạnh
            Edge? chosen    = null;
            bool foundNonBridge = false;

            foreach (var edge in available)
            {
                int next = Other(edge, current);
                bool isBridge = IsBridgeInRemaining(edge, usedEdges, graph);

                steps.Add(new AlgorithmStep
                {
                    Description    = $"Xét cạnh {EdgeLabel(graph, edge)} từ {graph.GetNode(current)!.Label}:\n" +
                                     (isBridge
                                         ? $"  ⚠️ Đây là CẦU — tránh nếu còn lựa chọn khác."
                                         : $"  ✅ Không phải cầu — an toàn để đi."),
                    StepType       = "check_bridge",
                    VisitedNodes   = new HashSet<int>(visitedSet),
                    ActiveNodes    = new HashSet<int> { current },
                    QueueOrStack   = new HashSet<int> { next },
                    ConsideredEdges= new HashSet<int> { edge.Id },
                    HighlightEdges = new HashSet<int>(eulerEdges),
                    NodeLabels     = BuildPathLabels(eulerPath, graph)
                });

                if (!isBridge)
                {
                    chosen = edge;
                    foundNonBridge = true;
                    break;
                }
            }

            // Tất cả đều là cầu → buộc phải dùng cầu đầu tiên
            chosen ??= available[0];

            if (!foundNonBridge)
            {
                steps.Add(new AlgorithmStep
                {
                    Description    = $"Tất cả cạnh từ {graph.GetNode(current)!.Label} đều là cầu.\n" +
                                     $"  Buộc phải đi qua cầu {EdgeLabel(graph, chosen)}.",
                    StepType       = "avoid_bridge",
                    VisitedNodes   = new HashSet<int>(visitedSet),
                    ActiveNodes    = new HashSet<int> { current },
                    QueueOrStack   = new HashSet<int>(),
                    HighlightEdges = new HashSet<int>(eulerEdges),
                    NodeLabels     = BuildPathLabels(eulerPath, graph)
                });
            }

            // Di chuyển theo cạnh đã chọn
            int nxt = Other(chosen, current);
            steps.Add(new AlgorithmStep
            {
                Description    = $"🚶 Di chuyển: {graph.GetNode(current)!.Label} → {graph.GetNode(nxt)!.Label} " +
                                 $"(cạnh {EdgeLabel(graph, chosen)}).\n" +
                                 $"  Đường đi hiện tại: {string.Join(" → ", eulerPath.Concat(new[] { nxt }).Select(id => graph.GetNode(id)?.Label ?? "?"))}",
                StepType       = "move",
                VisitedNodes   = new HashSet<int>(visitedSet),
                ActiveNodes    = new HashSet<int> { nxt },
                QueueOrStack   = new HashSet<int>(),
                HighlightEdges = new HashSet<int>(eulerEdges.Concat(new[] { chosen.Id })),
                NodeLabels     = BuildPathLabels(eulerPath, graph)
            });

            usedEdges.Add(chosen.Id);
            eulerEdges.Add(chosen.Id);
            current = nxt;
            visitedSet.Add(current);
            eulerPath.Add(current);
        }

        // ── Kết quả ───────────────────────────────────────────────────
        bool success = usedEdges.Count == graph.Edges.Count;
        string pathStr = string.Join(" → ", eulerPath.Select(id => graph.GetNode(id)?.Label ?? "?"));

        string doneDesc;
        if (success)
        {
            bool isClosed = eulerPath.First() == eulerPath.Last();
            doneDesc = (isClosed ? "✅ CHU TRÌNH EULER tìm được:\n" : "✅ ĐƯỜNG EULER tìm được:\n") +
                       $"  {pathStr}\n" +
                       $"  Số cạnh: {eulerEdges.Count} / {graph.Edges.Count}";
        }
        else
        {
            doneDesc = $"⚠️ Chỉ đi được {eulerEdges.Count}/{graph.Edges.Count} cạnh.\n" +
                       $"  Có thể đồ thị không liên thông.\n" +
                       $"  Đường đi: {pathStr}";
        }

        steps.Add(new AlgorithmStep
        {
            Description    = doneDesc,
            StepType       = "done",
            VisitedNodes   = new HashSet<int>(visitedSet),
            ActiveNodes    = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(eulerEdges),
            NodeLabels     = BuildPathLabels(eulerPath, graph)
        });

        return steps;
    }

    // ─── Euler condition check ─────────────────────────────────────────

    private enum EulerType { Circuit, Path, None }

    private static (bool canRun, EulerType type, int startId, string desc)
        CheckEulerCondition(Graph graph, int requestedStart)
    {
        // Kiểm tra liên thông (chỉ xét các đỉnh có ít nhất 1 cạnh)
        var nodesWithEdges = graph.Edges
            .SelectMany(e => new[] { e.Source, e.Target })
            .Distinct().ToHashSet();

        if (nodesWithEdges.Count == 0)
            return (false, EulerType.None, requestedStart, "Đồ thị không có cạnh nào.");

        if (!IsConnectedForEuler(graph, nodesWithEdges))
            return (false, EulerType.None, requestedStart,
                "❌ Đồ thị không liên thông — không tồn tại đường/chu trình Euler.");

        if (!graph.Directed)
        {
            // Vô hướng: bậc của mỗi đỉnh
            var degree = new Dictionary<int, int>();
            foreach (var n in graph.Nodes) degree[n.Id] = 0;
            foreach (var e in graph.Edges)
            {
                degree[e.Source]++;
                degree[e.Target]++;
            }

            var oddNodes = degree.Where(kv => kv.Value % 2 != 0).Select(kv => kv.Key).ToList();

            if (oddNodes.Count == 0)
            {
                // Circuit
                int start = nodesWithEdges.Contains(requestedStart) ? requestedStart : nodesWithEdges.First();
                return (true, EulerType.Circuit, start,
                    "✅ Đồ thị có CHU TRÌNH EULER (tất cả đỉnh có bậc chẵn).\n" +
                    $"  Bậc các đỉnh: {string.Join(", ", degree.Where(kv => nodesWithEdges.Contains(kv.Key)).Select(kv => $"{graph.GetNode(kv.Key)?.Label}={kv.Value}"))}\n" +
                    $"  Bắt đầu từ đỉnh bất kỳ.");
            }

            if (oddNodes.Count == 2)
            {
                // Path: bắt đầu từ 1 trong 2 đỉnh bậc lẻ
                int start = oddNodes.Contains(requestedStart) ? requestedStart : oddNodes[0];
                return (true, EulerType.Path, start,
                    "✅ Đồ thị có ĐƯỜNG EULER (2 đỉnh bậc lẻ).\n" +
                    $"  Đỉnh bậc lẻ: {string.Join(", ", oddNodes.Select(id => $"{graph.GetNode(id)?.Label}(bậc {degree[id]})"))}\n" +
                    $"  Phải bắt đầu từ một trong hai đỉnh bậc lẻ.");
            }

            return (false, EulerType.None, requestedStart,
                $"❌ Không tồn tại đường Euler ({oddNodes.Count} đỉnh có bậc lẻ, cần 0 hoặc 2).\n" +
                $"  Đỉnh bậc lẻ: {string.Join(", ", oddNodes.Select(id => $"{graph.GetNode(id)?.Label}(bậc {degree[id]})"))}");
        }
        else
        {
            // Có hướng
            var inDeg  = new Dictionary<int, int>();
            var outDeg = new Dictionary<int, int>();
            foreach (var n in graph.Nodes) { inDeg[n.Id] = 0; outDeg[n.Id] = 0; }
            foreach (var e in graph.Edges) { outDeg[e.Source]++; inDeg[e.Target]++; }

            int srcCandidates = 0, sinkCandidates = 0;
            int eulerStart = requestedStart;

            foreach (var n in graph.Nodes)
            {
                int diff = outDeg[n.Id] - inDeg[n.Id];
                if (diff == 1) { srcCandidates++; eulerStart = n.Id; }
                else if (diff == -1) sinkCandidates++;
                else if (diff != 0)
                    return (false, EulerType.None, requestedStart,
                        $"❌ Không tồn tại đường Euler (đỉnh {graph.GetNode(n.Id)?.Label} có |out-in|={Math.Abs(diff)} > 1).");
            }

            if (srcCandidates == 0 && sinkCandidates == 0)
            {
                int start = nodesWithEdges.Contains(requestedStart) ? requestedStart : nodesWithEdges.First();
                return (true, EulerType.Circuit, start,
                    "✅ Đồ thị có CÓ HƯỚNG CHU TRÌNH EULER (in-degree = out-degree với mọi đỉnh).");
            }

            if (srcCandidates == 1 && sinkCandidates == 1)
            {
                int start = nodesWithEdges.Contains(eulerStart) ? eulerStart : nodesWithEdges.First();
                return (true, EulerType.Path, start,
                    "✅ Đồ thị có ĐƯỜNG EULER CÓ HƯỚNG.\n" +
                    $"  Bắt đầu từ đỉnh có out-in=+1.");
            }

            return (false, EulerType.None, requestedStart,
                $"❌ Không tồn tại đường Euler có hướng ({srcCandidates} đỉnh out>in, {sinkCandidates} đỉnh in>out).");
        }
    }

    // ─── Bridge detection (naive DFS) ─────────────────────────────────

    /// <summary>
    /// Kiểm tra xem <paramref name="edge"/> có phải cầu trong đồ thị còn lại không.
    /// Tạm xóa cạnh, kiểm tra đỉnh đích có còn đi được từ đỉnh nguồn không.
    /// O(V + E) mỗi lần gọi.
    /// </summary>
    private static bool IsBridgeInRemaining(Edge edge, HashSet<int> usedEdges, Graph graph)
    {
        // Tạm xóa edge
        var tempUsed = new HashSet<int>(usedEdges) { edge.Id };
        int src = edge.Source, tgt = edge.Target;

        // DFS/BFS từ src, không dùng tempUsed edges
        var visited = new HashSet<int>();
        var stack   = new Stack<int>();
        stack.Push(src);

        while (stack.Count > 0)
        {
            int v = stack.Pop();
            if (!visited.Add(v)) continue;

            foreach (var e in graph.Edges)
            {
                if (tempUsed.Contains(e.Id)) continue;
                if (e.Source == v && !visited.Contains(e.Target)) stack.Push(e.Target);
                if (!graph.Directed && e.Target == v && !visited.Contains(e.Source)) stack.Push(e.Source);
            }
        }

        return !visited.Contains(tgt);  // tgt không đạt được → edge là cầu
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    private static bool IsConnectedForEuler(Graph graph, HashSet<int> nodesWithEdges)
    {
        if (nodesWithEdges.Count == 0) return true;
        int startNode = nodesWithEdges.First();
        var visited   = new HashSet<int>();
        var stack     = new Stack<int>();
        stack.Push(startNode);

        while (stack.Count > 0)
        {
            int v = stack.Pop();
            if (!visited.Add(v)) continue;
            foreach (var e in graph.Edges)
            {
                if (e.Source == v && !visited.Contains(e.Target)) stack.Push(e.Target);
                if (!graph.Directed && e.Target == v && !visited.Contains(e.Source)) stack.Push(e.Source);
            }
        }

        return nodesWithEdges.All(n => visited.Contains(n));
    }

    private static List<Edge> GetAvailableEdges(Graph graph, int nodeId, HashSet<int> usedEdges)
    {
        var result = new List<Edge>();
        foreach (var e in graph.Edges)
        {
            if (usedEdges.Contains(e.Id)) continue;
            if (e.Source == nodeId) result.Add(e);
            if (!graph.Directed && e.Target == nodeId) result.Add(e);
        }
        return result;
    }

    private static int Other(Edge edge, int current) =>
        edge.Source == current ? edge.Target : edge.Source;

    private static string EdgeLabel(Graph graph, Edge edge)
    {
        string s = graph.GetNode(edge.Source)?.Label ?? "?";
        string t = graph.GetNode(edge.Target)?.Label ?? "?";
        return graph.Directed ? $"{s}→{t}" : $"{s}—{t}";
    }

    private static Dictionary<int, string> BuildDegreeLabels(Graph graph)
    {
        var deg = new Dictionary<int, int>();
        foreach (var n in graph.Nodes) deg[n.Id] = 0;
        foreach (var e in graph.Edges)
        {
            deg[e.Source]++;
            if (!graph.Directed) deg[e.Target]++;
        }
        return deg.ToDictionary(kv => kv.Key, kv => $"bậc {kv.Value}");
    }

    private static Dictionary<int, string> BuildPathLabels(List<int> path, Graph graph)
    {
        var labels = new Dictionary<int, string>();
        for (int i = 0; i < path.Count; i++)
        {
            int step = i + 1;
            if (!labels.TryGetValue(path[i], out _))
                labels[path[i]] = $"#{step}";
            else
                labels[path[i]] += $",{step}";   // đỉnh đi qua nhiều lần
        }
        return labels;
    }
}

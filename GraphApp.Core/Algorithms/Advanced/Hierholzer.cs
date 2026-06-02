using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Advanced;

/// <summary>
/// Thuật toán Hierholzer tìm đường/chu trình Euler (O(E)).
/// Nhanh hơn Fleury vì không cần kiểm tra bridge.
/// Ý tưởng: dùng stack để tích lũy đường, khi bế tắc → quay lại merge sub-circuit.
///
/// Điều kiện (chia sẻ cùng logic với Fleury):
///   Vô hướng: bậc chẵn → circuit | 2 bậc lẻ → path
///   Có hướng: in=out mọi đỉnh → circuit | 1 out-in=+1, 1 in-out=+1 → path
/// </summary>
public class Hierholzer : IGraphAlgorithm
{
    public string Name => "Hierholzer – Đường/Chu trình Euler (O(E))";

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

        // ── Kiểm tra điều kiện Euler (tái sử dụng logic từ Fleury) ───
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

        // ── Khởi tạo ─────────────────────────────────────────────────
        var startNode  = graph.GetNode(bestStart)!;
        var usedEdges  = new HashSet<int>();
        // edgePointer[nodeId] = index con trỏ vào danh sách cạnh kề (lazy iteration)
        var edgeIndex  = new Dictionary<int, int>();
        foreach (var n in graph.Nodes) edgeIndex[n.Id] = 0;

        // Danh sách cạnh kề cho mỗi đỉnh (chỉ forward nếu directed)
        var adjEdges = BuildAdjEdges(graph);

        var eulerPath  = new List<int>();    // kết quả cuối cùng (ngược)
        var eulerEdgeOrder = new List<int>(); // edge ids theo thứ tự

        // ── Step 1: Khởi tạo ─────────────────────────────────────────
        steps.Add(new AlgorithmStep
        {
            Description    = $"Khởi tạo Hierholzer từ đỉnh {startNode.Label}.\n" +
                             $"  Dùng stack để đi theo cạnh cho đến khi bế tắc,\n" +
                             $"  sau đó merge sub-circuit vào đường chính.",
            StepType       = "start_circuit",
            VisitedNodes   = new HashSet<int>(),
            ActiveNodes    = new HashSet<int> { bestStart },
            QueueOrStack   = new HashSet<int> { bestStart },
            HighlightEdges = new HashSet<int>(),
            NodeLabels     = BuildDegreeLabels(graph)
        });

        // ── Hierholzer stack algorithm ────────────────────────────────
        var stack    = new Stack<(int node, int? edgeId)>();  // (đỉnh, cạnh dùng để đến đỉnh này)
        var pathEdges = new List<int>();   // edges in final path order (reversed)

        stack.Push((bestStart, null));

        while (stack.Count > 0)
        {
            int v = stack.Peek().node;
            var vEdges = adjEdges.GetValueOrDefault(v) ?? new List<Edge>();

            // Tìm cạnh kề chưa dùng (dùng con trỏ)
            int idx = edgeIndex.GetValueOrDefault(v);
            while (idx < vEdges.Count && usedEdges.Contains(vEdges[idx].Id))
                idx++;
            edgeIndex[v] = idx;

            if (idx < vEdges.Count)
            {
                // Còn cạnh → push đỉnh tiếp theo lên stack
                var edge = vEdges[idx];
                int next = graph.Directed ? edge.Target
                    : (edge.Source == v ? edge.Target : edge.Source);

                usedEdges.Add(edge.Id);
                edgeIndex[v] = idx + 1;

                // Step: extend_path
                steps.Add(new AlgorithmStep
                {
                    Description    = $"📌 Đẩy lên stack: {graph.GetNode(v)!.Label} → {graph.GetNode(next)!.Label}\n" +
                                     $"  Đi theo cạnh {EdgeLabel(graph, edge)}, stack có {stack.Count + 1} đỉnh.",
                    StepType       = "extend_path",
                    VisitedNodes   = new HashSet<int>(eulerPath),
                    ActiveNodes    = new HashSet<int> { v },
                    QueueOrStack   = new HashSet<int>(stack.Select(x => x.node)) { next },
                    HighlightEdges = new HashSet<int>(usedEdges),
                    NodeLabels     = BuildStackLabels(stack.Select(x => x.node).ToList(), graph)
                });

                stack.Push((next, edge.Id));
            }
            else
            {
                // Bế tắc → pop ra, đưa vào đường Euler
                var (popped, eid) = stack.Pop();
                eulerPath.Add(popped);
                if (eid.HasValue) pathEdges.Add(eid.Value);

                // Step: merge_circuit
                string mergedSoFar = eulerPath.Count > 1
                    ? string.Join(" ← ", eulerPath.TakeLast(Math.Min(5, eulerPath.Count))
                        .Select(id => graph.GetNode(id)?.Label ?? "?"))
                    : graph.GetNode(popped)?.Label ?? "?";

                steps.Add(new AlgorithmStep
                {
                    Description    = $"🔁 Bế tắc tại {graph.GetNode(popped)!.Label} — đưa vào chu trình.\n" +
                                     $"  Đường tích lũy: ...{mergedSoFar}\n" +
                                     $"  Stack còn {stack.Count} đỉnh.",
                    StepType       = "merge_circuit",
                    VisitedNodes   = new HashSet<int>(eulerPath),
                    ActiveNodes    = new HashSet<int> { popped },
                    QueueOrStack   = new HashSet<int>(stack.Select(x => x.node)),
                    HighlightEdges = new HashSet<int>(pathEdges),
                    NodeLabels     = BuildStepLabels(eulerPath, graph)
                });
            }
        }

        // ── Xây dựng kết quả ─────────────────────────────────────────
        eulerPath.Reverse();
        pathEdges.Reverse();

        bool success = pathEdges.Count == graph.Edges.Count;
        bool isClosed = eulerPath.Count > 1 && eulerPath.First() == eulerPath.Last();

        string pathStr = string.Join(" → ",
            eulerPath.Select(id => graph.GetNode(id)?.Label ?? "?"));

        string doneDesc;
        if (success)
        {
            doneDesc = (isClosed ? "✅ CHU TRÌNH EULER (Hierholzer):\n"
                                 : "✅ ĐƯỜNG EULER (Hierholzer):\n") +
                       $"  {pathStr}\n" +
                       $"  Số cạnh: {pathEdges.Count} — Độ phức tạp O(E)";
        }
        else
        {
            doneDesc = $"⚠️ Chỉ tìm được {pathEdges.Count}/{graph.Edges.Count} cạnh.\n" +
                       $"  Đồ thị có thể không liên thông.\n  {pathStr}";
        }

        steps.Add(new AlgorithmStep
        {
            Description    = doneDesc,
            StepType       = "done",
            VisitedNodes   = new HashSet<int>(eulerPath),
            ActiveNodes    = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(pathEdges),
            NodeLabels     = BuildStepLabels(eulerPath, graph)
        });

        return steps;
    }

    // ─── Euler condition check (same logic as Fleury) ──────────────────

    private enum EulerType { Circuit, Path }

    private static (bool canRun, EulerType type, int startId, string desc)
        CheckEulerCondition(Graph graph, int requestedStart)
    {
        var nodesWithEdges = graph.Edges
            .SelectMany(e => new[] { e.Source, e.Target })
            .Distinct().ToHashSet();

        if (nodesWithEdges.Count == 0)
            return (false, EulerType.Circuit, requestedStart, "Đồ thị không có cạnh nào.");

        if (!IsConnectedForEuler(graph, nodesWithEdges))
            return (false, EulerType.Circuit, requestedStart,
                "❌ Đồ thị không liên thông — không tồn tại đường/chu trình Euler.");

        if (!graph.Directed)
        {
            var degree = new Dictionary<int, int>();
            foreach (var n in graph.Nodes) degree[n.Id] = 0;
            foreach (var e in graph.Edges) { degree[e.Source]++; degree[e.Target]++; }

            var oddNodes = degree.Where(kv => kv.Value % 2 != 0).Select(kv => kv.Key).ToList();

            if (oddNodes.Count == 0)
            {
                int start = nodesWithEdges.Contains(requestedStart) ? requestedStart : nodesWithEdges.First();
                return (true, EulerType.Circuit, start,
                    "✅ CHU TRÌNH EULER — tất cả đỉnh có bậc chẵn.\n" +
                    $"  Bậc: {string.Join(", ", degree.Where(kv => nodesWithEdges.Contains(kv.Key)).Select(kv => $"{graph.GetNode(kv.Key)?.Label}={kv.Value}"))}" );
            }
            if (oddNodes.Count == 2)
            {
                int start = oddNodes.Contains(requestedStart) ? requestedStart : oddNodes[0];
                return (true, EulerType.Path, start,
                    "✅ ĐƯỜNG EULER — 2 đỉnh bậc lẻ.\n" +
                    $"  Bắt đầu từ: {graph.GetNode(start)?.Label}");
            }
            return (false, EulerType.Circuit, requestedStart,
                $"❌ {oddNodes.Count} đỉnh có bậc lẻ — không tồn tại đường Euler.");
        }
        else
        {
            var inDeg  = new Dictionary<int, int>();
            var outDeg = new Dictionary<int, int>();
            foreach (var n in graph.Nodes) { inDeg[n.Id] = 0; outDeg[n.Id] = 0; }
            foreach (var e in graph.Edges) { outDeg[e.Source]++; inDeg[e.Target]++; }

            int srcCand = 0, sinkCand = 0;
            int eulerStart = requestedStart;

            foreach (var n in graph.Nodes)
            {
                int diff = outDeg[n.Id] - inDeg[n.Id];
                if (diff == 1) { srcCand++; eulerStart = n.Id; }
                else if (diff == -1) sinkCand++;
                else if (diff != 0)
                    return (false, EulerType.Circuit, requestedStart,
                        $"❌ Đỉnh {graph.GetNode(n.Id)?.Label} có |out-in|={Math.Abs(diff)} > 1.");
            }
            if (srcCand == 0 && sinkCand == 0)
            {
                int start = nodesWithEdges.Contains(requestedStart) ? requestedStart : nodesWithEdges.First();
                return (true, EulerType.Circuit, start, "✅ CHU TRÌNH EULER có hướng (in=out mọi đỉnh).");
            }
            if (srcCand == 1 && sinkCand == 1)
            {
                int start = nodesWithEdges.Contains(eulerStart) ? eulerStart : nodesWithEdges.First();
                return (true, EulerType.Path, start, "✅ ĐƯỜNG EULER có hướng.");
            }
            return (false, EulerType.Circuit, requestedStart,
                $"❌ {srcCand} đỉnh out>in, {sinkCand} đỉnh in>out — không hợp lệ.");
        }
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    private static Dictionary<int, List<Edge>> BuildAdjEdges(Graph graph)
    {
        var adj = new Dictionary<int, List<Edge>>();
        foreach (var n in graph.Nodes) adj[n.Id] = new List<Edge>();
        foreach (var e in graph.Edges)
        {
            adj[e.Source].Add(e);
            if (!graph.Directed) adj[e.Target].Add(e);
        }
        return adj;
    }

    private static bool IsConnectedForEuler(Graph graph, HashSet<int> nodesWithEdges)
    {
        if (nodesWithEdges.Count == 0) return true;
        int s0 = nodesWithEdges.First();
        var vis = new HashSet<int>();
        var stk = new Stack<int>();
        stk.Push(s0);
        while (stk.Count > 0)
        {
            int v = stk.Pop();
            if (!vis.Add(v)) continue;
            foreach (var e in graph.Edges)
            {
                if (e.Source == v && !vis.Contains(e.Target)) stk.Push(e.Target);
                if (!graph.Directed && e.Target == v && !vis.Contains(e.Source)) stk.Push(e.Source);
            }
        }
        return nodesWithEdges.All(n => vis.Contains(n));
    }

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
        foreach (var e in graph.Edges) { deg[e.Source]++; if (!graph.Directed) deg[e.Target]++; }
        return deg.ToDictionary(kv => kv.Key, kv => $"bậc {kv.Value}");
    }

    private static Dictionary<int, string> BuildStackLabels(List<int> stackNodes, Graph graph)
    {
        // Hiển thị vị trí trong stack
        var labels = new Dictionary<int, string>();
        for (int i = 0; i < stackNodes.Count; i++)
        {
            int id = stackNodes[i];
            string lbl = i == 0 ? "TOP" : $"S{i + 1}";
            if (!labels.TryGetValue(id, out _)) labels[id] = lbl;
        }
        return labels;
    }

    private static Dictionary<int, string> BuildStepLabels(List<int> path, Graph graph)
    {
        var labels = new Dictionary<int, string>();
        for (int i = 0; i < path.Count; i++)
        {
            if (!labels.ContainsKey(path[i])) labels[path[i]] = $"#{i + 1}";
            else labels[path[i]] += $",{i + 1}";
        }
        return labels;
    }
}

using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Traversal;

/// <summary>
/// Thuật toán duyệt đồ thị theo chiều sâu (Depth-First Search).
/// Sử dụng Stack tường minh — KHÔNG dùng đệ quy.
/// Trả về List&lt;AlgorithmStep&gt; để UI animation từng bước.
/// </summary>
public class DFS : IGraphAlgorithm
{
    public string Name => "DFS – Duyệt theo chiều sâu";

    // ─── IGraphAlgorithm ───────────────────────────────────────────────
    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        int startId = parameters != null && parameters.TryGetValue("startId", out var v)
            ? Convert.ToInt32(v) : (graph.Nodes.FirstOrDefault()?.Id ?? -1);
        return Run(graph, startId);
    }

    // ─── Static entry point (gọi trực tiếp từ UI) ──────────────────────
    /// <summary>
    /// Chạy DFS từ đỉnh <paramref name="startId"/> dùng Stack tường minh.
    /// </summary>
    public static List<AlgorithmStep> Run(Graph graph, int startId)
    {
        var steps = new List<AlgorithmStep>();

        // ── Kiểm tra đầu vào ──────────────────────────────────────────
        var startNode = graph.GetNode(startId);
        if (startNode == null)
        {
            steps.Add(AlgorithmStep.Create(
                $"Lỗi: Đỉnh có Id={startId} không tồn tại trong đồ thị.", "error"));
            return steps;
        }

        if (graph.Nodes.Count == 0)
        {
            steps.Add(AlgorithmStep.Create("Đồ thị rỗng, không thể duyệt.", "error"));
            return steps;
        }

        // ── Khởi tạo cấu trúc dữ liệu ────────────────────────────────
        var visited   = new HashSet<int>();    // đã thăm (xanh lá)
        var inStack   = new HashSet<int>();    // đang trong stack (cam)
        var treeEdges = new HashSet<int>();    // cạnh DFS tree (tím)
        var stack     = new Stack<int>();

        // Ghi nhớ cạnh dùng để đến mỗi đỉnh (để highlight DFS tree edge)
        var edgeToNode = new Dictionary<int, int>();   // nodeId → edgeId dùng để vào node đó

        // ── Step 0: Khởi tạo ─────────────────────────────────────────
        stack.Push(startId);
        inStack.Add(startId);

        steps.Add(new AlgorithmStep
        {
            Description    = $"Khởi tạo DFS: đẩy đỉnh {startNode.Label} vào ngăn xếp (Stack).\n" +
                             $"  DFS sẽ đi sâu theo từng nhánh trước khi quay lui.",
            StepType       = "init",
            VisitedNodes   = new HashSet<int>(visited),
            ActiveNodes    = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(inStack),
            HighlightEdges = new HashSet<int>(treeEdges),
            NodeLabels     = BuildStackLabel(stack)
        });

        // ── Vòng lặp DFS ─────────────────────────────────────────────
        while (stack.Count > 0)
        {
            int current     = stack.Pop();
            inStack.Remove(current);
            var currentNode = graph.GetNode(current)!;

            // Step: Pop — lấy đỉnh ra khỏi stack
            steps.Add(new AlgorithmStep
            {
                Description    = $"Pop đỉnh {currentNode.Label} khỏi ngăn xếp → kiểm tra.\n" +
                                 $"  Stack còn lại: [{StackToString(stack, graph)}]",
                StepType       = "pop",
                VisitedNodes   = new HashSet<int>(visited),
                ActiveNodes    = new HashSet<int> { current },
                QueueOrStack   = new HashSet<int>(inStack),
                HighlightEdges = new HashSet<int>(treeEdges),
                NodeLabels     = BuildStackLabel(stack)
            });

            // Đã thăm → bỏ qua (có thể bị push nhiều lần trước đó)
            if (visited.Contains(current))
            {
                steps.Add(new AlgorithmStep
                {
                    Description    = $"  Đỉnh {currentNode.Label} đã được thăm trước đó → bỏ qua, tiếp tục Pop.",
                    StepType       = "already_visited",
                    VisitedNodes   = new HashSet<int>(visited),
                    ActiveNodes    = new HashSet<int>(),
                    QueueOrStack   = new HashSet<int>(inStack),
                    HighlightEdges = new HashSet<int>(treeEdges),
                    NodeLabels     = BuildStackLabel(stack)
                });
                continue;
            }

            // Đánh dấu đã thăm + highlight cạnh DFS tree
            visited.Add(current);
            if (edgeToNode.TryGetValue(current, out int usedEdgeId))
                treeEdges.Add(usedEdgeId);

            // Step: Visit
            steps.Add(new AlgorithmStep
            {
                Description    = $"Đánh dấu đỉnh {currentNode.Label} đã thăm ✓\n" +
                                 $"  Thứ tự thăm: {string.Join(" → ", visited.Select(id => graph.GetNode(id)?.Label ?? id.ToString()))}",
                StepType       = "visit",
                VisitedNodes   = new HashSet<int>(visited),
                ActiveNodes    = new HashSet<int> { current },
                QueueOrStack   = new HashSet<int>(inStack),
                HighlightEdges = new HashSet<int>(treeEdges),
                NodeLabels     = BuildStackLabel(stack)
            });

            // ── Xét láng giềng ───────────────────────────────────────
            var neighbors = graph.Neighbors(current);

            // Đẩy ngược để đỉnh đầu tiên trong danh sách được xử lý trước
            // (Stack LIFO: push sau → pop trước)
            bool pushedAny = false;

            for (int i = neighbors.Count - 1; i >= 0; i--)
            {
                var (neighborId, edgeId, _) = neighbors[i];
                var neighborNode = graph.GetNode(neighborId)!;

                if (visited.Contains(neighborId))
                {
                    // Bỏ qua — sẽ báo ở step riêng (không tạo step để tránh quá nhiều bước)
                    continue;
                }

                // Cho phép push nhiều lần (khác BFS) — sẽ bị bắt khi pop
                stack.Push(neighborId);
                inStack.Add(neighborId);

                // Chỉ ghi nhớ edge đầu tiên (nếu chưa có)
                edgeToNode.TryAdd(neighborId, edgeId);

                pushedAny = true;

                steps.Add(new AlgorithmStep
                {
                    Description    = $"  Push láng giềng {neighborNode.Label} vào ngăn xếp.\n" +
                                     $"  Stack: [{StackToString(stack, graph)}]",
                    StepType       = "push_neighbor",
                    VisitedNodes   = new HashSet<int>(visited),
                    ActiveNodes    = new HashSet<int> { current },
                    QueueOrStack   = new HashSet<int>(inStack),
                    HighlightEdges = new HashSet<int>(treeEdges),
                    NodeLabels     = BuildStackLabel(stack)
                });
            }

            // Thông báo nếu không có láng giềng chưa thăm → quay lui
            if (!pushedAny)
            {
                steps.Add(new AlgorithmStep
                {
                    Description    = $"  Không có láng giềng chưa thăm của {currentNode.Label}.\n" +
                                     $"  Quay lui (Backtrack) → Pop đỉnh tiếp theo trong stack.",
                    StepType       = "backtrack",
                    VisitedNodes   = new HashSet<int>(visited),
                    ActiveNodes    = new HashSet<int>(),
                    QueueOrStack   = new HashSet<int>(inStack),
                    HighlightEdges = new HashSet<int>(treeEdges),
                    NodeLabels     = BuildStackLabel(stack)
                });
            }
        }

        // ── Step cuối: Kết quả ────────────────────────────────────────
        int unvisited  = graph.Nodes.Count - visited.Count;
        string order   = string.Join(" → ",
            visited.Select(id => graph.GetNode(id)?.Label ?? id.ToString()));

        string resultDesc = unvisited == 0
            ? $"DFS hoàn tất! Đã thăm tất cả {visited.Count} đỉnh.\n  Thứ tự: {order}"
            : $"DFS hoàn tất! Đã thăm {visited.Count}/{graph.Nodes.Count} đỉnh.\n" +
              $"  Thứ tự: {order}\n" +
              $"  {unvisited} đỉnh không thể tới từ {startNode.Label} (đồ thị không liên thông).";

        steps.Add(new AlgorithmStep
        {
            Description    = resultDesc,
            StepType       = "done",
            VisitedNodes   = new HashSet<int>(visited),
            ActiveNodes    = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(treeEdges)
        });

        return steps;
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Sinh NodeLabels hiển thị vị trí trong stack (Stack[1] = top).
    /// </summary>
    private static Dictionary<int, string> BuildStackLabel(Stack<int> stack)
    {
        var labels = new Dictionary<int, string>();
        int pos = 1;
        foreach (int id in stack)               // Stack<T> enumerate từ top xuống bottom
            labels[id] = $"Stack[{pos++}]";
        return labels;
    }

    /// <summary>Hiển thị stack dưới dạng "E, B, C" (top → bottom).</summary>
    private static string StackToString(Stack<int> stack, Graph graph)
    {
        if (stack.Count == 0) return "rỗng";
        var labels = stack.Select(id => graph.GetNode(id)?.Label ?? id.ToString());
        return string.Join(", ", labels) + " ← top";
    }
}

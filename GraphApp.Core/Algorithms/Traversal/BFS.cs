using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Traversal;

/// <summary>
/// Thuật toán duyệt đồ thị theo chiều rộng (Breadth-First Search).
/// Sử dụng Queue (hàng đợi FIFO).
/// Trả về List&lt;AlgorithmStep&gt; để UI animation từng bước.
/// </summary>
public class BFS : IGraphAlgorithm
{
    public string Name => "BFS – Duyệt theo chiều rộng";

    // ─── IGraphAlgorithm ───────────────────────────────────────────────
    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        int startId = parameters != null && parameters.TryGetValue("startId", out var v)
            ? Convert.ToInt32(v) : (graph.Nodes.FirstOrDefault()?.Id ?? -1);
        return Run(graph, startId);
    }

    // ─── Static entry point (gọi trực tiếp từ UI) ──────────────────────
    /// <summary>
    /// Chạy BFS từ đỉnh <paramref name="startId"/>.
    /// Trả về danh sách các bước animation.
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
        var visited   = new HashSet<int>();   // đã thăm (xanh lá)
        var inQueue   = new HashSet<int>();   // đang trong queue (cam)
        var treeEdges = new HashSet<int>();   // cạnh BFS tree (tím)
        var queue     = new Queue<int>();

        // ── Step 0: Khởi tạo ─────────────────────────────────────────
        visited.Add(startId);
        inQueue.Add(startId);
        queue.Enqueue(startId);

        steps.Add(new AlgorithmStep
        {
            Description    = $"Khởi tạo BFS: đánh dấu đỉnh {startNode.Label} đã thăm và" +
                             $" thêm vào hàng đợi (Queue).",
            StepType       = "init",
            VisitedNodes   = new HashSet<int>(visited),
            ActiveNodes    = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(inQueue),
            HighlightEdges = new HashSet<int>(treeEdges),
            NodeLabels     = BuildQueueLabel(queue)
        });

        // ── Vòng lặp BFS ─────────────────────────────────────────────
        while (queue.Count > 0)
        {
            int current     = queue.Dequeue();
            inQueue.Remove(current);
            var currentNode = graph.GetNode(current)!;

            // Step: Dequeue — đỉnh hiện tại đang được xét (đỏ)
            steps.Add(new AlgorithmStep
            {
                Description    = $"Lấy đỉnh {currentNode.Label} ra khỏi hàng đợi → bắt đầu xét các láng giềng.\n" +
                                 $"  Queue còn lại: [{QueueToString(queue, graph)}]",
                StepType       = "dequeue",
                VisitedNodes   = new HashSet<int>(visited),
                ActiveNodes    = new HashSet<int> { current },
                QueueOrStack   = new HashSet<int>(inQueue),
                HighlightEdges = new HashSet<int>(treeEdges),
                NodeLabels     = BuildQueueLabel(queue)
            });

            // ── Xét từng láng giềng ──────────────────────────────────
            var neighbors = graph.Neighbors(current);

            if (neighbors.Count == 0)
            {
                steps.Add(new AlgorithmStep
                {
                    Description    = $"  Đỉnh {currentNode.Label} không có láng giềng nào.",
                    StepType       = "no_neighbor",
                    VisitedNodes   = new HashSet<int>(visited),
                    ActiveNodes    = new HashSet<int> { current },
                    QueueOrStack   = new HashSet<int>(inQueue),
                    HighlightEdges = new HashSet<int>(treeEdges),
                    NodeLabels     = BuildQueueLabel(queue)
                });
            }

            foreach (var (neighborId, edgeId, _) in neighbors)
            {
                var neighborNode = graph.GetNode(neighborId)!;

                if (visited.Contains(neighborId))
                {
                    // Đã thăm → bỏ qua
                    steps.Add(new AlgorithmStep
                    {
                        Description    = $"  Xét láng giềng {neighborNode.Label}: đã thăm rồi → bỏ qua.",
                        StepType       = "already_visited",
                        VisitedNodes   = new HashSet<int>(visited),
                        ActiveNodes    = new HashSet<int> { current },
                        QueueOrStack   = new HashSet<int>(inQueue),
                        HighlightEdges = new HashSet<int>(treeEdges),
                        NodeLabels     = BuildQueueLabel(queue)
                    });
                }
                else
                {
                    // Chưa thăm → thêm vào queue
                    visited.Add(neighborId);
                    inQueue.Add(neighborId);
                    queue.Enqueue(neighborId);
                    treeEdges.Add(edgeId);

                    steps.Add(new AlgorithmStep
                    {
                        Description    = $"  Xét láng giềng {neighborNode.Label}: chưa thăm → " +
                                         $"đánh dấu và thêm vào hàng đợi.\n" +
                                         $"  Queue: [{QueueToString(queue, graph)}]",
                        StepType       = "visit_neighbor",
                        VisitedNodes   = new HashSet<int>(visited),
                        ActiveNodes    = new HashSet<int> { current },
                        QueueOrStack   = new HashSet<int>(inQueue),
                        HighlightEdges = new HashSet<int>(treeEdges),
                        NodeLabels     = BuildQueueLabel(queue)
                    });
                }
            }
        }

        // ── Step cuối: Kết quả ────────────────────────────────────────
        int unvisited = graph.Nodes.Count - visited.Count;
        string resultDesc = unvisited == 0
            ? $"BFS hoàn tất! Đã thăm tất cả {visited.Count} đỉnh của đồ thị."
            : $"BFS hoàn tất! Đã thăm {visited.Count}/{graph.Nodes.Count} đỉnh.\n" +
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

    /// <summary>Sinh NodeLabels dạng "in queue" để hiển thị thứ tự trong queue.</summary>
    private static Dictionary<int, string> BuildQueueLabel(Queue<int> queue)
    {
        var labels = new Dictionary<int, string>();
        int pos = 1;
        foreach (int id in queue)
            labels[id] = $"Queue[{pos++}]";
        return labels;
    }

    /// <summary>Hiển thị nội dung queue dưới dạng "A, B, C".</summary>
    private static string QueueToString(Queue<int> queue, Graph graph)
    {
        var labels = queue.Select(id => graph.GetNode(id)?.Label ?? id.ToString());
        return string.Join(", ", labels);
    }
}

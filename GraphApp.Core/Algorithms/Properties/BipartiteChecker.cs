using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Properties;

/// <summary>
/// Kiểm tra đồ thị có phải là đồ thị 2 phía (Bipartite) không.
/// Dùng BFS 2-coloring: tô 2 màu xen kẽ, nếu 2 đỉnh kề cùng màu → KHÔNG 2 phía.
/// Hoạt động với cả đồ thị có hướng và vô hướng, xử lý đồ thị không liên thông.
/// </summary>
public class BipartiteChecker : IGraphAlgorithm
{
    public string Name => "Kiểm tra đồ thị 2 phía (Bipartite)";

    // ─── IGraphAlgorithm ───────────────────────────────────────────────
    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
        => Run(graph);

    // ─── Static entry point ────────────────────────────────────────────
    /// <summary>
    /// Kiểm tra toàn bộ đồ thị (kể cả đồ thị không liên thông).
    /// </summary>
    public static List<AlgorithmStep> Run(Graph graph)
    {
        var steps = new List<AlgorithmStep>();

        if (graph.Nodes.Count == 0)
        {
            steps.Add(AlgorithmStep.Create(
                "Đồ thị rỗng. Quy ước: đồ thị rỗng là đồ thị 2 phía.", "result_true"));
            return steps;
        }

        // ── Khởi tạo ─────────────────────────────────────────────────
        // color: -1 = chưa tô, 0 = Nhóm A, 1 = Nhóm B
        var color       = new Dictionary<int, int>();
        var groupA      = new HashSet<int>();   // VisitedNodes (xanh lá)
        var groupB      = new HashSet<int>();   // QueueOrStack (cam)
        var bEdges      = new HashSet<int>();   // cạnh giữa 2 nhóm (tím)
        var conflictNodes = new HashSet<int>(); // 2 đỉnh cùng màu kề nhau (đỏ)
        bool isBipartite  = true;
        int componentCount = 0;

        foreach (var n in graph.Nodes)
            color[n.Id] = -1;

        // ── Step 0: Mô tả thuật toán ──────────────────────────────────
        steps.Add(new AlgorithmStep
        {
            Description    = "Kiểm tra đồ thị 2 phía bằng BFS 2-coloring:\n" +
                             "  Tô màu xen kẽ: đỉnh kề luôn khác nhóm.\n" +
                             "  Nếu 2 đỉnh kề cùng nhóm → KHÔNG 2 phía.",
            StepType       = "init",
            VisitedNodes   = new HashSet<int>(),
            QueueOrStack   = new HashSet<int>(),
            ActiveNodes    = new HashSet<int>(),
            HighlightEdges = new HashSet<int>(),
            NodeLabels     = new Dictionary<int, string>()
        });

        // ── Duyệt từng thành phần liên thông ─────────────────────────
        foreach (var startNode in graph.Nodes)
        {
            if (color[startNode.Id] != -1) continue;   // đã tô từ component trước
            componentCount++;

            // Gán Nhóm A cho đỉnh bắt đầu
            color[startNode.Id] = 0;
            groupA.Add(startNode.Id);

            var queue = new Queue<int>();
            queue.Enqueue(startNode.Id);

            steps.Add(new AlgorithmStep
            {
                Description    = componentCount == 1
                    ? $"Bắt đầu BFS từ đỉnh {startNode.Label} → gán Nhóm A (xanh lá)."
                    : $"Thành phần liên thông mới: bắt đầu từ đỉnh {startNode.Label} → gán Nhóm A.",
                StepType       = "color_a",
                VisitedNodes   = new HashSet<int>(groupA),
                QueueOrStack   = new HashSet<int>(groupB),
                ActiveNodes    = new HashSet<int> { startNode.Id },
                HighlightEdges = new HashSet<int>(bEdges),
                NodeLabels     = BuildGroupLabels(color, graph)
            });

            // BFS 2-coloring
            while (queue.Count > 0)
            {
                int current      = queue.Dequeue();
                var currentNode  = graph.GetNode(current)!;
                int currentColor = color[current];
                int nextColor    = 1 - currentColor;
                string currentGroup = currentColor == 0 ? "A" : "B";
                string nextGroup    = nextColor    == 0 ? "A" : "B";

                // Step: xét đỉnh hiện tại
                steps.Add(new AlgorithmStep
                {
                    Description    = $"Xét đỉnh {currentNode.Label} (Nhóm {currentGroup}) " +
                                     $"→ các láng giềng phải thuộc Nhóm {nextGroup}.",
                    StepType       = "check_node",
                    VisitedNodes   = new HashSet<int>(groupA),
                    QueueOrStack   = new HashSet<int>(groupB),
                    ActiveNodes    = new HashSet<int> { current },
                    HighlightEdges = new HashSet<int>(bEdges),
                    NodeLabels     = BuildGroupLabels(color, graph)
                });

                foreach (var (neighborId, edgeId, _) in graph.Neighbors(current))
                {
                    var neighborNode = graph.GetNode(neighborId)!;

                    if (color[neighborId] == -1)
                    {
                        // Chưa tô → tô màu đối diện
                        color[neighborId] = nextColor;
                        if (nextColor == 0) groupA.Add(neighborId);
                        else                groupB.Add(neighborId);
                        bEdges.Add(edgeId);
                        queue.Enqueue(neighborId);

                        string assignedGroup = nextColor == 0 ? "A" : "B";
                        string stepType      = nextColor == 0 ? "color_a" : "color_b";

                        steps.Add(new AlgorithmStep
                        {
                            Description    = $"  Láng giềng {neighborNode.Label} chưa tô màu\n" +
                                             $"  → Gán Nhóm {assignedGroup} và thêm vào hàng đợi.",
                            StepType       = stepType,
                            VisitedNodes   = new HashSet<int>(groupA),
                            QueueOrStack   = new HashSet<int>(groupB),
                            ActiveNodes    = new HashSet<int> { current, neighborId },
                            HighlightEdges = new HashSet<int>(bEdges),
                            NodeLabels     = BuildGroupLabels(color, graph)
                        });
                    }
                    else if (color[neighborId] == currentColor)
                    {
                        // XUNG ĐỘT: 2 đỉnh kề cùng nhóm → KHÔNG 2 phía!
                        conflictNodes = new HashSet<int> { current, neighborId };
                        isBipartite   = false;

                        steps.Add(new AlgorithmStep
                        {
                            Description    = $"  ⚠️ XUNG ĐỘT! Đỉnh {neighborNode.Label} cùng nhóm {currentGroup} với {currentNode.Label}.\n" +
                                             $"  → Đồ thị KHÔNG phải đồ thị 2 phía!",
                            StepType       = "conflict",
                            VisitedNodes   = new HashSet<int>(groupA),
                            QueueOrStack   = new HashSet<int>(groupB),
                            ActiveNodes    = conflictNodes,
                            HighlightEdges = new HashSet<int>(bEdges),
                            NodeLabels     = BuildGroupLabels(color, graph)
                        });

                        // Dừng ngay khi phát hiện xung đột
                        goto DoneLabel;
                    }
                    else
                    {
                        // Đã tô màu đúng → bỏ qua
                        string neighborGroup = color[neighborId] == 0 ? "A" : "B";
                        steps.Add(new AlgorithmStep
                        {
                            Description    = $"  Láng giềng {neighborNode.Label} đã ở Nhóm {neighborGroup} ✓ (đúng).",
                            StepType       = "already_colored",
                            VisitedNodes   = new HashSet<int>(groupA),
                            QueueOrStack   = new HashSet<int>(groupB),
                            ActiveNodes    = new HashSet<int> { current },
                            HighlightEdges = new HashSet<int>(bEdges),
                            NodeLabels     = BuildGroupLabels(color, graph)
                        });
                    }
                }
            }
        }

        DoneLabel:

        // ── Step cuối ────────────────────────────────────────────────
        if (isBipartite)
        {
            string groupAList = string.Join(", ",
                groupA.Select(id => graph.GetNode(id)?.Label ?? id.ToString()));
            string groupBList = string.Join(", ",
                groupB.Select(id => graph.GetNode(id)?.Label ?? id.ToString()));

            steps.Add(new AlgorithmStep
            {
                Description    = $"✅ KẾT QUẢ: Đồ thị LÀ đồ thị 2 phía!\n" +
                                 $"  Nhóm A (xanh lá): {{ {groupAList} }}\n" +
                                 $"  Nhóm B (cam):     {{ {groupBList} }}\n" +
                                 (componentCount > 1 ? $"  (Gồm {componentCount} thành phần liên thông)" : string.Empty),
                StepType       = "result_true",
                VisitedNodes   = new HashSet<int>(groupA),
                QueueOrStack   = new HashSet<int>(groupB),
                ActiveNodes    = new HashSet<int>(),
                HighlightEdges = new HashSet<int>(bEdges),
                NodeLabels     = BuildGroupLabels(color, graph)
            });
        }
        else
        {
            steps.Add(new AlgorithmStep
            {
                Description    = $"❌ KẾT QUẢ: Đồ thị KHÔNG phải đồ thị 2 phía!\n" +
                                 $"  Tìm thấy 2 đỉnh kề cùng nhóm: " +
                                 string.Join(" và ", conflictNodes
                                     .Select(id => $"{graph.GetNode(id)?.Label}")) + ".",
                StepType       = "result_false",
                VisitedNodes   = new HashSet<int>(groupA),
                QueueOrStack   = new HashSet<int>(groupB),
                ActiveNodes    = conflictNodes,
                HighlightEdges = new HashSet<int>(bEdges),
                NodeLabels     = BuildGroupLabels(color, graph)
            });
        }

        return steps;
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Sinh NodeLabels: "A" cho Nhóm A, "B" cho Nhóm B, "?" cho chưa tô.
    /// </summary>
    private static Dictionary<int, string> BuildGroupLabels(
        Dictionary<int, int> color, Graph graph)
    {
        var labels = new Dictionary<int, string>();
        foreach (var node in graph.Nodes)
        {
            if (!color.TryGetValue(node.Id, out int c)) continue;
            labels[node.Id] = c switch
            {
                0  => "Nhóm A",
                1  => "Nhóm B",
                _  => "?"
            };
        }
        return labels;
    }
}

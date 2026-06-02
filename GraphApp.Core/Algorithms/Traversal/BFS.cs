using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Traversal;

/// <summary>
/// Thuật toán duyệt đồ thị theo chiều rộng (Breadth-First Search).
/// </summary>
public class BFS : IGraphAlgorithm
{
    public string Name => "BFS – Duyệt theo chiều rộng";

    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        // TODO: TASK-05 — sẽ implement ở Phase 2
        throw new NotImplementedException("BFS chưa được implement — sẽ làm ở TASK-05.");
    }
}

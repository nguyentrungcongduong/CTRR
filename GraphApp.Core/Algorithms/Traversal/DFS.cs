using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Traversal;

/// <summary>
/// Thuật toán duyệt đồ thị theo chiều sâu (Depth-First Search).
/// Sử dụng Stack tường minh (không dùng đệ quy).
/// </summary>
public class DFS : IGraphAlgorithm
{
    public string Name => "DFS – Duyệt theo chiều sâu";

    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        // TODO: TASK-06 — sẽ implement ở Phase 2
        throw new NotImplementedException("DFS chưa được implement — sẽ làm ở TASK-06.");
    }
}

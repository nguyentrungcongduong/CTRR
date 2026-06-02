using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Properties;

/// <summary>
/// Kiểm tra đồ thị có phải là đồ thị 2 phía (Bipartite) không.
/// Sử dụng BFS 2-coloring.
/// </summary>
public class BipartiteChecker : IGraphAlgorithm
{
    public string Name => "Kiểm tra đồ thị 2 phía (Bipartite)";

    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        // TODO: TASK-09 — sẽ implement ở Phase 2
        throw new NotImplementedException("BipartiteChecker chưa được implement — sẽ làm ở TASK-09.");
    }
}

using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Advanced;

/// <summary>
/// Thuật toán Ford-Fulkerson tìm luồng cực đại (Max Flow) trên đồ thị có hướng.
/// </summary>
public class FordFulkerson : IGraphAlgorithm
{
    public string Name => "Ford-Fulkerson – Luồng cực đại";

    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        // TODO: TASK-16 — sẽ implement ở Phase 3
        throw new NotImplementedException("FordFulkerson chưa được implement — sẽ làm ở TASK-16.");
    }
}

using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.ShortestPath;

/// <summary>
/// Thuật toán Dijkstra tìm đường đi ngắn nhất từ một đỉnh nguồn.
/// </summary>
public class Dijkstra : IGraphAlgorithm
{
    public string Name => "Dijkstra – Đường đi ngắn nhất";

    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        // TODO: TASK-08 — sẽ implement ở Phase 2
        throw new NotImplementedException("Dijkstra chưa được implement — sẽ làm ở TASK-08.");
    }
}

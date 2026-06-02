using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Advanced;

/// <summary>
/// Thuật toán Kruskal tìm cây khung nhỏ nhất (MST) sử dụng Union-Find.
/// </summary>
public class Kruskal : IGraphAlgorithm
{
    public string Name => "Kruskal – Cây khung nhỏ nhất";

    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        // TODO: TASK-15 — sẽ implement ở Phase 3
        throw new NotImplementedException("Kruskal chưa được implement — sẽ làm ở TASK-15.");
    }
}

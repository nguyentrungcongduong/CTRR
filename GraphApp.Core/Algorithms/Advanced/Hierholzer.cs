using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Advanced;

/// <summary>
/// Thuật toán Hierholzer tìm chu trình Euler hiệu quả hơn Fleury.
/// </summary>
public class Hierholzer : IGraphAlgorithm
{
    public string Name => "Hierholzer – Chu trình Euler";

    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        // TODO: TASK-18 — sẽ implement ở Phase 3
        throw new NotImplementedException("Hierholzer chưa được implement — sẽ làm ở TASK-18.");
    }
}

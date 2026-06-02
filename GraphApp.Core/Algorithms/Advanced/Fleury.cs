using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Advanced;

/// <summary>
/// Thuật toán Fleury tìm đường Euler / chu trình Euler.
/// Sử dụng kiểm tra cầu (bridge detection) để chọn cạnh an toàn.
/// </summary>
public class Fleury : IGraphAlgorithm
{
    public string Name => "Fleury – Đường/Chu trình Euler";

    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        // TODO: TASK-17 — sẽ implement ở Phase 3
        throw new NotImplementedException("Fleury chưa được implement — sẽ làm ở TASK-17.");
    }
}

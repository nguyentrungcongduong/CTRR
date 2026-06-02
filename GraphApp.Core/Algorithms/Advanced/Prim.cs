using GraphApp.Core.Algorithms.Base;
using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Advanced;

/// <summary>
/// Thuật toán Prim tìm cây khung nhỏ nhất (MST) cho đồ thị vô hướng có trọng số.
/// </summary>
public class Prim : IGraphAlgorithm
{
    public string Name => "Prim – Cây khung nhỏ nhất";

    public List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null)
    {
        // TODO: TASK-14 — sẽ implement ở Phase 3
        throw new NotImplementedException("Prim chưa được implement — sẽ làm ở TASK-14.");
    }
}

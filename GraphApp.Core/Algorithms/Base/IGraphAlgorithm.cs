using GraphApp.Core.Models;

namespace GraphApp.Core.Algorithms.Base;

/// <summary>
/// Interface chung cho tất cả thuật toán đồ thị.
/// Mọi thuật toán đều trả về List&lt;AlgorithmStep&gt; để UI có thể animation từng bước.
/// </summary>
public interface IGraphAlgorithm
{
    /// <summary>Tên thuật toán (hiển thị trên UI)</summary>
    string Name { get; }

    /// <summary>
    /// Chạy thuật toán trên đồ thị và trả về danh sách các bước animation.
    /// </summary>
    /// <param name="graph">Đồ thị đầu vào</param>
    /// <param name="parameters">Tham số bổ sung (VD: startId, endId, sourceId, sinkId)</param>
    List<AlgorithmStep> Run(Graph graph, Dictionary<string, object>? parameters = null);
}

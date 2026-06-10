# Kỹ Thuật Triển Khai — GraphApp

Tài liệu này giải thích **cách làm** từng chức năng trong dự án: luồng xử lý, cấu trúc dữ liệu, và logic code quan trọng.

---

## 1. Kiến trúc tổng quan

Dự án chia thành 2 project riêng biệt theo nguyên tắc tách biệt logic và giao diện.

**`GraphApp.Core`** chứa toàn bộ logic thuần — không có tham chiếu đến WinForms. Bên trong có 3 nhóm chính: `Models` định nghĩa cấu trúc dữ liệu đồ thị, `Algorithms` chứa từng thuật toán, `Converters` và `Persistence` xử lý chuyển đổi và lưu trữ.

**`GraphApp.UI`** là lớp giao diện WinForms. `GraphCanvas` vẽ đồ thị bằng GDI+, `AnimationEngine` điều phối hoạt ảnh theo từng bước, `MainForm` kết nối mọi thứ lại với nhau.

Luồng dữ liệu cơ bản của ứng dụng là: người dùng tương tác với Canvas → Canvas cập nhật đối tượng `Graph` → khi chạy thuật toán, `Graph` được clone và truyền cho thuật toán → thuật toán trả về `List<AlgorithmStep>` → `AnimationEngine` phát từng bước → `GraphCanvas` đổi màu đỉnh/cạnh theo từng bước đó.

---

## 2. Mô hình dữ liệu đồ thị (Graph, Node, Edge)

### Thiết kế Graph

Lớp `Graph` lưu hai danh sách: `List<Node>` và `List<Edge>`. Mỗi `Node` có `Id` (int tự tăng), `Label` (chuỗi hiển thị), và `Position` (PointF tọa độ trên canvas). Mỗi `Edge` có `Id`, `Source`, `Target` (đều là Node Id), và `Weight` (double).

Thuộc tính `Directed` kiểm soát cách ứng xử của cả mô hình lẫn các thuật toán — khi `Directed = false`, một đối tượng `Edge` trong danh sách đại diện cho hai chiều đi lại.

### Phương thức Neighbors

Đây là phương thức được gọi nhiều nhất trong tất cả thuật toán:

```
Neighbors(nodeId) → List<(Node kề, EdgeId, Weight)>
```

Với đồ thị có hướng, chỉ trả về những cạnh mà `Source == nodeId` (cạnh đi ra). Với đồ thị vô hướng, trả về cả hai đầu — nếu cạnh có `Source == nodeId` hoặc `Target == nodeId` thì đều được tính. Điều này giúp mọi thuật toán viết theo cú pháp thống nhất mà không cần biết loại đồ thị.

### Sinh nhãn tự động

Khi không truyền label, phương thức `GenerateLabel(id)` chuyển id số nguyên thành chuỗi chữ cái theo kiểu Excel: 1→A, 2→B, ..., 26→Z, 27→AA, 28→AB. Thuật toán dùng phép chia lấy dư số 26 và xây chuỗi từ phải sang trái.

### Clone để bảo vệ đồ thị gốc

Trước khi chạy bất kỳ thuật toán nào, `MainForm` luôn gọi `graph.Clone()` để tạo bản sao sâu (deep copy). Điều này đảm bảo các bước animation không làm thay đổi trạng thái đồ thị đang hiển thị trên canvas.

---

## 3. Vẽ đồ thị — GraphCanvas (GDI+)

### Pipeline vẽ

Mỗi khi canvas cần cập nhật (do thêm đỉnh, kéo thả, hoặc animation bước mới), WinForms gọi `OnPaint`. Phương thức này làm theo thứ tự sau: đặt chế độ vẽ anti-aliased, vẽ nền chấm lưới (`DrawBackground`), áp dụng phép biến đổi affine zoom+pan (`TranslateTransform` + `ScaleTransform`), vẽ cạnh (`DrawEdges`), vẽ đỉnh (`DrawNodes`), sau đó reset transform và vẽ các gợi ý chế độ ở screen-space (`DrawModeHint`, `DrawZoomIndicator`).

Quan trọng: `DoubleBuffered = true` được đặt trong constructor để tránh nhấp nháy khi vẽ lại nhiều lần liên tiếp.

### Hệ tọa độ

Canvas có hai không gian tọa độ: **screen-space** (pixel trên màn hình) và **world-space** (tọa độ đồ thị). Khi người dùng click chuột, tọa độ screen được chuyển về world bằng công thức:

```
worldX = (screenX - panOffsetX) / zoom
worldY = (screenY - panOffsetY) / zoom
```

Khi vẽ, thư viện Graphics tự áp phép biến đổi ngược lại thông qua `TranslateTransform` và `ScaleTransform`.

### Màu sắc theo bước animation

Mỗi đỉnh và cạnh có màu mặc định. Khi `_currentStep` không null (đang có bước animation), hàm `GetNodeColor(id)` kiểm tra:

- Nếu id nằm trong `ActiveNodes` → màu đỏ (đang xét hiện tại)
- Nếu id nằm trong `QueueOrStack` → màu cam (đang chờ xử lý)
- Nếu id nằm trong `VisitedNodes` → màu xanh lá (đã xử lý xong)
- Còn lại → màu xanh dương mặc định

Tương tự cho cạnh: `HighlightEdges` → tím, `RejectedEdges` → đỏ nhạt, `ConsideredEdges` → vàng.

### Vẽ đỉnh

Mỗi đỉnh được vẽ 3 lớp chồng nhau: bóng đổ (ellipse đen trong suốt lệch 3px), gradient fill (sáng trên tối dưới), viền trắng. Nhãn đỉnh được vẽ chính giữa bằng font Segoe UI Bold. Nếu bước animation có `NodeLabels[id]` (ví dụ khoảng cách Dijkstra "dist=5"), chuỗi đó được vẽ ngay dưới đỉnh với nền trắng trong suốt.

### Vẽ cạnh

Với đồ thị có hướng, cạnh được vẽ bằng hàm `DrawingHelper.DrawArrow` — hàm này tính điểm đặt mũi tên dựa trên vector hướng của cạnh và bán kính đỉnh, đảm bảo mũi tên tiếp xúc đúng với viền đỉnh chứ không đâm vào trong. Với đồ thị vô hướng, vẽ đường thẳng đơn giản. Nhãn trọng số được đặt ở giữa cạnh, lệch vuông góc để không che cạnh.

### Zoom và Pan

`_zoom` (float) và `_panOffset` (PointF) lưu trạng thái zoom/pan. Cuộn bánh xe chuột tăng/giảm `_zoom` quanh điểm con trỏ (không phải góc canvas). Giữ chuột giữa kéo cập nhật `_panOffset`. Phím F gọi `FitToScreen()` — hàm này tính bounding box của tất cả đỉnh, sau đó tính tỉ lệ zoom vừa khít với canvas (có margin 50px).

---

## 4. AnimationEngine — Điều phối hoạt ảnh

### Cấu trúc AlgorithmStep

Mọi thuật toán đều trả về `List<AlgorithmStep>`. Mỗi `AlgorithmStep` là một snapshot trạng thái tại một bước:
- `Description`: chuỗi mô tả bằng tiếng Việt
- `StepType`: chuỗi định danh loại bước (ví dụ "init", "dequeue", "update_dist")
- `VisitedNodes`, `ActiveNodes`, `QueueOrStack`: HashSet của Node Id dùng để tô màu
- `HighlightEdges`, `RejectedEdges`, `ConsideredEdges`: HashSet của Edge Id
- `NodeLabels`: Dictionary<int, string> — nhãn phụ hiển thị dưới đỉnh
- `EdgeLabels`: Dictionary<int, string> — nhãn thay thế trên cạnh (dùng trong Ford-Fulkerson để hiện "flow/capacity")

### Cơ chế Timer

`AnimationEngine` có một `System.Windows.Forms.Timer` bên trong. Khi gọi `Play(speedMs)`, timer được khởi động với `Interval = speedMs`. Mỗi tick của timer gọi `Next()` — tăng `_currentIndex` và kích sự kiện `OnStepChanged`. Khi đến bước cuối, timer tự dừng và kích `OnFinished`.

`MainForm` lắng nghe `OnStepChanged` để: cập nhật label "Bước N/M", gọi `canvas.ApplyStep(step)` để đổi màu, thêm mô tả vào RichTextBox log. Người dùng cũng có thể tua thủ công bằng các nút |◀ ◀ ▶ ▶| hoặc phím ←/→/Home/End.

### Tốc độ phát

Thanh trượt tốc độ (1→10) được map sang milliseconds theo công thức: `ms = (int)(2000.0 / trackValue * 0.9 + 100)`. Giá trị 1 (chậm nhất) → ~1900ms/bước, giá trị 10 (nhanh nhất) → ~280ms/bước.

---

## 5. BFS — Duyệt chiều rộng

### Cấu trúc dữ liệu

BFS dùng `Queue<int>` (hàng đợi FIFO chuẩn của .NET), cùng với `HashSet<int> visited` để theo dõi đỉnh đã thăm và `HashSet<int> inQueue` cho biết đỉnh nào đang trong queue (dùng để tô màu cam trong animation).

### Luồng xử lý

Đầu tiên kiểm tra đỉnh xuất phát có tồn tại không. Sau đó đánh dấu đỉnh xuất phát là visited và đưa vào queue. Vòng lặp chính: lấy đỉnh ra khỏi queue (`Dequeue`), với mỗi láng giềng chưa thăm thì đánh dấu visited và đưa vào queue. Mỗi thao tác quan trọng (dequeue, xét láng giềng, thêm vào queue) đều tạo ra một `AlgorithmStep` với mô tả chi tiết và trạng thái màu sắc tại thời điểm đó.

### Xử lý đồ thị không liên thông

Sau khi vòng lặp kết thúc, thuật toán kiểm tra xem có đỉnh nào chưa được thăm không. Nếu có, bước kết quả cuối cùng thông báo: "đã thăm X/Y đỉnh, Z đỉnh không thể tới từ đỉnh bắt đầu".

---

## 6. DFS — Duyệt chiều sâu

### Triển khai đệ quy với bước animation

DFS được viết dưới dạng đệ quy (recursive), nhưng vì cần tạo `AlgorithmStep` cho từng hành động, hàm đệ quy nhận `steps` như tham số ref và thêm bước vào danh sách trong khi chạy. Trước khi gọi đệ quy vào đỉnh kề, tạo bước "đang đi sâu vào", và sau khi đệ quy quay về, tạo bước "quay lui từ". Điều này giúp animation thể hiện đúng cơ chế backtracking của DFS.

### Cấu trúc dữ liệu

Khác với BFS dùng Queue, DFS dùng `HashSet<int> visited` và call stack của ngôn ngữ làm stack ngầm. Trong animation, `QueueOrStack` hiển thị các đỉnh đang trên đường đệ quy chưa quay về.

---

## 7. Dijkstra — Đường đi ngắn nhất

### Priority Queue

.NET 6 trở lên có `PriorityQueue<TElement, TPriority>` (min-heap). Dijkstra sử dụng `PriorityQueue<int, double>` — `int` là Node Id, `double` là khoảng cách ưu tiên. Mỗi khi tìm được đường ngắn hơn đến một đỉnh, đỉnh đó được đưa vào queue lại với priority mới (có thể có nhiều entry cùng đỉnh trong queue).

### Xử lý entry lỗi thời

Vì không có API giảm priority, Dijkstra dùng kỹ thuật "lazy deletion": khi dequeue ra một đỉnh đã nằm trong `settled` (đã xử lý xong), bỏ qua nó và tiếp tục. Điều này được biểu diễn trong animation bằng bước "skip_settled" với mô tả giải thích.

### Bảng khoảng cách và nhãn phụ

`dist[]` Dictionary lưu khoảng cách ngắn nhất hiện tại từ nguồn đến mọi đỉnh (khởi tạo = ∞). Sau mỗi lần cập nhật, hàm `BuildDistLabels()` chuyển toàn bộ `dist[]` thành `NodeLabels` để hiển thị số dưới mỗi đỉnh trên canvas.

### Tái tạo đường đi

Khi thuật toán kết thúc, `prev[]` Dictionary lưu đỉnh liền trước trong đường đi ngắn nhất. Hàm tái tạo đường đi bằng cách truy ngược từ `endId` về `startId` theo chuỗi `prev[cur]`, thu thập các `edgeId` vào `pathEdges`. Canvas tô tím các cạnh này trong bước "reconstruct".

---

## 8. Kiểm tra Bipartite

### Thuật toán tô màu 2 màu

Bipartite checker dùng BFS để thử tô màu đồ thị bằng 2 màu (0 và 1) sao cho mỗi cạnh nối hai đỉnh khác màu. `color[]` Dictionary ánh xạ Node Id → màu (0 hoặc 1, hoặc -1 nếu chưa tô).

Với mỗi đỉnh chưa được tô, bắt đầu một BFS mới (để xử lý đồ thị không liên thông). Trong BFS, khi xét cạnh (u, v): nếu v chưa tô thì tô màu ngược với u; nếu v đã được tô cùng màu với u thì phát hiện xung đột — đồ thị không phải bipartite.

### Kết quả animation

Nếu là bipartite: các đỉnh màu 0 được tô `VisitedNodes` (xanh lá), màu 1 tô `QueueOrStack` (cam). Nếu không phải: hai đỉnh xung đột được tô `ActiveNodes` (đỏ) và bước cuối giải thích vì sao.

---

## 9. Prim — Cây khung nhỏ nhất

### Cấu trúc dữ liệu

Prim dùng `key[]` Dictionary lưu trọng số nhỏ nhất để kết nối mỗi đỉnh vào MST (khởi tạo = ∞), `parent[]` để tái tạo cây, `inMst` HashSet đánh dấu đỉnh đã vào MST, và `PriorityQueue<int, double>` tương tự Dijkstra.

### Luồng xử lý

Bắt đầu từ đỉnh nguồn với `key[source] = 0`. Mỗi vòng lặp: dequeue đỉnh u có key nhỏ nhất, thêm u vào MST, thêm cạnh (parent[u], u) vào tập cạnh MST. Sau đó xét mọi láng giềng v của u: nếu v chưa trong MST và trọng số cạnh (u,v) < key[v], cập nhật key[v] và parent[v], enqueue v lại với priority mới.

`NodeLabels` hiển thị giá trị `key[v]` dưới mỗi đỉnh (hiện tại là "key=3" hay "∞").

---

## 10. Kruskal — Cây khung nhỏ nhất

### Union-Find (Disjoint Set Union)

Kruskal cần cấu trúc Union-Find để kiểm tra chu trình. Được cài đặt trực tiếp trong class Kruskal với 2 mảng: `parent[]` và `rank[]`. Hàm `Find(x)` dùng path compression (tối ưu hóa: gán cha trực tiếp về root). Hàm `Union(x, y)` dùng union by rank (luôn nối cây nhỏ vào cây lớn hơn).

### Luồng xử lý

Sắp xếp tất cả cạnh theo trọng số tăng dần. Duyệt từng cạnh: nếu hai đầu cạnh thuộc 2 thành phần khác nhau (`Find(u) != Find(v)`) thì chọn cạnh này vào MST và `Union(u, v)`. Nếu cùng thành phần thì bỏ qua (sẽ tạo chu trình). Dừng khi đã có `n-1` cạnh trong MST.

Trong animation, cạnh đang xét dùng `ConsideredEdges` (vàng), cạnh được chọn dùng `HighlightEdges` (tím), cạnh bị từ chối dùng `RejectedEdges` (đỏ nhạt).

---

## 11. Ford-Fulkerson — Luồng cực đại

### Đồ thị thặng dư (Residual Graph)

Ford-Fulkerson không thay đổi `Graph` gốc mà xây dựng ma trận dung lượng thặng dư `residual[u,v]`. Khởi tạo: với mỗi cạnh (u,v) trọng số w, `residual[u,v] = w` và `residual[v,u] = 0` (dung lượng ngược ban đầu = 0). Sau mỗi lần tăng luồng: `residual[u,v] -= flow` và `residual[v,u] += flow`.

### Tìm đường tăng luồng bằng BFS (Edmonds-Karp)

Biến thể sử dụng BFS thay vì DFS để tìm đường tăng luồng (augmenting path). BFS trên đồ thị thặng dư tìm đường từ Source đến Sink theo chiều cạnh có `residual > 0`. Đường BFS đảm bảo lấy đường ngắn nhất (số cạnh ít nhất), giúp độ phức tạp đạt O(VE²).

### Cập nhật luồng và EdgeLabels

Sau khi tìm được đường, tính `bottleneck = min(residual[u,v])` trên đường đó. Cập nhật `residual` dọc đường. Sau đó chuyển ngược lại: với mỗi cạnh gốc (u,v), `actualFlow[edgeId] = capacity - residual[u,v]`. `EdgeLabels[edgeId]` được đặt thành `"flow/capacity"` để canvas hiển thị trên cạnh.

### Kết thúc

Lặp cho đến khi không còn đường từ Source đến Sink trong đồ thị thặng dư. Tổng luồng cực đại = tổng luồng chảy ra từ Source.

---

## 12. Fleury — Đường/Chu trình Euler

### Kiểm tra điều kiện Euler

Trước khi chạy, Fleury kiểm tra: đồ thị có liên thông không (DFS đơn giản), và số đỉnh bậc lẻ có thỏa điều kiện (0 cho chu trình Euler, 2 cho đường Euler). Nếu không thỏa, trả về bước lỗi giải thích.

### Phát hiện cầu (Bridge Detection)

Mỗi khi chọn cạnh tiếp theo, Fleury ưu tiên chọn cạnh **không phải cầu**. Để kiểm tra cạnh (u,v) có phải cầu không: tạm thời xóa cạnh đó khỏi đồ thị, chạy DFS từ u, nếu không đến được v thì cạnh là cầu (xóa nó làm đồ thị mất liên thông). Khôi phục cạnh sau khi kiểm tra.

Cách làm này có độ phức tạp O(E²) vì mỗi bước đi gọi DFS kiểm tra mỗi cạnh kề. Đây là điểm khác biệt so với Hierholzer.

### Vẽ đường đi

Danh sách `path` lưu thứ tự các đỉnh đã đi qua. Mỗi cạnh đã dùng được đưa vào `usedEdges` và `HighlightEdges` (tím). Cạnh đang được kiểm tra bridge đưa vào `ConsideredEdges` (vàng). Cạnh là cầu (bị bỏ qua) đưa vào `RejectedEdges` (đỏ nhạt).

---

## 13. Hierholzer — Chu trình Euler O(E)

### Thuật toán Stack

Hierholzer dùng một stack chính và danh sách kề có thể xóa cạnh đã dùng (dùng `LinkedList` hoặc `List` với index). Bắt đầu đẩy đỉnh nguồn vào stack. Trong khi stack không rỗng: nếu đỉnh trên đầu stack còn cạnh kề chưa dùng, chọn cạnh đó, đánh dấu dùng rồi, đẩy đỉnh kề vào stack; nếu không còn cạnh, pop đỉnh ra và thêm vào đầu `path`. Lặp cho đến khi stack rỗng.

Kết quả `path` chính là chu trình Euler hoặc đường Euler theo thứ tự duyệt. Mỗi lần push/pop là một bước animation.

### Tại sao O(E)

Mỗi cạnh được xét đúng 1 lần (đánh dấu và không dùng lại). Mỗi đỉnh được pop đúng 1 lần. Tổng thao tác = O(V + E) = O(E) cho đồ thị liên thông.

---

## 14. Chuyển đổi biểu diễn đồ thị (GraphConverter)

### Graph → 3 dạng

`ToAdjMatrix(graph)` xây bảng index ngược `nodeId → chỉ số ma trận`, sau đó duyệt `Edges` để điền `matrix[r,c] = weight`. Đồ thị vô hướng điền cả `matrix[c,r]`.

`ToAdjList(graph)` tạo Dictionary rỗng cho mọi đỉnh, sau đó duyệt `Edges` thêm láng giềng vào danh sách. Vô hướng thêm cả chiều ngược.

`ToEdgeList(graph)` đơn giản là map từng `Edge` thành tuple `(sourceLabel, targetLabel, weight)`.

### 3 dạng → Graph

`FromAdjMatrix` tạo đỉnh theo lưới tự động (N hàng × căn bậc hai N cột), sau đó duyệt ma trận, các ô khác 0 tạo cạnh. Đồ thị vô hướng chỉ xét nửa tam giác trên (c > r) để không thêm cạnh trùng.

`FromEdgeList` trước tiên thu thập tất cả nhãn đỉnh duy nhất từ danh sách cạnh, tạo đỉnh theo lưới, sau đó thêm cạnh. Dùng `HashSet<(int,int)> addedEdges` để tránh trùng khi vô hướng.

`FromAdjList` thu thập tất cả nhãn từ cả key lẫn value của dictionary, tạo đỉnh, rồi thêm cạnh.

### Text parsing cho Nhập nhanh

Hàm `ShowQuickImportDialog` trong MainForm đọc từng dòng của RichTextBox, split theo khoảng trắng/tab/dấu phẩy, lấy phần tử [0] và [1] làm Source/Target, phần tử [2] (nếu có) parse thành double làm trọng số. Dòng trống hoặc bắt đầu `#` bị bỏ qua. Kết quả là `List<(string, string, double)>` truyền vào `GraphConverter.FromEdgeList`.

---

## 15. Lưu và Mở file (GraphSerializer)

`GraphSerializer` dùng `System.Text.Json` để serialize/deserialize `Graph`. Đối tượng `Graph` được ánh xạ thành JSON với 3 trường: `directed` (bool), `nodes` (mảng `{id, label, x, y}`), `edges` (mảng `{id, source, target, weight}`).

Sau khi deserialize, gọi `graph.RestoreCountersFromData()` để tính lại `_nextNodeId` và `_nextEdgeId` từ max Id hiện có. Điều này đảm bảo khi thêm đỉnh/cạnh mới sau khi mở file, Id không bị trùng.

---

## 16. Undo/Redo (Lịch sử thao tác)

### Cơ chế snapshot

`MainForm` duy trì `List<string> _history` lưu chuỗi JSON của đồ thị sau mỗi thay đổi, và `_historyIndex` trỏ đến vị trí hiện tại. Hàm `PushHistory(graph)` gọi `GraphSerializer.SerializeToString(g)` để tạo snapshot và thêm vào lịch sử.

Khi `PushHistory` được gọi mà `_historyIndex` không ở cuối danh sách (tức là người dùng đã undo trước đó rồi làm thao tác mới), tất cả lịch sử phía sau bị xóa — đây là hành vi chuẩn của Undo/Redo.

### Khôi phục

`DoUndo` giảm `_historyIndex` và gọi `RestoreHistoryState`. Hàm này deserialize JSON → `Graph`, cập nhật canvas, combo đỉnh, và trạng thái nút Directed mà không trigger vòng lặp sự kiện (dùng flag `_suppressDirectedChange`).

---

## 17. Đổi tên đỉnh (Double-click)

Khi `OnMouseDoubleClick` được gọi ở chế độ Select, `HitTestNode` kiểm tra xem con trỏ có trúng đỉnh nào không (so sánh khoảng cách Euclidean với `NodeRadius`). Nếu trúng, `StartRename(node)` tạo một `TextBox` WinForms, đặt vị trí tại tọa độ screen của đỉnh (chuyển đổi world→screen bằng `node.Position * zoom + panOffset`), thêm vào `Controls` của canvas, và focus vào đó.

`TextBox` lắng nghe `KeyDown` — Enter gọi `CommitRename(cancel: false)`, Escape gọi `CommitRename(cancel: true)`. `LostFocus` cũng gọi `CommitRename(cancel: false)` để lưu khi click ra ngoài. Sau khi commit, TextBox bị xóa khỏi Controls và Dispose, canvas vẽ lại.

---

## 18. Export PNG

`ExportToBitmap(scaleFactor: 2)` tạo `Bitmap` có kích thước `canvas.Width*2 × canvas.Height*2`. Tạo `Graphics` từ bitmap, đặt `SmoothingMode = AntiAlias`, rồi gọi lại đúng pipeline vẽ (`DrawBackground`, `DrawEdges`, `DrawNodes`) với transform scale×2. Kết quả là ảnh pixel density cao hơn 2× màn hình, rõ nét khi xem ở kích thước thực.

---

*Tài liệu kỹ thuật — GraphApp v1.1 — 2026-06-10*

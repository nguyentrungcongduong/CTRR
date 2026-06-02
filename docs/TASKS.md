# Graph Application — Task List
> Checklist hoàn thành từng module theo thứ tự ưu tiên

---

## Legend
- 🔴 Blocking — phải xong trước khi làm task khác
- 🟡 Core — chức năng chính của đề bài
- 🟢 Advanced — phần nâng cao
- 🔵 Polish — UI/UX, không ảnh hưởng điểm core

---

## Phase 1 — Foundation (Làm trước tiên)

### TASK-01 🔴 Setup Solution
- [ ] Tạo Solution `GraphApp` gồm 2 project: `GraphApp.Core` (Class Library) + `GraphApp.UI` (WinForms)
- [ ] Add reference từ UI → Core
- [ ] Install `Newtonsoft.Json` hoặc dùng `System.Text.Json`
- [ ] Tạo đủ thư mục theo architecture

---

### TASK-02 🔴 Models
- [ ] Tạo `Node.cs` với properties: `Id`, `Label`, `Position`
- [ ] Tạo `Edge.cs` với properties: `Id`, `Source`, `Target`, `Weight`
- [ ] Tạo `Graph.cs`:
    - [ ] `AddNode(x, y, label?)` → trả về Id
    - [ ] `RemoveNode(id)` → xóa node + cạnh liên quan
    - [ ] `AddEdge(source, target, weight)` → trả về Id
    - [ ] `RemoveEdge(id)`
    - [ ] `Neighbors(nodeId)` → `List<(int Node, int EdgeId, double Weight)>`
    - [ ] `Clone()` deep copy
    - [ ] `Clear()`
    - [ ] Property `Directed` (bool)
- [ ] Tạo `AlgorithmStep.cs` với đầy đủ fields

---

### TASK-03 🔴 GraphCanvas — render tĩnh
- [ ] Tạo UserControl `GraphCanvas`
- [ ] `OnPaint`: vẽ tất cả edges (đường thẳng + label weight)
- [ ] `OnPaint`: vẽ tất cả nodes (hình tròn + label)
- [ ] Vẽ mũi tên trên edge khi `Graph.Directed == true`
- [ ] Method `ApplyStep(AlgorithmStep)` → đổi màu theo step → `Invalidate()`
- [ ] Method `Refresh(Graph)` → bind graph mới vào canvas

**Kiểm tra**: thêm vài node/edge bằng code, chạy app thấy vẽ được là xong.

---

### TASK-04 🔴 GraphCanvas — mouse interaction
- [ ] Enum `CanvasMode { Select, AddNode, AddEdge, Delete }`
- [ ] Toolbar buttons thay đổi `CanvasMode`
- [ ] `MouseDown` + `Mode=AddNode` → `Graph.AddNode()` → `Invalidate()`
- [ ] `MouseDown` + `Mode=AddEdge` → click node 1 (highlight), click node 2 → mở `EdgeWeightDialog` → `Graph.AddEdge()`
- [ ] `MouseDown` + `Mode=Delete` → detect click trúng node/edge → xóa
- [ ] `MouseDown` + `Mode=Select` → bắt đầu drag nếu click trúng node
- [ ] `MouseMove` → cập nhật `node.Position` khi đang drag → `Invalidate()`
- [ ] `MouseUp` → kết thúc drag
- [ ] Hit-test node: khoảng cách điểm click đến tâm < `NodeRadius`
- [ ] Hit-test edge: khoảng cách điểm click đến đoạn thẳng < 5px

**Kiểm tra**: vẽ đồ thị bằng chuột, kéo thả được node.

---

## Phase 2 — Core Features (Chức năng cơ bản)

### TASK-05 🟡 BFS
- [ ] `BFS.Run(Graph, startId)` → `List<AlgorithmStep>`
- [ ] Step types: `init`, `dequeue`, `visit_neighbor`, `already_visited`, `done`
- [ ] `Description` tiếng Việt rõ ràng ở mỗi step
- [ ] Điền đúng `VisitedNodes`, `ActiveNodes`, `QueueOrStack`, `HighlightEdges`

---

### TASK-06 🟡 DFS
- [ ] `DFS.Run(Graph, startId)` → `List<AlgorithmStep>` (dùng stack, không đệ quy)
- [ ] Step types tương tự BFS

---

### TASK-07 🟡 AnimationEngine
- [ ] `Load(List<AlgorithmStep>)` → reset về step 0
- [ ] `Next()`, `Prev()`
- [ ] `Play(speedMs)` dùng `System.Windows.Forms.Timer`
- [ ] `Pause()`
- [ ] Event/callback `OnStepChanged(step, index, total)` để UI cập nhật label
- [ ] Bind vào MainForm: label "Step 3/12", textbox description, buttons ◀ ▶ ▶▶ ⏸

**Kiểm tra**: chạy BFS, bấm Next/Prev thấy màu node thay đổi đúng.

---

### TASK-08 🟡 Dijkstra
- [ ] `Dijkstra.Run(Graph, startId, endId?)` → `List<AlgorithmStep>`
- [ ] Dùng `SortedSet` hoặc priority queue thủ công
- [ ] `NodeLabels` chứa distance hiện tại của mỗi node (hiển thị phụ trên canvas)
- [ ] Highlight path ngắn nhất ở step cuối (`HighlightEdges`)

---

### TASK-09 🟡 Bipartite Checker
- [ ] `BipartiteChecker.Run(Graph)` → `List<AlgorithmStep>`
- [ ] Dùng BFS 2-coloring
- [ ] `NodeLabels` chứa "Nhóm A" / "Nhóm B" của mỗi node
- [ ] Step cuối: trả về kết quả `true/false` + description

---

### TASK-10 🟡 GraphConverter
- [ ] `ToAdjMatrix(Graph)` → `(int[,] matrix, string[] labels)`
- [ ] `ToAdjList(Graph)` → `Dictionary<string, List<(string, double)>>`
- [ ] `ToEdgeList(Graph)` → `List<(string, string, double)>`
- [ ] `FromAdjMatrix(int[,], string[], bool directed)` → `Graph`
- [ ] `FromAdjList(...)` → `Graph`
- [ ] `FromEdgeList(...)` → `Graph`

---

### TASK-11 🟡 RepresentationPanel + InputMatrixForm
- [ ] `RepresentationPanel`: TabControl 3 tab (Adj Matrix / Adj List / Edge List)
    - [ ] Tab Adj Matrix: `DataGridView` tự động generate hàng/cột từ `ToAdjMatrix()`
    - [ ] Tab Adj List: `RichTextBox` format text
    - [ ] Tab Edge List: `ListView` 3 cột Source/Target/Weight
- [ ] Nút "Áp dụng" trên mỗi tab → parse ngược lại → `FromAdjMatrix/List/EdgeList()` → cập nhật canvas
- [ ] `InputMatrixForm`: form riêng để nhập ma trận từ đầu (số node, tên node, rồi nhập ma trận)
- [ ] Sync: khi graph thay đổi (thêm/xóa node/edge) → tự cập nhật RepresentationPanel

---

### TASK-12 🟡 Save / Load
- [ ] `GraphSerializer.Save(Graph, filePath)` → ghi JSON
- [ ] `GraphSerializer.Load(filePath)` → trả về `Graph`
- [ ] Nút Save → `SaveFileDialog` → ghi file `.graph.json`
- [ ] Nút Load → `OpenFileDialog` → load file → render lại canvas

---

### TASK-13 🟡 Toggle Directed / Undirected
- [ ] Checkbox "Đồ thị có hướng" trên toolbar
- [ ] Khi toggle → cập nhật `Graph.Directed` → vẽ lại canvas (có/không mũi tên)
- [ ] Cảnh báo nếu đang có edges: "Chuyển đổi sẽ giữ nguyên các cạnh hiện tại"

---

## Phase 3 — Advanced Algorithms

### TASK-14 🟢 Prim — MST
- [ ] `Prim.Run(Graph, startId)` → `List<AlgorithmStep>`
- [ ] Step types: `init`, `select_min_edge`, `add_to_mst`, `update_key`, `done`
- [ ] `NodeLabels` hiển thị key value hiện tại của mỗi node
- [ ] `HighlightEdges` chứa các cạnh MST đã chọn
- [ ] Chỉ chạy được trên đồ thị vô hướng — kiểm tra và báo lỗi nếu có hướng

---

### TASK-15 🟢 Kruskal — MST
- [ ] `Kruskal.Run(Graph)` → `List<AlgorithmStep>`
- [ ] Implement Union-Find (DisjointSet) trong `Kruskal.cs`
- [ ] Step types: `sort_done`, `consider_edge`, `add_to_mst`, `skip_edge` (tạo cycle), `done`
- [ ] Step `skip_edge`: tô cạnh màu đỏ nhạt (bị loại)
- [ ] Step `add_to_mst`: tô cạnh màu tím

---

### TASK-16 🟢 Ford-Fulkerson — Max Flow
- [ ] `FordFulkerson.Run(Graph, sourceId, sinkId)` → `List<AlgorithmStep>`
- [ ] Step types: `find_path`, `augment_flow`, `update_residual`, `no_path`, `done`
- [ ] `NodeLabels` hiển thị flow hiện tại trên mỗi cạnh: "flow/capacity"
- [ ] Vẽ **residual graph** song song hoặc toggle-able
- [ ] Chỉ chạy được trên đồ thị có hướng — kiểm tra và báo lỗi nếu vô hướng
- [ ] Hiển thị max flow value ở step cuối

---

### TASK-17 🟢 Fleury — Euler Path/Circuit
- [ ] `Fleury.Run(Graph, startId)` → `List<AlgorithmStep>`
- [ ] Kiểm tra điều kiện tồn tại Euler path/circuit trước khi chạy
- [ ] Implement bridge detection (Tarjan hoặc naive DFS)
- [ ] Step types: `choose_start`, `select_edge`, `check_bridge`, `move`, `done`
- [ ] Step `check_bridge`: highlight cạnh đang xét màu vàng
- [ ] Hiển thị Euler path/circuit dạng chuỗi "1 → 3 → 2 → 4 → 1" ở step cuối

---

### TASK-18 🟢 Hierholzer — Euler Circuit
- [ ] `Hierholzer.Run(Graph, startId)` → `List<AlgorithmStep>`
- [ ] Kiểm tra điều kiện Euler circuit
- [ ] Step types: `start_circuit`, `extend_path`, `merge_circuit`, `done`
- [ ] Highlight thứ tự cạnh được đi qua

---

## Phase 4 — Polish

### TASK-19 🔵 UI/UX hoàn thiện
- [ ] Toolbar đẹp: icon + tooltip cho từng nút
- [ ] Status bar: hiển thị "Nodes: 5 | Edges: 7 | Directed: Yes"
- [ ] Speed slider cho animation (Chậm → Nhanh)
- [ ] Combobox chọn thuật toán + combobox chọn node bắt đầu/kết thúc
- [ ] Disable/enable nút đúng lúc (không cho bấm Next khi chưa chọn thuật toán)

---

### TASK-20 🔵 Đồ thị mẫu
- [ ] Menu "File → Đồ thị mẫu" với ít nhất 3 đồ thị có sẵn:
    - [ ] Đồ thị vô hướng 6 đỉnh (test BFS/DFS/Prim/Kruskal)
    - [ ] Đồ thị có hướng có trọng số (test Dijkstra/Ford-Fulkerson)
    - [ ] Đồ thị 2 phía (test Bipartite)
    - [ ] Đồ thị Euler (test Fleury/Hierholzer)

---

### TASK-21 🔵 Zoom + Pan canvas
- [ ] Scroll wheel → zoom in/out (transform matrix)
- [ ] Middle-click drag → pan canvas
- [ ] Nút "Fit to screen" → auto-scale để thấy toàn bộ đồ thị

---

## Thứ tự làm theo tuần

| Tuần | Tasks | Mục tiêu kiểm tra |
|------|-------|-------------------|
| 1 | 01 → 02 → 03 → 04 | Vẽ và kéo thả đồ thị bằng chuột |
| 2 | 05 → 06 → 07 → 08 | BFS/DFS/Dijkstra có animation |
| 3 | 09 → 10 → 11 → 12 → 13 | Bipartite, chuyển đổi biểu diễn, save/load |
| 4 | 14 → 15 → 16 | Prim, Kruskal, Ford-Fulkerson |
| 5 | 17 → 18 → 19 → 20 | Fleury, Hierholzer, polish UI |

---

## Checklist nộp bài

- [ ] Tất cả 6 chức năng cơ bản chạy được (TASK 05–13)
- [ ] Ít nhất 3/5 thuật toán nâng cao (TASK 14–18)
- [ ] Có đồ thị mẫu để demo nhanh
- [ ] Save/Load hoạt động
- [ ] Không crash khi nhập sai input
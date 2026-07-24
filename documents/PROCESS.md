# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：

- Agent：Codex
- 模型：GPT-5 系列（目前介面未顯示精確 model slug）
- 專案設定：`training-repo/AGENTS.md`
- 專案工具：`code-reviewer`、`test-runner`、`fix-bug` skill

---

## 通用四問

### 1. 我的任務拆解

（開工前你把任務拆成哪幾步？實際做的時候順序有變嗎？為什麼變？）

- 原始拆解：
  1. 讀完專案與培訓文件。
  2. 建立 Codex 專案設定。
  3. 逐一重現並修復 3 個 bug。
  4. 新增低庫存頁面。
  5. 重構 `CreateOrderAsync`。
- 實際執行時先處理 Codex 設定，接著處理分頁 bug。
- 原本預計設定完成後立即 commit，但 Git 沒有 `user.name`／`user.email`，
  因此先完成分頁 bug 的重現與測試，再由使用者提供
  `IM0054 <im0054@oberps.com>` 後補做設定 commit。
- 本機原本的 `origin` 指向原作者；確認使用者已 fork 後，調整為：
  - `origin`：`https://github.com/IM0054/training.git`
  - `upstream`：`https://github.com/sox6769/training.git`

### 2. AI 幫上大忙的地方

（哪件事 agent 做得又快又好？**貼上當時的提問原文**，說明為什麼這樣問有效。）

- 提問原文：

  > 反正你知道要做什麼了 一步一步來 有什麼問題 才問我

- 這個指示讓 agent 可以依文件中的順序持續工作，只在 Git 身份、
  fork URL、UI 人工驗證等需要使用者決定的地方停下。
- Agent 自動把培訓要求拆成計畫，建立 `AGENTS.md`、rules、hooks、
  subagents 與 `fix-bug` skill，並實際驗證：
  - `git push --force` → `forbidden`
  - 一般 `git push` → `prompt`
  - `dotnet test` → `allow`
  - `TRUNCATE TABLE` → hook exit code 2
  - edit hook 成功記錄 `sample.txt`
- 處理分頁 bug 時，agent 先從 UI 抓出具體數據，再修改程式：
  - `/Orders?page=1`：20 筆
  - `/Orders?page=2`：20 筆
  - `/Orders?page=10`：0 筆
  - 分頁列仍顯示 10 頁
- 新增的回歸測試在修正前確實失敗：
  - 預期第一筆時間：`2026-07-24 12:00 UTC`
  - 實際第一筆時間：`2026-07-24 11:40 UTC`
  - 差距剛好 20 筆，直接證明第一頁多跳過一頁。

### 3. AI 誤導我的地方，與我如何發現

（agent 說錯／改錯／過度自信的時刻。你靠什麼抓到——對照程式碼？頁面實測？跑測試？）

- 一開始現有 28 個測試全部通過，但這不代表頁面沒有 bug。
  實際打開第 10 頁後仍是空白；原因是原測試只驗證
  `TotalCount`／`TotalPages`，沒有驗證各頁實際資料。
- 初次只把 offset 修成 `(page - 1) * pageSize` 後，`code-reviewer`
  指出只按 `CreatedAt` 排序不穩定：兩筆訂單時間相同時，SQL Server
  不保證固定順序，跨頁可能重複或漏資料。最後補上
  `ThenByDescending(o => o.Id)`，測試也加入相同時間的訂單。
- `test-runner` 曾因公司 NuGet feed 無法從 sandbox 連線而卡住，
  並留下鎖住 `obj` DLL 的 `dotnet` 子程序。透過錯誤訊息
  `NU1301`、程序啟動時間及 PID 確認後，只終止該 runner 的 orphan
  process，保留正在執行的 OrderHub 網站。
- 網站第一次無法連 DB 並不是程式商業邏輯錯誤：
  sandbox 身份無法使用 `Trusted_Connection`，先出現 encryption 錯誤，
  加上一次性 `Encrypt=False` 後進一步顯示 `Failed to generate SSPI
  context`。改用正常 Windows 身份啟動後，`/Orders` 回 HTTP 200 且讀到
  seed 資料。

### 4. 我會帶回日常工作的一招

（一個具體、可複製的做法，不要寫「要多驗證」這種口號——寫出**操作步驟**。）

- 修 bug 時使用以下固定流程：
  1. 先從 UI 記下具體輸入與數字。
  2. 追 Controller → Service → Repository。
  3. 修改 production code 前，先寫一個會重現問題的測試。
  4. 確認測試在舊程式上失敗。
  5. 做最小修正並跑單一測試。
  6. 用獨立 reviewer 檢查邊界與測試盲點。
  7. 跑全部測試。
  8. 重啟網站，回到原頁面驗證。
  9. 一個 bug 一個 commit，不混入其他重構。

---

## 執行紀錄

### 練習 1 — Codex 專案設定

- 建立 `training-repo/AGENTS.md`。
- 建立 `.codex/rules/orderhub.rules`。
- 建立 SQL 防護及 edit audit hooks。
- 建立 `code-reviewer`、`test-runner`。
- 建立 `.agents/skills/fix-bug/SKILL.md`。
- 完整測試基線：28 passed、0 failed。
- Commit：`506bb3a chore: configure Codex guardrails and bug-fix workflow`

### 練習 2 — Bug 1：訂單分頁

- 症狀已在 UI 重現：第 10 頁顯示 0 筆。
- 根因：頁碼從 1 開始，但 repository 使用
  `Skip(page * pageSize)`，第一頁先跳過 20 筆。
- 修正：
  - offset 改為 `Skip((page - 1) * pageSize)`。
  - 增加 `ThenByDescending(o => o.Id)`，確保同時間訂單排序穩定。
- 回歸測試：`GetOrders_UsesOneBasedPageOffsets`。
- 修正後完整測試：29 passed、0 failed。
- 修正後 HTTP 驗證：
  - 第 1 頁：20 筆
  - 第 10 頁：20 筆
- 使用者已在瀏覽器確認第 10 頁不再空白。
- Code review：無剩餘 finding。
- 狀態：驗證完成，建立 Bug 1 獨立 commit。

### 練習 2 — Bug 2：Gold 會員重複折扣

- UI 重現資料：
  - 商品：`SKU-1001 極光 無線滑鼠`
  - 原價：NT$1,420.00
  - Gold 重現訂單：`#201`
  - 正確 9 折：NT$1,278.00
  - 修正前頁面總額：NT$1,150.20
  - Silver 對照訂單：`#202`
  - Silver 正確及實際總額皆為 NT$1,349.00
- 根因：Gold 建單時先把 `UnitPriceSnapshot` 折成 NT$1,278.00，
  `CalculateTotal` 又在 subtotal 上套用一次 10% 折扣；Silver 的 snapshot
  保留原價，所以沒有重複折扣。
- 修正：所有會員的 `UnitPriceSnapshot` 都保存商品原價，會員折扣只在
  `OrderService.CalculateTotal` 套用一次。
- 回歸測試：
  `CreateOrder_GoldCustomer_AppliesDiscountOnce`。
- 修正前測試失敗：
  - 預期 snapshot：1,420
  - 實際 snapshot：1,278
- 修正後完整測試：30 passed、0 failed。
- Code review：無 finding；Standard、Silver 行為不變。
- 修正後 UI 驗證訂單：`#203`
  - snapshot：NT$1,420.00
  - 折扣：NT$142.00
  - 應付總額：NT$1,278.00
  - 舊錯誤總額 NT$1,150.20 已消失。
- 使用者已在瀏覽器確認訂單 `#203` 金額正確。
- 狀態：驗證完成，建立 Bug 2 獨立 commit。

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1

1. 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責
2. 我核對過 agent 描述的建單流程，且**至少找出一處不精確或過度簡化的說法**
3. 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方

練習 2

1. 三個 bug 我都先在頁面上重現過，才開始找程式
2. 我給 agent 的資訊包含具體觀察（頁碼／金額數字／庫存數字），而不是只貼客訴原文
3. 每個修復都回到頁面驗證過症狀消失
4. 每個 bug 都補了一個回歸測試，`dotnet test` 全綠
5. 三個獨立 commit，message 說明症狀與根因
6. （思考題）為什麼原本的測試沒抓到這三個 bug？

練習 3

1. `/Products/LowStock` 不帶參數 → 門檻 10 的結果；帶 `?threshold=3` → 結果隨之改變
2. `?threshold=0`、`?threshold=-1` → 頁面顯示驗證錯誤，不是 500
3. 售出數量欄位排除了 Cancelled 訂單（可用一筆已取消的訂單驗證）
4. 停售（已停售 badge）商品不出現在列表
5. 程式分層與命名跟既有的 Products 功能一致（請 agent 自我 review 一次，並自己確認）
6. 至少 3 個新測試，`dotnet test` 全綠

練習 4

1. 重構後 `dotnet test` 全綠
2. 我能說出這次重構「改善了什麼、沒有改變什麼」
3. 我有在 code review 的角度看過 diff（不是 agent 說好就好）

---

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）

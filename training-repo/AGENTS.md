# OrderHub — 專案指引

## 專案

OrderHub 是一個小型的內部訂單管理培訓應用程式。解決方案的規模應符合
單一 ASP.NET Core 應用程式搭配一個 SQL Server 資料庫的需求；不要引入
分散式系統或多租戶架構。

## 技術堆疊

- .NET 8、ASP.NET Core MVC、Razor Views、Bootstrap 5
- EF Core 8 與 SQL Server
- 使用 xUnit 搭配 EF Core InMemory 進行測試

## 架構與慣例

- `OrderHub.Web` 負責 Controller、ViewModel 與 Razor View。
- `OrderHub.Core` 負責領域模型、Service 介面與商業規則。
- `OrderHub.Infrastructure` 負責 EF Core、Repositories、Migrations 與種子資料。
- Controller 應保持精簡，商業規則應放在 Core Service。
- 只有 Repository 可以存取 `OrderHubDbContext`；Controller 與 Service
  不得直接使用 EF Core。
- View 必須綁定 ViewModel，不得直接綁定領域模型。
- 預期內的失敗應使用 `ServiceResult<T>` 表示，不要使用例外。
- 使用 DataAnnotations 與 ModelState 驗證使用者輸入；無效輸入不得變成
  HTTP 500 回應。
- 金額使用 `decimal`。會員折扣只能在
  `OrderService.CalculateTotal` 套用一次。
- 使用 `TempData["Success"]` 與 `TempData["Error"]` 提供操作結果訊息。
- 命名與結構請遵循 `ProductsController.cs` 和 `ProductService.cs`。

## 命令

- 建置：`dotnet build`
- 測試：`dotnet test`
- 執行：`dotnet run --project src/OrderHub.Web`

## 審查與驗證

- 修改程式碼前先重現回報的 bug，並記錄具體觀察結果。
- 只修正最小且相關的範圍；不要在修復中混入無關的重構。
- 每個 bug 都要加入回歸測試。
- 修改程式碼後，若可使用委派功能，先呼叫專案的 `code-reviewer`
  agent，再使用 `test-runner` 執行完整測試套件。
- 回報所有異動檔案與驗證結果。
- 使用者完成必要的瀏覽器驗證前，不得 commit。

## 敏感與自動產生的檔案

- 不得手動修改 `src/OrderHub.Infrastructure/Migrations/**`。
- 修改連線字串或 `appsettings*.json` 前必須先詢問。
- 不得讀取或寫入 `*.pfx`、`appsettings.Production.json` 或 User Secrets。

## 禁止事項

- 未經明確核准，不得新增 NuGet 套件。
- 不得使用 `git reset --hard`、force-push 或破壞性的遞迴刪除。
- 未經明確核准，不得刪除資料庫或執行破壞性 SQL。
- 不得只為了掩蓋正式程式碼的缺陷而修改測試。
- 不得重構與目前任務無關的程式碼。

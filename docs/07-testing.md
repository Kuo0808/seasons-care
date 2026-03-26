# 測試規範

## 目的

本文件定義 `seasons-care` 專案的人類開發者與 AI Agent 最低測試流程。

只要是有意義的後端變更，都必須留下可驗證的結果。

## 必要測試層級

每個新模組，優先補以下四層測試：

1. Validator tests
2. Service tests
3. Controller integration tests
4. Real database integration tests

Auth onboarding changes must include an end-to-end real database integration test for:

1. `POST /api/auth/register`
2. `PATCH /api/users/me`
3. `POST /api/auth/login`

## 測試資料夾結構

所有測試程式碼都必須放在 `Tests/` 下。

建議結構如下：

```text
Tests/
  Shared/
  Validations/
    <Module>/
  Services/
    <Module>/
  Integration/
    <Module>/
```

## 共用測試模板

可重用的測試基礎設施放在：

- `Tests/Shared/TestUsers.cs`
- `Tests/Shared/SeedDataHelper.cs`
- `Tests/Shared/JsonResponseHelper.cs`
- `Tests/Shared/Http/TestAuthHandler.cs`
- `Tests/Shared/Http/StubApiFactory.cs`
- `Tests/Shared/Http/RealApiFactory.cs`

建立新模組時，應優先重用這些 helper，不要重複建立新的測試基礎設施。

## 新模組最低覆蓋要求

對於 `CareLogs`、`Expenses`、`Tasks` 這類 CRUD 型模組，最低應包含：

1. 驗證失敗回 `400`
2. 非成員或無權限存取回 `403`
3. 跨 `careGroupId` 存取必須正確隔離
4. Create 會正確寫入資料
5. Update 會正確更新資料
6. Delete 符合預期刪除行為

若模組有 optimistic concurrency，必須再補 `409 Conflict` 測試。

## 完成定義

任務不算完成，除非：

1. 相關測試已新增或更新
2. 已執行測試命令
3. 已清楚回報測試結果

如果無法執行測試，必須明確說明原因。

## 標準測試命令

優先使用共用 PowerShell 腳本：

```powershell
.\bin\test.ps1
```

等價的直接命令為：

```powershell
dotnet test Tests\SeasonsCare.Api.Tests.csproj /p:UseAppHost=false /p:OutDir=.\bin\test-verify\
```

## AI Agent 規則

任何 AI Agent 只要有修改程式碼，且變更可能影響 build、執行行為或 API 行為，就應在結案前執行標準測試命令。

最低要求：

1. 依變更內容補上或更新測試
2. 執行 `.\bin\test.ps1`
3. 在最後回報中說明測試是否通過

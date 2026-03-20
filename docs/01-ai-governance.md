# AI Governance

本文件定義 AI Agent 在 seasons-care 專案中的協作規範。

## Stability Rule

AI 不得擅自修改以下內容：

- 現有 API 路由命名
- 現有 DTO 欄位命名
- 現有資料表欄位命名
- 現有資料夾架構

## Architecture Protection

未經允許，AI 不得自行引入以下架構：

- CQRS
- DDD
- Microservices
- Event Sourcing

## Safe Write Rule

AI 生成的分析建議僅為 Proposed Actions。

任何資料寫入、刪除、更新，必須由使用者明確要求後才能執行。

## Code Change Principle

AI 產生新程式碼時，必須優先遵守：

1. 現有架構
2. 現有命名規範
3. 現有 API 合約
4. 現有資料庫規範

## Testing Execution Rule

AI Agent 只要有修改程式碼、測試、驗證規則、資料存取邏輯或 API 行為，就必須參考 `docs/07-testing.md`。

在可執行的情況下，AI Agent 完工前必須執行：

```powershell
.\bin\test.ps1
```

AI Agent 在最後回報時，必須明確說明：

1. 是否已執行測試
2. 測試是否通過
3. 若未執行，原因是什麼

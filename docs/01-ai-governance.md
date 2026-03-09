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
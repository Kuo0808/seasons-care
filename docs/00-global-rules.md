# Global Rules — seasons-care

本文件為 seasons-care 專案的總入口規範。

所有 AI Agent、開發者與自動化工具，必須優先遵守 `/docs` 內的規範文件。

## Docs Index

- `01-ai-governance.md`：AI 協作憲法
- `02-api-standards.md`：API 設計規範與回應格式
- `03-architecture.md`：系統架構與資料夾結構
- `04-database-standards.md`：資料庫設計規範
- `05-security-rules.md`：安全性規範
- `06-project-domain.md`：專案核心業務概念
- `07-testing.md`：測試規範與完成前檢查流程
- `08-health-records.md`：健康數據模組規範

## Global Language Rules

- 所有註解、文件、API 說明必須使用繁體中文。
- 所有程式碼命名、類別名稱、方法名稱、資料表欄位、API 路由必須使用英文。
- 嚴禁使用簡體中文。
- JSON 欄位名稱統一使用 camelCase。
## Handoff Rules

- `handoff/` 專門存放臨時協作文檔
- 臨時文件不放在 `docs/`
- `handoff/` 已加入 `.gitignore`，不會進入 GitHub

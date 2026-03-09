# Architecture

## Technology Stack

- ASP.NET Core Web API
- C#
- Entity Framework Core
- PostgreSQL

## Architecture Pattern

採用 Strict Layered Architecture：

Controller
→ Service
→ Repository
→ Database

## Layer Responsibilities

### Controller Layer

只負責：

- HTTP Request 接收
- DTO 驗證
- 呼叫 Service
- Swagger 文件

禁止：

- 商業邏輯
- 權限判斷
- 資料庫操作

### Service Layer

負責：

- Business Logic
- 權限檢查
- 資料驗證
- 流程控制

### Repository Layer

負責：

- 資料存取
- Entity Query
- EF Core 操作

必須實作：

- Global Query Filters
- Soft Delete 過濾
- Multi-tenant 過濾

## Folder Structure

```text
/controllers
/services
/repositories
/models
  /entities
  /enums
/dtos
/validations
/middleware
/config
所有 Request/Response 必須使用 DTO，命名後綴為 Request 或 Response（例如 CreateCareLogRequest）
# Database Standards

## Database Engine

- PostgreSQL

## Primary Key Rule

所有資料表 Primary Key 必須使用：

- UUID (Guid)

## Audit Columns

所有資料表必須包含：

- `created_at`
- `updated_at`
- `created_by`

## Soft Delete

重要資料禁止物理刪除。

必須使用：

- `deleted_at`

## Concurrency

更新操作必須使用以下其中一種方式實作樂觀鎖：

- `updated_at`
- `row_version`

## Multi-Tenancy

所有與照護資料相關的表，必須包含：

- `care_group_id`

所有查詢必須進行 care group 隔離。
所有時間欄位統一使用 UTC 存儲，回傳 ISO 8601 格式
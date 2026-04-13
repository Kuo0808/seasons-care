# Database Standards

## Database Engine

- PostgreSQL

## Primary Key Rule

所有主要資料表的 Primary Key 一律使用：

- UUID (`Guid`)

## Audit Columns

所有需要追蹤建立與更新資訊的資料表，至少包含：

- `created_at`
- `updated_at`
- `created_by`

## Soft Delete

需要支援軟刪除的資料表，使用：

- `deleted_at`

## Concurrency

需要處理並發更新的資料表，可使用以下欄位之一：

- `updated_at`
- `row_version`

## Multi-Tenancy

所有屬於照護群組的資料，必須包含：

- `care_group_id`

## Time Storage Rule

資料庫中的時間欄位一律以 UTC 儲存。

- 建立、更新、刪除等審計欄位，統一寫入 UTC。
- 查詢若需要依台灣本地日期切分，先在應用層換算出台灣日界線，再轉回 UTC 查詢。
- 回傳前端時，如畫面需要台灣時間顯示，再由 API response 或前端做時區轉換。

## Time Helper Rule

專案中的時間工具規則如下：

- `TimeHelper.UtcNow`：資料庫寫入、審計欄位、系統內部比較使用。
- `TimeHelper.TaiwanNow`：畫面顯示或依台灣在地時間判斷日期時使用。
- `TimeHelper.GetTaiwanDateStartUtc(...)`：需要依台灣日期做查詢區間切分時使用。

不要直接把台灣本地時間寫進 PostgreSQL 的 `timestamp with time zone` 欄位。

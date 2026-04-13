# Timezone Handling

## Goal

本專案統一採用以下原則：

- 資料庫存 UTC
- 台灣時間只用在顯示與台灣本地日期區間計算

這樣可以避免：

- PostgreSQL `timestamp with time zone` 因寫入 Local/Unspecified `DateTime` 而出錯
- 雲端與本機時區不同造成的查詢偏移
- Azure Windows / Linux 對時區 ID 支援不同導致執行時例外

## Why 500 Can Happen

如果把台灣本地時間直接寫進 `timestamp with time zone` 欄位，Npgsql 可能拋出例外並造成 API `500`。

常見風險包含：

- 直接用台灣時間寫入 `created_at`、`updated_at`、`record_date`
- 用 `Asia/Taipei` 當唯一時區 ID，在部分 Windows 環境找不到
- 同一支 API 有些地方用 UTC、有些地方用台灣時間，導致比較與篩選條件不一致

## Project Rule

### 1. Database Write

所有寫入資料庫的時間，一律使用 UTC：

- `TimeHelper.UtcNow`
- 或已經標準化為 UTC 的 `DateTime`

### 2. Display

只有在需要顯示給使用者時，才轉成台灣時間：

- `TimeHelper.TaiwanNow`
- `TimeHelper.ToTaiwanTime(...)`

### 3. Taiwan Date Boundary

若業務規則是「今天」、「本週」、「近 7 天」等以台灣本地日期為準：

1. 先算出台灣時間的日期起點
2. 再轉成 UTC
3. 用這個 UTC 區間查資料庫

請使用：

- `TimeHelper.GetTaiwanDateStartUtc(...)`

## Azure Compatibility

時區 ID 需要做跨平台 fallback：

- Linux 常用：`Asia/Taipei`
- Windows 常用：`Taipei Standard Time`

`TimeHelper` 應同時支援這兩種寫法，避免部署到不同環境時直接拋錯。

## Checklist

調整時間邏輯時，請一起檢查：

- 是否把本地時間直接寫入資料庫
- 是否仍以 UTC 存審計欄位
- 是否有依台灣日界線查詢的 API
- 是否在 DTO / 前端顯示層才做台灣時間轉換
- 是否有 `ToString("O")` 這類字串比較，因時區變化導致比對異常

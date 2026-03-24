# Health Records 模組規範

## 目的

本文件定義 `HealthRecords` 模組的資料夾分層、路由命名與後續擴充規則。

## 模組位置

健康數據屬於同一領域，統一放在 `HealthRecords` 底下：

```text
Controllers/HealthRecords/
DTOs/HealthRecords/
Models/Entities/HealthRecords/
Repositories/HealthRecords/
Services/HealthRecords/
Validations/HealthRecords/
Tests/Integration/HealthRecords/
```

## 路由規則

所有健康數據 API 都必須掛在：

`/api/care-groups/{careGroupId}/health-records/<resource>`

目前已使用：

- `/api/care-groups/{careGroupId}/health-records/blood-sugars`
- `/api/care-groups/{careGroupId}/health-records/blood-pressures`

未來若新增心率、血氧、體溫，也應沿用相同規則。

## 命名規則

- Resource 名稱使用英文複數，例如 `blood-sugars`
- DTO 依 resource 分資料夾，例如 `DTOs/HealthRecords/BloodSugars`
- Service / Repository 介面與實作使用單數領域名，例如 `IBloodSugarService`
- Entity 使用 `Record` 結尾，例如 `BloodSugarRecord`
- `CreatedBy`、`UpdatedAt`、`DeletedAt` 等 audit 欄位必須與既有模組保持一致

## 測試最低要求

每個健康數據子模組至少應包含：

1. Validator tests
2. Service tests
3. Controller integration tests
4. Tenant isolation integration tests
5. Create / Update / Delete 行為測試
6. `409 Conflict` 測試

## API 說明

### Blood Sugars

- `GET /api/care-groups/{careGroupId}/health-records/blood-sugars`
- `GET /api/care-groups/{careGroupId}/health-records/blood-sugars/{recordId}`
- `POST /api/care-groups/{careGroupId}/health-records/blood-sugars`
- `PUT /api/care-groups/{careGroupId}/health-records/blood-sugars/{recordId}`
- `DELETE /api/care-groups/{careGroupId}/health-records/blood-sugars/{recordId}`

建立與更新欄位：

- `glucoseLevel`：血糖值，單位預設為 `mg/dL`
- `measurementContext`：量測情境，建議使用固定值，例如 `飯前`、`飯後`、`睡前`
- `notes`：備註
- `recordDate`：量測時間，可省略，省略時由後端使用目前 UTC 時間

更新時必填：

- `updatedAt`：前端必須送出上次讀取到的 `updatedAt`，後端會進行 optimistic concurrency 檢查；若不一致，回 `409 Conflict`

### Blood Pressures

- `GET /api/care-groups/{careGroupId}/health-records/blood-pressures`
- `GET /api/care-groups/{careGroupId}/health-records/blood-pressures/{recordId}`
- `POST /api/care-groups/{careGroupId}/health-records/blood-pressures`
- `PUT /api/care-groups/{careGroupId}/health-records/blood-pressures/{recordId}`
- `DELETE /api/care-groups/{careGroupId}/health-records/blood-pressures/{recordId}`

建立與更新欄位：

- `systolic`：收縮壓
- `diastolic`：舒張壓
- `notes`：備註
- `recordDate`：量測時間，可省略，省略時由後端使用目前 UTC 時間

更新時必填：

- `updatedAt`：前端必須送出上次讀取到的 `updatedAt`，後端會進行 optimistic concurrency 檢查；若不一致，回 `409 Conflict`

## 前端串接注意事項

- `GET list` 與 `GET by id` 回傳的 `updatedAt` 必須由前端保存
- `PUT` 更新時不可自行產生新的 `updatedAt`
- `DELETE` 為 soft delete，列表查詢不應再看到已刪除資料
- 若後續加入語音輸入，建議語音解析後仍先回到前端確認，再送出正式 `POST` 或 `PUT`

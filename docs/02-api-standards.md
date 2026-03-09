# API Standards

## Base Prefix

所有 API 必須使用：

`/api`

## RESTful Rules

API 路由必須採用 Resource-based design。

正確範例：

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/care-groups`
- `GET /api/care-groups/{id}`
- `GET /api/care-groups/{careGroupId}/care-logs`

禁止使用函式式命名：

- `RegisterUser`
- `GetCareLogs`
- `CreateCareGroup`

## Care Group Scope Rule

所有照護相關 API 必須包含 `{careGroupId}`。

範例：

- `/api/care-groups/{careGroupId}/care-logs`
- `/api/care-groups/{careGroupId}/health-records`

## Pagination

所有列表 API 必須支援：

- `page`
- `pageSize`
- `sort`

預設排序：

- `createdAt_desc`

## Success Response Format

```json
{
  "success": true,
  "message": "",
  "data": {},
  "traceId": "GUID"
}

列表 API 可額外包含：

{
  "pagination": {
    "totalCount": 0,
    "totalPages": 0,
    "currentPage": 1,
    "pageSize": 20
  }
}

Error Response Format

錯誤回應採用 Problem Details 結構：

{
  "type": "https://api.seasons-care.com/errors/validation-failed",
  "title": "Validation failed",
  "status": 400,
  "detail": "資料驗證失敗",
  "errorCode": "VALIDATION_FAILED",
  "traceId": "GUID"
}


HTTP Status Rules

200 OK：查詢成功

201 Created：建立成功

400 Bad Request：請求資料錯誤

401 Unauthorized：未登入

403 Forbidden：權限不足

404 Not Found：找不到資源

409 Conflict：資料衝突

500 Internal Server Error：系統錯誤
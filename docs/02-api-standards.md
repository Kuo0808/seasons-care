# API Standards

## Base Prefix

All API routes must start with:

`/api`

## RESTful Rules

Use resource-based route design.

Preferred examples:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `PATCH /api/users/me`
- `GET /api/care-groups`
- `GET /api/care-groups/{id}`
- `GET /api/care-groups/{careGroupId}/care-logs`

Avoid function-style route names such as:

- `RegisterUser`
- `GetCareLogs`
- `CreateCareGroup`
- `CompleteProfile`

## Auth Flow Rules

- `POST /api/auth/register` is the first-step account creation endpoint.
- `POST /api/auth/login` is the login endpoint.
- `PATCH /api/users/me` is the endpoint for updating the current user's profile data such as `userName` and `avatarKey`.
- Auth responses may include `isProfileCompleted` so the frontend can decide whether onboarding is finished.

## Care Group Scope Rule

All care-related APIs must include `{careGroupId}` in the route.

Examples:

- `/api/care-groups/{careGroupId}/care-logs`
- `/api/care-groups/{careGroupId}/health-records`

## Pagination

List APIs should support:

- `page`
- `pageSize`
- `sort`
- `startDate`
- `endDate`

Default sort example:

- `createdAt_desc`

## Date Range Filter

For cumulative list resources such as care logs, expenses, and health records:

- `startDate`: inclusive start date of the business timeline
- `endDate`: inclusive end date of the business timeline
- when both dates are omitted, backend should return the most recent 30 days by default
- business date should be used instead of `createdAt`

Examples:

- care logs use `startsAt`
- expenses use `expenseDate`
- health records use `recordDate`

## Success Response Format

```json
{
  "success": true,
  "message": "",
  "data": {},
  "traceId": "GUID"
}
```

Paged APIs should additionally include:

```json
{
  "pagination": {
    "totalCount": 0,
    "totalPages": 0,
    "currentPage": 1,
    "pageSize": 20
  }
}
```

## Error Response Format

Use Problem Details style responses:

```json
{
  "type": "https://api.seasons-care.com/errors/validation-failed",
  "title": "Validation failed",
  "status": 400,
  "detail": "Validation failed",
  "errorCode": "VALIDATION_FAILED",
  "traceId": "GUID"
}
```

## HTTP Status Rules

- `200 OK`: successful read or update
- `201 Created`: successful create
- `400 Bad Request`: invalid request data
- `401 Unauthorized`: authentication required or failed
- `403 Forbidden`: authenticated but not allowed
- `404 Not Found`: resource does not exist
- `409 Conflict`: duplicate or concurrency conflict
- `500 Internal Server Error`: unexpected server failure

# Security Rules

## Secrets Management

以下資料禁止寫死在程式碼中：

- API Keys
- JWT Secret
- Database Password
- Third-party credentials

必須使用 Environment Variables 或 Secret Manager。

## Password Rule

密碼不得明碼儲存。

必須使用：

- BCrypt
- 或 Argon2

## Email Rule

Email 存入資料庫前必須統一轉為 lowercase。

## CORS Rule

必須明確設定 Allowed Origins。

禁止使用：

- `AllowAnyOrigin()`

## Response Rule

API Response 不可回傳：

- password
- password hash
- sensitive secrets
# Deployment

## Target Environment

- Backend: Azure App Service
- Database: Azure Database for PostgreSQL Flexible Server
- Runtime: .NET 8

## Important Notes

- This project is an API-only service.
- The root path `/` does not indicate deployment success.
- Use `/swagger` or a real `/api/...` route to verify the app is running.
- Production secrets must be provided through Azure App Settings.
- Do not treat `publish/` or `publish.zip` as source-of-truth configuration.

## Required Azure App Settings

- `ASPNETCORE_ENVIRONMENT=Production`
- `Swagger__Enabled=true`
- `ConnectionStrings__DefaultConnection=<Azure PostgreSQL connection string>`
- `Jwt__Issuer=SeasonsCareApi`
- `Jwt__Audience=SeasonsCareClient`
- `Jwt__SecretKey=<real secret, 32+ chars>`

## Database Connection

- The production database is Azure Database for PostgreSQL Flexible Server.
- Do not deploy with `localhost` connection strings.
- Before first use, run EF Core migrations against the Azure database.

## Deployment Verification

1. Open `/swagger` and confirm HTTP 200.
2. Test `POST /api/auth/register`.
3. Confirm the response returns a token and `isProfileCompleted=false`.
4. Test `PATCH /api/users/me` with the token from register or login.
5. Confirm the response returns `userName`, `avatarKey`, and `isProfileCompleted=true`.
6. Test `POST /api/auth/login`.
7. Confirm the app can read and write to the Azure PostgreSQL database.

## GitHub Actions Auto Deploy

- Workflow file: `.github/workflows/deploy-azure-webapp.yml`
- Trigger: push to `main` or manual run from GitHub Actions
- Required GitHub secret: `AZURE_WEBAPP_PUBLISH_PROFILE`
- Required GitHub secret: `AZURE_DATABASE_CONNECTION_STRING`
- Required GitHub secret: `AZURE_JWT_SECRET_KEY`
- The workflow restores, builds, applies EF Core migrations to Azure PostgreSQL, then publishes and deploys the ASP.NET Core app to Azure App Service.

## Repository Rules

- Do not commit real secrets.
- Do not commit generated publish output.
- Keep example configuration in `appsettings.json.example` and `appsettings.Production.json.example`.
- If Azure settings change, update this document and the example files together.

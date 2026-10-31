# EduWallet Backend

The EduWallet backend is an ASP.NET Core Web API for student budgeting and financial wellness. It provides JWT-authenticated endpoints for accounts, budgets, transactions, insights, reports, onboarding, and administration.

## Technology

- .NET 10
- ASP.NET Core controllers and Swagger / OpenAPI
- Entity Framework Core 10 with SQL Server
- ASP.NET Core Identity and JWT bearer authentication
- PDF parsing with PdfPig

## Requirements

- .NET 10 SDK
- Access to a SQL Server database
- Resend API credentials if password-reset email delivery is needed

## Configure

The API reads configuration from `appsettings.json`, environment variables, and ASP.NET Core user-secrets. Keep database passwords, JWT signing keys, email API keys, and admin credentials out of source control.

From the project directory (`EduWalletApp`), configure development secrets:

```shell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your SQL Server connection string>"
dotnet user-secrets set "Jwt:Key" "<a private signing key with at least 32 characters>"
```

The application requires `Jwt:Key` to be set and at least 32 characters long. `Jwt:Issuer` and `Jwt:Audience` default to `EduWalletApp` in `appsettings.json`; configure these values consistently in environments that override them.

To enable password-reset email delivery, configure Resend:

```shell
dotnet user-secrets set "Resend:ApiKey" "<resend-api-key>"
dotnet user-secrets set "Resend:FromEmail" "<verified-sender-address>"
dotnet user-secrets set "Frontend:ResetPasswordUrl" "http://localhost:5173/reset-password"
```

An initial admin account can be provisioned by setting both `Admin:Email` and `Admin:Password`. They must either both be set or both omitted in production. If configured, the application creates the account with the `Admin` role if it does not already exist. Use a strong password and store both values as secrets.

In deployed environments, the same settings can be supplied as environment variables using `__` in place of `:`. For example:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
Resend__ApiKey
Admin__Email
Admin__Password
```

## Run locally

From `EduWalletApp`:

```shell
dotnet restore
dotnet run --launch-profile http
```

The HTTP launch profile listens at `http://localhost:5199`. In Development, Swagger UI is available at `http://localhost:5199/swagger`.

On startup, the backend applies pending EF Core migrations and ensures the `Admin` and `User` roles exist. The configured database must be reachable and the database user must have permissions to apply migrations.

To build a production publish:

```shell
dotnet publish -c Release -o ./publish
```

## API overview

The frontend's default API base URL is `http://localhost:5199/api`. Configure the frontend's `VITE_API_URL` when using a different API address.

| Route | Purpose |
| --- | --- |
| `/api/Auth` | Registration, login, and password reset |
| `/api/Onboarding` | Student onboarding data and financial profile setup |
| `/api/Profile` | Current user's profile |
| `/api/Dashboard` | Dashboard summary |
| `/api/Budget` | Budgets and budget allocations |
| `/api/IncomeSources`, `/api/Expense`, `/api/Transaction` | Income, expenses, and transactions |
| `/api/BankStatement` | Bank statement upload and parsed transaction insights |
| `/api/Report` | Financial report generation and retrieval |
| `/api/Category`, `/api/SubCategory` | Categories and subcategories |
| `/api/FinancialTip`, `/api/tips` | Financial tips |
| `/api/Notification` | User notifications |
| `/api/feedback` | Feedback submission |
| `/api/admin`, `/api/admin/categories`, `/api/admin/tips` | Admin summaries, user/activity management, and content management |

Most financial and profile endpoints require a bearer token. Admin endpoints require the `Admin` role. Authentication endpoints use a stricter rate limit; the API also applies a global per-IP rate limit. See the controllers in `EduWalletApp/Controllers` for individual actions, request DTOs, and authorization requirements.

## Project layout

```text
EduWalletApp/
├── EduWalletApp.slnx
└── EduWalletApp/
    ├── Controllers/   # HTTP endpoints
    ├── Data/          # EF Core database context
    ├── DTOs/           # API request and response models
    ├── Migrations/     # EF Core schema migrations
    ├── Middleware/     # Exception handling and activity tracking
    ├── Models/         # Identity and finance entities
    └── Services/       # Financial calculations, insights, alerts, and email
```

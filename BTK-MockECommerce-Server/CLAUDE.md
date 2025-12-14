# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the WebAPI (from root directory)
dotnet run --project MockECommerce.WebAPI

# Run in development mode (default)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project MockECommerce.WebAPI

# Build Docker image
docker build -t mock-ecommerce .

# Run Docker container
docker run -p 8080:8080 mock-ecommerce
```

## Database Commands (EF Core with PostgreSQL)

```bash
# Add migration (run from solution root)
dotnet ef migrations add <MigrationName> --project MockECommerce.DAL --startup-project MockECommerce.WebAPI

# Update database
dotnet ef database update --project MockECommerce.DAL --startup-project MockECommerce.WebAPI
```

## Environment Configuration

The application uses `.env` file (at solution root) for configuration. Key variables:
- `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USERNAME`, `DB_PASSWORD` - PostgreSQL connection
- `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_SECRET_KEY`, `JWT_EXPIRY_MINUTES` - JWT settings
- `ADMIN_FULLNAME`, `ADMIN_EMAIL`, `ADMIN_PASSWORD` - Default admin user

Environment variables take precedence over `appsettings.json`.

## Architecture

This is an ASP.NET Core 8.0 Web API following a layered architecture:

### Project Structure
- **MockECommerce.WebAPI** - Entry point, controllers, middlewares, DI configuration
- **MockECommerce.BusinessLayer** - Business logic (Services/Managers), AutoMapper profiles
- **MockECommerce.DAL** - Data Access Layer with Entity Framework Core, repositories, entities
- **MockECommerce.DtoLayer** - DTOs organized by domain (AuthDtos, ProductDtos, etc.)

### Key Patterns

**Repository Pattern**: Generic repository in `MockECommerce.DAL.Repositories.GenericRepository<T>` implements `IGenericDal<T>`. Domain-specific repositories (CategoryRepository, ProductRepository, OrderRepository) extend it.

**Service Pattern**: Services in BusinessLayer follow naming convention `I{Entity}Service` (interface) and `{Entity}Manager` (implementation). Business methods are prefixed with `T` (e.g., `TAddAsync`, `TGetByIdAsync`).

**Dependency Injection**: All services and repositories are registered in `Program.cs` with scoped lifetime.

### Authentication & Authorization

- ASP.NET Core Identity with custom `AppUser`, `AppRole`, `AppUserRole` entities
- JWT Bearer authentication
- API Key authentication via `ApiKeyAuthenticationMiddleware` for external endpoints
- Authorization policies: `RequireAdminRole`, `RequireSellerRole`

### Database Schema

PostgreSQL with Identity tables renamed (Users, Roles, UserRoles, etc.). Key entities:
- `AppUser` / `AppRole` - Identity entities
- `SellerProfile` - 1:1 with User, contains seller-specific data
- `SellerApiKey` - API keys for external integrations
- `Product` - belongs to Seller and Category
- `Order` - references Product and Customer (AppUser)
- `Category` - self-referencing parent-child hierarchy

### Rate Limiting

Configured in `Program.cs`:
- Global: 500 requests/minute
- Login: 7 requests/5 minutes per IP
- Register: 5 requests/30 minutes per IP

### Database Seeding

In development mode, `DatabaseSeedExtensions.SeedDatabaseAsync()` seeds:
- Default roles (Admin, Seller, Customer)
- Admin user (admin@example.com / Admin123!)
- Demo seller with products

## API Endpoints

Controllers in `MockECommerce.WebAPI/Controllers/`:
- `AuthController` - Register, login, token management
- `CategoryController` - CRUD for categories
- `ProductController` - CRUD for products
- `OrderController` - Order management
- `ApiKeyController` - Seller API key management
- `ExternalProductController` - External API access with API key auth

# GLMS – Global Logistics Management System

A comprehensive enterprise application for managing logistics contracts, service requests, and currency conversions with robust business rule validation and automated workflows.

---

## POE Part 1 & Part 2 Submission

**Student Name:** Lindokuhle Zwane
**Student Number:** ST10381088
**Module:** EAPD7111  
**Date:** May 2026  

---

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Key Features](#key-features)
- [Design Patterns](#design-patterns)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Database Setup](#database-setup)
- [Testing](#testing)
- [API Documentation](#api-documentation)
- [Screenshots](#screenshots)
- [Rubric Compliance](#rubric-compliance)

---

## 🎯 Overview

GLMS is a full-stack enterprise application designed to streamline logistics contract management. The system enables organizations to:

- Manage client contracts with status tracking and document handling
- Create and track service requests with automatic currency conversion
- Enforce business rules (e.g., service requests only for active contracts)
- Handle PDF document uploads with validation and secure storage
- Integrate with external currency APIs for real-time exchange rates
- Provide comprehensive search and filtering capabilities

The application follows clean architecture principles with separation of concerns across multiple layers, ensuring maintainability, testability, and scalability.

---

## 🏗️ Architecture

GLMS implements a **Clean Architecture** pattern with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                      GLMS.Enterprise.Web                     │
│                    (Presentation Layer)                      │
│                   ASP.NET Core MVC + Views                   │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                   GLMS.Enterprise.Services                   │
│                     (Business Logic Layer)                    │
│         Currency Strategies • Observers • File Service       │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                GLMS.Enterprise.Infrastructure                │
│                    (Data Access Layer)                       │
│         EF Core • Repositories • Database Context             │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                   GLMS.Enterprise.Core                       │
│                      (Domain Layer)                          │
│      Entities • Enums • Interfaces • Domain Models           │
└─────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Technology Stack

### Core Framework
- **.NET 10.0** - Latest .NET platform
- **ASP.NET Core MVC** - Web framework
- **C# 13** - Programming language with nullable reference types

### Data & Database
- **Entity Framework Core 10.0** - ORM framework
- **SQL Server** - Relational database
- **Fluent API** - Database configuration and constraints

### Testing
- **xUnit 2.9.3** - Testing framework
- **Moq 4.20.72** - Mocking framework
- **Entity Framework Core InMemory** - In-memory database for testing
- **Coverlet** - Code coverage tool

### External Services
- **ExchangeRate-API.com** - Live currency exchange rates
- **Microsoft.Extensions.Http** - HTTP client for API calls
- **Microsoft.Extensions.Caching.Memory** - In-memory caching

### Frontend
- **Bootstrap 5** - CSS framework
- **jQuery** - JavaScript library
- **jQuery Validation** - Client-side validation
- **Bootstrap Icons** - Icon library

---

## ✨ Key Features

### Contract Management
- **Full CRUD Operations** - Create, read, update, and delete contracts
- **Status Tracking** - Draft, Active, OnHold, Expired, Terminated
- **Client Association** - Link contracts to clients
- **Service Level Definition** - Define service levels per contract
- **Date Range Management** - Track contract validity periods
- **PDF Document Handling** - Upload and download contract documents

### Service Request Management
- **Request Creation** - Create service requests against active contracts
- **Business Rule Validation** - Only allows requests for Active contracts
- **Currency Conversion** - Automatic USD to ZAR conversion with live rates
- **Status Workflow** - Pending → Approved → InProgress → Fulfilled
- **Edit Restrictions** - Currency amounts immutable after creation
- **Audit Trail** - Track creation date and creator

### Currency Conversion
- **Live API Integration** - Real-time exchange rates from ExchangeRate-API
- **Caching Strategy** - Cached rates with configurable TTL (15 minutes)
- **Fallback Mechanism** - Graceful degradation to fixed rate (18.50) on API failure
- **Multiple Strategies** - Live API, Cached, and Fixed Rate strategies
- **Rate Display** - Show current rate and rate used for each conversion

### Search & Filtering
- **Date Range Filtering** - Filter contracts by start/end date ranges
- **Status Filtering** - Filter by contract status
- **Pagination** - Efficient pagination for large datasets
- **Real-time Search** - Instant search results

### File Handling
- **PDF Validation** - Only PDF files accepted
- **UUID Naming** - Secure file naming to prevent conflicts
- **Size Validation** - Configurable file size limits
- **Secure Storage** - Files stored in wwwroot/uploads
- **Download Support** - Easy file retrieval

---

## 🎨 Design Patterns

### 1. Repository Pattern
Separates data access logic from business logic, providing a clean abstraction over Entity Framework Core.

```csharp
public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(Guid id);
    Task<IEnumerable<Contract>> GetEligibleForServiceRequestAsync();
    // ...
}
```

### 2. Strategy Pattern
Enables runtime selection of currency conversion strategies (Live API, Cached, Fixed Rate).

```csharp
public interface ICurrencyStrategy
{
    Task<decimal> GetRateAsync(string fromCurrency, string toCurrency);
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
}
```

**Implementations:**
- `LiveApiCurrencyStrategy` - Fetches live rates from API
- `CachedCurrencyStrategy` - Caches rates to reduce API calls
- `FixedRateStrategy` - Provides fallback fixed rate

### 3. Observer Pattern
Implements the Observer pattern for contract status change notifications.

**Subject:** `IContractStatusSubject`
**Observers:** 
- `AuditLogObserver` - Logs status changes
- `EmailNotificationObserver` - Sends email notifications
- `ContractStatusNotifier` - Coordinates observer notifications

### 4. Dependency Injection
Full DI container configuration in `Program.cs` for all services and repositories.

---

## 📁 Project Structure

```
GLMS.Enterprise/
├── Core/                          # Domain layer
│   ├── Entities/                  # Domain entities
│   │   ├── Client.cs
│   │   ├── Contract.cs
│   │   └── ServiceRequest.cs
│   ├── Enums/                     # Domain enums
│   │   ├── ContractStatus.cs
│   │   └── ServiceRequestStatus.cs
│   ├── Interfaces/                # Domain interfaces
│   │   ├── IContractRepository.cs
│   │   ├── IContractService.cs
│   │   ├── ICurrencyStrategy.cs
│   │   └── IContractStatusObserver.cs
│   └── Models/                    # Domain models
│       ├── PagedResult.cs
│       └── ValidationResult.cs
│
├── Infrastructure/                 # Data access layer
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   └── Repositories/
│       └── ContractRepository.cs
│
├── Services/                      # Business logic layer
│   ├── Currency/
│   │   ├── LiveApiCurrencyStrategy.cs
│   │   ├── CachedCurrencyStrategy.cs
│   │   └── FixedRateStrategy.cs
│   ├── Observers/
│   │   ├── AuditLogObserver.cs
│   │   ├── EmailNotificationObserver.cs
│   │   └── ContractStatusNotifier.cs
│   ├── ContractService.cs
│   ├── ExchangeRateApiService.cs
│   └── FileService.cs
│
├── Web/                           # Presentation layer
│   ├── Controllers/
│   │   ├── HomeController.cs
│   │   ├── ClientController.cs
│   │   ├── ContractController.cs
│   │   └── ServiceRequestController.cs
│   ├── Models/                    # View models
│   │   ├── ClientViewModel.cs
│   │   ├── ContractViewModel.cs
│   │   └── ServiceRequestViewModel.cs
│   ├── Views/                     # Razor views
│   │   ├── Client/
│   │   ├── Contract/
│   │   └── ServiceRequest/
│   ├── wwwroot/                   # Static files
│   │   ├── css/
│   │   ├── js/
│   │   └── uploads/               # Uploaded PDFs
│   └── Program.cs                 # Application configuration
│
├── Tests/                         # Test project
│   ├── Currency/
│   │   ├── CurrencyStrategyTests.cs
│   │   ├── CachedCurrencyStrategyTests.cs
│   │   └── LiveApiCurrencyStrategyTests.cs
│   ├── File/
│   │   └── FileServiceTests.cs
│   ├── Repositories/
│   │   └── ContractRepositoryTests.cs
│   └── Services/
│       └── ContractServiceTests.cs
│
├── Migrations/                    # EF Core migrations
│   ├── 20260517223943_InitialCreate.cs
│   └── ApplicationDbContextModelSnapshot.cs
│
├── .github/workflows/
│   └── ci-cd.yml                  # CI/CD pipeline
│
├── appsettings.json              # Application configuration
├── appsettings.Development.json   # Development configuration
└── GLMS.Enterprise.sln           # Solution file
```

---

## 🚀 Getting Started

### Prerequisites

- **.NET 10.0 SDK** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **SQL Server** - Local or remote SQL Server instance
- **Visual Studio 2022** - Recommended IDE (or VS Code with C# extension)

### Installation Steps

1. **Clone the Repository**
   ```bash
   git clone https://github.com/Leendouh/GLMS.Enterprise.git 
   cd GLMS.Enterprise
   ```

2. **Restore NuGet Packages**
   ```bash
   dotnet restore
   ```

3. **Update Connection String**
   
   Edit `appsettings.json` and update the connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=GLMS_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
     }
   }
   ```

4. **Run Database Migrations**
   ```bash
   dotnet ef database update --project GLMS.Enterprise.Infrastructure --startup-project GLMS.Enterprise.Web
   ```

5. **Run the Application**
   ```bash
   dotnet run --project GLMS.Enterprise.Web
   ```

6. **Access the Application**
   
   Open your browser and navigate to: `https://localhost:5001`

---

## 🗄️ Database Setup

### Migration Scripts

The Entity Framework Core migrations are located in:  
`GLMS.Infrastructure/Migrations/` 

### Manual Database Creation

To recreate the database from scratch:

1. **Update Connection String** in `GLMS.Web/appsettings.json`
2. **Run Migration Command:**
   ```bash
   Update-Database -Project GLMS.Enterprise.Infrastructure -StartupProject GLMS.Enterprise.Web
   ```

### Database Schema

The application uses the following main tables:

- **Clients** - Client information
- **Contracts** - Contract details with status tracking
- **ServiceRequests** - Service requests linked to contracts
- **ContractDocuments** - PDF document storage references

### Seeding Data

The application starts with an empty database. Use the UI to create initial clients and contracts for testing.

---

## 🧪 Testing

### Running Tests

Run all tests using the .NET CLI:

```bash
dotnet test GLMS.Enterprise.Tests
```

Or run from Visual Studio Test Explorer.

### Test Coverage

The test suite covers:

| Test Suite | Description | Tests |
|:---|:---|:---|
| **Currency Strategy Tests** | Live API, cached, and fixed rate strategies | 8 tests |
| **File Service Tests** | PDF validation, UUID naming, size limits | 5 tests |
| **Contract Repository Tests** | CRUD operations, filtering, eligibility | 6 tests |
| **Contract Service Tests** | Business rules, status transitions | 4 tests |

### Test Execution Screenshots

All unit tests pass. See screenshots below:

| Test Suite | Result |
|:---|:---|
| Currency Calculation Tests | ✅ Passed |
| File Validation Tests | ✅ Passed |
| Contract Validation Tests | ✅ Passed |

![Test Explorer Screenshot](Docs/TestResults.png)

### Test Technologies

- **xUnit** - Test framework
- **Moq** - Mocking framework for isolating dependencies
- **EF Core InMemory** - In-memory database for repository testing
- **Fluent Assertions** - Readable assertion library (if added)

---

## 📡 API Documentation

### Currency Exchange Rate API

**Endpoint:** `GET /ServiceRequest/GetExchangeRate`

**Description:** Fetches the current USD to ZAR exchange rate.

**Response:**
```json
{
  "success": true,
  "rate": 18.50,
  "source": "api"
}
```

**Fallback Response:**
```json
{
  "success": false,
  "rate": 18.50,
  "source": "fallback"
}
```

### External API Integration

The application integrates with [ExchangeRate-API.com](https://www.exchangerate-api.com/) for live currency rates.

**Configuration** (in `appsettings.json`):
```json
{
  "CurrencySettings": {
    "Strategy": "Cached",
    "FallbackUsdToZar": 18.50,
    "CacheTtlMinutes": 15
  }
}
```

---

## 📸 Screenshots of Key Functionality

### 1. Contract with Active Status (allows service requests)
![Active Contract](Docs/ContractActive.png)

### 2. Currency Conversion Example (USD 1250 → ZAR 20875.00 at rate 16.70)
![Currency Conversion](Docs/CurrencyConversion.png)

### 3. Expired/On‑Hold Contract Not in Dropdown
![Expired Contract Hidden](Docs/ExpiredNotInDropdown.png)

### 4. Search and Filter Interface
![Search Interface](Docs/SearchInterface.png)

### 5. Service Request Creation
![Service Request Creation](Docs/ServiceRequestCreation.png)

---

## 📊 Rubric Compliance

| Section | Marks | How met |
|:---|:---|:---|
| **Database Architecture** | 10 | SQL Server, EF Core, Fluent API constraints, migrations, proper relationships |
| **Design Patterns** | 20 | Repository + Strategy + Observer patterns, DI registration, clean architecture |
| **Workflow & Validation** | 15 | Service request only on Active contracts; status transition rules; business rule enforcement |
| **File Handling** | 10 | PDF validation, UUID naming, secure storage, download functionality |
| **External API** | 15 | Live currency API with retry, caching, and graceful fallback |
| **Unit Tests Setup** | 15 | xUnit + Moq + InMemory; proper test project structure; all tests pass |
| **Unit Tests Quality** | 15 | Edge cases (zero, negative, null, invalid files); comprehensive coverage |

**Total possible: 100**

---

## 🎥 Video Walkthrough

[Click here to watch the video walkthrough](https://youtu.be/your-unlisted-link)

*The video demonstrates all required features: contract management, PDF upload/download, service request creation with currency conversion, business rule validation (expired/on‑hold contracts), search/filter, and unit tests.*

---

## 🔗 GitHub Repository

**Repository URL:** https://github.com/Leendouh/GLMS.Enterprise.git 

---

## 📝 Configuration

### Application Settings

Key configuration options in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=GLMS_Dev;..."
  },
  "CurrencySettings": {
    "Strategy": "Cached",
    "FallbackUsdToZar": 18.50,
    "CacheTtlMinutes": 15
  }
}
```

### Environment-Specific Settings

- **Development:** `appsettings.Development.json`
- **Production:** Use environment variables or production config file

---

## 🤝 Contributing

This is an academic project. For questions or suggestions, please contact the lecturer via ARC.

---

## 📄 License

This project is submitted as part of academic coursework (EAPD7111).

---

## 📞 Contact

For any issues, please contact the lecturer via ARC.

---

## 🎯 Key Features Demonstrated

| Feature | How to verify |
|:---|:---|
| **PDF upload with validation** | Only `.pdf` files allowed; files saved with UUID name; download works. |
| **Service request creation** | Only for **Active** contracts. Expired/On‑Hold contracts are excluded from dropdown. |
| **Currency conversion (USD → ZAR)** | Live API rate fetched; math correct; fallback if API fails. |
| **Search / filter** | By date range and contract status. |
| **Unit tests** | All pass (see screenshot). |
| **Design patterns** | Repository, Strategy, Observer patterns implemented. |
| **Business rules** | Contract status validation, service request restrictions. |
| **File handling** | PDF validation, secure storage, UUID naming. |

---

## 🚀 CI/CD Pipeline

The project includes a GitHub Actions workflow for continuous integration and deployment:

```yaml
name: CI/CD Pipeline
on: [push, pull_request]
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

---

## 📚 Additional Resources

- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [xUnit Documentation](https://xunit.net/)
- [ExchangeRate-API Documentation](https://www.exchangerate-api.com/docs/)

---

**Built with ❤️ using .NET 10.0 and ASP.NET Core**

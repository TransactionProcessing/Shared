# Shared

Reusable .NET libraries for transaction-processing services, including logging, serialization, web helpers, Entity Framework support, EventStore integration, result handling, and test infrastructure.

[![Nightly Build](https://github.com/TransactionProcessing/Shared/actions/workflows/nightlybuild.yml/badge.svg)](https://github.com/TransactionProcessing/Shared/actions/workflows/nightlybuild.yml)
[![Release](https://github.com/TransactionProcessing/Shared/actions/workflows/createrelease.yml/badge.svg)](https://github.com/TransactionProcessing/Shared/actions/workflows/createrelease.yml)
[![Codacy Code Quality](https://app.codacy.com/project/badge/Grade/470520d84090465eaaa59b6061882392)](https://app.codacy.com/gh/TransactionProcessing/Shared/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![Codacy Coverage](https://app.codacy.com/project/badge/Coverage/470520d84090465eaaa59b6061882392)](https://app.codacy.com/gh/TransactionProcessing/Shared/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_coverage)

## NuGet packages

| Package | Purpose | Version |
|---|---|---|
| `ClientProxyBase` | HTTP client proxy foundations | [![Version](https://img.shields.io/feedz/v/transactionprocessing/nugets/ClientProxyBase.svg?label=Version)](https://feedz.io/portal/org/transactionprocessing/repository/nugets/packages/ClientProxyBase) |
| `Shared` | Common web, serialization, middleware, and EF helpers | [![Version](https://img.shields.io/feedz/v/transactionprocessing/nugets/Shared.svg?label=Version)](https://feedz.io/portal/org/transactionprocessing/repository/nugets/packages/Shared) |
| `Shared.DomainDrivenDesign` | Domain-driven design primitives | [![Version](https://img.shields.io/feedz/v/transactionprocessing/nugets/Shared.DomainDrivenDesign.svg?label=Version)](https://feedz.io/portal/org/transactionprocessing/repository/nugets/packages/Shared.DomainDrivenDesign) |
| `Shared.EventStore` | EventStore/KurrentDB integration | [![Version](https://img.shields.io/feedz/v/transactionprocessing/nugets/Shared.EventStore.svg?label=Version)](https://feedz.io/portal/org/transactionprocessing/repository/nugets/packages/Shared.EventStore) |
| `Shared.IntegrationTesting` | Docker and integration-test helpers | [![Version](https://img.shields.io/feedz/v/transactionprocessing/nugets/Shared.IntegrationTesting.svg?label=Version)](https://feedz.io/portal/org/transactionprocessing/repository/nugets/packages/Shared.IntegrationTesting) |
| `Shared.Logger` | Logging and correlation context | [![Version](https://img.shields.io/feedz/v/transactionprocessing/nugets/Shared.Logger.svg?label=Version)](https://feedz.io/portal/org/transactionprocessing/repository/nugets/packages/Shared.Logger) |
| `Shared.Results` | Result types and retry policies | [![Version](https://img.shields.io/feedz/v/transactionprocessing/nugets/Shared.Results.svg?label=Version)](https://feedz.io/portal/org/transactionprocessing/repository/nugets/packages/Shared.Results) |
| `Shared.Results.Web` | ASP.NET result mappings | [![Version](https://img.shields.io/feedz/v/transactionprocessing/nugets/Shared.Results.Web.svg?label=Version)](https://feedz.io/portal/org/transactionprocessing/repository/nugets/packages/Shared.Results.Web) |

## Requirements

- .NET 10 SDK
- Docker Desktop for integration tests
- Access to the configured NuGet feeds

## Installation

Install the package required by your application:

```bash
dotnet add package Shared
```

Other packages can be installed independently depending on the functionality required.

## Key capabilities

- Structured logging and correlation IDs
- Tenant-aware request context
- JSON serialization helpers and naming policies
- ASP.NET Core middleware and result mappings
- Entity Framework Core helpers
- EventStore/KurrentDB integration
- Reusable integration-testing infrastructure

## Testing

Run the test suite with:

```bash
dotnet test Shared.sln
```

Docker-backed integration tests require a compatible Linux Docker engine.

## Repository structure

The repository is split into focused packages so applications can reference only the shared functionality they need.

- `Shared` — core web, serialization, middleware, and persistence helpers
- `Shared.Logger` — logging and tenant context
- `Shared.Results` — result and retry-policy abstractions
- `Shared.EventStore` — EventStore integration
- `Shared.IntegrationTesting` — integration-test infrastructure
- `Shared.Tests` — unit tests for the core shared library

## Contributing

Pull requests should include relevant tests and maintain the project’s coverage expectations.

## License

Add license information here.

## Repository activity

![Repository activity](https://repobeats.axiom.co/api/embed/68c83258ba479c3295325871755c68ccaa0f4647.svg "Repository activity")

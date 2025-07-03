# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Development Commands
```bash
# Backend Development
cd Backend
dotnet restore                          # Restore NuGet packages
dotnet build                           # Build the solution
dotnet run --project src/FeedbackSorter.Presentation  # Run the API server

# Frontend Development  
cd frontend
npm install                            # Install dependencies
npm run dev                           # Run Next.js development server

# Database Operations
cd Backend
dotnet ef migrations add <MigrationName> --project src/FeedbackSorter.Infrastructure --startup-project src/FeedbackSorter.Presentation
dotnet ef database update --project src/FeedbackSorter.Infrastructure --startup-project src/FeedbackSorter.Presentation
```

### Testing Commands
```bash
cd Backend
dotnet test                                    # Run all tests
dotnet test --verbosity normal                # Run tests with detailed output
dotnet test tests/FeedbackSorter.SystemTests  # Run specific test project
dotnet test --filter "FullyQualifiedName~TestMethodName"  # Run specific test

# For individual test projects:
dotnet test tests/FeedbackSorter.Core.UnitTests
dotnet test tests/FeedbackSorter.Application.UnitTests
dotnet test tests/FeedbackSorter.Infrastructure.UnitTests
dotnet test tests/FeedbackSorter.Presentation.UnitTests
dotnet test tests/FeedbackSorter.SystemTests
```

## Architecture Overview

This is a **Clean Architecture** implementation following **Domain-Driven Design (DDD)** principles with **CQRS** patterns. The system is an LLM-powered feedback analysis application that categorizes user feedback, extracts sentiment, and identifies related features.

### Core Business Domain
- **User Feedback**: Natural language text input from users (3-2000 characters)
- **Feature Categories**: Project features identified by LLM analysis (e.g., "Login Form", "Authentication")  
- **Feedback Categories**: Predefined classifications ("General feedback", "Bug report", "Feature Request")
- **Sentiment Analysis**: Emotional tone classification ("Positive", "Negative", "Neutral", "Mixed")
- **Analysis Pipeline**: Background processing using LLM to analyze submitted feedback

### Project Structure (Clean Architecture Layers)

```
Backend/src/
├── FeedbackSorter.SharedKernel/     # Shared base classes, Result patterns
├── FeedbackSorter.Core/             # Domain Layer - Business logic & entities
├── FeedbackSorter.Application/      # Application Layer - Use cases & orchestration  
├── FeedbackSorter.Infrastructure/   # Infrastructure Layer - Data access & external services
└── FeedbackSorter.Presentation/     # Presentation Layer - REST API controllers
```

**Layer Dependencies** (following Dependency Inversion):
- Core: Only references SharedKernel
- Application: References Core + SharedKernel  
- Infrastructure: References Application + Core + SharedKernel
- Presentation: References Application + SharedKernel

### Key Architectural Patterns

**CQRS Implementation:**
- Commands modify state (e.g., `SubmitFeedbackUseCase`, `MarkFeedbackAnalyzedUseCase`)
- Queries retrieve data using optimized read models (e.g., `QueryAnalyzedFeedbacksUseCase`)
- Separate read/write repositories for different concerns

**Background Processing:**
- `BackgroundAnalysisService` processes feedback asynchronously
- LLM analysis happens after feedback submission acknowledgment
- Optimistic locking prevents race conditions during concurrent updates

**Domain-Driven Design:**
- `UserFeedback` aggregate root with business rules enforcement
- Value objects like `FeedbackId`, `FeedbackText`, `FeedbackTitle` with validation
- Domain services for complex business logic coordination

## Important Implementation Details

### Database & Concurrency
- **Entity Framework Core** with SQLite for development
- **Optimistic Concurrency Control** using `RowVersion` on UserFeedback entities
- **Migrations**: Always use `dotnet ef` commands and `Migrate()` (not `EnsureCreated()`)

### LLM Integration
- Interface: `ILLMFeedbackAnalyzer` (implemented via Semantic Kernel)
- Analysis failures are tracked with retry mechanisms
- Feature categories are dynamically created and reused to avoid redundancy

### Testing Strategy
- **System Tests**: Use `CustomWebApplicationFactory` with in-memory SQLite database
- **Unit Tests**: Use `NSubstitute` for mocking, xUnit for assertions
- **Test Builders**: Located in `FeedbackSorter.Tests.Utilities` for clean test data creation
- **Database Setup**: System tests use `Migrate()` to ensure schema consistency

### Frontend Architecture
- **Next.js** application with TypeScript
- **Component Structure**: Reusable UI components in `components/` directory
- **API Integration**: Service layer in `lib/feedbackService.ts` for backend communication
- **Styling**: Tailwind CSS with shadcn/ui components

## Development Guidelines

### Code Standards
- Use nullable annotations (`string?`) consistently
- Validate inputs at public API boundaries using `ArgumentNullException.ThrowIfNull()`
- Avoid primary constructors when validation is needed - use traditional constructors
- Value objects should use `readonly record struct` for simple cases, `record class` for complex ones

### Testing Practices
- Create test builders for domain objects to avoid test brittleness
- Use meaningful defaults in builders (e.g., `PersonNameBuilder` with "John Doe")
- System tests should cover full request/response cycles
- Mock external dependencies (LLM, time provider) in tests
- Where appropriate, prefer writing system tests (tests that use a database and call the system through REST api) over unit tests.
  System tests are located in the .\Backend\tests\FeedbackSorter.SystemTests\FeedbackSorter.SystemTests.csproj project.
  There is a test class called `FeedbackControllerSystemTests` which can be expanded. Or if wanted, other similar test
  classes should be created
- In tests, always mock `ILlmFeedbackAnalyzer` if you expect its methods to be called.

### Entity Framework Patterns
- Use explicit configuration in `OnModelCreating()` for entity relationships
- Implement optimistic concurrency with `RowVersion` properties
- Handle `DbUpdateConcurrencyException` gracefully in repositories
- Use `Migrate()` for database initialization, never `EnsureCreated()`

### Error Handling
- Use `Result<T>` pattern for operation outcomes instead of exceptions
- Domain validation exceptions for business rule violations
- Middleware handles domain exceptions and converts to appropriate HTTP responses
- Log concurrency conflicts and retry scenarios appropriately

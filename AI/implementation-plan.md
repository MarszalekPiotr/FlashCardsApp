# FlashCardsApp — Implementation Plan

**Prepared:** 2026-05-01  
**Based on:** `cr` (code review), live source code analysis  
**Target audience:** Next implementing agent + human reviewer

---

## How to Use This Plan

Each numbered point below is an **independent unit of work** that can be implemented in isolation.  
Points are ordered by priority (critical → important → optional).  
Every point contains:
- **Why** — what problem is being solved and why it matters
- **Where** — exact files to modify/create
- **How** — complete code snippets ready to copy

> ⚠️ Do **NOT** implement all points at once. Pick one, implement, run tests, move to the next.

---

## Priority Map

| # | Title | Priority | Effort |
|---|-------|----------|--------|
| 1 | Remove TodoItem boilerplate | 🔴 Critical | Small |
| 2 | Add RowVersion + Audit Fields to Entity | 🔴 Critical | Medium |
| 3 | Handle DbUpdateConcurrencyException | 🔴 Critical | Small |
| 4 | Fix Outbox Transaction Safety | 🔴 Critical | Medium |
| 5 | Move Domain Events into Entity Factories | 🟡 Important | Small |
| 6 | Refactor IApplicationDbContext (remove child DbSets) | 🟡 Important | Medium |
| 7 | Fix Email Value Object (Result instead of throw) | 🟡 Important | Medium |
| 8 | Unit Tests for SrsCalculationService | 🟡 Important | Medium |
| 9 | Integration Tests — Full Happy Path | 🟢 Medium-term | Large |
| 10 | Replace Guid.NewGuid() with Guid.CreateVersion7() | 🔵 Optional | Small |
| 11 | SRS Magic Numbers to Named Constants | 🔵 Optional | Small |
| 12 | Soft Delete Pattern (ISoftDeletable) | 🟡 Important | Medium |

---

---

## Point 1 — Remove TodoItem Boilerplate

### Why
The project was generated from a template that includes a full TodoItem example across all layers. This boilerplate pollutes the domain model with a fake bounded context and must be removed before production.

### What to Delete

The following files must be **deleted entirely**:

```
src/Domain/Todos/TodoItem.cs
src/Domain/Todos/TodoItemErrors.cs
src/Domain/Todos/TodoItemCreatedDomainEvent.cs
src/Domain/Todos/TodoItemCompletedDomainEvent.cs
src/Domain/Todos/TodoItemDeletedDomainEvent.cs
src/Domain/Todos/Priority.cs

src/Application/Todos/Create/CreateTodoCommand.cs
src/Application/Todos/Create/CreateTodoCommandHandler.cs
src/Application/Todos/Create/CreateTodoCommandValidator.cs
src/Application/Todos/Complete/CompleteTodoCommand.cs
src/Application/Todos/Complete/CompleteTodoCommandHandler.cs
src/Application/Todos/Complete/CompleteTodoCommandValidator.cs
src/Application/Todos/Copy/CopyTodoCommand.cs
src/Application/Todos/Copy/CopyTodoCommandHandler.cs
src/Application/Todos/Copy/CopyTodoCommandValidator.cs
src/Application/Todos/Delete/DeleteTodoCommand.cs
src/Application/Todos/Delete/DeleteTodoCommandHandler.cs
src/Application/Todos/Delete/DeleteTodoCommandValidator.cs
src/Application/Todos/Update/UpdateTodoCommand.cs
src/Application/Todos/Update/UpdateTodoCommandHandler.cs
src/Application/Todos/Get/GetTodosQuery.cs
src/Application/Todos/Get/GetTodosQueryHandler.cs
src/Application/Todos/Get/TodoResponse.cs
src/Application/Todos/GetById/GetTodoByIdQuery.cs
src/Application/Todos/GetById/GetTodoByIdQueryHandler.cs
src/Application/Todos/GetById/TodoResponse.cs

src/Web.Api/Endpoints/Todos/Create.cs
src/Web.Api/Endpoints/Todos/Complete.cs
src/Web.Api/Endpoints/Todos/Copy.cs
src/Web.Api/Endpoints/Todos/Delete.cs
src/Web.Api/Endpoints/Todos/Get.cs
src/Web.Api/Endpoints/Todos/GetById.cs

src/Infrastructure/Todos/TodoItemConfiguration.cs
```

### What to Modify

**`src/Application/Abstractions/Data/IApplicationDbContext.cs`**  
Remove the `TodoItems` line:
```csharp
// BEFORE
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TodoItem> TodoItems { get; }   // ← DELETE THIS LINE
    DbSet<Domain.LanguageAccount.LanguageAccount> LanguageAccounts { get; }
    // ...
}

// AFTER
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Domain.LanguageAccount.LanguageAccount> LanguageAccounts { get; }
    // ...
}
```

Also remove the `using Domain.Todos;` using statement from the file.

**`src/Infrastructure/Database/ApplicationDbContext.cs`**  
Remove the `TodoItems` DbSet property and the `using Domain.Todos;` import:
```csharp
// BEFORE
public DbSet<User> Users { get; set; }
public DbSet<TodoItem> TodoItems { get; set; }    // ← DELETE THIS LINE
public DbSet<Domain.LanguageAccount.LanguageAccount> LanguageAccounts { get; set; }

// AFTER
public DbSet<User> Users { get; set; }
public DbSet<Domain.LanguageAccount.LanguageAccount> LanguageAccounts { get; set; }
```

### Migration

After deleting the files, create and apply a new migration to drop the `TodoItems` table:

```bash
dotnet ef migrations add RemoveTodoItemBoilerplate --project src/Infrastructure --startup-project src/Web.Api
dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
```

### Verification
- Build succeeds with no compile errors
- No `TodoItem` references remain in the codebase (`grep -r "TodoItem" src/` returns nothing)
- Application starts and `/health` endpoint returns 200

---

---

## Point 2 — Add RowVersion and Audit Fields to Entity

### Why
**This is a race condition bug in core functionality.** Without `RowVersion`, two users reviewing the same flashcard simultaneously can silently overwrite each other's SRS progress. The second write wins and the first review is lost with no error.

**Scenario without fix:**
```
T0: Flashcard.SrsState.Interval = 10 in DB
T1: User A reads Flashcard (Interval = 10)
T2: User B reads Flashcard (Interval = 10)
T3: User A reviews "Easy"  → Interval = 15 → saved
T4: User B reviews "Again" → Interval = 1  → saved, silently overwrites User A
```

**With RowVersion, T4 throws `DbUpdateConcurrencyException` → 409 Conflict.**

### Files to Modify

**`src/SharedKernel/Entity.cs`**

Current:
```csharp
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public List<IDomainEvent> DomainEvents => [.. _domainEvents];

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
```

Replace with:
```csharp
using System.ComponentModel.DataAnnotations;

namespace SharedKernel;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public List<IDomainEvent> DomainEvents => [.. _domainEvents];

    /// <summary>
    /// Optimistic concurrency token. EF Core uses this to detect concurrent modifications.
    /// SQL Server automatically updates this value on every row change.
    /// Throws DbUpdateConcurrencyException when a concurrent modification is detected.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; protected set; }

    /// <summary>UTC timestamp of when this entity was first created.</summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>UTC timestamp of the most recent modification, or null if never updated.</summary>
    public DateTime? UpdatedAt { get; protected set; }

    protected Entity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
```

> ⚠️ Note: `IDateTimeProvider` is NOT injected here because `Entity` is in `SharedKernel` which has no DI dependency. `DateTime.UtcNow` is acceptable here because `CreatedAt` is a one-time set value and tests do not need to control it.

### EF Core Configuration — Audit Timestamps

In each entity configuration class, the `UpdatedAt` field should be configured. The best approach is to use EF Core's `SaveChangesAsync` override in `ApplicationDbContext` to auto-set `UpdatedAt`.

**`src/Infrastructure/Database/ApplicationDbContext.cs`**  
Modify the `SaveChangesAsync` override to auto-stamp `UpdatedAt`:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Auto-stamp UpdatedAt on every modified entity
    foreach (var entry in ChangeTracker.Entries<Entity>())
    {
        if (entry.State == EntityState.Modified)
        {
            entry.Entity.UpdatedAt = dateTimeProvider.UtcNow;
        }
    }

    await PublishDomainEventsAsync(cancellationToken);
    int result = await base.SaveChangesAsync(cancellationToken);
    return result;
}
```

> Note: `UpdatedAt` has `protected set` in `Entity`, so you need to make it `internal set` or use EF Core's shadow property approach. Simplest fix: change `protected set` → `internal set` in `Entity.cs`, since `Infrastructure` is a sibling assembly, OR expose a method `SetUpdatedAt(DateTime dt)` on the entity:

```csharp
// Option A: internal setter (recommended for this project structure)
public DateTime? UpdatedAt { get; internal set; }

// Option B: explicit method on Entity
public void SetUpdatedAt(DateTime utcNow)
{
    UpdatedAt = utcNow;
}
```

Use **Option B** (`SetUpdatedAt`) to keep encapsulation clean across assembly boundaries.

### EF Core — RowVersion is auto-configured by [Timestamp]

The `[Timestamp]` attribute tells EF Core to:
1. Map this property to a SQL Server `rowversion` column
2. Use it as the concurrency token on every `UPDATE`/`DELETE`
3. Throw `DbUpdateConcurrencyException` if the value changed since the entity was loaded

No additional EF configuration is needed for `RowVersion`.

### Migration

```bash
dotnet ef migrations add AddRowVersionAndAuditFields --project src/Infrastructure --startup-project src/Web.Api
dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
```

The generated migration will add `RowVersion`, `CreatedAt`, and `UpdatedAt` columns to **every table** whose entity class inherits from `Entity`. Verify the migration output contains changes for `Users`, `LanguageAccounts`, `FlashcardCollections`, `Flashcards`, `SrsStates`, `FlashcardReviews`.

### Verification
- Build succeeds
- Migration applies without error
- `SELECT TOP 1 RowVersion, CreatedAt, UpdatedAt FROM Flashcards` in SQL returns the new columns

---

---

## Point 3 — Handle DbUpdateConcurrencyException in GlobalExceptionHandler

### Why
Point 2 adds `RowVersion` which enables EF Core to detect concurrent modifications. But without handling `DbUpdateConcurrencyException`, it will bubble up as an unhandled exception → 500 Internal Server Error. The correct HTTP response is **409 Conflict** with a meaningful message so the client knows to retry.

> ⚠️ Implement this **after** Point 2, but it can also be done before — it just won't trigger yet.

### Files to Modify

**`src/Web.Api/Infrastructure/GlobalExceptionHandler.cs`**

Current:
```csharp
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            Title = "Server failure"
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
```

Replace with:
```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Infrastructure;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Handle concurrency conflicts (from RowVersion optimistic locking)
        if (exception is DbUpdateConcurrencyException)
        {
            logger.LogWarning(exception, "Concurrency conflict detected");

            var conflictDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "Concurrency conflict",
                Detail = "The record was modified by another request. Please reload and try again."
            };

            httpContext.Response.StatusCode = conflictDetails.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(conflictDetails, cancellationToken);
            return true;
        }

        // Default handler for all other unhandled exceptions
        logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            Title = "Server failure"
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
```

### Verification
- Simulate two concurrent requests to `POST /language-accounts/{id}/collections/{id}/flashcards/{id}/reviews` with the same flashcard
- The second request should return HTTP 409, not 500
- Logs should show `LogWarning`, not `LogError` for concurrency conflicts

---

---

## Point 4 — Fix Outbox Transaction Safety

### Why
The current `OutBoxMessagesBackgroundService` has a race condition: if `SaveChangesAsync` fails after the handler succeeds, the `OutboxMessageConsumer` record (which prevents double-processing) is never saved. On the next background service run, the same message is processed again, causing duplicate handler execution.

**Current bug scenario:**
```
T1: Handler executes successfully
T2: OutboxMessageConsumer added to DbContext (in memory, not saved)
T3: OutboxMessage.ProcessedOnUtc set (in memory, not saved)
T4: SaveChangesAsync FAILS (DB timeout)
    → Both changes are lost
T5: Background service runs again after 10s
T6: Handler executes AGAIN (duplicate!)
T7: SaveChangesAsync succeeds
```

### Files to Modify

**`src/Web.Api/BackgroundServices/OutBoxMessagesBackgroundService.cs`**

The key change is wrapping each message's processing in an explicit transaction so that either **everything** (consumer record + processed timestamp) saves atomically, or **nothing** does.

Current problematic section (lines ~70–120):
```csharp
// CURRENT (no transaction — BUGGY)
try
{
    await handlerWrapper.Handle(domainEvent, stoppingToken);
    dbContext.OutboxMessageConsumers.Add(new OutboxMessageConsumer { ... });
}
catch (Exception ex)
{
    isSucceded = false;
    outboxMessage.Error = $"Failed to process...";
    continue;
}

if(isSucceded)
    outboxMessage.ProcessedOnUtc = _dateTimeProvider.UtcNow;
else
    outboxMessage.RetryCount += 1;

await dbContext.SaveChangesAsync(stoppingToken);  // ← single save, no transaction
```

Replace the entire `foreach` loop body with a transactional approach:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        IEnumerable<OutboxMessage> outboxMessages = dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < MaxRetryCount)
            .ToList();

        foreach (var outboxMessage in outboxMessages)
        {
            // --- Type resolution (unchanged) ---
            Type? domainEventType = Type.GetType(outboxMessage.Type);
            if (domainEventType is null)
            {
                outboxMessage.Error = $"Failed to get domain event type {outboxMessage.Type}";
                outboxMessage.RetryCount += 1;
                await dbContext.SaveChangesAsync(stoppingToken);
                continue;
            }

            IDomainEvent? domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(outboxMessage.Content, domainEventType);
            if (domainEvent is null)
            {
                outboxMessage.Error = $"Failed to deserialize domain event of type {outboxMessage.Type}";
                outboxMessage.RetryCount += 1;
                await dbContext.SaveChangesAsync(stoppingToken);
                continue;
            }

            // --- Handler resolution (unchanged) ---
            Type handlerType = HandlerTypeDictionary.GetOrAdd(
                domainEventType,
                et => typeof(IDomainEventHandler<>).MakeGenericType(et));

            IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(handlerType);
            bool allHandlersSucceeded = true;

            foreach (object? handler in handlers)
            {
                if (handler is null) continue;

                string handlerTypeName = handler.GetType().FullName!;

                // --- Idempotency check (unchanged) ---
                bool alreadyProcessed = dbContext.OutboxMessageConsumers
                    .Any(c => c.OutboxMessageId == outboxMessage.Id && c.HandlerType == handlerTypeName);
                if (alreadyProcessed) continue;

                var handlerWrapper = HandlerWrapper.Create(handler, domainEventType);

                // --- EXPLICIT TRANSACTION per handler ---
                IDbContextTransaction transaction = await dbContext.BeginTransactionAsync(stoppingToken);
                try
                {
                    await handlerWrapper.Handle(domainEvent, stoppingToken);

                    dbContext.OutboxMessageConsumers.Add(new OutboxMessageConsumer
                    {
                        OutboxMessageId = outboxMessage.Id,
                        HandlerType = handlerTypeName,
                        ProcessedOnUtc = _dateTimeProvider.UtcNow
                    });

                    await dbContext.SaveChangesAsync(stoppingToken);
                    await transaction.CommitAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(stoppingToken);

                    allHandlersSucceeded = false;
                    outboxMessage.Error = $"Failed to process domain event of type {outboxMessage.Type}. Error: {ex.Message}";
                }
                finally
                {
                    await transaction.DisposeAsync();
                }
            }

            // Mark the whole message as processed only if all handlers succeeded
            if (allHandlersSucceeded)
            {
                outboxMessage.ProcessedOnUtc = _dateTimeProvider.UtcNow;
            }
            else
            {
                outboxMessage.RetryCount += 1;
            }
            await dbContext.SaveChangesAsync(stoppingToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
    }
}
```

> Note: `IDbContextTransaction` requires adding `using Microsoft.EntityFrameworkCore.Storage;` to the file.

### Verification
- Manually trigger a handler that throws an exception
- Verify that `OutboxMessage.RetryCount` increments but no `OutboxMessageConsumer` row is created
- Verify that after the handler succeeds on retry, `OutboxMessageConsumer` is created and `ProcessedOnUtc` is set
- Verify no duplicate handler executions by adding a counter or log check

---

---

## Point 5 — Move Domain Events into Entity Factories

### Why
Currently `UserRegisteredDomainEvent` and `FlashcardCollectionCreatedDomainEvent` are raised in command handlers, not inside the domain entities. This violates DDD: the domain should **guarantee** that specific events are raised whenever a business rule is executed — regardless of who calls the factory method.

If tomorrow someone adds a new handler that calls `User.Create(...)` and forgets to also call `user.Raise(new UserRegisteredDomainEvent(...))`, the event is silently lost.

**Current (wrong — handler responsibility):**
```csharp
// RegisterUserCommandHandler.cs
var user = User.Create(new Email(command.Email), ...);
await userWriteRepository.AddAsync(user);
user.Raise(new UserRegisteredDomainEvent(user.Id));  // ← in handler, not domain
```

**Target (correct — domain responsibility):**
```csharp
// RegisterUserCommandHandler.cs
var user = User.Create(new Email(command.Email), ...);
await userWriteRepository.AddAsync(user);
// Event is already raised inside User.Create — nothing to do here
```

### Files to Modify

#### 5a. `src/Domain/Users/User.cs`

Add the event raise inside the private constructor:
```csharp
using Domain.Users.Events;  // ← add this using
using Domain.Users.ValueObjects;
using SharedKernel;

namespace Domain.Users;

public sealed class User : Entity
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PasswordHash { get; private set; }

    private User(Email email, string firstName, string lastName, string passwordHash)
    {
        Id = Guid.NewGuid();
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;

        Raise(new UserRegisteredDomainEvent(Id));  // ← MOVED HERE from handler
    }

    public static User Create(Email email, string firstName, string lastName, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(firstName);
        ArgumentNullException.ThrowIfNull(lastName);
        ArgumentNullException.ThrowIfNull(passwordHash);

        return new User(email, firstName, lastName, passwordHash);
    }
}
```

> ⚠️ `UserRegisteredDomainEvent` is currently in `Domain.Users` (file: `src/Domain/Users/Events/UserRegisteredDomainEvent.cs`). The using is already `using Domain.Users;` — check if the event is in the same namespace or a sub-namespace and adjust accordingly.

#### 5b. `src/Application/Users/Register/RegisterUserCommandHandler.cs`

Remove the manual `Raise(...)` call:
```csharp
// BEFORE
var user = User.Create(new Email(command.Email), command.FirstName, command.LastName, hashedPassword);
await userWriteRepository.AddAsync(user);
user.Raise(new UserRegisteredDomainEvent(user.Id));  // ← DELETE THIS LINE
await applicationDbContext.SaveChangesAsync(cancellationToken);

// AFTER
var user = User.Create(new Email(command.Email), command.FirstName, command.LastName, hashedPassword);
await userWriteRepository.AddAsync(user);
await applicationDbContext.SaveChangesAsync(cancellationToken);
```

Also remove the `using Domain.Users.Events;` import from the handler if it was only used for `UserRegisteredDomainEvent`.

#### 5c. `src/Domain/FlashcardCollection/FlashcardCollection.cs`

Add the event raise inside the private constructor:
```csharp
using Domain.FlashcardCollection.Events;  // ← add this using
using SharedKernel;

namespace Domain.FlashcardCollection;

public class FlashcardCollection : Entity
{
    public Guid Id { get; private set; }
    public Guid LanguageAccountId { get; private set; }
    public string Name { get; private set; }

    private FlashcardCollection() { } // Required by EF Core

    private FlashcardCollection(Guid languageAccountId, string name)
    {
        Id = Guid.NewGuid();
        LanguageAccountId = languageAccountId;
        Name = name;

        Raise(new FlashcardCollectionCreatedDomainEvent(Id));  // ← MOVED HERE from handler
    }

    public static FlashcardCollection Create(Guid languageAccountId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        return new FlashcardCollection(languageAccountId, name);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
        Name = name;
    }
}
```

> ⚠️ The EF Core parameterless constructor `private FlashcardCollection() { }` must **not** call `Raise(...)`. EF Core uses this constructor when materializing entities from the database — raising a domain event during DB read would be incorrect.

#### 5d. `src/Application/FlashcardCollection/Commands/CreateFlashcardCollection/CreateFlashcardCollectionCommandHandler.cs`

Remove the manual `Raise(...)` call:
```csharp
// BEFORE
Domain.FlashcardCollection.FlashcardCollection collection = Domain.FlashcardCollection.FlashcardCollection.Create(languageAccount.Id, command.Name);
await flashcardCollectionRepository.AddAsync(collection);
collection.Raise(new FlashcardCollectionCreatedDomainEvent(collection.Id));  // ← DELETE THIS LINE
await applicationDbContext.SaveChangesAsync(cancellationToken);

// AFTER
Domain.FlashcardCollection.FlashcardCollection collection = Domain.FlashcardCollection.FlashcardCollection.Create(languageAccount.Id, command.Name);
await flashcardCollectionRepository.AddAsync(collection);
await applicationDbContext.SaveChangesAsync(cancellationToken);
```

Also remove the `using Domain.FlashcardCollection.Events;` import from the handler if unused.

### Verification
- Register a new user → verify `UserRegisteredDomainEvent` appears in `OutboxMessages` table
- Create a collection → verify `FlashcardCollectionCreatedDomainEvent` appears in `OutboxMessages` table
- Both events should work exactly as before from a functional standpoint

---

---

## Point 6 — Refactor IApplicationDbContext (Remove Child Entity DbSets)

### Why
`IApplicationDbContext` currently exposes `DbSet<FlashcardReview>`, `DbSet<SrsState>`, and `DbSet<OutboxMessageConsumer>` to the Application layer. These are either child entities (only accessible through the aggregate root) or infrastructure implementation details.

This creates the risk that a future handler could bypass the aggregate root and write directly to `SrsStates` — breaking domain invariants with no compile-time warning.

### Files to Modify

#### 6a. `src/Application/Abstractions/Data/IApplicationDbContext.cs`

```csharp
using Domain.FlashcardCollection;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Abstractions.Data;

/// <summary>
/// Database abstraction exposed to the Application layer.
/// Only aggregate roots are exposed here.
/// Child entities (FlashcardReview, SrsState) must be accessed through their aggregate root.
/// Infrastructure concerns (OutboxMessageConsumer) are handled in the Infrastructure layer directly.
/// </summary>
public interface IApplicationDbContext
{
    // Aggregate Roots only
    DbSet<User> Users { get; }
    DbSet<Domain.LanguageAccount.LanguageAccount> LanguageAccounts { get; }
    DbSet<Domain.FlashcardCollection.FlashcardCollection> FlashcardCollections { get; }
    DbSet<Flashcard> Flashcards { get; }

    // Outbox (Application layer needs to write OutboxMessage during SaveChanges)
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    // REMOVED:
    // DbSet<TodoItem> TodoItems          ← covered by Point 1
    // DbSet<FlashcardReview> FlashcardReviews  ← child entity, access via Flashcard.Reviews
    // DbSet<SrsState> SrsStates               ← child entity, access via Flashcard.SrsState
    // DbSet<OutboxMessageConsumer> OutboxMessageConsumers ← infrastructure detail
}
```

#### 6b. `src/Web.Api/BackgroundServices/OutBoxMessagesBackgroundService.cs`

The background service currently accesses `dbContext.OutboxMessageConsumers` via `IApplicationDbContext`. After removing it from the interface, the service must use the concrete `ApplicationDbContext` instead.

Change the service scope resolution from:
```csharp
// BEFORE
var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
```

To:
```csharp
// AFTER
using Infrastructure.Database;  // ← add using
var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
```

> This is acceptable because `OutBoxMessagesBackgroundService` is in `Web.Api` and already has a reference to `Infrastructure` (see `using Infrastructure.Database;` in the file). The background service is an infrastructure concern and should have access to the concrete context.

#### 6c. Verify No Application Layer Code Uses Removed DbSets

Run a grep to confirm no handler, query, or specification uses `FlashcardReviews`, `SrsStates`, or `OutboxMessageConsumers` directly:
```bash
grep -r "FlashcardReviews\|SrsStates\|OutboxMessageConsumers" src/Application/
```

If any results are found, those usages must be refactored to go through the aggregate root (e.g., `flashcard.Reviews` instead of `applicationDbContext.FlashcardReviews`).

### Verification
- Build succeeds
- All Application layer code still compiles (no references to removed DbSets)
- Background service still processes outbox messages correctly

---

---

## Point 7 — Fix Email Value Object (Return Result\<Email\> Instead of Throwing)

### Why
`Email` throws `ArgumentException` on invalid format while the entire rest of the codebase uses the `Result<T>` pattern to communicate failures. This inconsistency means:
1. Callers of `new Email(value)` must wrap it in a `try/catch` instead of the standard `Result` check
2. An invalid email from a user request becomes an unhandled exception → 500 error instead of a clean 400 Validation error

### Files to Modify

#### 7a. `src/Domain/Users/ValueObjects/Email.cs`

Current:
```csharp
public record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (!IsValidEmail(value))
        {
            throw new ArgumentException("Invalid email format.", nameof(value));
        }
        Value = value;
    }
    
    private static bool IsValidEmail(string email) =>
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    public override string ToString() => Value;
}
```

Replace with a factory method pattern (keep the private constructor so EF Core can still use it via the conversion):
```csharp
using System.Text.RegularExpressions;
using SharedKernel;

namespace Domain.Users.ValueObjects;

public record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    // Private constructor — used by EF Core value conversion and by Create()
    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an Email value object.
    /// Returns a failure Result if the format is invalid.
    /// </summary>
    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(EmailErrors.Empty);
        }

        if (!EmailRegex.IsMatch(value))
        {
            return Result.Failure<Email>(EmailErrors.InvalidFormat);
        }

        return Result.Success(new Email(value));
    }

    // Used only by EF Core value conversion (infrastructure layer)
    // Do not call this from application/domain code
    internal static Email FromPersistence(string value) => new(value);

    public override string ToString() => Value;
}
```

#### 7b. Create `src/Domain/Users/ValueObjects/EmailErrors.cs`

```csharp
using SharedKernel;

namespace Domain.Users.ValueObjects;

public static class EmailErrors
{
    public static readonly Error Empty = Error.Validation(
        "Email.Empty",
        "Email address cannot be empty.");

    public static readonly Error InvalidFormat = Error.Validation(
        "Email.InvalidFormat",
        "Email address format is invalid.");
}
```

> Note: `Error.Validation(...)` may not exist yet on the `Error` record. Check `SharedKernel/Error.cs`. If `ErrorType.Validation` is defined but `Error.Validation(code, desc)` static method is missing, add it:
> ```csharp
> public static Error Validation(string code, string description) =>
>     new(code, description, ErrorType.Validation);
> ```

#### 7c. Update `src/Application/Users/Register/RegisterUserCommandHandler.cs`

Current:
```csharp
var user = User.Create(
    new Email(command.Email),   // ← throws if invalid
    command.FirstName,
    command.LastName,
    hashedPassword);
```

Replace with:
```csharp
Result<Email> emailResult = Email.Create(command.Email);
if (emailResult.IsFailure)
{
    return Result.Failure<Guid>(emailResult.Error);
}

var user = User.Create(
    emailResult.Value,
    command.FirstName,
    command.LastName,
    hashedPassword);
```

#### 7d. Update `src/Infrastructure/Users/UserConfiguration.cs`

The EF Core value conversion currently calls `new Email(value)` (which is now private). Update it to use `Email.FromPersistence`:

```csharp
builder.Property(u => u.Email)
    .HasConversion(
        email => email.Value,
        value => Email.FromPersistence(value))  // ← use the internal factory
    .HasMaxLength(255);
```

#### 7e. Update `src/Application/Users/Register/RegisterUserCommandValidator.cs`

The FluentValidation validator likely already validates email format at the application boundary. Verify this handles the case gracefully — the `Email.Create` result check in the handler is a second layer of defense.

### Verification
- POST `/users/register` with `"email": "notanemail"` → returns 400 with validation error, not 500
- POST `/users/register` with valid email → still works correctly
- EF Core still loads users from the database (Email value conversion works)

---

---

## Point 8 — Unit Tests

### Why
The application has zero unit tests. The core SRS algorithm, value objects with validation logic, and domain entity behaviour are completely unprotected — any refactoring can silently break business rules.

### Test project structure

```
tests/
  Domain.Tests/
    Domain.Tests.csproj
    FlashcardCollection/
      SrsCalculationServiceTests.cs   ← SM-2 algorithm
      FlashcardTests.cs               ← Flashcard.AddReview() + Flashcard.Create() guards
      SynonymsTests.cs                ← Synonyms value object
    Users/
      EmailTests.cs                   ← Email.Create() Result pattern
      UserTests.cs                    ← User.Create() + ISoftDeletable behaviour
    LanguageAccount/
      LanguageAccountTests.cs         ← LanguageAccount.Delete() soft delete
    FlashcardCollectionTests.cs       ← FlashcardCollection.Create() + Rename() + Delete()
  Application.Tests/
    Application.Tests.csproj
    Users/
      RegisterUserCommandValidatorTests.cs
    FlashcardCollection/
      AddFlashcardReviewCommandValidatorTests.cs
```

**`tests/Domain.Tests/Domain.Tests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Shouldly" Version="4.2.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Domain\Domain.csproj" />
  </ItemGroup>
</Project>
```

**`tests/Application.Tests/Application.Tests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Shouldly" Version="4.2.1" />
    <PackageReference Include="FluentValidation.TestHelper" Version="11.9.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Application\Application.csproj" />
    <ProjectReference Include="..\..\src\Domain\Domain.csproj" />
  </ItemGroup>
</Project>
```

> ⚠️ `SrsState.UpdateState()` is currently `public` — no `InternalsVisibleTo` needed. Verify before running tests.

---

### Test suite 1 — `SrsCalculationServiceTests.cs`

**File:** `tests/Domain.Tests/FlashcardCollection/SrsCalculationServiceTests.cs`

Tests the SM-2 algorithm in `SrsCalculationService`. All tests are pure (no mocks, no DB).

```csharp
using Domain.FlashcardCollection;
using Domain.FlashcardCollection.DomainServices;
using Domain.FlashcardCollection.Enums;
using Shouldly;

namespace Domain.Tests.FlashcardCollection;

public sealed class SrsCalculationServiceTests
{
    private readonly SrsCalculationService _sut = new();
    private readonly DateTime _t0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private SrsState FreshState() => SrsState.CreateInitialState(Guid.NewGuid(), _t0);

    // ── First review ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ReviewResult.Easy)]
    [InlineData(ReviewResult.Know)]
    public void FirstReview_Success_SetsIntervalTo1AndRepetitionsTo1(ReviewResult result)
    {
        var state = FreshState();
        var calc = _sut.CalculateNextState(state, result, _t0);
        calc.Interval.ShouldBe(1);
        calc.Repetitions.ShouldBe(1);
        calc.NextReviewDate.ShouldBe(_t0.AddDays(1));
    }

    // ── Interval progression (SM-2 standard sequence) ─────────────────────────

    [Fact]
    public void SecondSuccessfulReview_SetsIntervalTo3()
    {
        var state = FreshState();
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Know, _t0));
        var r2 = _sut.CalculateNextState(state, ReviewResult.Know, _t0.AddDays(1));
        r2.Interval.ShouldBe(3);
        r2.Repetitions.ShouldBe(2);
    }

    [Fact]
    public void ThirdSuccessfulReview_UsesEaseFactorMultiplier()
    {
        var state = FreshState();
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Know, _t0));
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Know, _t0.AddDays(1)));
        var r3 = _sut.CalculateNextState(state, ReviewResult.Know, _t0.AddDays(4));
        int expected = (int)Math.Round(3 * state.EaseFactor);
        r3.Interval.ShouldBe(expected);
    }

    // ── Failed review — resets progress ───────────────────────────────────────

    [Theory]
    [InlineData(ReviewResult.Again)]
    [InlineData(ReviewResult.DontKnow)]
    public void FailedReview_ResetsIntervalAndRepetitionsToZero(ReviewResult failResult)
    {
        var state = FreshState();
        // Advance to repetitions = 3
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Know, _t0));
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Know, _t0.AddDays(1)));
        state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Easy, _t0.AddDays(4)));

        var calc = _sut.CalculateNextState(state, failResult, _t0.AddDays(10));

        calc.Repetitions.ShouldBe(0);
        calc.Interval.ShouldBe(1);
    }

    // ── Ease factor adjustments ───────────────────────────────────────────────

    [Fact]
    public void Easy_IncreasesEaseFactorBy0_15()
    {
        var state = FreshState();
        var calc = _sut.CalculateNextState(state, ReviewResult.Easy, _t0);
        calc.EaseFactor.ShouldBe(state.EaseFactor + 0.15, tolerance: 0.001);
    }

    [Fact]
    public void Know_IncreasesEaseFactorBy0_05()
    {
        var state = FreshState();
        var calc = _sut.CalculateNextState(state, ReviewResult.Know, _t0);
        calc.EaseFactor.ShouldBe(state.EaseFactor + 0.05, tolerance: 0.001);
    }

    [Theory]
    [InlineData(ReviewResult.Again)]
    [InlineData(ReviewResult.DontKnow)]
    public void Fail_DecreasesEaseFactorBy0_20(ReviewResult failResult)
    {
        var state = FreshState();
        double expected = Math.Max(1.3, state.EaseFactor - 0.2);
        var calc = _sut.CalculateNextState(state, failResult, _t0);
        calc.EaseFactor.ShouldBe(expected, tolerance: 0.001);
    }

    // ── Ease factor minimum clamp ─────────────────────────────────────────────

    [Fact]
    public void EaseFactor_NeverDropsBelowMinimum_After20Failures()
    {
        var state = FreshState();
        for (int i = 0; i < 20; i++)
        {
            state.UpdateState(_sut.CalculateNextState(state, ReviewResult.Again, _t0.AddDays(i)));
        }
        state.EaseFactor.ShouldBeGreaterThanOrEqualTo(1.3);
    }

    // ── NextReviewDate ────────────────────────────────────────────────────────

    [Fact]
    public void NextReviewDate_IsReviewTimePlusInterval()
    {
        var state = FreshState();
        var reviewTime = new DateTime(2026, 6, 15, 9, 30, 0, DateTimeKind.Utc);
        var calc = _sut.CalculateNextState(state, ReviewResult.Know, reviewTime);
        calc.NextReviewDate.ShouldBe(reviewTime.AddDays(calc.Interval));
    }

    // ── Guard clauses ─────────────────────────────────────────────────────────

    [Fact]
    public void NullState_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => _sut.CalculateNextState(null!, ReviewResult.Know, _t0));
    }

    // ── SrsStateCalculation.IsValid() ─────────────────────────────────────────

    [Fact]
    public void AllReviewResults_ProduceValidCalculation()
    {
        var state = FreshState();
        foreach (ReviewResult r in Enum.GetValues<ReviewResult>())
        {
            _sut.CalculateNextState(state, r, _t0).IsValid().ShouldBeTrue();
        }
    }
}
```

---

### Test suite 2 — `EmailTests.cs`

**File:** `tests/Domain.Tests/Users/EmailTests.cs`

Tests the `Email.Create()` Result pattern — the first place where user input is validated.

```csharp
using Domain.Users.ValueObjects;
using SharedKernel;
using Shouldly;

namespace Domain.Tests.Users;

public sealed class EmailTests
{
    // ── Valid inputs ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]          // case-insensitive
    [InlineData("user.name+tag@sub.domain.io")]
    public void Create_ValidEmail_ReturnsSuccess(string email)
    {
        var result = Email.Create(email);
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(email);
    }

    // ── Invalid inputs ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_NullOrWhitespace_ReturnsEmptyError(string? email)
    {
        var result = Email.Create(email!);
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Email.Empty");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@tld")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void Create_InvalidFormat_ReturnsInvalidFormatError(string email)
    {
        var result = Email.Create(email);
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Email.InvalidFormat");
    }

    // ── Value equality (record semantics) ─────────────────────────────────────

    [Fact]
    public void TwoEmailsWithSameValue_AreEqual()
    {
        var a = Email.Create("user@example.com").Value;
        var b = Email.Create("user@example.com").Value;
        a.ShouldBe(b);
    }
}
```

---

### Test suite 3 — `SynonymsTests.cs`

**File:** `tests/Domain.Tests/FlashcardCollection/SynonymsTests.cs`

Tests the `Synonyms` value object which enforces uniqueness and no-whitespace rules.

```csharp
using Domain.FlashcardCollection;
using Shouldly;

namespace Domain.Tests.FlashcardCollection;

public sealed class SynonymsTests
{
    [Fact]
    public void Create_EmptyList_IsAllowed()
    {
        var s = new Synonyms([]);
        s.Value.ShouldBeEmpty();
    }

    [Fact]
    public void Create_ValidList_StoresValues()
    {
        var s = new Synonyms(["walked", "traveled"]);
        s.Value.ShouldBe(["walked", "traveled"]);
    }

    [Fact]
    public void Create_DuplicateCaseInsensitive_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Synonyms(["walk", "WALK"]));
    }

    [Fact]
    public void Create_WhitespaceSynonym_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Synonyms(["valid", "   "]));
    }

    [Fact]
    public void Create_NullList_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new Synonyms(null!));
    }
}
```

---

### Test suite 4 — `FlashcardTests.cs`

**File:** `tests/Domain.Tests/FlashcardCollection/FlashcardTests.cs`

Tests `Flashcard.Create()` guard clauses and `AddReview()` — the core domain behaviour that integrates `SrsCalculationService`.

```csharp
using Domain.FlashcardCollection;
using Domain.FlashcardCollection.DomainServices;
using Domain.FlashcardCollection.Enums;
using Shouldly;

namespace Domain.Tests.FlashcardCollection;

public sealed class FlashcardTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid CollectionId = Guid.NewGuid();
    private readonly SrsCalculationService _srs = new();

    private static Flashcard CreateDefault() =>
        Flashcard.Create(CollectionId, "I ___ yesterday", "Byłem tu wczoraj", "was", new Synonyms([]), T0);

    // ── Flashcard.Create() guards ─────────────────────────────────────────────

    [Theory]
    [InlineData("", "translation", "answer")]
    [InlineData("sentence", "", "answer")]
    [InlineData("sentence", "translation", "")]
    public void Create_EmptyRequiredField_ThrowsArgumentException(
        string sentence, string translation, string answer)
    {
        Should.Throw<ArgumentException>(() =>
            Flashcard.Create(CollectionId, sentence, translation, answer, new Synonyms([]), T0));
    }

    [Fact]
    public void Create_NullSynonyms_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            Flashcard.Create(CollectionId, "s", "t", "a", null!, T0));
    }

    // ── Initial SrsState ──────────────────────────────────────────────────────

    [Fact]
    public void Create_InitialSrsState_HasZeroIntervalAndRepetitions()
    {
        var f = CreateDefault();
        f.SrsState.Interval.ShouldBe(0);
        f.SrsState.Repetitions.ShouldBe(0);
        f.SrsState.EaseFactor.ShouldBe(2.5);
    }

    [Fact]
    public void Create_InitialReviews_IsEmpty()
    {
        var f = CreateDefault();
        f.Reviews.ShouldBeEmpty();
    }

    // ── AddReview() ───────────────────────────────────────────────────────────

    [Fact]
    public void AddReview_Know_UpdatesSrsStateAndAddsReview()
    {
        var f = CreateDefault();
        f.AddReview(ReviewResult.Know, _srs, T0);

        f.Reviews.Count.ShouldBe(1);
        f.SrsState.Repetitions.ShouldBe(1);
        f.SrsState.Interval.ShouldBe(1);
        f.SrsState.NextReviewDate.ShouldBe(T0.AddDays(1));
    }

    [Fact]
    public void AddReview_Again_ResetsProgressAndAddsReview()
    {
        var f = CreateDefault();
        f.AddReview(ReviewResult.Know, _srs, T0);       // advance to rep=1
        f.AddReview(ReviewResult.Again, _srs, T0.AddDays(1));  // fail

        f.Reviews.Count.ShouldBe(2);
        f.SrsState.Repetitions.ShouldBe(0);
        f.SrsState.Interval.ShouldBe(1);
    }

    [Fact]
    public void AddReview_RaisesFlashcardReviewedDomainEvent()
    {
        var f = CreateDefault();
        f.AddReview(ReviewResult.Easy, _srs, T0);
        f.DomainEvents.ShouldContain(e => e.GetType().Name == "FlashcardReviewedDomainEvent");
    }

    [Fact]
    public void AddReview_NullSrsService_ThrowsArgumentNullException()
    {
        var f = CreateDefault();
        Should.Throw<ArgumentNullException>(() => f.AddReview(ReviewResult.Know, null!, T0));
    }
}
```

---

### Test suite 5 — `UserTests.cs`

**File:** `tests/Domain.Tests/Users/UserTests.cs`

Tests `User.Create()` guard clauses, auto-raised domain event, and `ISoftDeletable` behaviour.

```csharp
using Domain.Users;
using Domain.Users.ValueObjects;
using SharedKernel;
using Shouldly;

namespace Domain.Tests.Users;

public sealed class UserTests
{
    private static Email ValidEmail() => Email.Create("user@example.com").Value;

    // ── User.Create() guards ──────────────────────────────────────────────────

    [Fact]
    public void Create_NullEmail_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            User.Create(null!, "First", "Last", "hash"));
    }

    [Fact]
    public void Create_ValidInputs_ReturnsUser()
    {
        var user = User.Create(ValidEmail(), "Alice", "Smith", "hash");
        user.Email.Value.ShouldBe("user@example.com");
        user.FirstName.ShouldBe("Alice");
        user.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Create_RaisesUserRegisteredDomainEvent()
    {
        var user = User.Create(ValidEmail(), "Alice", "Smith", "hash");
        user.DomainEvents.ShouldContain(e => e.GetType().Name == "UserRegisteredDomainEvent");
    }

    [Fact]
    public void Create_IdIsNotEmpty()
    {
        var user = User.Create(ValidEmail(), "Alice", "Smith", "hash");
        user.Id.ShouldNotBe(Guid.Empty);
    }

    // ── ISoftDeletable ────────────────────────────────────────────────────────

    [Fact]
    public void Delete_SetsIsDeletedTrueAndDeletedAt()
    {
        var user = User.Create(ValidEmail(), "Alice", "Smith", "hash");
        var deletedAt = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

        user.Delete(deletedAt);

        user.IsDeleted.ShouldBeTrue();
        user.DeletedAt.ShouldBe(deletedAt);
    }
}
```

---

### Test suite 6 — `FlashcardCollectionTests.cs`

**File:** `tests/Domain.Tests/FlashcardCollectionTests.cs`

Tests `FlashcardCollection.Create()`, `Rename()`, and `Delete()`.

```csharp
using Domain.FlashcardCollection;
using Shouldly;

namespace Domain.Tests;

public sealed class FlashcardCollectionTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInputs_ReturnsCollection()
    {
        var c = FlashcardCollection.Create(AccountId, "German Basics");
        c.Name.ShouldBe("German Basics");
        c.LanguageAccountId.ShouldBe(AccountId);
        c.IsDeleted.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => FlashcardCollection.Create(AccountId, name!));
    }

    [Fact]
    public void Create_RaisesFlashcardCollectionCreatedDomainEvent()
    {
        var c = FlashcardCollection.Create(AccountId, "Test");
        c.DomainEvents.ShouldContain(e => e.GetType().Name == "FlashcardCollectionCreatedDomainEvent");
    }

    // ── Rename ────────────────────────────────────────────────────────────────

    [Fact]
    public void Rename_ValidName_UpdatesName()
    {
        var c = FlashcardCollection.Create(AccountId, "Old");
        c.Rename("New Name");
        c.Name.ShouldBe("New Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WhitespaceName_ThrowsArgumentException(string name)
    {
        var c = FlashcardCollection.Create(AccountId, "Test");
        Should.Throw<ArgumentException>(() => c.Rename(name));
    }

    // ── ISoftDeletable ────────────────────────────────────────────────────────

    [Fact]
    public void Delete_SetsIsDeletedAndDeletedAt()
    {
        var c = FlashcardCollection.Create(AccountId, "Test");
        var deletedAt = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

        c.Delete(deletedAt);

        c.IsDeleted.ShouldBeTrue();
        c.DeletedAt.ShouldBe(deletedAt);
    }
}
```

---

### Test suite 7 — `RegisterUserCommandValidatorTests.cs`

**File:** `tests/Application.Tests/Users/RegisterUserCommandValidatorTests.cs`

Tests the FluentValidation rules in `RegisterUserCommandValidator` using `TestValidate()`.

```csharp
using Application.Users.Register;
using FluentValidation.TestHelper;
using Shouldly;

namespace Application.Tests.Users;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    private static RegisterUserCommand ValidCommand() => new(
        "user@example.com", "Alice", "Smith", "SecurePass1!");

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyEmail_FailsValidation(string? email)
    {
        var cmd = ValidCommand() with { Email = email! };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyFirstName_FailsValidation(string? name)
    {
        var cmd = ValidCommand() with { FirstName = name! };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.FirstName);
    }

    [Fact]
    public void PasswordTooShort_FailsValidation()
    {
        var cmd = ValidCommand() with { Password = "abc" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void EmptyPassword_FailsValidation()
    {
        var cmd = ValidCommand() with { Password = "" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Password);
    }
}
```

> ⚠️ `RegisterUserCommand` must be a `record` with a `with` expression — verify it is not a plain `class`. If it is a class, construct separate instances instead.

---

### Test suite 8 — `AddFlashcardReviewCommandValidatorTests.cs`

**File:** `tests/Application.Tests/FlashcardCollection/AddFlashcardReviewCommandValidatorTests.cs`

```csharp
using Application.FlashcardCollection.Commands.AddFlashcardReview;
using Domain.FlashcardCollection.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.FlashcardCollection;

public sealed class AddFlashcardReviewCommandValidatorTests
{
    private readonly AddFlashcardReviewCommandValidator _validator = new();

    private static AddFlashcardReviewCommand ValidCommand() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ReviewResult.Know);

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCollectionId_FailsValidation()
    {
        var cmd = ValidCommand() with { FlaschardCollectionId = Guid.Empty };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.FlaschardCollectionId);
    }

    [Fact]
    public void EmptyFlashcardId_FailsValidation()
    {
        var cmd = ValidCommand() with { FlashcardId = Guid.Empty };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.FlashcardId);
    }

    [Fact]
    public void InvalidReviewResult_FailsValidation()
    {
        var cmd = ValidCommand() with { ReviewResult = (ReviewResult)999 };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.ReviewResult);
    }
}
```

> ⚠️ Check the actual property name on `AddFlashcardReviewCommand` — the validator uses `FlaschardCollectionId` (note the typo). Match whatever name exists in the command record.

---

### Add projects to solution

```bash
dotnet sln CleanArchitecture.slnx add tests/Domain.Tests/Domain.Tests.csproj
dotnet sln CleanArchitecture.slnx add tests/Application.Tests/Application.Tests.csproj
```

### Run all unit tests

```bash
dotnet test tests/Domain.Tests/ tests/Application.Tests/ --logger "console;verbosity=normal"
```

### Verification — expected results

| Test class | Tests | What it protects |
|------------|-------|-----------------|
| `SrsCalculationServiceTests` | ~11 | SM-2 algorithm correctness — interval, ease factor, clamp, reset |
| `EmailTests` | ~8 | Validation Result pattern — no exceptions leak to callers |
| `SynonymsTests` | ~5 | Uniqueness + whitespace rules |
| `FlashcardTests` | ~7 | `AddReview()` wires SRS + appends review + raises event |
| `UserTests` | ~5 | Guard clauses, domain event auto-raise, soft delete |
| `FlashcardCollectionTests` | ~7 | Create/Rename guards, domain event, soft delete |
| `RegisterUserCommandValidatorTests` | ~6 | FluentValidation boundary — bad input caught before handler |
| `AddFlashcardReviewCommandValidatorTests` | ~4 | Command validation |

**Total: ~53 tests, zero mocks, zero DB.**

---

---

## Point 9 — Integration Tests — Full Happy Path

### Why
The application has zero integration tests. The entire flow from HTTP request to database response is untested. This means bugs in request routing, validation, authorization, EF Core mapping, or response serialization would only be caught in production.

### New Project Structure

```
tests/
  Application.IntegrationTests/
    Application.IntegrationTests.csproj
    Infrastructure/
      IntegrationTestWebAppFactory.cs
      DatabaseFixture.cs
    Users/
      RegisterUserTests.cs
      LoginUserTests.cs
    LanguageAccounts/
      CreateLanguageAccountTests.cs
    FlashcardCollections/
      CreateCollectionTests.cs
      AddFlashcardReviewTests.cs
```

**`tests/Application.IntegrationTests/Application.IntegrationTests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
    <PackageReference Include="Testcontainers.MsSql" Version="3.9.0" />
    <PackageReference Include="Shouldly" Version="4.2.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Web.Api\Web.Api.csproj" />
  </ItemGroup>

</Project>
```

**`tests/Application.IntegrationTests/Infrastructure/IntegrationTestWebAppFactory.cs`**
```csharp
using Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Application.IntegrationTests.Infrastructure;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the real DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Register test DbContext pointing to the container
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(_dbContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Apply migrations to the test database
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
```

**`tests/Application.IntegrationTests/FlashcardCollections/AddFlashcardReviewTests.cs`**
```csharp
using System.Net;
using System.Net.Http.Json;
using Application.IntegrationTests.Infrastructure;
using Shouldly;

namespace Application.IntegrationTests.FlashcardCollections;

/// <summary>
/// Integration test covering the full happy path:
/// Register → Login → Create LanguageAccount → Create Collection → Add Flashcard → Review Flashcard
/// </summary>
public sealed class AddFlashcardReviewTests(IntegrationTestWebAppFactory factory)
    : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task FullHappyPath_RegisterToReview_ShouldSucceed()
    {
        // Step 1: Register user
        var registerResponse = await _client.PostAsJsonAsync("/users/register", new
        {
            email = "test@example.com",
            firstName = "Test",
            lastName = "User",
            password = "SecurePass123!"
        });
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Step 2: Login
        var loginResponse = await _client.PostAsJsonAsync("/users/login", new
        {
            email = "test@example.com",
            password = "SecurePass123!"
        });
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginBody!.Token);

        // Step 3: Create LanguageAccount
        var languageAccountResponse = await _client.PostAsJsonAsync("/language-accounts", new
        {
            languageId = 1 // assumes seed data or a known test language ID
        });
        languageAccountResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var languageAccountId = await languageAccountResponse.Content.ReadFromJsonAsync<Guid>();

        // Step 4: Create FlashcardCollection
        var collectionResponse = await _client.PostAsJsonAsync(
            $"/language-accounts/{languageAccountId}/collections",
            new { name = "Test Collection" });
        collectionResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var collectionId = await collectionResponse.Content.ReadFromJsonAsync<Guid>();

        // Step 5: Add Flashcard
        var flashcardResponse = await _client.PostAsJsonAsync(
            $"/language-accounts/{languageAccountId}/collections/{collectionId}/flashcards",
            new
            {
                sentenceWithBlanks = "I ___ to the store",
                translation = "Poszedłem do sklepu",
                answer = "went",
                synonyms = new[] { "walked", "traveled" }
            });
        flashcardResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var flashcardId = await flashcardResponse.Content.ReadFromJsonAsync<Guid>();

        // Step 6: Review Flashcard — assert SRS state advances
        var reviewResponse = await _client.PostAsJsonAsync(
            $"/language-accounts/{languageAccountId}/collections/{collectionId}/flashcards/{flashcardId}/reviews",
            new { reviewResult = "Know" });
        reviewResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private record LoginResponse(string Token);
}
```

> ⚠️ Adjust URL paths to match actual endpoint routes defined in `src/Web.Api/Endpoints/`. Run `grep -r "MapPost\|MapGet" src/Web.Api/Endpoints/` to discover the actual route patterns.

### Verification
```bash
dotnet test tests/Application.IntegrationTests/ --logger "console;verbosity=detailed"
```

---

---

## Point 10 — Replace Guid.NewGuid() with Guid.CreateVersion7()

### Why
`Guid.NewGuid()` generates random (version 4) GUIDs. When used as a clustered primary key in SQL Server, random GUIDs cause **page fragmentation** — every new row is inserted at a random position in the B-tree index, not at the end. This degrades write performance at scale.

`Guid.CreateVersion7()` (available since .NET 9) generates **time-ordered** UUIDs (RFC 9562). They are monotonically increasing within a millisecond, so new rows are always appended near the end of the index — no fragmentation.

### Files to Modify

All entity constructors that call `Guid.NewGuid()`:

**`src/Domain/Users/User.cs`**
```csharp
private User(Email email, string firstName, string lastName, string passwordHash)
{
    Id = Guid.CreateVersion7();  // ← was Guid.NewGuid()
    // ...
}
```

**`src/Domain/FlashcardCollection/FlashcardCollection.cs`**
```csharp
private FlashcardCollection(Guid languageAccountId, string name)
{
    Id = Guid.CreateVersion7();  // ← was Guid.NewGuid()
    // ...
}
```

**`src/Domain/FlashcardCollection/Flashcard.cs`**
```csharp
private Flashcard(Guid flashcardCollectionId, string sentenceWithBlanks, ...)
{
    Id = Guid.CreateVersion7();  // ← was Guid.NewGuid()
    // ...
}
```

**`src/Domain/LanguageAccount/LanguageAccount.cs`** (and any other entity)
```csharp
Id = Guid.CreateVersion7();
```

> ⚠️ `Guid.CreateVersion7()` requires **.NET 9+**. Check `src/Web.Api/Web.Api.csproj` and `Directory.Build.props` for `<TargetFramework>`. If the project targets .NET 8, skip this point or use the `UuidV7` NuGet package.

### Verification
- Insert a few rows and inspect the `Id` columns — they should be lexicographically increasing with time
- Architecture tests still pass
- No functional change to application behavior

---

---

## Point 11 — SRS Magic Numbers to Named Constants

### Why
The SRS algorithm contains hardcoded numeric values (`0.15`, `0.05`, `0.2`, `1`, `3`) with no link to the SM-2 algorithm specification. A developer maintaining this code has no way to know what these values mean or where they come from without consulting external documentation.

### Files to Modify

**`src/Domain/FlashcardCollection/DomainServices/SrsCalculationService.cs`**

Add named constants at the top of the class, then replace all magic numbers with the constants:

```csharp
/// <summary>
/// Domain service responsible for calculating SRS (Spaced Repetition System) state
/// based on the SuperMemo SM-2 algorithm.
/// Reference: https://www.supermemo.com/en/archives1990-2015/english/ol/sm2
/// </summary>
public sealed class SrsCalculationService
{
    // ── SM-2 Algorithm Constants ──────────────────────────────────────────────
    // Source: SuperMemo SM-2 paper. Quality values: q=5 (Easy), q=4 (Know), q<3 (Fail)

    /// <summary>Minimum allowed ease factor (EF). SM-2 defines EF_min = 1.3</summary>
    private const double MinEaseFactor = 1.3;

    /// <summary>
    /// Ease factor bonus for "Easy" response (SM-2: q=5).
    /// Increases how quickly interval grows on subsequent reviews.
    /// </summary>
    private const double EaseFactorBonusEasy = 0.15;

    /// <summary>
    /// Ease factor bonus for "Know" response (SM-2: q=4).
    /// Small positive reinforcement for correct recall.
    /// </summary>
    private const double EaseFactorBonusKnow = 0.05;

    /// <summary>
    /// Ease factor penalty for failed review (SM-2: q less than 3).
    /// Applied when the user selected "Again" or "DontKnow".
    /// </summary>
    private const double EaseFactorPenaltyFail = 0.2;

    /// <summary>Interval in days after the first successful review (SM-2 standard: 1 day)</summary>
    private const int FirstSuccessInterval = 1;

    /// <summary>Interval in days after the second successful review (SM-2 standard: 6 days, adjusted to 3 here)</summary>
    private const int SecondSuccessInterval = 3;

    /// <summary>Reset interval in days after a failed review (SM-2: restart from 1)</summary>
    private const int FailResetInterval = 1;

    // ── Algorithm ────────────────────────────────────────────────────────────

    public SrsStateCalculation CalculateNextState(
        SrsState currentState,
        ReviewResult reviewResult,
        DateTime currentTime)
    {
        ArgumentNullException.ThrowIfNull(currentState);

        int newInterval;
        double newEaseFactor = currentState.EaseFactor;
        int newRepetitions;

        if (reviewResult is ReviewResult.Again or ReviewResult.DontKnow)
        {
            newRepetitions = 0;
            newInterval = FailResetInterval;
            newEaseFactor = Math.Max(MinEaseFactor, currentState.EaseFactor - EaseFactorPenaltyFail);
        }
        else
        {
            newRepetitions = currentState.Repetitions + 1;

            newInterval = newRepetitions switch
            {
                1 => FirstSuccessInterval,
                2 => SecondSuccessInterval,
                _ => (int)Math.Round(currentState.Interval * currentState.EaseFactor)
            };

            if (reviewResult is ReviewResult.Easy)
                newEaseFactor += EaseFactorBonusEasy;
            else if (reviewResult is ReviewResult.Know)
                newEaseFactor += EaseFactorBonusKnow;
        }

        newEaseFactor = Math.Max(MinEaseFactor, newEaseFactor);

        return new SrsStateCalculation(
            newInterval,
            newEaseFactor,
            newRepetitions,
            currentTime.AddDays(newInterval));
    }
}
```

### Verification
- All existing unit tests from Point 8 still pass
- No behavioral change — only readability improvement

---

## Point 12 — Soft Delete Pattern (ISoftDeletable)

### Why
Three delete handlers call `Remove()` which physically deletes records and all cascade-linked data. This destroys historical review data permanently, breaks audit trails, and makes accidental deletions unrecoverable.

**Scope decision:** Soft delete is applied **only to aggregate roots where deletion is a user-facing action** — NOT to child entities (their lifecycle is governed by the aggregate root):

| Entity | Soft Delete | Reason |
|--------|-------------|--------|
| `User` | ✅ Yes | Account management, recovery, GDPR audit |
| `LanguageAccount` | ✅ Yes | Cascade risk — owns FlashcardCollections |
| `FlashcardCollection` | ✅ Yes | User-facing deletion, contains valuable history |
| `Flashcard` | ❌ No | Child of `FlashcardCollection` — governed by aggregate |
| `FlashcardReview` | ❌ No | Immutable historical record — never delete |
| `SrsState` | ❌ No | 1-to-1 with `Flashcard` — no independent lifecycle |

---

### Complete File Inventory

#### Files to Create
| File | Change |
|------|--------|
| `src/SharedKernel/ISoftDeletable.cs` | New interface |

#### Domain — Add `IsDeleted`, `DeletedAt`, `Delete()` to each
| File | Change |
|------|--------|
| `src/Domain/Users/User.cs` | Implement `ISoftDeletable` |
| `src/Domain/LanguageAccount/LanguageAccount.cs` | Implement `ISoftDeletable` |
| `src/Domain/FlashcardCollection/FlashcardCollection.cs` | Implement `ISoftDeletable` |

#### Infrastructure — EF Core global query filter + hard-delete interceptor
| File | Change |
|------|--------|
| `src/Infrastructure/Database/ApplicationDbContext.cs` | `OnModelCreating` + `SaveChangesAsync` |

#### Command Handlers — Replace `Remove()` with `entity.Delete()`
| File | Current Call | Fix |
|------|-------------|-----|
| `src/Application/LanguageAccounts/Commands/DeleteLanguageAccount/DeleteLanguageAccountCommandHandler.cs` | `languageAccountRepository.Remove(account)` | `account.Delete(dateTimeProvider.UtcNow)` |
| `src/Application/FlashcardCollection/Commands/DeleteFlashcardCollection/DeleteFlashcardCollectionCommandHandler.cs` | `flashcardCollectionRepository.Remove(collection)` | `collection.Delete(dateTimeProvider.UtcNow)` |

> ℹ️ `DeleteFlashcardCommandHandler` calls `flashcardRepository.Remove(flashcard)` — **no change needed** because `Flashcard` is NOT soft-deleted (it is a child governed by the collection).

#### Dapper Read Repositories — Add `IsDeleted = 0` filters to SQL
The EF Core global query filter only applies to EF Core queries. Every Dapper query that touches a soft-deletable table must be updated manually:

| File | Method | Tables needing `AND x.IsDeleted = 0` |
|------|--------|--------------------------------------|
| `src/Infrastructure/Users/UserReadRepository.cs` | `GetByEmailAsync` | `Users` |
| `src/Infrastructure/Users/UserReadRepository.cs` | `GetById` | `Users` |
| `src/Infrastructure/Users/UserReadRepository.cs` | `GetByEmailForAuthAsync` | `Users` |
| `src/Infrastructure/LanguageAccount/LanguageAccountReadRepository.cs` | `GetByUserIdAsync` | `LanguageAccounts` |
| `src/Infrastructure/LanguageAccount/LanguageAccountReadRepository.cs` | `GetByIdAsync` | `LanguageAccounts` |
| `src/Infrastructure/FlashcardCollection/FlashcardCollectionReadRepository.cs` | `GetLanguageAccountUserIdAsync` | `LanguageAccounts` |
| `src/Infrastructure/FlashcardCollection/FlashcardCollectionReadRepository.cs` | `GetByLanguageAccountIdAsync` | `FlashcardCollections` |
| `src/Infrastructure/FlashcardCollection/FlashcardCollectionReadRepository.cs` | `GetByIdAsync` | `FlashcardCollections`, `LanguageAccounts` (joined) |
| `src/Infrastructure/FlashcardCollection/FlashcardCollectionReadRepository.cs` | `GetFlashcardByIdAsync` | `FlashcardCollections`, `LanguageAccounts` (joined) |
| `src/Infrastructure/FlashcardCollection/FlashcardCollectionReadRepository.cs` | `GetDueFlashcardsAsync` | `FlashcardCollections`, `LanguageAccounts` (joined) |

#### EF Core Configurations — Cascade delete behaviour to review
The current cascade configs will physically delete child rows when the parent is removed. After soft delete, `Remove()` is never called on soft-deletable entities, so these configs no longer trigger for `LanguageAccount` and `FlashcardCollection`. No change needed, but verify:

| File | Current cascade | Status after Point 12 |
|------|----------------|----------------------|
| `src/Infrastructure/FlashcardCollection/FlashcardCollectionConfiguration.cs` | `OnDelete(DeleteBehavior.Cascade)` for `LanguageAccount → FlashcardCollection` | ✅ Safe — `LanguageAccount.Remove()` is never called |
| `src/Infrastructure/FlashcardCollection/FlashcardCollectionConfiguration.cs` | `OnDelete(DeleteBehavior.Cascade)` for `FlashcardCollection → Flashcard` | ✅ Safe — `FlashcardCollection.Remove()` is never called |
| `src/Infrastructure/FlashcardCollection/FlashcardConfiguration.cs` | `OnDelete(DeleteBehavior.Cascade)` for `Flashcard → FlashcardReview` | ✅ Unchanged — `Flashcard` still supports hard delete |

#### Migration
```bash
dotnet ef migrations add AddSoftDeleteFields --project src/Infrastructure --startup-project src/Web.Api
dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
```

---

### How

**Step 1 — Create `ISoftDeletable` interface:**
```csharp
// src/SharedKernel/ISoftDeletable.cs
namespace SharedKernel;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    void Delete(DateTime utcNow);
}
```

**Step 2 — Implement on `User`, `LanguageAccount`, `FlashcardCollection`:**

Add to each entity (same pattern for all three):
```csharp
// Example shown for User.cs — apply identically to LanguageAccount.cs and FlashcardCollection.cs
public bool IsDeleted { get; private set; }
public DateTime? DeletedAt { get; private set; }

public void Delete(DateTime utcNow)
{
    IsDeleted = true;
    DeletedAt = utcNow;
}
```
Then add `: ISoftDeletable` to each class declaration.

**Step 3 — Global query filter + hard-delete intercept in `ApplicationDbContext`:**
```csharp
// In OnModelCreating:
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var filter = Expression.Lambda(Expression.Not(property), parameter);
        entityType.SetQueryFilter(filter);
    }
}

// In SaveChangesAsync, BEFORE PublishDomainEventsAsync:
foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
    .Where(e => e.State == EntityState.Deleted))
{
    entry.State = EntityState.Modified;
    entry.Entity.Delete(dateTimeProvider.UtcNow);
}
```

Also add `using System.Linq.Expressions;` to the file.

**Step 4 — Update the two delete command handlers:**
```csharp
// DeleteLanguageAccountCommandHandler.cs
// BEFORE:
languageAccountRepository.Remove(account);
await applicationDbContext.SaveChangesAsync(cancellationToken);

// AFTER:
account.Delete(dateTimeProvider.UtcNow);
await applicationDbContext.SaveChangesAsync(cancellationToken);
```

```csharp
// DeleteFlashcardCollectionCommandHandler.cs
// BEFORE:
flashcardCollectionRepository.Remove(collection);
await applicationDbContext.SaveChangesAsync(cancellationToken);

// AFTER:
collection.Delete(dateTimeProvider.UtcNow);
await applicationDbContext.SaveChangesAsync(cancellationToken);
```

Both handlers need `IDateTimeProvider dateTimeProvider` added to their primary constructor parameters.

**Step 5 — Add `AND IsDeleted = 0` to every Dapper query touching soft-deletable tables:**

```csharp
// UserReadRepository.cs — all three methods
WHERE Email = @Email AND IsDeleted = 0
WHERE Id = @UserId AND IsDeleted = 0

// LanguageAccountReadRepository.cs
WHERE UserId = @UserId AND IsDeleted = 0
WHERE Id = @Id AND IsDeleted = 0

// FlashcardCollectionReadRepository.cs — GetLanguageAccountUserIdAsync
WHERE Id = @LanguageAccountId AND IsDeleted = 0

// FlashcardCollectionReadRepository.cs — GetByLanguageAccountIdAsync
WHERE LanguageAccountId = @LanguageAccountId AND IsDeleted = 0

// FlashcardCollectionReadRepository.cs — GetByIdAsync (multi-table join)
FROM dbo.FlashcardCollections fc
INNER JOIN dbo.LanguageAccounts la ON la.Id = fc.LanguageAccountId
WHERE fc.Id = @Id
  AND fc.IsDeleted = 0        -- ← add
  AND la.IsDeleted = 0        -- ← add

// FlashcardCollectionReadRepository.cs — GetFlashcardByIdAsync (multi-table join)
FROM dbo.Flashcards f
INNER JOIN dbo.FlashcardCollections fc ON fc.Id = f.FlashcardCollectionId
INNER JOIN dbo.LanguageAccounts la ON la.Id = fc.LanguageAccountId
WHERE f.Id = @FlashcardId
  AND fc.IsDeleted = 0        -- ← add
  AND la.IsDeleted = 0        -- ← add

// FlashcardCollectionReadRepository.cs — GetDueFlashcardsAsync (multi-table join)
FROM dbo.Flashcards f
INNER JOIN dbo.FlashcardCollections fc ON fc.Id = f.FlashcardCollectionId
INNER JOIN dbo.LanguageAccounts la ON la.Id = fc.LanguageAccountId
WHERE f.FlashcardCollectionId = @CollectionId
  AND la.UserId = @UserId
  AND fc.IsDeleted = 0        -- ← add
  AND la.IsDeleted = 0        -- ← add
  AND (ss.NextReviewDate IS NULL OR ss.NextReviewDate <= GETUTCDATE())
```

### Verification
- `GET /language-accounts/{id}` returns 404 after delete (global EF filter active)
- `GET /language-accounts` list excludes deleted accounts (Dapper filter active)
- `GET /flashcard-collections/{id}` returns 404 after collection delete (Dapper filter on `fc.IsDeleted = 0`)
- `GET /flashcards/{id}` returns 404 when parent collection or account is deleted (joined Dapper filters)
- `GET /flashcards/due` returns empty list when collection or account is deleted
- Login still works for a soft-deleted user (deliberate — auth check should happen before soft-delete check; adjust `GetByEmailForAuthAsync` filter if account deactivation should block login)
- Records still exist in DB with `IsDeleted = true`, `DeletedAt` set
- No cascade physical delete occurs on `FlashcardCollections` or `Flashcards` when `LanguageAccount` is soft-deleted

---

## Summary Checklist

Use this as a quick status tracker:

| # | Point | Status |
|---|-------|--------|
| 1 | Remove TodoItem boilerplate | ☐ |
| 2 | Add RowVersion + Audit Fields to Entity | ☐ |
| 3 | Handle DbUpdateConcurrencyException | ☐ |
| 4 | Fix Outbox Transaction Safety | ☐ |
| 5 | Move Domain Events into Entity Factories | ☐ |
| 6 | Refactor IApplicationDbContext | ☐ |
| 7 | Fix Email Value Object | ☐ |
| 8 | Unit Tests for SrsCalculationService | ☐ |
| 9 | Integration Tests | ☐ |
| 10 | Replace Guid.NewGuid() with Guid.CreateVersion7() | ☐ |
| 11 | SRS Magic Numbers to Named Constants | ☐ |
| 12 | Soft Delete Pattern (ISoftDeletable) | ☐ |

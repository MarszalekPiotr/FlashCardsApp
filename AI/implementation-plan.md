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

## Point 8 — Unit Tests for SrsCalculationService

### Why
`SrsCalculationService` implements the SM-2 spaced repetition algorithm which is the **core business logic** of the application. Currently it has **zero tests**. Any refactoring could silently break the algorithm and no one would know until users report incorrect review schedules.

### New Project / Test File

Create a new test project (or add to an existing `Domain.Tests` project):

```
tests/
  Domain.Tests/
    Domain.Tests.csproj
    FlashcardCollection/
      SrsCalculationServiceTests.cs
```

**`tests/Domain.Tests/Domain.Tests.csproj`**
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
    <PackageReference Include="Shouldly" Version="4.2.1" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Domain\Domain.csproj" />
  </ItemGroup>

</Project>
```

**`tests/Domain.Tests/FlashcardCollection/SrsCalculationServiceTests.cs`**
```csharp
using Domain.FlashcardCollection;
using Domain.FlashcardCollection.DomainServices;
using Domain.FlashcardCollection.Enums;
using Shouldly;

namespace Domain.Tests.FlashcardCollection;

public sealed class SrsCalculationServiceTests
{
    private readonly SrsCalculationService _sut = new();
    private readonly DateTime _referenceTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    private static SrsState CreateSrsState(
        int interval = 1,
        double easeFactor = 2.5,
        int repetitions = 0) =>
        // SrsState.CreateInitialState sets known defaults; we bypass it
        // by using the internal test helper or reflection if needed.
        // If SrsState has no test-friendly factory, use CreateInitialState and
        // then call UpdateState to set the desired values.
        SrsState.CreateInitialState(Guid.NewGuid(), DateTime.UtcNow);

    // ─────────────────────────────────────────────────────────────
    // FIRST REVIEW (repetitions == 0)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateNextState_FirstReview_Easy_SetsIntervalTo1()
    {
        // Arrange
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);

        // Act
        var result = _sut.CalculateNextState(state, ReviewResult.Easy, _referenceTime);

        // Assert
        result.Interval.ShouldBe(1);
        result.Repetitions.ShouldBe(1);
        result.EaseFactor.ShouldBeGreaterThan(2.5); // Easy increases ease
        result.NextReviewDate.ShouldBe(_referenceTime.AddDays(1));
    }

    [Fact]
    public void CalculateNextState_FirstReview_Know_SetsIntervalTo1()
    {
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);

        var result = _sut.CalculateNextState(state, ReviewResult.Know, _referenceTime);

        result.Interval.ShouldBe(1);
        result.Repetitions.ShouldBe(1);
        result.NextReviewDate.ShouldBe(_referenceTime.AddDays(1));
    }

    // ─────────────────────────────────────────────────────────────
    // FAILED REVIEW — resets progress
    // ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ReviewResult.Again)]
    [InlineData(ReviewResult.DontKnow)]
    public void CalculateNextState_FailedReview_ResetsRepetitionsAndInterval(ReviewResult result)
    {
        // Arrange — simulate a card with existing progress
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);
        // Advance state to repetitions=3, interval=10 via two successful reviews
        var after1 = _sut.CalculateNextState(state, ReviewResult.Know, _referenceTime);
        state.UpdateState(after1);
        var after2 = _sut.CalculateNextState(state, ReviewResult.Know, _referenceTime.AddDays(1));
        state.UpdateState(after2);
        var after3 = _sut.CalculateNextState(state, ReviewResult.Easy, _referenceTime.AddDays(4));
        state.UpdateState(after3);
        // state.Repetitions should now be 3

        // Act — fail
        var failResult = _sut.CalculateNextState(state, result, _referenceTime.AddDays(10));

        // Assert
        failResult.Repetitions.ShouldBe(0);
        failResult.Interval.ShouldBe(1);
        failResult.EaseFactor.ShouldBeGreaterThanOrEqualTo(1.3); // minimum clamped
    }

    // ─────────────────────────────────────────────────────────────
    // EASE FACTOR ADJUSTMENTS
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateNextState_Easy_IncreasesEaseFactorBy015()
    {
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);
        double originalEase = state.EaseFactor;

        var result = _sut.CalculateNextState(state, ReviewResult.Easy, _referenceTime);

        result.EaseFactor.ShouldBe(originalEase + 0.15, tolerance: 0.001);
    }

    [Fact]
    public void CalculateNextState_Know_IncreasesEaseFactorBy005()
    {
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);
        double originalEase = state.EaseFactor;

        var result = _sut.CalculateNextState(state, ReviewResult.Know, _referenceTime);

        result.EaseFactor.ShouldBe(originalEase + 0.05, tolerance: 0.001);
    }

    [Theory]
    [InlineData(ReviewResult.Again)]
    [InlineData(ReviewResult.DontKnow)]
    public void CalculateNextState_Fail_DecreasesEaseFactorBy02(ReviewResult reviewResult)
    {
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);
        double originalEase = state.EaseFactor;
        double expected = Math.Max(1.3, originalEase - 0.2);

        var result = _sut.CalculateNextState(state, reviewResult, _referenceTime);

        result.EaseFactor.ShouldBe(expected, tolerance: 0.001);
    }

    // ─────────────────────────────────────────────────────────────
    // MINIMUM EASE FACTOR CLAMP
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateNextState_EaseFactorNeverDropsBelowMinimum()
    {
        // Simulate many failures to try to drive EaseFactor below 1.3
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);

        for (int i = 0; i < 20; i++)
        {
            var result = _sut.CalculateNextState(state, ReviewResult.Again, _referenceTime.AddDays(i));
            state.UpdateState(result);
        }

        state.EaseFactor.ShouldBeGreaterThanOrEqualTo(1.3);
    }

    // ─────────────────────────────────────────────────────────────
    // INTERVAL PROGRESSION — SM-2 STANDARD SEQUENCE
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateNextState_SecondSuccessfulReview_SetsIntervalTo3()
    {
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);

        // First review
        var r1 = _sut.CalculateNextState(state, ReviewResult.Know, _referenceTime);
        state.UpdateState(r1);

        // Second review
        var r2 = _sut.CalculateNextState(state, ReviewResult.Know, _referenceTime.AddDays(1));

        r2.Interval.ShouldBe(3);
        r2.Repetitions.ShouldBe(2);
    }

    [Fact]
    public void CalculateNextState_ThirdSuccessfulReview_UsesEaseFactorMultiplier()
    {
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);

        var r1 = _sut.CalculateNextState(state, ReviewResult.Know, _referenceTime);
        state.UpdateState(r1);
        var r2 = _sut.CalculateNextState(state, ReviewResult.Know, _referenceTime.AddDays(1));
        state.UpdateState(r2);
        var r3 = _sut.CalculateNextState(state, ReviewResult.Know, _referenceTime.AddDays(4));

        // interval = round(3 * easeFactor)
        int expected = (int)Math.Round(3 * state.EaseFactor);
        r3.Interval.ShouldBe(expected);
    }

    // ─────────────────────────────────────────────────────────────
    // NEXT REVIEW DATE
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateNextState_NextReviewDate_IsCurrentTimePlusInterval()
    {
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);
        var reviewTime = new DateTime(2026, 6, 15, 9, 30, 0, DateTimeKind.Utc);

        var result = _sut.CalculateNextState(state, ReviewResult.Know, reviewTime);

        result.NextReviewDate.ShouldBe(reviewTime.AddDays(result.Interval));
    }

    [Fact]
    public void CalculateNextState_MidnightReview_NextReviewDateIsCorrect()
    {
        // Regression: ensure midnight boundary doesn't cause off-by-one
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);
        var midnight = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = _sut.CalculateNextState(state, ReviewResult.Know, midnight);

        result.NextReviewDate.ShouldBe(midnight.AddDays(result.Interval));
        result.NextReviewDate.Hour.ShouldBe(0);
        result.NextReviewDate.Minute.ShouldBe(0);
    }

    // ─────────────────────────────────────────────────────────────
    // VALIDATION
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateNextState_Result_IsAlwaysValid()
    {
        var state = SrsState.CreateInitialState(Guid.NewGuid(), _referenceTime);

        foreach (ReviewResult reviewResult in Enum.GetValues<ReviewResult>())
        {
            var result = _sut.CalculateNextState(state, reviewResult, _referenceTime);
            result.IsValid().ShouldBeTrue($"Result should be valid for ReviewResult.{reviewResult}");
        }
    }

    [Fact]
    public void CalculateNextState_NullState_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => _sut.CalculateNextState(null!, ReviewResult.Know, _referenceTime));
    }
}
```

> ⚠️ **Important:** `SrsState.UpdateState(SrsStateCalculation)` must have `internal` or `public` visibility for tests to advance state between reviews. Check `SrsState.cs` — if `UpdateState` is `internal`, add `[assembly: InternalsVisibleTo("Domain.Tests")]` to `src/Domain/Domain.csproj` or make the method `public`.

### Add Project to Solution

```bash
dotnet sln CleanArchitecture.slnx add tests/Domain.Tests/Domain.Tests.csproj
```

### Verification
```bash
dotnet test tests/Domain.Tests/
```
All tests should pass. If `SrsState` doesn't expose enough for testing, expose a test-only factory or use `InternalsVisibleTo`.

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
`DeleteLanguageAccountCommandHandler` calls `Remove()` which physically deletes the record and all cascade-linked data (FlashcardCollections, Flashcards, SrsStates, FlashcardReviews). This destroys historical review data permanently, breaks audit trails, and makes accidental deletions unrecoverable.

**Scope decision:** Soft delete is applied **only to aggregate roots where deletion is a user-facing action** — NOT to child entities (their lifecycle is governed by the aggregate root):

| Entity | Soft Delete | Reason |
|--------|-------------|--------|
| `User` | ✅ Yes | Account management, recovery, GDPR audit |
| `LanguageAccount` | ✅ Yes | Explicitly called out in `cr` — cascade risk |
| `FlashcardCollection` | ✅ Yes | User-facing deletion, contains valuable history |
| `Flashcard` | ❌ No | Child of `FlashcardCollection` — governed by aggregate |
| `FlashcardReview` | ❌ No | Immutable historical record — never delete |
| `SrsState` | ❌ No | 1-to-1 with `Flashcard` — no independent lifecycle |

### Where
1. `src/SharedKernel/ISoftDeletable.cs` — **new file**
2. `src/Domain/Users/User.cs` — implement `ISoftDeletable`, add `Delete()` method
3. `src/Domain/LanguageAccount/LanguageAccount.cs` — implement `ISoftDeletable`, add `Delete()` method
4. `src/Domain/FlashcardCollection/FlashcardCollection.cs` — implement `ISoftDeletable`, add `Delete()` method
5. `src/Infrastructure/Database/ApplicationDbContext.cs` — global query filter + intercept hard deletes in `SaveChangesAsync`
6. `src/Application/LanguageAccounts/Commands/DeleteLanguageAccount/DeleteLanguageAccountCommandHandler.cs` — replace `Remove()` with `entity.Delete()`
7. Migration: `AddSoftDeleteFields`

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

**Step 2 — Implement on `User`:**
```csharp
// Add to User.cs
public bool IsDeleted { get; private set; }
public DateTime? DeletedAt { get; private set; }

public void Delete(DateTime utcNow)
{
    IsDeleted = true;
    DeletedAt = utcNow;
}
```
Apply same pattern to `LanguageAccount` and `FlashcardCollection`.

**Step 3 — Global query filter + intercept in `ApplicationDbContext`:**
```csharp
// In OnModelCreating — apply filter to all ISoftDeletable entities
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

// In SaveChangesAsync — convert hard deletes to soft deletes
foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
    .Where(e => e.State == EntityState.Deleted))
{
    entry.State = EntityState.Modified;
    entry.Entity.Delete(dateTimeProvider.UtcNow);
}
```

**Step 4 — Update `DeleteLanguageAccountCommandHandler`:**
```csharp
// Before (hard delete)
languageAccountRepository.Remove(languageAccount);

// After (soft delete — Remove() now triggers the interceptor automatically,
// OR call Delete() explicitly and keep the entity in Modified state)
languageAccount.Delete(dateTimeProvider.UtcNow);
```

> ⚠️ If you use the `SaveChangesAsync` interceptor approach, calling `Remove()` will automatically be converted — no handler change needed. But explicit `entity.Delete()` is cleaner and more readable.

### Verification
- `GET /language-accounts/{id}` returns 404 after delete (filtered by global query filter)
- Record still exists in DB with `IsDeleted = true`, `DeletedAt` set
- `FlashcardCollections` linked to deleted `LanguageAccount` are still physically intact
- No cascade physical delete occurs

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

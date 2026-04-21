# FlashCardsApp — Implementation Review
**Date:** 2026-04-18  
**Reviewer:** Senior .NET Developer  
**Reference Plan:** `architectural-review-and-refactoring-plan.md` + `ddd-implementation-review.md`  
**Status:** Post-refactor review — what was fixed, what is still broken, and what new bugs were introduced

---

## Overview

Good progress has been made since the original review. Several foundational DDD violations were corrected. However, the refactoring introduced a set of new — and in some cases critical — bugs that need to be addressed before the application can function correctly. The outbox pattern is known to be missing and is tagged as a future task; every other issue in this document should be treated as a genuine defect.

---

## ✅ What Was Successfully Fixed

Before diving into problems, let's acknowledge what was properly addressed:

| Issue from Original Review | Status |
|---|---|
| Bounded context violation: SRS imported from LanguageAccount | ✅ Fixed — SRS merged into `FlashcardCollection` context |
| Duplicate domain models: `LanguageAccount.FlashcardReview` | ✅ Fixed — only `FlashcardCollection.FlashcardReview` remains |
| Repository creating domain entities (`UserRepository.CreateUser`) | ✅ Fixed — now uses `AddAsync(User user)` |
| `LanguageAccount` containing `FlashcardCollection` as a child aggregate | ✅ Fixed — both are now independent aggregate roots |
| `DateTime.UtcNow` hardcoded in domain logic | ✅ Fixed — `IDateTimeProvider` passed as a parameter |
| Authorization specifications introduced | ✅ Partially fixed — see issue #5 below |

---

## 🔴 CRITICAL ISSUES — Application Is Broken At Runtime

These are defects that either prevent the application from starting or cause silent data corruption. They must be fixed before any testing.

---

### Issue #1 — `CreateFlashcardCollectionCommandHandler`: Collection Is Created But Never Persisted

**File:** `src/Application/FlashcardCollection/Commands/CreateFlashcardCollection/CreateFlashcardCollectionCommandHandler.cs`

**What happens:**

```csharp
// ✅ Collection object is created in memory...
Domain.FlashcardCollection.FlashcardCollection collection =
    Domain.FlashcardCollection.FlashcardCollection.Create(languageAccount.Id, command.Name);

// ❌ ...but it is NEVER added to a repository or DbContext!

await applicationDbContext.SaveChangesAsync(cancellationToken); // saves nothing related to this collection

collection.Raise(new FlashcardCollectionCreatedDomainEvent(collection.Id)); // event too late, Id is Guid.Empty

return collection.Id; // returns Guid.Empty
```

**Why this is critical:**  
The collection exists only in local memory. `SaveChangesAsync` has nothing to persist because the entity was never tracked by EF Core. The method returns `Guid.Empty` to the caller. Every `CreateFlashcardCollection` request silently succeeds with no data written to the database.

**What it should look like:**

```csharp
// You need to also inject IFlashcardCollectionRepository and call Add()
Domain.FlashcardCollection.FlashcardCollection collection =
    Domain.FlashcardCollection.FlashcardCollection.Create(languageAccount.Id, command.Name);

flashcardCollectionRepository.Add(collection); // ← this line is missing

collection.Raise(new FlashcardCollectionCreatedDomainEvent(collection.Id));
// (and the event must be raised BEFORE SaveChangesAsync — see Issue #2)

await applicationDbContext.SaveChangesAsync(cancellationToken);
return collection.Id;
```

---

### Issue #2 — All Domain Events Are Orphaned: They Are Raised After `SaveChangesAsync` Completes

**Files affected:**
- `RegisterUserCommandHandler.cs`
- `CreateLanguageAccountCommandHandler.cs`
- `CreateFlashcardCollectionCommandHandler.cs`
- `AddFlashcardToCollectionCommandHandler.cs`
- `AddFlashcardReviewCommandHandler.cs`

**This is the single most impactful bug in the current codebase.**

The `ApplicationDbContext.SaveChangesAsync` is implemented as follows — and this is correct:

```csharp
// ApplicationDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    await PublishDomainEventsAsync(); // ← Step 1: collect and dispatch events
    int result = await base.SaveChangesAsync(cancellationToken); // ← Step 2: commit
    return result;
}
```

This means: **all events must be raised on entities BEFORE `SaveChangesAsync` is called**, so that `PublishDomainEventsAsync` can pick them up.

Instead, every handler does the exact opposite — it raises events AFTER the save:

```csharp
// RegisterUserCommandHandler.cs — representative example
await userWriteRepository.AddAsync(user);
await applicationDbContext.SaveChangesAsync(cancellationToken); // ← events dispatched here (user has 0 events)

user.Raise(new UserRegisteredDomainEvent(user.Id)); // ← event added AFTER dispatch — lost forever
```

The call flow is:
1. `SaveChangesAsync` calls `PublishDomainEventsAsync` — no events exist yet on any entity.
2. The database is committed.
3. `user.Raise(...)` is called — the event sits on the entity object.
4. **Nobody ever calls `PublishDomainEventsAsync` again.** The event is orphaned.

**Real-world consequence:**  
`AddFlashcardReviewCommandHandler` saves a `FlashcardReview` record, then raises `FlashcardReviewedDomainEvent` — which is intended to trigger `FlashcardReviewedDomainEventHandler`, which updates the `SrsState`. Because the event is never dispatched, the SRS state is **never updated**. The entire SRS algorithm is silently broken.

**The fix is simple — raise events before the save:**

```csharp
// Every handler should follow this pattern:
flashcardCollectionRepository.Add(collection);

collection.Raise(new FlashcardCollectionCreatedDomainEvent(collection.Id)); // ← BEFORE save

await applicationDbContext.SaveChangesAsync(cancellationToken); // ← dispatches and commits together

return collection.Id;
```

Alternatively — and this is the architecturally cleaner choice — domain entities should raise events inside their own factory methods or business methods, not in the command handler:

```csharp
// In FlashcardCollection.cs
public static FlashcardCollection Create(Guid languageAccountId, string name)
{
    var collection = new FlashcardCollection(languageAccountId, name);
    collection.Raise(new FlashcardCollectionCreatedDomainEvent(collection.Id)); // ← here
    return collection;
}
```

The handler then just adds the entity and saves. This is the correct DDD approach — the aggregate root is responsible for raising its own events.

> **Note on the Outbox Pattern:** The current `PublishDomainEventsAsync` approach (publish-then-save in one round trip) is the short-term fix. The long-term solution — the Outbox Pattern — is acknowledged as a future task. When the outbox is implemented, events will be written to an `OutboxMessages` table atomically with the domain change, then dispatched by a background worker.

---

### Issue #3 — `IFlashcardReviewRepository` Has No Infrastructure Implementation and Is Not Registered in DI

**Files:**
- `src/Application/FlashcardCollection/IFlashcardReviewRepository.cs` — interface exists
- Expected location: `src/Infrastructure/SRS/FlashcardReviewRepository.cs` — **does not exist**
- `src/Infrastructure/DependencyInjection.cs` — **no registration**

**What happens at runtime:**  
`AddFlashcardReviewCommandHandler` declares `IFlashcardReviewRepository` as a constructor dependency. Since no implementation is registered, the ASP.NET Core DI container will throw `InvalidOperationException: Unable to resolve service for type 'Application.FlashcardCollection.IFlashcardReviewRepository'` on the first request to that endpoint. The endpoint does not work at all.

**Fix required:**  
1. Create `FlashcardReviewRepository` in `Infrastructure` implementing `IFlashcardReviewRepository`.
2. Register it: `services.AddScoped<IFlashcardReviewRepository, FlashcardReviewRepository>();`

---

### Issue #4 — Authorization Specifications Are Not Registered in DI

**Files:**
- `src/Application/Authorization/FlashcardCollection/CanAccessFlashcardCollectionSpecification.cs`
- `src/Application/Authorization/LanguageAccount/CanAccessLanguageAccountSpecification.cs`
- `src/Infrastructure/DependencyInjection.cs` — neither is registered

**What happens at runtime:**  
Both specification classes are injected directly (as concrete types, not interfaces) into multiple command and query handlers. The DI container does not auto-register concrete types that were not explicitly configured. Every handler that depends on these specifications will throw `InvalidOperationException` at runtime.

The Application-layer DI scanning only covers `ICommandHandler<>`, `IQueryHandler<,>`, and `IDomainEventHandler<>`. Specifications implement `IAuthorizationSpecification<Guid>` and are **not** auto-scanned.

**Fix required:**  
```csharp
// In Infrastructure/DependencyInjection.cs or Application/DependencyInjection.cs
services.AddScoped<CanAccessFlashcardCollectionSpecification>();
services.AddScoped<CanAccessLanguageAccountSpecification>();
```

---

### Issue #5 — `SrsCalculationService` Is Not Registered in DI

**Files:**
- `src/Domain/FlashcardCollection/DomainServices/SrsCalculationService.cs` — domain service, exists
- `src/Application/LanguageAccounts/Events/FlashcardReviewedDomainEventHandler.cs` — injects it as a parameter
- DI registrations in both `Application` and `Infrastructure` — **missing**

**What happens at runtime:**  
`FlashcardReviewedDomainEventHandler` is registered via assembly scanning (`IDomainEventHandler<>`), but its dependency `SrsCalculationService` is a concrete class with no registration. When the DI container tries to resolve this handler, it fails with `InvalidOperationException`.

**Fix required:**  
```csharp
services.AddSingleton<SrsCalculationService>(); // or AddScoped
```

Note: there is also a deeper design concern here. A domain service (`SrsCalculationService`) living inside the `Domain` project is injected into an application-layer handler, which is correct. However, the handler also constructs new `SrsState` objects and calls `flashcard.SetSrsState(srsState)` — reaching directly into domain internals from the application layer. This couples the handler to a specific SRS initialization strategy. That logic belongs inside the aggregate root itself.

---

### Issue #6 — Incorrect Authorization Logic in `UpdateFlashcardCommandHandler`

**File:** `src/Application/FlashcardCollection/Commands/UpdateFlashcard/UpdateFlashcardCommandHandler.cs`

```csharp
if (collection.LanguageAccountId != userContext.UserId)
{
    return Result.Failure(UserErrors.Unauthorized());
}
```

**Why this is always wrong:**  
`collection.LanguageAccountId` is a `Guid` referencing the `LanguageAccount` table. `userContext.UserId` is a `Guid` referencing the `Users` table. These two IDs are from completely different entities and will almost never be equal. The result is that **every user is blocked from updating every flashcard**, because the condition almost always evaluates to `true`.

This handler also does not use the `CanAccessFlashcardCollectionSpecification` that all other handlers use, introducing inconsistency. Both problems must be fixed:

```csharp
// Replace the broken check with:
bool canAccess = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(
    collection.Id, userContext.UserId, cancellationToken);

if (!canAccess)
{
    return Result.Failure(AuthorizationError.Forbidden());
}
```

---

## 🟡 HIGH-SEVERITY ISSUES — Correctness and Design Problems

These issues will cause incorrect behavior or maintenance difficulty but do not necessarily crash the application on startup.

---

### Issue #7 — EF Core Relationship Configuration Conflict for `Flashcard`→`SrsState`

Two separate EF Core configurations define the same 1:1 relationship, with **different foreign keys**, which is contradictory.

**`FlashcardConfiguration.cs`:**
```csharp
builder.HasOne(f => f.SrsState)
    .WithOne()
    .HasForeignKey<Flashcard>(f => f.Id) // ← says Flashcard.Id is the FK
    .OnDelete(DeleteBehavior.Cascade);
```

**`SrsStateConfiguration.cs`:**
```csharp
builder.HasKey(s => s.FlashcardId); // SrsState.FlashcardId is the PK
builder.HasOne<Flashcard>()
    .WithOne()
    .HasForeignKey<SrsState>(s => s.FlashcardId) // ← says SrsState.FlashcardId is the FK
    .OnDelete(DeleteBehavior.Cascade);
```

`HasForeignKey<Flashcard>(f => f.Id)` in the first config declares `Flashcard.Id` as the FK for the relationship. `HasForeignKey<SrsState>(s => s.FlashcardId)` in the second declares `SrsState.FlashcardId` as the FK. EF Core applies both configurations, and the result is a conflicting or undefined mapping. The migration snapshots (`ApplicationDbContextModelSnapshot.cs`) may not match what EF Core generates at runtime.

The correct "shared primary key" pattern for an SrsState owned by a Flashcard is:

```csharp
// Only ONE place — ideally SrsStateConfiguration.cs or FlashcardConfiguration.cs, not both
builder.HasKey(s => s.FlashcardId); // SrsState.FlashcardId is PK and FK

builder.HasOne<Flashcard>()
    .WithOne(f => f.SrsState)
    .HasForeignKey<SrsState>(s => s.FlashcardId)
    .OnDelete(DeleteBehavior.Cascade);
```

The conflicting entry in `FlashcardConfiguration.cs` should be removed.

---

### Issue #8 — Domain Events Are Raised Inside Application Layer, Not Inside Domain Entities

**Pattern observed across all handlers:**

```csharp
// From AddFlashcardToCollectionCommandHandler
Flashcard flashcard = collection.AddFlashcard(...);
await applicationDbContext.SaveChangesAsync(cancellationToken);
flashcard.Raise(new FlashcardCreatedDomainEvent(flashcard.Id)); // ← event raised in Application
```

In DDD, domain events should be raised by the aggregate root inside the domain method that causes the state change. The application layer should be unaware of which events a domain action produces.

```csharp
// Correct DDD approach — inside FlashcardCollection.cs
public Flashcard AddFlashcard(string sentenceWithBlanks, ...)
{
    var flashcard = new Flashcard(Id, sentenceWithBlanks, ...);
    _flashcards.Add(flashcard);
    Raise(new FlashcardCreatedDomainEvent(flashcard.Id)); // ← raised by the aggregate
    return flashcard;
}
```

When events are raised outside the domain, the domain model no longer encapsulates its own behavior. New developers don't know that adding a flashcard should raise an event — it's buried in the command handler.

---

### Issue #9 — `IUserWriteRepository` Still Mixes Read and Write Concerns

**File:** `src/Application/Users/IUserRepository.cs`

```csharp
public interface IUserWriteRepository
{
    Task<Guid> AddAsync(User user);          // Write ✓
    Task<bool> UserExists(string email);     // Read ❌ — belongs to IUserReadRepository
    Task<User?> GetUserByEmail(string email, CancellationToken cancellationToken); // Read ❌
}
```

`IUserReadRepository` already exists but does not include `UserExists`. The read methods on the write interface violate CQRS separation that the rest of the codebase follows consistently. `RegisterUserCommandHandler` calls `userWriteRepository.UserExists(...)` — a read operation through a write interface.

The fix is to move `UserExists` to `IUserReadRepository` and use `IUserReadRepository` for that check in the handler.

---

### Issue #10 — `IDateTimeProvider` Passed Into Domain Entity Methods (Abstraction Layer Violation)

**Files:**
- `src/Domain/FlashcardCollection/SrsState.cs` — `CreateInitialState(Guid flashcardId, IDateTimeProvider dateTimeProvider)`
- `src/Domain/FlashcardCollection/FlashcardReview.cs` — `Create(..., IDateTimeProvider dateTimeProvider)`

`IDateTimeProvider` is an application/infrastructure abstraction. Passing an interface into a domain entity factory method couples the domain model to an external contract. This is a subtle violation of the dependency rule.

The correct approach is to pass a `DateTime` value (already resolved by the caller), not the interface:

```csharp
// SrsState.cs — correct
public static SrsState CreateInitialState(Guid flashcardId, DateTime currentTime)
{
    return new SrsState(flashcardId, interval: 0, easeFactor: 2.5, repetitions: 0, nextReviewDate: currentTime);
}

// Application layer resolves the time
var state = SrsState.CreateInitialState(flashcardId, dateTimeProvider.UtcNow);
```

---

### Issue #11 — `FlashcardReview.Create` Timing Guard Is Fragile and Redundant

**File:** `src/Application/FlashcardCollection/Commands/AddFlashcardReview/AddFlashcardReviewCommandHandler.cs`

```csharp
var review = FlashcardReview.Create(command.FlashcardId, dateTimeProvider.UtcNow, reviewResult, dateTimeProvider);
```

**In `FlashcardReview.Create`:**
```csharp
public static FlashcardReview Create(Guid flashcardId, DateTime reviewDate, ReviewResult reviewResult, IDateTimeProvider dateTimeProvider)
{
    if (reviewDate > dateTimeProvider.UtcNow)  // comparing captured time vs fresh read
        throw new ArgumentException("Review date cannot be in the future.");
    ...
}
```

The caller passes `dateTimeProvider.UtcNow` as `reviewDate` and also passes `dateTimeProvider` itself. Then inside `Create`, a second call to `dateTimeProvider.UtcNow` is made and compared against the already-captured time. Because of the nanoseconds between the two calls, `reviewDate` could theoretically be microscopically larger than the fresh `dateTimeProvider.UtcNow`, causing an unexpected `ArgumentException` under load.

More importantly, since the caller is always passing `dateTimeProvider.UtcNow` (i.e., "right now"), the guard can never legitimately fail — every review's `reviewDate` will always be the current moment. The guard has no practical value and adds fragility. It should be removed, or `IDateTimeProvider` should be replaced with a simple `DateTime` parameter resolved upstream.

---

### Issue #12 — `SrsState` Constructor Is Public — Domain Invariants Can Be Bypassed

**File:** `src/Domain/FlashcardCollection/SrsState.cs`

```csharp
// This should NOT be public
public SrsState(Guid flashcardId, int interval, double easeFactor, int repetitions, DateTime nextReviewDate)
```

Any code in the solution can create an `SrsState` with arbitrary invalid values (negative interval, ease factor below 1.3, etc.) without going through `CreateInitialState`. The factory method `CreateInitialState` was introduced precisely to enforce valid initial state — but if the constructor is public, it's bypassed entirely. The constructor should be `private` (the `private()` EF Core stub already exists for that purpose).

---

### Issue #13 — `Flashcard.SetSrsState` Is Public and Called From Application Layer

**File:** `src/Domain/FlashcardCollection/Flashcard.cs`

```csharp
public void SetSrsState(SrsState srsState)
{
    ArgumentNullException.ThrowIfNull(srsState);
    SrsState = srsState;
}
```

**Used in:** `FlashcardReviewedDomainEventHandler`

This method allows any code to replace a flashcard's SRS state with an arbitrary value. The SRS state is a core domain invariant — its lifecycle should be controlled by the aggregate root. The fact that a domain event handler in the application layer is calling `flashcard.SetSrsState(...)` means that business logic is leaking out of the domain.

The correct design is:
- `Flashcard` initializes its own `SrsState` when it is created (inside its constructor).
- `Flashcard` exposes an `UpdateSrsState(SrsStateCalculation calculation)` domain method that applies the calculation result.
- The event handler should never be creating `SrsState` objects directly.

---

### Issue #14 — `CreateFlashcardCollectionCommandHandler` Does Not Use Authorization Specification

**File:** `src/Application/FlashcardCollection/Commands/CreateFlashcardCollection/CreateFlashcardCollectionCommandHandler.cs`

```csharp
if (languageAccount.UserId != userContext.UserId)
{
    return Result.Failure<Guid>(UserErrors.Unauthorized());
}
```

This is inline authorization, which was one of the patterns the original review identified as a problem. All other handlers were updated to use `CanAccessFlashcardCollectionSpecification` or `CanAccessLanguageAccountSpecification`, but this handler was not. There are now two different authorization patterns in the codebase. `CanAccessLanguageAccountSpecification` exists and would be the correct tool here.

---

### Issue #15 — No Concurrency Control on Entities

**File:** `src/SharedKernel/Entity.cs`

The original review raised this as a "High" severity issue and it was not addressed. The `Entity` base class has no `RowVersion` or concurrency token. If two requests simultaneously review the same flashcard, the "last write wins" silently and one update is lost. For a spaced repetition application where tracking review progress accurately is core functionality, silent data loss is a significant problem.

```csharp
// Entity.cs — missing
[Timestamp]
public byte[]? RowVersion { get; protected set; }
```

---

### Issue #16 — Orphaned `using Domain.LanguageAccount.Events` in `FlashcardCollection.cs`

**File:** `src/Domain/FlashcardCollection/FlashcardCollection.cs`

```csharp
using Domain.FlashcardCollection.Events;
using Domain.LanguageAccount.Events;  // ← nothing from this namespace is used
```

`FlashcardCollection.cs` does not reference anything from `Domain.LanguageAccount.Events`. This is a leftover import from the refactoring. More importantly, having the `FlashcardCollection` domain class import from the `LanguageAccount` context is exactly the kind of cross-context coupling the refactoring was meant to eliminate. Even if unused today, it makes it easy to accidentally introduce a real dependency. It should be removed.

---

### Issue #17 — Outdated Migration Snapshots Reference Old Namespace

**Files:** `src/Infrastructure/Migrations/*.Designer.cs` and `ApplicationDbContextModelSnapshot.cs`

```csharp
// Still referencing the old location:
modelBuilder.Entity("Domain.SRS.FlashcardReview", b => { ... })
```

The entity was moved to `Domain.FlashcardCollection.FlashcardReview`, but the migration snapshots still reference `Domain.SRS.FlashcardReview`. EF Core uses these full type names to locate entities. When EF Core tries to map this entity to the current model, it may fail silently, apply the wrong configuration, or produce incorrect migrations. A new migration should be generated to align the snapshot with the current model state.

---

## 🟢 MEDIUM ISSUES — Code Quality and DDD Correctness

---

### Issue #18 — Enum Cast Without Validation in `AddFlashcardReviewCommandHandler`

**File:** `src/Application/FlashcardCollection/Commands/AddFlashcardReview/AddFlashcardReviewCommandHandler.cs`

```csharp
var reviewResult = (ReviewResult)command.ReviewResult;
```

This cast succeeds for any integer value, even if it doesn't correspond to a defined enum member. If a client sends `{ "reviewResult": 99 }`, the cast succeeds and `reviewResult` becomes an invalid enum value. The `SrsCalculationService.CalculateNextState` switch statement then falls through without matching any case, producing a default behavior that silently keeps the original SRS state — which is incorrect.

At system boundaries (the command coming in from the API), enum values must be validated. `FluentValidation` is already wired up via the `ValidationDecorator`. A validator for `AddFlashcardReviewCommand` should check `Enum.IsDefined(typeof(ReviewResult), command.ReviewResult)`.

---

### Issue #19 — Primitive Obsession: `Flashcard` Properties Are Plain Strings

**File:** `src/Domain/FlashcardCollection/Flashcard.cs`

```csharp
public string SentenceWithBlanks { get; private set; }
public string Translation { get; private set; }
public string Answer { get; private set; }
```

Validation for these fields is duplicated in the constructor guard (`string.IsNullOrWhiteSpace(...)`) and repeated in `FlashcardCollection.AddFlashcard(...)`. This is a code smell that the original review flagged. Value objects (`Sentence`, `Translation`, `Answer`) would centralize validation, add semantic meaning, and open the door to rich behavior like `Answer.Matches(userInput)` or `Sentence.FillBlanks(answer)`.

This is a medium-priority item and can be deferred, but the duplication of validation logic is already present and will grow.

---

### Issue #20 — `SrsState` Does Not Raise a Domain Event When Updated

**File:** `src/Domain/FlashcardCollection/SrsState.cs`

When `UpdateState(SrsStateCalculation)` is called, updating the learning progress of a flashcard, no domain event is raised. This makes it impossible to:
- Audit learning progress changes
- Trigger side effects (e.g., analytics, achievements)
- Build event sourcing later

```csharp
// Missing from UpdateState:
Raise(new SrsStateUpdatedDomainEvent(FlashcardId, oldInterval, Interval, ...));
```

---

### Issue #21 — `PermissionAuthorizationHandler` Always Grants Access to Authenticated Users

**File:** `src/Infrastructure/Authorization/PermissionAuthorizationHandler.cs`

```csharp
if (context.User is { Identity.IsAuthenticated: true })
{
    // TODO: Remove this call when you implement the PermissionProvider.GetForUserIdAsync
    context.Succeed(requirement);
    return;
}
```

Any authenticated user passes all permission checks, regardless of what permission is required. This is a development shortcut that is not safe to leave in. As the application grows and role-based or resource-based permissions are introduced, this stub will mask real access control failures.

---

## Summary

| # | Severity | Status | Problem |
|---|---|---|---|
| 1 | 🔴 Critical | New Bug | `CreateFlashcardCollectionCommandHandler` never adds collection to repository — nothing is saved |
| 2 | 🔴 Critical | New Bug | All domain events raised after `SaveChangesAsync` — events are permanently orphaned, SRS is broken |
| 3 | 🔴 Critical | Not Fixed | `IFlashcardReviewRepository` has no implementation and is not registered in DI |
| 4 | 🔴 Critical | Not Fixed | Auth specifications are not registered in DI — handlers crash at runtime |
| 5 | 🔴 Critical | Not Fixed | `SrsCalculationService` is not registered in DI — event handler crashes at runtime |
| 6 | 🔴 Critical | New Bug | `UpdateFlashcardCommandHandler` compares `LanguageAccountId` to `UserId` — always unauthorized |
| 7 | 🟡 High | New Bug | EF Core `Flashcard`→`SrsState` relationship configured twice with conflicting FK declarations |
| 8 | 🟡 High | Not Fixed | Domain events raised in Application layer, not inside domain entities |
| 9 | 🟡 High | Not Fixed | `IUserWriteRepository` still has read methods (`UserExists`, `GetUserByEmail`) |
| 10 | 🟡 High | Not Fixed | `IDateTimeProvider` passed into domain entity methods — abstraction layer violation |
| 11 | 🟡 High | New Bug | `FlashcardReview.Create` timing guard is fragile and unreliable |
| 12 | 🟡 High | Not Fixed | `SrsState` public constructor bypasses factory method invariants |
| 13 | 🟡 High | Not Fixed | `Flashcard.SetSrsState` is public — domain state manipulated from application layer |
| 14 | 🟡 High | Not Fixed | `CreateFlashcardCollectionCommandHandler` uses inline auth instead of specification |
| 15 | 🟡 High | Not Fixed | No concurrency control on entities (no `RowVersion`) |
| 16 | 🟡 High | Introduced | Orphaned `using Domain.LanguageAccount.Events` in `FlashcardCollection.cs` — cross-context coupling risk |
| 17 | 🟡 High | Introduced | Migration snapshots reference `Domain.SRS.FlashcardReview` — old namespace, entity was moved |
| 18 | 🟢 Medium | Not Fixed | Enum cast without validation at API boundary |
| 19 | 🟢 Medium | Not Fixed | Primitive obsession — `Flashcard` properties are plain strings |
| 20 | 🟢 Medium | Not Fixed | `SrsState.UpdateState` raises no domain event |
| 21 | 🟢 Medium | Existing | `PermissionAuthorizationHandler` always succeeds for authenticated users — TODo stub |

---

## Recommended Fix Order

Given that multiple endpoints will simply crash on any request, the priority should be:

1. **Issues #3, #4, #5** — Register missing DI services (30 minutes total)
2. **Issue #1** — Add the missing `flashcardCollectionRepository.Add(collection)` call (5 minutes)
3. **Issue #2** — Move all `entity.Raise(...)` calls to before `SaveChangesAsync`, and then move them inside the domain methods themselves (1-2 hours)
4. **Issue #6** — Fix the broken authorization comparison in `UpdateFlashcardCommandHandler` (5 minutes)
5. **Issue #7** — Remove the conflicting EF Core relationship configuration from `FlashcardConfiguration.cs` (10 minutes)
6. **Issue #17** — Generate a new EF Core migration to align snapshots with the current model (10 minutes)
7. **Issues #8, #12, #13** — Clean up the domain model (SrsState constructor, SetSrsState, raise events inside entities)
8. **Remaining medium issues** — Can be addressed iteratively

---

*Known missing feature (not a defect): Outbox Pattern for domain event persistence and reliable delivery — planned for future implementation.*

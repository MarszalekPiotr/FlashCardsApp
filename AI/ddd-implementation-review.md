# Review Implementacji DDD - Senior C# Developer Perspective

**Data review:** 2026-04-15  
**Reviewer:** Senior C# Developer  
**Projekt:** FlashCardsApp - Aplikacja do nauki języków

---

## 1. Cel Aplikacji

Aplikacja wspiera naukę języka poprzez:
- Naukę słownictwa z systemem fiszek
- System SRS (Spaced Repetition System)
- Aktywną produkcję języka (tworzenie zdań)
- Ćwiczenia gramatyczne
- Ocenę dłuższych wypowiedzi przez AI
- System tokenów do kontroli zużycia funkcji AI

**Bounded Contexts:**
- User Management
- Language Account (Language Learning)
- SRS (Spaced Repetition System)
- Flashcard System

---

## 2. Analiza Bounded Contexts

### 2.1 User Context

**Encje:**
- `User` (Aggregate Root)
  - Id, Email, FirstName, LastName, PasswordHash

**Value Objects:**
- `Email`

**Domain Events:**
- `UserRegisteredDomainEvent`

**Repozytoria:**
- `IUserWriteRepository` (EF Core)
- `IUserReadRepository` (Dapper)

**Ocena:** ✅ **Dobra implementacja**
- User jest poprawnie zdefiniowany jako Aggregate Root
- Email jako Value Object jest odpowiedni (ma validation logic)
- Private constructor + static factory method (Create) - zgodne z DDD
- Domain events są podnoszone przy tworzeniu

**Uwagi:**
- Brak Update method (np. UpdateProfile) - może być celowe dla MVP
- PasswordHash jako string - rozważyć Value Object dla bezpieczeństwa
- Brak invariants poza null checks
- **KRYTYCZNE:** Id nie jest generowany w konstruktorze - domain event otrzymuje `Guid.Empty`
- **KRYTYCZNE:** Domain event podnoszony PRZED przypisaniem Id

**Zgodność z planem AI:**
- Plan zakłada: TokenBalance, TokenTransactions - jeszcze nie zaimplementowane
- Plan zakazuje: Domain services w tym kontekście - OK
- Plan wymaga: Audit trail - brak

---

### 2.2 LanguageAccount Context

**🔴 KRYTYCZNY BŁĄD DDD: Aggregate Root jako child innego Aggregate Root**

**Aktualna (BŁĘDNA) struktura:**
- `LanguageAccount` (Aggregate Root)
  - Id, UserId, ProficiencyLevel, Language
  - FlashcardCollections: List<FlashcardCollection> ❌
- `FlashcardCollection` (Entity, child of LanguageAccount) ❌
  - Id, LanguageAccountId, Name
  - Flashcards: List<Flashcard>
- `Flashcard` (Entity, child of FlashcardCollection)
  - Id, FlashcardCollectionId, SentenceWithBlanks, Translation, Answer, Synonyms

**Dlaczego to jest KRYTYCZNY błąd:**
1. **Narusza fundamentalną zasadę DDD:** Jeden Aggregate = Jeden Aggregate Root
2. **FlashcardCollection ma własne repozytorium** (`IFlashcardCollectionRepository`) - co oznacza że jest traktowany jako Aggregate Root
3. **LanguageAccount zawiera FlashcardCollection jako child** - co oznacza że jest traktowany jako Entity
4. **Niespójność:** FlashcardCollection jest jednocześnie child entity i Aggregate Root - to niemożliwe w poprawnym DDD
5. **Narusza invariants:** LanguageAccount nie może kontrolować FlashcardCollection bo ma własne repozytorium
6. **Problem z transakcjami:** Nie wiadomo który Aggregate Root zarządza transakcją

**Poprawna (ZGODNA Z PLANEM AI) struktura:**
- `LanguageAccount` (Aggregate Root)
  - Id, UserId, ProficiencyLevel, Language
  - Brak FlashcardCollections jako child
- `FlashcardCollection` (Aggregate Root - NIEZALEŻNY)
  - Id, LanguageAccountId (foreign key, nie child)
  - Name
  - Flashcards: List<Flashcard>
- `Flashcard` (Entity, child of FlashcardCollection)
  - Id, FlashcardCollectionId, SentenceWithBlanks, Translation, Answer, Synonyms

**Value Objects:**
- `Language`
- `ProficiencyLevel`
- `Synonyms`

**Domain Events:**
- `LanguageAccountCreatedDomainEvent`
- `FlashcardCollectionCreatedDomainEvent`
- `FlashcardCreatedDomainEvent`

**Repozytoria:**
- `ILanguageAccountRepository` (EF Core)
- `ILanguageAccountReadRepository` (Dapper)
- `IFlashcardCollectionRepository` (EF Core) ✅ - Poprawne dla niezależnego Aggregate Root
- `IFlashcardCollectionReadRepository` (Dapper)
- `IFlashcardRepository` (EF Core)

**Ocena:** 🔴 **Wymaga refaktoringu - Fundamentalny błąd DDD**

**Aktualne problemy:**
- 🔴 **KRYTYCZNE:** FlashcardCollection jest childem LanguageAccount ALE ma własne repozytorium - narusza zasadę DDD
- Collections jako backing fields z IReadOnlyCollection - chroni invariants (ale niepoprawnie zastosowane)
- Private constructors + static factory methods - poprawne
- Domain events podnoszone przy tworzeniu - poprawne
- UpdateProficiencyLevel z invariant (nie można downgrade) - poprawne

**Plan refaktoringu (Option B - Dwa niezależne Aggregates):**

1. **Usunąć FlashcardCollections z LanguageAccount:**
```csharp
// LanguageAccount.cs - USUNĄĆ
private readonly List<FlashcardCollection> _flashcardCollections = new();
public IReadOnlyCollection<FlashcardCollection> FlashcardCollections => _flashcardCollections.AsReadOnly();
```

2. **Dodać LanguageAccountId do FlashcardCollection:**
```csharp
// FlashcardCollection.cs - DODAĆ
public Guid LanguageAccountId { get; private set; }
```

3. **Zmienić FlashcardCollection.Create z internal na public:**
```csharp
// FlashcardCollection.cs - ZMIENIĆ
public static FlashcardCollection Create(Guid languageAccountId, string name)
```

4. **Usunąć AddFlashcard z FlashcardCollection (jeśli istnieje) - FlashcardCollection jest teraz Aggregate Root, nie powinien tworzyć Flashcard bezpośrednio**
```csharp
// FlashcardCollection.cs - ROZWAŻYĆ USUNIĘCIE LUB ZMIANĘ
// Flashcard powinien być dodawany przez command handler używając IFlashcardRepository
```

5. **Zaktualizować command handlers:**
```csharp
// Zamiast:
var languageAccount = await _languageAccountRepository.GetById(id);
languageAccount.AddCollection(name);

// Na:
var collection = FlashcardCollection.Create(languageAccountId, name);
await _flashcardCollectionRepository.Add(collection);
```

6. **Zaktualizować EF Core configuration:**
```csharp
// Usunąć konfigurację nawigacji LanguageAccount → FlashcardCollection
// Dodać konfigurację FlashcardCollection → LanguageAccount (optional navigation)
```

**Uwagi po refaktoringu:**
- `FlashcardCollection.Create` powinno być public (domain services mogą tworzyć kolekcje)
- `Flashcard.Update` pozostaje public (Flashcard jest childem FlashcardCollection Aggregate Root)
- Brak RemoveCollection/RemoveFlashcard methods na agregatach - dodać jeśli potrzebne
- **KRYTYCZNE:** Id nie jest generowany w konstruktorach - domain events otrzymują `Guid.Empty`
- **KRYTYCZNE:** Domain events podnoszone PRZED przypisaniem Id

**Zgodność z planem AI:**
- Plan zakłada: LanguageAccount jako Aggregate Root bez FlashcardCollection jako child ✅
- Plan zakłada: StudySession, Vocabulary - jeszcze nie zaimplementowane
- Plan zakłada: LearningSettings jako Value Object - brak
- Plan wymaga: GrammarExercises, SpeakingSessions - brak
- Plan wymaga: Domain services (ProgressTrackingService, DifficultyAssessmentService) - brak

---

### 2.3 SRS Context

**🔴 KRYTYCZNY BŁĄD: SrsState w osobnym kontekście zamiast child Flashcard**

**Aktualna (BŁĘDNA) struktura:**
- `SrsState` (Entity w osobnym SRS Context) ❌
  - FlashcardId, Interval, EaseFactor, Repetitions, NextReviewDate
- `FlashcardReview` (Entity w osobnym SRS Context) ❌
  - Id, FlashcardId, ReviewDate, ReviewResult

**Repozytoria (BŁĘDNE):**
- `ISrsStateRepository` (EF Core) ❌
- `IFlashcardReviewRepository` (EF Core) ❌

**Dlaczego to jest KRYTYCZNY błąd:**

1. **Narusza plan AI:** Plan AI zakłada SRS jako część Flashcard System Context, nie osobny kontekst
2. **SrsState ma 1:1 relację z Flashcard:** Każda fiszka ma jeden stan SRS - to jest naturalna relacja child-parent, nie osobny kontekst
3. **SrsState nie ma własnego biznesowego znaczenia:** SrsState bez Flashcard nie ma sensu - to jest stan fiszki, nie niezależna koncepcja
4. **Narusza zasady DDD:** Entity bez własnego Aggregate Root w osobnym kontekście to anty-pattern
5. **Problem z transakcjami:** SrsState i Flashcard są w różnych kontekstach ale muszą być aktualizowane atomowo
6. **Złożoność:** Osobny kontekst dla prostej relacji 1:1 to over-engineering

---

**Poprawna (ZGODNA Z PLANEM AI) struktura:**

```
Flashcard System Context
├── FlashcardCollection (Aggregate Root)
│   └── Flashcard (Entity)
│       ├── Id, FlashcardCollectionId, SentenceWithBlanks, Translation, Answer, Synonyms
│       └── SrsState (Entity, child of Flashcard)
│           ├── Interval, EaseFactor, Repetitions, NextReviewDate
│           └── UpdateState(ReviewResult) - algorytm SM-2
└── SpacedRepetitionService (Domain Service)
    └── CalculateNextReview(ReviewResult, Interval, EaseFactor, Repetitions)
```

**Repozytoria (POPRAWNE):**
- `IFlashcardCollectionRepository` (EF Core) - zarządza całym agregatem
- `IFlashcardCollectionReadRepository` (Dapper)
- Brak osobnych repozytoriów dla SrsState i FlashcardReview

---

**Argumentacja dlaczego SrsState powinien być childem Flashcard:**

### 1. Relacja 1:1
```csharp
// Każda fiszka ma jeden stan SRS
Flashcard (1) ←→ (1) SrsState
```
To jest klasyczna relacja child-parent, nie powód do osobnego kontekstu.

### 2. SrsState nie ma własnej tożsamości biznesowej
- SrsState = "stan powtórek dla fiszki X"
- Bez Flashcard, SrsState nie ma znaczenia
- To jest stan (state), nie niezależna encja

### 3. Zgodność z planem AI
Z planu AI (ddd-pojecia-wyjasnienie.md):
> "System Fiszek (Flashcard System Context): Tylko logika fiszek i powtórek SRS"
> "Dlaczego osobny kontekst: Specjalistyczne algorytmy SRS i automatyczna generacja wymagają izolacji"

Plan AI mówi o **Flashcard System Context** (jeden kontekst dla fiszek i SRS), nie dwóch osobnych kontekstach.

### 4. Algorytm SRS w Domain Service *do przemyślenia
Z planu AI:
> "SpacedRepetitionService - algorytm SRS"

Algorytm SM-2 powinien być w Domain Service, nie w Entity:
```csharp
public interface ISpacedRepetitionService
{
    SrsCalculationResult CalculateNextReview(
        ReviewResult result, 
        int currentInterval, 
        double currentEaseFactor, 
        int currentRepetitions);
}
```

### 5. Problem z timingiem domain events
Z osobnym kontekstem SRS:
```csharp
// AddFlashcardReviewCommandHandler
flashcardReviewRepository.Add(review);
await unitOfWork.SaveChangesAsync(cancellationToken); // ← FlashcardReview zapisany

// FlashcardReviewedDomainEventHandler (wywołany PO SaveChangesAsync)
srsState.UpdateState(reviewResult);
await unitOfWork.SaveChangesAsync(cancellationToken); // ← SrsState zapisany w osobnej operacji
```

**Problem:** Jeśli FlashcardReviewedDomainEventHandler fail, FlashcardReview jest już zapisany ale SrsState nie - **inconsistent state**.

Z child entity:
```csharp
// POPRAWNE - wszystko w jednej transakcji
var flashcard = await _flashcardRepository.GetById(flashcardId);
flashcard.UpdateSrsState(reviewResult); // Flashcard i SrsState w jednej encji
await _flashcardRepository.Save(flashcard); // Jedna transakcja
```

---

**Poprawna implementacja:**

### 1. SrsState jako child Flashcard
```csharp
// Domain/FlashcardSystem/Flashcard.cs
public class Flashcard : Entity
{
    public Guid Id { get; private set; }
    public Guid FlashcardCollectionId { get; private set; }
    public string SentenceWithBlanks { get; private set; }
    public string Translation { get; private set; }
    public string Answer { get; private set; }
    public Synonyms Synonyms { get; private set; }

    private SrsState? _srsState;
    public SrsState? SrsState => _srsState;

    private Flashcard() { }

    public Flashcard(Guid flashcardCollectionId, string sentenceWithBlanks, string translation, string answer, Synonyms synonyms)
    {
        Id = Guid.NewGuid();
        FlashcardCollectionId = flashcardCollectionId;
        SentenceWithBlanks = sentenceWithBlanks;
        Translation = translation;
        Answer = answer;
        Synonyms = synonyms;
        _srsState = SrsState.CreateInitialState(Id);
    }

    public void UpdateSrsState(ReviewResult reviewResult, ISpacedRepetitionService srsService, DateTime currentTime)
    {
        if (_srsState == null)
            _srsState = SrsState.CreateInitialState(Id);

        var calculation = srsService.CalculateNextReview(
            reviewResult, 
            _srsState.Interval, 
            _srsState.EaseFactor, 
            _srsState.Repetitions);

        _srsState.Update(calculation.Interval, calculation.EaseFactor, calculation.Repetitions, calculation.NextReviewDate);

        Raise(new FlashcardReviewedDomainEvent(Id, reviewResult));
    }
}

// Domain/FlashcardSystem/SrsState.cs
public class SrsState : Entity
{
    public Guid FlashcardId { get; private set; }
    public int Interval { get; private set; }
    public double EaseFactor { get; private set; }
    public int Repetitions { get; private set; }
    public DateTime NextReviewDate { get; private set; }

    private SrsState() { }

    private SrsState(Guid flashcardId, int interval, double easeFactor, int repetitions, DateTime nextReviewDate)
    {
        FlashcardId = flashcardId;
        Interval = interval;
        EaseFactor = easeFactor;
        Repetitions = repetitions;
        NextReviewDate = nextReviewDate;
    }

    public static SrsState CreateInitialState(Guid flashcardId)
    {
        return new SrsState(flashcardId, interval: 0, easeFactor: 2.5, repetitions: 0, nextReviewDate: DateTime.UtcNow);
    }

    internal void Update(int interval, double easeFactor, int repetitions, DateTime nextReviewDate)
    {
        if (interval < 0) throw new ArgumentException("Interval cannot be negative");
        if (easeFactor < 1.3) throw new ArgumentException("EaseFactor must be >= 1.3");

        Interval = interval;
        EaseFactor = easeFactor;
        Repetitions = repetitions;
        NextReviewDate = nextReviewDate;
    }
}
```

### 2. SpacedRepetitionService jako Domain Service
```csharp
// Domain/FlashcardSystem/Services/ISpacedRepetitionService.cs
public interface ISpacedRepetitionService
{
    SrsCalculationResult CalculateNextReview(
        ReviewResult result, 
        int currentInterval, 
        double currentEaseFactor, 
        int currentRepetitions);
}

public record SrsCalculationResult(
    int Interval, 
    double EaseFactor, 
    int Repetitions, 
    DateTime NextReviewDate);

// Domain/FlashcardSystem/Services/SpacedRepetitionService.cs
public sealed class SpacedRepetitionService : ISpacedRepetitionService
{
    public SrsCalculationResult CalculateNextReview(
        ReviewResult result, 
        int currentInterval, 
        double currentEaseFactor, 
        int currentRepetitions)
    {
        // Algorytm SM-2
        double newEaseFactor = currentEaseFactor;
        int newInterval = currentInterval;
        int newRepetitions = currentRepetitions + 1;

        switch (result)
        {
            case ReviewResult.Again:
                newEaseFactor = Math.Max(1.3, currentEaseFactor - 0.2);
                newInterval = 1;
                newRepetitions = 0;
                break;
            case ReviewResult.Hard:
                newEaseFactor = currentEaseFactor - 0.15;
                newInterval = Math.Max(1, currentInterval * 1.2);
                break;
            case ReviewResult.Good:
                newInterval = currentInterval == 0 ? 1 : (int)(currentInterval * currentEaseFactor);
                break;
            case ReviewResult.Easy:
                newEaseFactor = currentEaseFactor + 0.15;
                newInterval = (int)(currentInterval * currentEaseFactor * 1.3);
                break;
        }

        DateTime nextReviewDate = DateTime.UtcNow.AddDays(newInterval);

        return new SrsCalculationResult(newInterval, newEaseFactor, newRepetitions, nextReviewDate);
    }
}
```

### 3. Command Handler
```csharp
// Application/FlashcardSystem/Commands/ReviewFlashcard/ReviewFlashcardCommandHandler.cs
public sealed class ReviewFlashcardCommandHandler : ICommandHandler<ReviewFlashcardCommand, Guid>
{
    private readonly IFlashcardCollectionRepository _repository;
    private readonly ISpacedRepetitionService _srsService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Guid>> Handle(ReviewFlashcardCommand command, CancellationToken cancellationToken)
    {
        var collection = await _repository.GetByIdWithFlashcards(command.FlashcardCollectionId, cancellationToken);
        if (collection == null)
            return Result.Failure<Guid>(FlashcardCollectionErrors.NotFound(command.FlashcardCollectionId));

        var flashcard = collection.Flashcards.FirstOrDefault(f => f.Id == command.FlashcardId);
        if (flashcard == null)
            return Result.Failure<Guid>(FlashcardErrors.NotFound(command.FlashcardId));

        var reviewResult = ReviewResult.Create(command.Result);
        var currentTime = _dateTimeProvider.UtcNow;

        flashcard.UpdateSrsState(reviewResult, _srsService, currentTime);

        await _repository.Update(collection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return flashcard.Id;
    }
}
```

---

**Plan refaktoringu:**

1. **Usunąć SRS Context:**
   - Usunąć folder `Domain/SRS`
   - Usunąć `ISrsStateRepository`, `IFlashcardReviewRepository`

2. **Przenieść SrsState do Flashcard:**
   - Dodać `SrsState` jako child entity w `Flashcard`
   - Przenieść logikę SM-2 do `SpacedRepetitionService` (Domain Service)
   - Dodać `UpdateSrsState` w `Flashcard`

3. **Usunąć FlashcardReview jako osobną encję:**
   - Review może być value object lub po prostu parametr w `UpdateSrsState`
   - Historia review może być w osobnej tabeli (jeśli potrzebna) ale nie jako domain entity

4. **Zaktualizować EF Core configuration:**
   - Skonfigurować SrsState jako Owned Entity lub child entity
   - Usunąć osobne DbSet dla SrsState

5. **Zaktualizować command handlers:**
   - Używać `IFlashcardCollectionRepository` zamiast osobnych repozytoriów
   - Wywoływać `flashcard.UpdateSrsState()` z `ISpacedRepetitionService`

6. **Zaktualizować domain events:**
   - `FlashcardReviewedDomainEvent` podnoszony w `Flashcard.UpdateSrsState()`
   - Handler eventów aktualizuje inne agregaty (jeśli potrzebne)

---

**Ocena po refaktoringu:**
- ✅ SrsState jako child Flashcard - zgodne z DDD
- ✅ Algorytm SRS w Domain Service - zgodne z planem AI
- ✅ Jedna transakcja dla Flashcard + SrsState
- ✅ Zgodne z planem AI (SRS w Flashcard System Context)
- ✅ Mniejsze złożoność (brak osobnego kontekstu)

**Zgodność z planem AI:**
- Plan zakłada: SRS jako część Flashcard System Context ✅
- Plan wymaga: SpacedRepetitionService jako domain service ✅
- Plan wymaga: ReviewSchedulerService - nadal brak (może być dodany później)

---

## 3. Repozytoria - Write vs Read Split

### 3.1 Architektura CQRS

**Write Repositories (EF Core):**
- `UserWriteRepository`
- `LanguageAccountRepository`
- `FlashcardCollectionRepository`
- `FlashcardRepository`
- `SrsStateRepository`
- `FlashcardReviewRepository`

**Read Repositories (Dapper):**
- `UserReadRepository`
- `LanguageAccountReadRepository`
- `FlashcardCollectionReadRepository`

### 3.2 Krytyczne Problemy w Repozytoriach

**🔴 Problem 1: Repository Creating Domain Entities**

**Lokalizacja:** `src\Infrastructure\Users\UserRepository.cs`

**Problem:**
```csharp
public async Task<Guid> CreateUser(string email, string firstName, string lastName, string passwordHash)
{
    var user = User.Create(new Email(email), firstName, lastName, passwordHash); // ❌ WRONG LAYER
    await _applicationDbContext.Users.AddAsync(user);
    return user.Id;
}
```

**Dlaczego to jest krytyczne:**
- Repositories NIE powinny tworzyć domain entities
- Business logic wycieka do infrastructure layer
- Niespójne z innymi repozytoriami (LanguageAccountRepository tylko Add)
- Narusza separation of concerns

**Rozwiązanie:**
```csharp
// Usunąć CreateUser method
// W command handler:
var user = User.Create(...);
userWriteRepository.Add(user);
await unitOfWork.SaveChangesAsync(cancellationToken);
```

**🔴 Problem 2: Mixed Read/Write Responsibilities**

**Lokalizacja:** `src\Application\Users\IUserRepository.cs`

**Problem:**
```csharp
public interface IUserWriteRepository
{
    Task<Guid> CreateUser(...);        // Write ✓
    Task<bool> UserExists(...);        // Read ❌
    Task<User?> GetUserByEmail(...);   // Read ❌
}
```

**Dlaczego to jest problem:**
- Interfejs nazywany "Write" ale ma metody read
- Niespójne z innymi repozytoriami
- Niejasne odpowiedzialności

**Rozwiązanie:**
```csharp
public interface IUserWriteRepository
{
    void Add(User user);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public interface IUserReadRepository
{
    Task<UserReadModel> GetByEmailAsync(string email);
    Task<UserReadModel> GetById(Guid userId);
    Task<bool> UserExists(string email);
}
```

### 3.3 Ocena Podziału Write/Read

**✅ PODZIAŁ JEST POPRAWNY I REKOMENDOWANY**

**Argumenty za:**
1. **Separation of Concerns:** Write repos dbają o consistency i invariants, read repos o performance
2. **Technology Stack:** EF Core dla write (ORM z change tracking), Dapper dla read (raw SQL, szybki)
3. **Scalability:** Read queries mogą być skalowane niezależnie (read replicas)
4. **Optimization:** Read repos używają DTOs bez domain logic, co jest szybsze

**Argumenty przeciw:**
1. **Duplikacja:** Niektóre metody są podobne (np. GetById)
2. **Complexity:** Więcej kodu do utrzymania

**Rekomendacja:** Zachować ten podział - jest to zgodne z CQRS pattern i dobrze skaluje się.

### 3.4 Dodatkowe Problemy w Repozytoriach

**🔴 Problem 3: Authorization Logic in Application Layer**

**Lokalizacja:** `src\Application\LanguageAccounts\Commands\AddFlashcardReview\AddFlashcardReviewCommandHandler.cs`

**Aktualny kod (ŹLE):**
```csharp
public async Task<Result<Guid>> Handle(AddFlashcardReviewCommand command, CancellationToken cancellationToken)
{
    Flashcard? flashcard = await flashcardRepository.GetByIdWithCollectionAsync(command.FlashcardId, cancellationToken);

    if (flashcard is null)
        return Result.Failure<Guid>(FlashcardErrors.NotFound(command.FlashcardId));

    // ❌ AUTHORIZATION LOGIC WŚRÓD BUSINESS LOGIC
    if (flashcard.FlashcardCollection!.LanguageAccount!.UserId != userContext.UserId)
        return Result.Failure<Guid>(UserErrors.Unauthorized());

    var reviewResult = new ReviewResult((Domain.SRS.Enums.ReviewResult)command.ReviewResult);
    var review = Domain.SRS.FlashcardReview.Create(command.FlashcardId, DateTime.UtcNow, reviewResult);

    flashcardReviewRepository.Add(review);
    await unitOfWork.SaveChangesAsync(cancellationToken);

    return review.Id;
}
```

**Dlaczego to jest problem:**

### 1. **Mixed responsibilities**
Command handler robi dwie rzeczy:
- Business logic (dodawanie review)
- Authorization logic (sprawdzanie czy user ma dostęp)

### 2. **Scattered across handlers**
Każdy handler ma swoją własną logikę authorization:
```csharp
// Handler 1
if (flashcard.FlashcardCollection!.LanguageAccount!.UserId != userContext.UserId)
    return Result.Failure<Guid>(UserErrors.Unauthorized());

// Handler 2
if (collection.LanguageAccount!.UserId != userContext.UserId)
    return Result.Failure<Guid>(UserErrors.Unauthorized());

// Handler 3
if (account.UserId != userContext.UserId)
    return Result.Failure<Guid>(UserErrors.Unauthorized());
```

### 3. **Hard to test**
Aby przetestować business logic, musisz mockować authorization:
```csharp
[Fact]
public async Task Should_Add_Review()
{
    // Musisz mockować userContext, flashcard, collection, account...
    // Tylko żeby przetestować dodanie review
}
```

### 4. **Hard to maintain**
Jeśli zmienisz logikę authorization (np. dodasz role-based access), musisz zmienić 10+ handlerów.

---

**Rozwiązanie - Opcja 1: Middleware / Pipeline (Najlepsza dla Web API)**

```csharp
// Infrastructure/Authorization/AuthorizationMiddleware.cs
public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationMiddleware(RequestDelegate next, IAuthorizationService authorizationService)
    {
        _next = next;
        _authorizationService = authorizationService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var requiredPermission = endpoint?.Metadata.GetMetadata<RequiredPermissionAttribute>();

        if (requiredPermission != null)
        {
            var userId = context.User.FindFirst("sub")?.Value;
            var resourceId = context.Request.RouteValues["id"]?.ToString();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(resourceId))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var hasAccess = await _authorizationService.CheckAccessAsync(
                Guid.Parse(userId),
                resourceId,
                requiredPermission.Permission);

            if (!hasAccess)
            {
                context.Response.StatusCode = 403;
                return;
            }
        }

        await _next(context);
    }
}

// Application/Authorization/RequiredPermissionAttribute.cs
[AttributeUsage(AttributeTargets.Method)]
public class RequiredPermissionAttribute : Attribute
{
    public string Permission { get; }

    public RequiredPermissionAttribute(string permission)
    {
        Permission = permission;
    }
}

// Użycie w controller:
[HttpPost("flashcards/{id}/review")]
[RequiredPermission("flashcard:access")]
public async Task<IActionResult> ReviewFlashcard(Guid id, ReviewFlashcardCommand command)
{
    // ✅ Bez authorization logic - middleware już sprawdził
    var result = await _mediator.Send(command);
    return Ok(result);
}
```

**Zalety:**
- ✅ Standardowe podejście w ASP.NET Core
- ✅ Nie trzeba zmieniać handlerów
- ✅ Wbudowane support w frameworku
- ✅ Można użyć ASP.NET Core Authorization Policies

---

**Rozwiązanie - Opcja 2: Authorization Specifications (Dla CQRS)**

```csharp
// Application/Authorization/IAuthorizationSpecification.cs
public interface IAuthorizationSpecification<T>
{
    Task<bool> IsSatisfiedByAsync(T entity, Guid userId, CancellationToken cancellationToken = default);
    Error UnauthorizedError { get; }
}

// Application/Authorization/CanAccessFlashcardSpecification.cs
public sealed class CanAccessFlashcardSpecification(IFlashcardRepository repository)
    : IAuthorizationSpecification<Guid>
{
    public async Task<bool> IsSatisfiedByAsync(Guid flashcardId, Guid userId, CancellationToken cancellationToken = default)
    {
        var flashcard = await repository.GetByIdWithCollectionAsync(flashcardId, cancellationToken);
        return flashcard?.FlashcardCollection?.LanguageAccount?.UserId == userId;
    }

    public Error UnauthorizedError => UserErrors.Unauthorized();
}

// Użycie w handler:
public async Task<Result<Guid>> Handle(AddFlashcardReviewCommand command, CancellationToken cancellationToken)
{
    // ✅ Authorization logic oddzielona
    bool canAccess = await _canAccessFlashcard.IsSatisfiedByAsync(command.FlashcardId, userContext.UserId, cancellationToken);
    if (!canAccess)
        return Result.Failure<Guid>(_canAccessFlashcard.UnauthorizedError);

    // ✅ Tylko business logic
    var reviewResult = new ReviewResult((Domain.SRS.Enums.ReviewResult)command.ReviewResult);
    var review = Domain.SRS.FlashcardReview.Create(command.FlashcardId, DateTime.UtcNow, reviewResult);

    flashcardReviewRepository.Add(review);
    await unitOfWork.SaveChangesAsync(cancellationToken);

    return review.Id;
}
```

**Zalety:**
- ✅ Separation of Concerns
- ✅ Reusable w wielu handlerach
- ✅ Testable (można mockować specification)
- ✅ Maintainable (zmiana w jednym miejscu)

---

**Dlaczego nowe rozwiązanie jest lepsze:**

### 1. **Separation of Concerns**
- Command handler: tylko business logic
- Authorization specification/middleware: tylko authorization logic

### 2. **Reusable**
```csharp
// Użyj w wielu handlerach
var canAccess = await _canAccessFlashcard.IsSatisfiedByAsync(flashcardId, userId);
```

### 3. **Testable**
```csharp
// Test business logic bez authorization
[Fact]
public async Task Should_Add_Review()
{
    // Mock specification zawsze zwraca true
    _canAccessFlashcard.IsSatisfiedByAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
        .Returns(true);

    // Testuj tylko business logic
}
```

### 4. **Maintainable**
Zmień logikę authorization w jednym miejscu, a zmieni się wszędzie.

---

**Rekomendacja:**

- **Dla Web API:** Użyj **Middleware / Pipeline** (Opcja 1)
  - Najbardziej standardowe podejście
  - ASP.NET Core ma wbudowane authorization policies
  - Nie musisz zmieniać handlerów

- **Dla CQRS bez Web API:** Użyj **Authorization Specifications** (Opcja 2)
  - Jeśli nie używasz ASP.NET Core
  - Jeśli potrzebujesz bardziej złożonej logiki

---

### 3.4 Czy Write Repositories Mogą Mieć Metody Odczytu?

**✅ TAK, ALE Z OSTRZEŻENIAMI**

**Aktualna implementacja:**
```csharp
public class UserWriteRepository : BaseWriteRepository, IUserWriteRepository
{
    public async Task<User?> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
       var emailValueObject = new Email(email);
       return await _applicationDbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == emailValueObject, cancellationToken);
    }
}
```

**To jest POPRAWNE z następujących powodów:**
1. **Validation w Command Handlers:** Często command handler potrzebuje sprawdzić czy email już istnieje przed dodaniem
2. **Aggregate Loading:** Write repo powinien ładować całe agregaty z invariants
3. **AsNoTracking:** Używasz AsNoTracking, więc nie ma change tracking overhead
4. **Domain Logic:** Zwracasz entity, nie DTO - więc można wywoływać domain methods

**Kiedy NIE używać read w write repo:**
- Dla list/gridów (użyj read repo z Dapper)
- Dla raportów (użyj read repo z Dapper)
- Dla zapytań które nie potrzebują domain logic

**Rekomendacja:** Zachować metody odczytu w write repositories dla validation i aggregate loading.

---

## 4. Krytyczne Problemy z architectural-review-and-refactoring-plan.md

### 4.1 Aggregate Root jako child innego Aggregate Root

**Severity:** 🔴 Critical  
**Effort:** Medium (2-3 hours)  
**Files Affected:** `src\Domain\LanguageAccount\LanguageAccount.cs`, `src\Domain\LanguageAccount\FlashcardCollection.cs`

**Problem:**
```csharp
// LanguageAccount.cs
public class LanguageAccount : Entity  // ← Aggregate Root
{
    private readonly List<FlashcardCollection> _flashcardCollections = new();
    public IReadOnlyCollection<FlashcardCollection> FlashcardCollections => _flashcardCollections.AsReadOnly();
}

// FlashcardCollection.cs
public class FlashcardCollection : Entity  // ← Aggregate Root (ma własne repozytorium)
{
    // Ma IFlashcardCollectionRepository
}
```

**Dlaczego to jest krytyczne:**
- **Narusza fundamentalną zasadę DDD:** Jeden Aggregate = Jeden Aggregate Root
- **FlashcardCollection ma własne repozytorium** - co oznacza że jest traktowany jako Aggregate Root
- **LanguageAccount zawiera FlashcardCollection jako child** - co oznacza że jest traktowany jako Entity
- **Niespójność:** FlashcardCollection jest jednocześnie child entity i Aggregate Root - to niemożliwe w poprawnym DDD
- **Narusza invariants:** LanguageAccount nie może kontrolować FlashcardCollection bo ma własne repozytorium
- **Problem z transakcjami:** Nie wiadomo który Aggregate Root zarządza transakcją

**Rozwiązanie (Option B - Dwa niezależne Aggregates):**

Zgodnie z planem AI i sekcją 2.2 review:

```csharp
// LanguageAccount.cs - USUNĄĆ FlashcardCollections
public class LanguageAccount : Entity  // ← Aggregate Root
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    // Brak FlashcardCollections jako child
}

// FlashcardCollection.cs - DODAĆ LanguageAccountId jako foreign key
public class FlashcardCollection : Entity  // ← Aggregate Root (NIEZALEŻNY)
{
    public Guid Id { get; private set; }
    public Guid LanguageAccountId { get; private set; } // ← Foreign key, nie child
    public string Name { get; private set; }
    
    public static FlashcardCollection Create(Guid languageAccountId, string name)
    {
        return new FlashcardCollection(languageAccountId, name);
    }
}
```

**Szczegółowy plan refaktoringu - zobacz sekcję 2.2 LanguageAccount Context**

### 4.3 Inconsistent ID Generation Strategy

**Severity:** 🔴 Critical  
**Effort:** Medium (2 hours)  
**Files Affected:** All Entity classes

**Current Inconsistency:**
```csharp
// User.cs - ID NOT generated
private User(Email email, string firstName, string lastName, string passwordHash)
{
    //Id = Guid.NewGuid();  // ← COMMENTED OUT
    Raise(new UserRegisteredDomainEvent(Id)); // ⚠️ Id is default(Guid) here!
}

// FlashcardReview (LanguageAccount) - ID IS generated
internal FlashcardReview(Guid flashcardId, DateTime reviewDate, ReviewResult reviewResult)
{
    Id = Guid.NewGuid();  // ← GENERATED
}
```

**Dlaczego to jest krytyczne:**
- Domain events reference uninitialized IDs (`Guid.Empty` = 00000000-0000-0000-0000-000000000000)
- Event handlers receive invalid IDs
- Inconsistent behavior across entities
- Potential data integrity issues

**Problem z Guid.Empty:**
```csharp
// W konstruktorze User
Raise(new UserRegisteredDomainEvent(Id)); // Id = default(Guid) ❌

// Domain event handler otrzymuje:
public void Handle(UserRegisteredDomainEvent domainEvent)
{
    var userId = domainEvent.UserId; // = 00000000-0000-0000-0000-000000000000 ❌
}
```

---

**Decision Required - Choose ONE Strategy:**

### Option A: Database-Generated IDs (Rekomendowane w review, ale wymaga zmian)

**Kluczowa zmiana:** Domain events NIE mogą być podnoszone w konstruktorach przy Option A.

```csharp
// Domain entity - NIE ustawiaj Id, NIE podnoś events w konstruktorze
private User(Email email, string firstName, string lastName, string passwordHash)
{
    Email = email;
    FirstName = firstName;
    LastName = lastName;
    PasswordHash = passwordHash;
    // Brak Raise() tutaj!
}

// Command Handler
public async Task<Result<Guid>> Handle(RegisterUserCommand command, ...)
{
    var user = User.Create(...);
    await _repository.Add(user); // EF Core generuje Id tutaj
    await _unitOfWork.SaveChangesAsync(); // Id jest teraz poprawne
    
    // TERAZ podnieś event z poprawnym Id
    user.Raise(new UserRegisteredDomainEvent(user.Id)); // ✅ Poprawne Id
    await _eventDispatcher.DispatchAsync(user.DomainEvents);
    
    return user.Id;
}
```

**Wady Option A:**
- Wymaga zmiany architektury (usunięcie Raise() z konstruktorów)
- Domain events podnoszone PO SaveChangesAsync (problem z timingiem - zobacz sekcję 4.4)
- Więcej kodu w command handlers

### Option B: Constructor-Generated IDs (Prostsze, bardziej zgodne z DDD)

```csharp
// Domain entity - generuj Id w konstruktorze
private User(Email email, string firstName, string lastName, string passwordHash)
{
    Id = Guid.NewGuid(); // ✅ Generuj Id
    Email = email;
    FirstName = firstName;
    LastName = lastName;
    PasswordHash = passwordHash;
    
    Raise(new UserRegisteredDomainEvent(Id)); // ✅ Poprawne Id
}
```

**Zalety Option B:**
- Id dostępne od razu
- Domain events mogą być podnoszone w konstruktorze
- Wszystko w jednej transakcji (event handler + SaveChanges)
- Lepsza testowalność (nie potrzebujesz bazy danych żeby testować)

**Wady Option B:**
- Id generowane w domenie, nie przez bazę

---

**Rekomendacja:**

**Użyj Option B (Constructor-Generated IDs)** z dodatkową poprawką w ApplicationDbContext:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Publish events PRZED save - wszystko w jednej transakcji
    await PublishDomainEventsAsync(); 
    int result = await base.SaveChangesAsync(cancellationToken);
    return result;
}
```

To rozwiązanie:
- Id jest generowane w konstruktorze (Guid.NewGuid())
- Event podnoszony w konstruktorze z poprawnym Id
- Event handler wywoływany PRZED SaveChangesAsync
- Wszystko w jednej transakcji

**Dla long-term:** Rozważ Outbox Pattern (zobacz sekcję 4.4) dla eventual consistency w systemach produkcyjnych.

### 4.4 Transaction Boundary Issue with Domain Events

**Severity:** 🔴 Critical  
**Effort:** High (4-8 hours)  
**Files Affected:** `src\Infrastructure\Database\ApplicationDbContext.cs`

**Current Problem:**
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    int result = await base.SaveChangesAsync(cancellationToken); // ← Transaction 1 COMMITS
    await PublishDomainEventsAsync(); // ← Events published AFTER commit
    return result;
}
```

**Scenario:**
```
Time | Action                          | Database State
-----|--------------------------------|----------------------------------
T1   | User submits review            | -
T2   | FlashcardReview saved          | FlashcardReview created
T3   | Transaction 1 commits          | ✅ Review persisted
T4   | Event published                | -
T5   | Handler starts                 | -
T6   | Database connection timeout!   | ❌ SrsState NOT updated
T7   | Transaction 2 fails            | ⚠️ INCONSISTENT STATE
```

**Solution Option 1: Publish Events BEFORE SaveChangesAsync (Recommended for MVP)**
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    await PublishDomainEventsAsync(); // ✅ Same transaction
    int result = await base.SaveChangesAsync(cancellationToken);
    return result;
}
```

**Solution Option 2: Outbox Pattern (Best for Production)**
```csharp
// Create new entity
public class OutboxMessage : Entity
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
}

// ApplicationDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var outboxMessages = ChangeTracker
        .Entries<Entity>()
        .SelectMany(e => e.Entity.DomainEvents)
        .Select(domainEvent => new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().Name,
            Content = JsonSerializer.Serialize(domainEvent),
            OccurredOnUtc = DateTime.UtcNow
        })
        .ToList();
    
    await OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
    ChangeTracker.Entries<Entity>().ToList().ForEach(e => e.Entity.ClearDomainEvents());
    
    return await base.SaveChangesAsync(cancellationToken);
}
```

**Rekomendacja:** 
- **Short-term:** Use Option 1 (publish before save)
- **Long-term:** Implement Option 2 (outbox pattern)

### 4.5 DateTime.UtcNow in Domain Logic

**Severity:** 🟡 High  
**Effort:** Medium (2-3 hours)  
**Files Affected:** `src\Domain\SRS\SrsState.cs`, `src\Domain\SRS\FlashcardReview.cs`

**⚠️ UWAGA:** Po refaktoringu SRS zgodnie z sekcją 2.3 (SrsState jako child Flashcard w Flashcard System Context), pliki `src\Domain\SRS\SrsState.cs` i `src\Domain\SRS\FlashcardReview.cs` mogą zostać przeniesione lub usunięte. Ta sekcja może wymagać aktualizacji po implementacji planu refaktoringu.

**Problem:**
```csharp
// SrsState.cs
nextReviewDate: DateTime.UtcNow); // ❌ Not testable

// FlashcardReview.cs
if (reviewDate > DateTime.UtcNow) // ❌ Not testable
    throw new ArgumentException("Review date cannot be in the future.");
```

**Dlaczego to jest problem:**
- **Not testable:** Can't control time in unit tests
- **Can't test edge cases:** Midnight boundaries, timezone issues
- **Couples domain to system clock**
- **Hard to reproduce bugs** related to timing

**Solution:**
```csharp
// 1. Create abstraction
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

// 2. Implement for production
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

// 3. Inject into domain methods via command handlers
public static SrsState CreateInitialState(Guid flashcardId, DateTime currentTime)
{
    return new SrsState(flashcardId, interval: 0, easeFactor: 2.5, repetitions: 0, nextReviewDate: currentTime);
}

public void UpdateState(ReviewResult reviewResult, DateTime currentTime)
{
    NextReviewDate = currentTime.AddDays(Interval);
}
```

### 4.6 Duplicate Domain Event Handlers

**Severity:** 🟡 High  
**Effort:** Low (15 minutes)  
**Files Affected:** `src\Application\Users\Register\UserRegisteredDomainEventHandler.cs`

**Problem:**
```csharp
// W tym samym pliku istnieją dwa handlery dla tego samego eventu:

internal sealed class UserRegisteredDomainEventHandler : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // TODO: Send an email verification link, etc.
        return Task.CompletedTask;
    }
}

internal sealed class UserRegisteredDomainEventHandler1 : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // TODO: Send an email verification link, etc.
        return Task.CompletedTask;
    }
}
```

**Dlaczego to jest problem:**
- Duplicate handlers dla tego samego eventu
- Confusion - który handler jest właściwy?
- Obie są puste (TODO comments)
- Potencjalnie oba zostaną wywołane (double execution)
- Violates DRY principle

**Rozwiązanie:**
```csharp
// Usunąć UserRegisteredDomainEventHandler1
// Zostawić tylko UserRegisteredDomainEventHandler i zaimplementować logikę
```

---

### 4.7 No Concurrency Control

**Severity:** 🟡 High  
**Effort:** Low (1 hour)  
**Files Affected:** All entities via base class

---

### 4.8 Unnecessary Using Statements

**Severity:** 🟢 Low  
**Effort:** Low (15 minutes)  
**Files Affected:** 
- `src\Application\LanguageAccounts\Events\FlashcardReviewedDomainEventHandler.cs`
- `src\Infrastructure\SRS\SrsStateRepository.cs`
- `src\Infrastructure\LanguageAccount\FlashcardReviewRepository.cs`
- `src\Application\LanguageAccounts\Commands\AddFlashcardReview\AddFlashcardReviewCommandHandler.cs`
- `src\Infrastructure\Users\UserReadRepository.cs`
- `src\Infrastructure\Users\UserRepository.cs`
- `src\Infrastructure\DependencyInjection.cs`

**⚠️ UWAGA:** Po refaktoringu SRS zgodnie z sekcją 2.3 (SrsState jako child Flashcard), pliki `src\Infrastructure\SRS\SrsStateRepository.cs` i `src\Infrastructure\LanguageAccount\FlashcardReviewRepository.cs` mogą zostać usunięte. Ta sekcja może wymagać aktualizacji po implementacji planu refaktoringu.

**Problem 1: FlashcardReviewedDomainEventHandler.cs**
```csharp
using Application.Abstractions.Data;  // ✅ Potrzebne (IUnitOfWork)
using Application.SRS;               // ❌ NIEPOTRZEBNE - nie używane w kodzie
using Domain.SRS.Events;             // ✅ Potrzebne (FlashcardReviewedDomainEvent)
using Domain.SRS;                    // ✅ Potrzebne (SrsState, ReviewResult)
using SharedKernel;                  // ✅ Potrzebne (IDomainEventHandler)
```

**Problem 2: SrsStateRepository.cs**
```csharp
using Application.Abstractions.Data;     // ✅ Potrzebne
using Application.Abstractions.Repository; // ✅ Potrzebne
using Application.SRS;                   // ❌ NIEPOTRZEBNE - interfejs jest w Application.SRS ale klasa nie używa typów z tego namespace
using Domain.SRS;                        // ✅ Potrzebne (SrsState)
using Microsoft.EntityFrameworkCore;        // ✅ Potrzebne
```

**Problem 3: FlashcardReviewRepository.cs**
```csharp
using Application.Abstractions.Data;     // ✅ Potrzebne
using Application.Abstractions.Repository; // ✅ Potrzebne
using Application.SRS;                   // ❌ NIEPOTRZEBNE - interfejs jest w Application.SRS ale klasa nie używa typów z tego namespace
using Domain.SRS;                        // ✅ Potrzebne (FlashcardReview)
```

**Problem 4: AddFlashcardReviewCommandHandler.cs**
```csharp
using Application.Abstractions.Authentication; // ✅ Potrzebne
using Application.Abstractions.Data;          // ✅ Potrzebne
using Application.Abstractions.Messaging;    // ✅ Potrzebne
using Application.SRS;                      // ❌ NIEPOTRZEBNE - nie używane w kodzie
using Domain.LanguageAccount;               // ✅ Potrzebne
using Domain.SRS.ValueObjects;              // ✅ Potrzebne (ReviewResult)
using Domain.Users;                         // ✅ Potrzebne
using SharedKernel;                         // ✅ Potrzebne
```

**Problem 5: UserReadRepository.cs**
```csharp
using System.Data;                 // ✅ Potrzebne (IDbConnection)
using Application.Users;           // ✅ Potrzebne
using Application.Users.DTO;       // ✅ Potrzebne (UserReadModel)
using Dapper;                      // ✅ Potrzebne (QueryAsync)
using Domain.Users;                // ✅ Potrzebne
using Microsoft.Identity.Client;    // ❌ NIEPOTRZEBNE - nie używane w kodzie
```

**Problem 6: UserRepository.cs**
```csharp
using Application.Abstractions.Data;     // ✅ Potrzebne
using Application.Abstractions.Repository; // ✅ Potrzebne
using Application.Users;                 // ✅ Potrzebne
using Domain.Users;                      // ✅ Potrzebne
using Domain.Users.ValueObjects;          // ✅ Potrzebne (Email)
using Microsoft.EntityFrameworkCore;        // ✅ Potrzebne
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database; // ❌ NIEPOTRZEBNE - nie używane w kodzie
```

**Problem 7: DependencyInjection.cs**
```csharp
using Application.Abstractions.Authentication; // ✅ Potrzebne
using Application.Abstractions.Data;          // ✅ Potrzebne
using Application.LanguageAccounts;           // ✅ Potrzebne
using Application.SRS;                        // ✅ Potrzebne
using Application.Users;                     // ✅ Potrzebne
// ...
using Application.SRS;                        // ❌ DUPLIKAT - linia 13 powtarza linię 6
```

**Dlaczego to jest problem:**
- `using Application.SRS;` jest niepotrzebne w tych plikach - nie używamy żadnych typów z tego namespace w kodzie
- `using Microsoft.Identity.Client;` jest niepotrzebne - nie używane w kodzie
- `using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;` jest niepotrzebne - nie używane w kodzie
- Clutter w kodzie
- Może mylić innych deweloperów
- Narusza clean code principles

**Rozwiązanie:**
```csharp
// Usunąć linię z każdego pliku:
using Application.SRS;
using Microsoft.Identity.Client;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

// Usunąć duplikat w DependencyInjection.cs (linia 13)
```

**Weryfikacja:**
- Build projektu
- Brak błędów kompilacji

**Czas:** 15 minut

---

### 4.7 No Concurrency Control

**Severity:** 🟡 High  
**Effort:** Low (1 hour)  
**Files Affected:** All entities via base class

**⚠️ UWAGA:** Po refaktoringu SRS zgodnie z sekcją 2.3 (SrsState jako child Flashcard), problem concurrency control będzie dotyczył Flashcard aggregate, nie osobnego SrsState. Przykład w tej sekcji może wymagać aktualizacji.

---

## Problem - Race Condition:

**Co się dzieje gdy dwóch użytkowników jednocześnie ocenia tę samą fiszkę:**

```
Czas | User A                          | User B          | Baza danych
-----|--------------------------------|-----------------|------------------
T1   | Czyta SrsState (Interval=1)    |                 | Interval=1
T2   |                                | Czyta SrsState (Interval=1) | Interval=1
T3   | Aktualizuje Interval=3         |                 | Interval=1
T4   | Zapisuje                       |                 | Interval=3 ✅
T5   |                                | Aktualizuje Interval=2 | Interval=3
T6   |                                | Zapisuje       | Interval=2 ❌
```

**Problem:** User B cicho nadpisuje zmiany User A. User A ustawiał Interval=3, ale User B nadpisał to Interval=2.

---

## Dlaczego to się dzieje:

**Bez concurrency control:**
```csharp
// User A
var srsState = await _repository.GetById(flashcardId); // Interval=1
srsState.UpdateState(ReviewResult.Good); // Interval=3
await _repository.Save(srsState);

// User B (w tym samym czasie)
var srsState = await _repository.GetById(flashcardId); // Interval=1 (stary stan!)
srsState.UpdateState(ReviewResult.Hard); // Interval=2
await _repository.Save(srsState); // Nadpisuje User A!
```

**Problem:** User B czyta STARY stan danych (przed zmianami User A), ponieważ obaj użytkownicy czytają w tym samym czasie. Następnie User B nadpisuje zmiany User A.

---

## Rozwiązanie - Concurrency Token:

**Dodaj RowVersion do Entity:**
```csharp
public abstract class Entity
{
    public Guid Id { get; private set; }

    [Timestamp] // ✅ EF Core automatycznie aktualizuje przy każdym save
    public byte[]? RowVersion { get; protected set; }
}
```

**Jak to działa:**
```
Czas | User A                          | User B          | Baza danych (RowVersion)
-----|--------------------------------|-----------------|------------------
T1   | Czyta SrsState (Interval=1, RowVersion=101)    | Interval=1, RowVersion=101
T2   |                                | Czyta SrsState (Interval=1, RowVersion=101) | Interval=1, RowVersion=101
T3   | Aktualizuje Interval=3         |                 | Interval=1, RowVersion=101
T4   | Zapisuje (RowVersion=101)      |                 | Interval=3, RowVersion=102 ✅
T5   |                                | Aktualizuje Interval=2 | Interval=3, RowVersion=102
T6   |                                | Zapisuje (RowVersion=101) | ❌ ERROR!
```

**T6 - User B dostaje DbUpdateConcurrencyException:**
- User B próbuje zapisać z RowVersion=101
- Baza ma RowVersion=102 (zmienione przez User A)
- EF Core wyrzuca DbUpdateConcurrencyException
- User B dostaje komunikat "The record was modified by another user"

---

## Obsługa błędu:

```csharp
try
{
    await _repository.Save(srsState);
}
catch (DbUpdateConcurrencyException ex)
{
    // Zwróć błąd 409 Conflict
    return Result.Failure(Error.Conflict(
        "Concurrency.Conflict",
        "The record was modified by another user. Please refresh and try again."));
}
```

---

## Podsumowanie:

**Bez concurrency control:** Drugi użytkownik cicho nadpisuje zmiany pierwszego - inconsistent state.

**Z concurrency control:** Drugi użytkownik dostaje błąd i musi odświeżyć dane - consistent state.

To jest standardowy mechanizm w EF Core - `[Timestamp]` attribute automatycznie generuje concurrency token (row version) który jest aktualizowany przy każdym save do bazy.

**Solution:**
```csharp
// Add to base Entity
public abstract class Entity
{
    [Timestamp] // ✅ EF Core concurrency token
    public byte[]? RowVersion { get; protected set; }
}

// Handle concurrency exceptions
catch (DbUpdateConcurrencyException ex)
{
    var error = Error.Conflict("Concurrency.Conflict", "The record was modified by another user. Please refresh and try again.");
    context.Response.StatusCode = StatusCodes.Status409Conflict;
    await context.Response.WriteAsJsonAsync(error);
}
```

---

## 5. Value Objects - Czy Przesadziłeś?

### 5.1 Aktualne Value Objects

1. **Email** ✅ **WYMIERNE**
   - Ma validation logic (regex)
   - Jest immutable
   - Jest używany w wielu miejscach

2. **Language** ✅ **WYMIERNE**
   - Ma predefiniowane wartości (static properties)
   - Ma validation (supported languages)
   - Jest immutable

3. **ProficiencyLevel** ⚠️ **MOŻNA UPROŚCIĆ**
   - Obecnie: wrapper na enum
   - Rekomendacja: Użyć enum bezpośrednio w entity
   - Argument: Nie ma dodatkowej logiki poza przechowywaniem wartości

4. **Synonyms** ✅ **WYMIERNE**
   - Ma validation logic (unique, non-empty)
   - Ma business logic (case-insensitive comparison)
   - Jest immutable

5. **ReviewResult** ⚠️ **MOŻNA UPROŚCIĆ**
   - Obecnie: wrapper na enum
   - Rekomendacja: Użyć enum bezpośrednio w entity
   - Argument: Nie ma dodatkowej logiki

### 4.2 Ocena

**LICZBA VALUE OBJECTS: 5**  
**PRZEZADZONE: NIE**  
**ALE: 2 z nich są niepotrzebne (ProficiencyLevel, ReviewResult)**

**Rekomendacje:**
1. Zachować: Email, Language, Synonyms
2. Usunąć: ProficiencyLevel, ReviewResult (użyć enum bezpośrednio)
3. Rozważyć dodanie: FlashcardId, LanguageAccountId jako Value Objects (jeśli mają validation logic)

---

## 5. Unit of Work - Podejście

### 5.1 Aktualna Implementacja

```csharp
public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly IApplicationDbContext _context;
    // ... delegation to context
}
```

**Użycie w ApplicationDbContext:**
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    await PublishDomainEventsAsync();  // BEFORE SaveChanges
    int result = await base.SaveChangesAsync(cancellationToken);
    return result;
}
```

### 5.2 Ocena

**⚠️ WYMAGA POPRAWEK**

**Problemy:**
1. **UnitOfWork jest redundant:** DbContext już jest Unit of Work w EF Core
2. **Domain Events BEFORE SaveChanges:** To jest ryzykowne - jeśli handler fail, to transaction rollback ale event już published
3. **Brak commit/rollback w command handlers:** Nie widzę użycia UnitOfWork w handlers

**Rekomendacje:**
1. **Usunąć UnitOfWork class** - DbContext już pełni tę rolę
2. **Domain Events AFTER SaveChanges** - dla eventual consistency
3. **LUB: Domain Events w osobnej transaction** - dla outbox pattern
4. **Użyć transaction scope w command handlers** jeśli potrzebne:

```csharp
public async Task<Result> Handle(CreateUserCommand command, CancellationToken ct)
{
    using var transaction = await _context.BeginTransactionAsync(ct);
    try
    {
        var user = User.Create(...);
        await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync(ct);
        await _context.CommitTransactionAsync(ct);
        return Result.Success();
    }
    catch
    {
        await _context.RollbackTransactionAsync(ct);
        throw;
    }
}
```

**Alternatywa (lepsza):**
```csharp
// Usunąć UnitOfWork
// Używać bezpośrednio DbContext w handlers
public async Task<Result> Handle(CreateUserCommand command, CancellationToken ct)
{
    var user = User.Create(...);
    await _userRepository.AddAsync(user);
    await _context.SaveChangesAsync(ct);  // DbContext already handles transaction
    return Result.Success();
}
```

---

## 6. Konfiguracja EF Core

### 6.1 UsePropertyAccessMode.Field

```csharp
builder.Navigation(la => la.FlashcardCollections)
    .HasField("_flashcardCollections")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**✅ TO JEST POPRAWNE I REKOMENDOWANE**

**Dlaczego:**
1. **Encapsulation:** Kolekcje są private backing fields
2. **Invariants:** IReadOnlyCollection zwraca AsReadOnly() - chroni przed modyfikacją
3. **EF Core Access:** EF Core może bezpośrednio modyfikować private field
4. **Performance:** Unika property getter overhead

**Uwaga:** Musisz upewnić się, że wszystkie kolekcje używają backing fields:

```csharp
private readonly List<FlashcardCollection> _flashcardCollections = new();
public IReadOnlyCollection<FlashcardCollection> FlashcardCollections => _flashcardCollections.AsReadOnly();
```

To jest poprawnie zaimplementowane.

---

### 6.2 Value Object Conversions

**Email:**
```csharp
builder.Property(u => u.Email)
    .HasConversion(
        email => email.Value,
        value => new Email(value))
    .HasMaxLength(255);
```
✅ **Poprawne** - simple conversion

**Language (JSON):**
```csharp
builder.Property(la => la.Language)
    .HasConversion(
        language => JsonSerializer.Serialize(language, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<Language>(json, (JsonSerializerOptions?)null)!)
    .HasColumnType("nvarchar(100)");
```
⚠️ **PROBLEMATYCZNE** - JSON serialization w bazie danych

---

## Problem - Language przechowywane jako JSON w bazie:

**Aktualnie w bazie danych:**
```sql
-- Tabela LanguageAccounts
Id | UserId | Language
---|--------|------------------------------------------
1  | 123    | {"Code":"en","FullName":"English"}    ← JSON string
2  | 456    | {"Code":"pl","FullName":"Polish"}     ← JSON string
3  | 789    | {"Code":"de","FullName":"German"}     ← JSON string
```

**W kodzie C#:**
```csharp
public class LanguageAccount : Entity
{
    public Language Language { get; private set; } // Value object
}

// EF Core conversion:
// READ: JSON string → Language object (deserialize)
// WRITE: Language object → JSON string (serialize)
```

---

## Problemy z JSON serialization:

### 1. **Performance**

**Co się dzieje przy każdym read/write:**
```csharp
// READ z bazy
var account = await _repository.GetById(id);
// ↑ EF Core musi deserialize JSON string → Language object
// ↑ To zajmuje CPU time

// WRITE do bazy
account.Language = Language.English;
await _repository.Save(account);
// ↑ EF Core musi serialize Language object → JSON string
// ↑ To zajmuje CPU time
```

**Problem:** Każdy read/write wymaga serialize/deserialize, co spowalnia aplikację.

---

### 2. **Queryability**

**Problem:** Nie możesz query po Language.Code w SQL bez JSON functions.

```sql
-- ❌ NIE DZIAŁA - Language to JSON string, nie kolumna
SELECT * FROM LanguageAccounts WHERE Language.Code = 'en';

-- ✅ DZIAŁA ale jest powolne - używa JSON functions
SELECT * FROM LanguageAccounts
WHERE JSON_VALUE(Language, '$.Code') = 'en';
```

**Problem:**
- SQL query jest skomplikowany
- JSON functions są wolniejsze niż zwykłe kolumny
- Nie możesz użyć index na Language.Code
- Query optimizer nie może zoptymalizować zapytań

---

### 3. **Null handling**

**Problem w kodzie:**
```csharp
json => JsonSerializer.Deserialize<Language>(json, (JsonSerializerOptions?)null)!
// ↑ null! suppress warning, ale może być null
```

**Co się dzieje gdy JSON jest null:**
```csharp
// W bazie: Language = NULL
// EF Core wywoła: JsonSerializer.Deserialize<Language>(null)
// Wynik: null
// Ale kod mówi: null! (to nie może być null)
// ↑ Potencjalny NullReferenceException
```

---

## Rozwiązania:

### Option 1: Store as two columns (Najprostsze)

**W bazie danych:**
```sql
-- Tabela LanguageAccounts
Id | UserId | LanguageCode | LanguageFullName
---|--------|--------------|------------------
1  | 123    | en           | English
2  | 456    | pl           | Polish
3  | 789    | de           | German
```

**EF Core configuration:**
```csharp
builder.Property(la => la.LanguageCode).HasMaxLength(10);
builder.Property(la => la.LanguageFullName).HasMaxLength(100);
```

**W entity:**
```csharp
public class LanguageAccount : Entity
{
    private string _languageCode = string.Empty;
    private string _languageFullName = string.Empty;

    // Computed property - tworzy Language object z dwóch kolumn
    public Language Language => new Language(_languageCode, _languageFullName);
}
```

**Zalety:**
- ✅ Brak serialize/deserialize - szybkie read/write
- ✅ Można query po LanguageCode w SQL: `WHERE LanguageCode = 'en'`
- ✅ Można użyć index na LanguageCode
- ✅ Proste w implementacji

---

### Option 2: Owned Entity Type (Najbardziej DDD-friendly)

**W bazie danych (EF Core automatycznie utworzy dwie kolumny):**
```sql
-- Tabela LanguageAccounts
Id | UserId | Language_Code | Language_FullName
---|--------|--------------|------------------
1  | 123    | en           | English
2  | 456    | pl           | Polish
3  | 789    | de           | German
```

**EF Core configuration:**
```csharp
builder.OwnsOne(la => la.Language, language => {
    language.Property(l => l.Code).HasColumnName("Language_Code").HasMaxLength(10);
    language.Property(l => l.FullName).HasColumnName("Language_FullName").HasMaxLength(100);
});
```

**W entity:**
```csharp
public class LanguageAccount : Entity
{
    public Language Language { get; private set; } // Value object
}
```

**Zalety:**
- ✅ Brak serialize/deserialize
- ✅ Można query po Language.Code
- ✅ Zgodne z DDD (Language jako value object)
- ✅ EF Core automatycznie mapuje dwie kolumny

---

## Synonyms (pipe-separated) - to jest OK:

```csharp
builder.Property(f => f.Synonyms)
    .HasConversion(
        synonyms => string.Join("|", synonyms.Value),
        value => new Synonyms(value.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList()))
    .HasMaxLength(1000);
```

**Dlaczego to jest OK:**
- Synonyms to lista stringów - naturalne jest przechowanie jako pipe-separated
- Query po synonimach jest rzadki
- Performance impact jest minimalny
- Proste w implementacji

⚠️ **AKCEPTOWALNE, ale nie idealne**

**Problemy:**
1. **Delimiter collision:** Co jeśli synonym zawiera "|"
2. **Queryability:** Nie można query po pojedynczym synonimie

**Rekomendacja:** 
- Dla MVP: OK (proste)
- Dla production: Rozważyć osobną tabelę Synonyms (one-to-many)

---

### 6.3 Inne Konfiguracje

**Indexy:**
```csharp
builder.HasIndex(u => u.Email).IsUnique();
```
✅ **Poprawne**

**Cascade Delete:**
```csharp
builder.HasMany(la => la.FlashcardCollections)
       .WithOne(fc => fc.LanguageAccount)
       .HasForeignKey(fc => fc.LanguageAccountId)
       .OnDelete(DeleteBehavior.Cascade);
```
✅ **Poprawne dla agregatów** - child entities są cascade deleted

**MaxLength:**
```csharp
builder.Property(fc => fc.Name)
    .IsRequired()
    .HasMaxLength(200);
```
✅ **Poprawne** - data validation na poziomie bazy

---

## 7. Podsumowanie Błędów i Rekomendacji

### 🔴 Krytyczne Problemy

1. **SrsState nie jest częścią Flashcard aggregate**
   - Problem: Violates aggregate boundaries
   - Fix: Przenieść SrsState do Flashcard aggregate

2. **Domain Events BEFORE SaveChanges**
   - Problem: Event published before transaction commit
   - Fix: Publish AFTER SaveChanges lub użyć Outbox pattern

3. **Language jako JSON w bazie**
   - Problem: Performance i queryability
   - Fix: Użyć Owned Entity Type lub osobnych kolumn

### ⚠️ Ważne Ulepszenia

1. **ProficiencyLevel i ReviewResult jako Value Objects**
   - Problem: Niepotrzebne wrapper na enum
   - Fix: Użyć enum bezpośrednio w entity

2. **UnitOfWork redundant**
   - Problem: DbContext już jest Unit of Work
   - Fix: Usunąć UnitOfWork class, używać DbContext bezpośrednio

3. **Flashcard.Update public**
   - Problem: Może naruszać invariants agregatu
   - Fix: Przenieść logikę do FlashcardCollection.UpdateFlashcard

### ✅ Dobre Praktyki

1. **Write/Read repository split** - zachować
2. **UsePropertyAccessMode.Field** - zachować
3. **Domain events** - świetne, ale poprawić timing
4. **Private constructors + factory methods** - zachować
5. **IReadOnlyCollection dla kolekcji** - zachować
6. **Value objects z validation** - Email, Synonyms są świetne

---

## 8. Kluczowe Zmian do Wdrożenia

### Priority 1 (Krytyczne)

1. **Naprawić SrsState aggregate**
```csharp
// Przenieść do Flashcard aggregate
public class Flashcard : Entity
{
    private SrsState? _srsState;
    public SrsState? SrsState => _srsState;
    
    public void UpdateSrsState(ReviewResult result)
    {
        _srsState = _srsState?.UpdateState(result) ?? SrsState.CreateInitialState(Id);
    }
}
```

2. **Naprawić domain events timing**
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    int result = await base.SaveChangesAsync(cancellationToken);  // FIRST
    await PublishDomainEventsAsync();  // THEN
    return result;
}
```

3. **Naprawić Language persistence**
```csharp
builder.OwnsOne(la => la.Language, language => {
    language.Property(l => l.Code).HasMaxLength(10).IsRequired();
    language.Property(l => l.FullName).HasMaxLength(100).IsRequired();
});
```

### Priority 2 (Ważne)

4. **Usunąć niepotrzebne Value Objects**
```csharp
// Zamiast:
public ProficiencyLevel ProficiencyLevel { get; private set; }

// Użyć:
public Enums.ProficiencyLevel ProficiencyLevel { get; private set; }
```

5. **Usunąć UnitOfWork**
```csharp
// Używać DbContext bezpośrednio w handlers
await _context.SaveChangesAsync(ct);
```

6. **Poprawić Flashcard.Update**
```csharp
// W FlashcardCollection:
public void UpdateFlashcard(Guid flashcardId, string sentenceWithBlanks, ...)
{
    var flashcard = _flashcards.FirstOrDefault(f => f.Id == flashcardId);
    flashcard?.Update(sentenceWithBlanks, ...);  // Internal access
}
```

---

## 9. Ocena Ogólna

**Score: 7/10**

**Plusy:**
- Dobre zrozumienie DDD podstaw
- Poprawne aggregate boundaries (z wyjątkiem SRS)
- Dobry CQRS implementation
- Domain events są używane
- Value objects z validation są świetne
- Clean architecture structure

**Minusy:**
- SrsState aggregate boundary violation
- Domain events timing issue
- Niepotrzebne value objects (over-engineering)
- Redundant Unit of Work
- JSON serialization w bazie (anti-pattern)

**Rekomendacja:** Projekt jest na dobrej drodze, ale wymaga poprawek krytycznych przed production use.

---

## 10. Szczegółowy Plan Implementacji Następnych Kroków

### 10.1 Priorytetyzacją i Estymowany Czas

| Priorytet | Krok | Estymowany Czas | Zależności | Wpływ na System |
|-----------|------|-----------------|------------|-----------------|
| 🔴 P0 | Naprawić bounded context violation w SrsState | 15 min | Brak | Krytyczne - naprawia architekturę |
| 🔴 P0 | Usunąć duplicate FlashcardReview | 30 min | Brak | Krytyczne - usuwa confusion |
| 🔴 P0 | Naprawić ID generation strategy | 2h | Brak | Krytyczne - naprawia domain events |
| 🔴 P0 | Naprawić domain events timing | 1h | Po ID generation | Krytyczne - naprawia consistency |
| 🔴 P0 | Naprawić Language persistence | 1h | Brak | Krytyczne - naprawia performance |
| 🟡 P1 | Usunąć duplicate UserRegisteredDomainEventHandler1 | 15 min | Brak | Ważne - usuwa confusion |
| 🟡 P1 | Usunąć niepotrzebne using statements | 15 min | Brak | Ważne - poprawia clean code |
| 🟡 P1 | Usunąć CreateUser z repozytorium | 30 min | Brak | Ważne - naprawia separation of concerns |
| 🟡 P1 | Naprawić mixed read/write w IUserWriteRepository | 30 min | Brak | Ważne - naprawia consistency |
| 🟡 P1 | Usunąć niepotrzebne Value Objects | 1h | Brak | Ważne - upraszcza kod |
| 🟡 P1 | Usunąć UnitOfWork | 1h | Po domain events timing | Ważne - upraszcza architekturę |
| 🟡 P1 | Dodać IDateTimeProvider | 2h | Brak | Ważne - poprawia testowalność |
| 🟡 P1 | Dodać concurrency control | 1h | Brak | Ważne - zapobiega race conditions |
| 🟢 P2 | Dodać authorization specifications | 4h | Brak | Ulepszenie - poprawia maintainability |
| 🟢 P2 | Dodać integration tests | 8h | Po wszystkich P0/P1 | Ulepszenie - poprawia jakość |
| 🟢 P2 | Dodać unit tests dla domain logic | 8h | Po IDateTimeProvider | Ulepszenie - poprawia jakość |
| 🟢 P3 | Implementować Outbox pattern | 16h | Po domain events timing | Opcjonalne - dla production |
| 🟢 P3 | Dodać audit trail | 8h | Brak | Opcjonalne - dla security |
| 🟢 P3 | Rozważyć Event Sourcing dla SRS | 40h | Po wszystkich | Opcjonalne - długoterminowe |

**Całkowity czas dla P0-P1:** ~12.5 godzin  
**Całkowity czas dla P0-P2:** ~32.5 godzin  
**Całkowity czas dla wszystkich:** ~88.5 godzin

---

### 10.2 Szczegółowe Kroki Implementacji

#### 🔴 P0-1: Naprawić Bounded Context Violation w SrsState

**Cel:** Usunąć import z LanguageAccount w SRS context

**Pliki do zmiany:**
- `src\Domain\SRS\SrsState.cs`

**Kroki:**
1. Usunąć linię 5: `using Domain.LanguageAccount.Enums;`
2. Dodać linię: `using Domain.SRS.Enums;`
3. Uprościć pattern matching w liniach 40, 42, 57, 61:
   ```csharp
   // Z:
   if (reviewResult.Value is (Enums.ReviewResult)ReviewResult.Again or (Enums.ReviewResult)ReviewResult.DontKnow)
   
   // Na:
   if (reviewResult.Value is ReviewResult.Again or ReviewResult.DontKnow)
   ```

**Weryfikacja:**
- Build projektu
- Uruchomienie testów (jeśli istnieją)
- Sprawdzenie czy nie ma innych importów między bounded contexts

**Czas:** 15 minut

---

#### 🔴 P0-2: Usunąć Duplicate Domain Models - FlashcardReview

**Cel:** Usunąć nieużywane FlashcardReview z LanguageAccount context

**Pliki do usunięcia:**
- `src\Domain\LanguageAccount\FlashcardReview.cs`
- `src\Domain\LanguageAccount\ValueObjects\ReviewResult.cs`
- `src\Domain\LanguageAccount\Enums\ReviewResult.cs`

**Kroki:**
1. Przeszukaj całe solution po `Domain.LanguageAccount.FlashcardReview`
2. Potwierdź, że nie ma żadnych referencji
3. Usuń 3 pliki
4. Uruchom `dotnet build` aby potwierdzić

**Weryfikacja:**
- Build succeeds
- Brak błędów kompilacji
- Brak warningów o nieznanych typach

**Czas:** 30 minut

---

#### 🔴 P0-3: Naprawić ID Generation Strategy

**Cel:** Ujednolicić strategię generowania ID - użyć database-generated IDs

**Pliki do zmiany:**
- `src\Domain\Users\User.cs`
- `src\Domain\LanguageAccount\LanguageAccount.cs`
- `src\Domain\LanguageAccount\FlashcardCollection.cs`
- `src\Domain\LanguageAccount\Flashcard.cs`
- `src\Domain\SRS\SrsState.cs`
- `src\Domain\SRS\FlashcardReview.cs`
- Wszystkie command handlers które podnoszą domain events

**Decyzja:** Użyć **Option A - Database-Generated IDs**

**Kroki:**
1. Usunąć wszystkie `Id = Guid.NewGuid()` z konstruktorów
2. Usunąć podnoszenie domain events z konstruktorów
3. Przenieść podnoszenie domain events do command handlers po `SaveChangesAsync`:
   ```csharp
   public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken ct)
   {
       var user = User.Create(...);
       await _repository.Add(user);
       await _unitOfWork.SaveChangesAsync(ct);
       
       // Raise event AFTER SaveChanges (Id is now assigned)
       user.Raise(new UserRegisteredDomainEvent(user.Id));
       return user.Id;
   }
   ```

**Weryfikacja:**
- Build projektu
- Testy integracyjne (jeśli istnieją)
- Sprawdzenie czy domain events mają poprawne Id

**Czas:** 2 godziny

---

#### 🔴 P0-4: Naprawić Domain Events Timing

**Cel:** Publikować domain events PRZED SaveChanges dla immediate consistency

**Pliki do zmiany:**
- `src\Infrastructure\Database\ApplicationDbContext.cs`

**Kroki:**
1. Zmienić kolejność w `SaveChangesAsync`:
   ```csharp
   public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
   {
       await PublishDomainEventsAsync();  // ✅ FIRST - same transaction
       int result = await base.SaveChangesAsync(cancellationToken);
       return result;
   }
   ```

**Argumentacja:**
- Immediate consistency - wszystkie zmiany w jednej transakcji
- Event handlers mogą fail transaction
- Proste rozwiązanie dla MVP
- W przyszłości można przejść na Outbox pattern

**Weryfikacja:**
- Build projektu
- Testy integracyjne dla domain events
- Sprawdzenie czy FlashcardReviewedDomainEvent poprawnie aktualizuje SrsState

**Czas:** 1 godzina

---

#### 🔴 P0-5: Naprawić Language Persistence

**Cel:** Zastąpić JSON serialization Owned Entity Type

**Pliki do zmiany:**
- `src\Infrastructure\LanguageAccount\LanguageAccountConfiguration.cs`

**Kroki:**
1. Zastąpić JSON conversion Owned Entity Type:
   ```csharp
   builder.OwnsOne(la => la.Language, language =>
   {
       language.Property(l => l.Code).HasMaxLength(10).IsRequired();
       language.Property(l => l.FullName).HasMaxLength(100).IsRequired();
   });
   ```

2. Dodać migration:
   ```bash
   dotnet ef migrations add ChangeLanguageToOwnedType
   dotnet ef database update
   ```

**Argumentacja:**
- Owned Entity Type jest natywnie wspierany przez EF Core
- Lepsza queryability (można query po Code)
- Lepsza performance (brak JSON serialization)
- Brak delimiter collision

**Weryfikacja:**
- Build projektu
- Migration succeeds
- Testy integracyjne dla LanguageAccount

**Czas:** 1 godzina

---

#### 🟡 P1-1: Usunąć Duplicate UserRegisteredDomainEventHandler1

**Cel:** Usunąć duplikat handlera dla UserRegisteredDomainEvent

**Pliki do zmiany:**
- `src\Application\Users\Register\UserRegisteredDomainEventHandler.cs`

**Kroki:**
1. Usunąć drugą klasę `UserRegisteredDomainEventHandler1` (linie 15-22)
2. Zostawić tylko `UserRegisteredDomainEventHandler`
3. Zaimplementować logikę w handlerze (jeśli potrzebna):
   ```csharp
   internal sealed class UserRegisteredDomainEventHandler : IDomainEventHandler<UserRegisteredDomainEvent>
   {
       private readonly IEmailService _emailService;
       
       public UserRegisteredDomainEventHandler(IEmailService emailService)
       {
           _emailService = emailService;
       }
       
       public async Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
       {
           await _emailService.SendVerificationEmailAsync(domainEvent.Email, cancellationToken);
       }
   }
   ```

**Argumentacja:**
- Duplicate handlers dla tego samego eventu
- Confusion - który handler jest właściwy?
- Potencjalnie oba zostaną wywołane (double execution)
- Violates DRY principle

**Weryfikacja:**
- Build projektu
- Sprawdzenie czy DI poprawnie rejestruje handler
- Test dla handlera (jeśli zaimplementowano)

**Czas:** 15 minut

---

#### 🟡 P1-2: Usunąć Niepotrzebne Using Statements

**Cel:** Usunąć niepotrzebne using statements

**Pliki do zmiany:**
- `src\Application\LanguageAccounts\Events\FlashcardReviewedDomainEventHandler.cs`
- `src\Infrastructure\SRS\SrsStateRepository.cs`
- `src\Infrastructure\LanguageAccount\FlashcardReviewRepository.cs`
- `src\Application\LanguageAccounts\Commands\AddFlashcardReview\AddFlashcardReviewCommandHandler.cs`
- `src\Infrastructure\Users\UserReadRepository.cs`
- `src\Infrastructure\Users\UserRepository.cs`
- `src\Infrastructure\DependencyInjection.cs`

**Kroki:**
1. Usunąć linię `using Application.SRS;` z pierwszych 4 plików
2. Usunąć linię `using Microsoft.Identity.Client;` z UserReadRepository.cs
3. Usunąć linię `using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;` z UserRepository.cs
4. Usunąć duplikat `using Application.SRS;` z DependencyInjection.cs (linia 13)
5. Uruchomić `dotnet build` aby potwierdzić brak błędów kompilacji

**Argumentacja:**
- Nie używamy żadnych typów z Application.SRS namespace w tych plikach
- Microsoft.Identity.Client nie jest używane w UserReadRepository.cs
- Microsoft.EntityFrameworkCore.DbLoggerCategory.Database nie jest używane w UserRepository.cs
- Duplicate using Application.SRS w DependencyInjection.cs
- Clutter w kodzie
- Może mylić innych deweloperów
- Narusza clean code principles

**Weryfikacja:**
- Build projektu
- Brak błędów kompilacji

**Czas:** 15 minut

---

#### 🟡 P1-3: Usunąć CreateUser z Repozytorium

**Cel:** Repositories nie powinny tworzyć domain entities

**Pliki do zmiany:**
- `src\Infrastructure\Users\UserRepository.cs`
- `src\Application\Users\IUserRepository.cs`
- `src\Application\Users\Register\RegisterUserCommandHandler.cs`

**Kroki:**
1. Usunąć metodę `CreateUser` z `UserRepository`
2. Zmienić interfejs:
   ```csharp
   public interface IUserWriteRepository
   {
       void Add(User user);
       Task<bool> UserExists(string email);
       Task<User?> GetUserByEmail(string email, CancellationToken cancellationToken);
   }
   ```
3. Zmienić implementation:
   ```csharp
   public void Add(User user)
   {
       _applicationDbContext.Users.Add(user);
   }
   ```
4. Zmienić command handler:
   ```csharp
   var user = User.Create(...);  // ✅ Create in application layer
   userWriteRepository.Add(user);
   await unitOfWork.SaveChangesAsync(cancellationToken);
   ```

**Argumentacja:**
- Repositories should only persist, not create
- Business logic stays in application layer
- Consistent with other repositories

**Weryfikacja:**
- Build projektu
- Testy rejestracji użytkownika

**Czas:** 30 minut

---

#### 🟡 P1-4: Naprawić Mixed Read/Write w IUserWriteRepository

**Cel:** Interfejs Write nie powinien mieć metod read

**Pliki do zmiany:**
- `src\Application\Users\IUserRepository.cs`
- `src\Application\Users\IUserReadRepository.cs`
- `src\Application\Users\Register\RegisterUserCommandHandler.cs`

**Kroki:**
1. Przenieść metody read do `IUserReadRepository`:
   ```csharp
   public interface IUserReadRepository
   {
       Task<UserReadModel> GetByEmailAsync(string email);
       Task<UserReadModel> GetById(Guid userId);
       Task<bool> UserExists(string email);
   }
   ```
2. Zmienić `IUserWriteRepository`:
   ```csharp
   public interface IUserWriteRepository
   {
       void Add(User user);
       Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
   }
   ```

**Argumentacja:**
- Clear separation of concerns
- Consistent with CQRS pattern
- Better testability

**Weryfikacja:**
- Build projektu
- Testy command handlers

**Czas:** 30 minut

---

#### 🟡 P1-5: Usunąć Niepotrzebne Value Objects

**Cel:** Uprościć ProficiencyLevel i ReviewResult do enum

**Pliki do zmiany:**
- `src\Domain\LanguageAccount\ValueObjects\ProficiencyLevel.cs`
- `src\Domain\LanguageAccount\ValueObjects\ReviewResult.cs`
- `src\Domain\LanguageAccount\LanguageAccount.cs`
- `src\Domain\SRS\SrsState.cs`
- `src\Domain\SRS\FlashcardReview.cs`
- Wszystkie miejsca gdzie używane są te VO

**Kroki:**
1. Usunąć pliki Value Objects
2. Zmienić w entities:
   ```csharp
   // Z:
   public ProficiencyLevel ProficiencyLevel { get; private set; }
   
   // Na:
   public Enums.ProficiencyLevel ProficiencyLevel { get; private set; }
   ```
3. Zaktualizować konfiguracje EF Core:
   ```csharp
   builder.Property(la => la.ProficiencyLevel)
       .IsRequired();  // Usunąć HasConversion
   ```

**Argumentacja:**
- Nie ma dodatkowej logiki w tych VO
- Enumy są prostsze i bardziej idiomatyczne
- Mniej kodu do utrzymania

**Weryfikacja:**
- Build projektu
- Migration dla zmian w bazie
- Testy wszystkich command handlers

**Czas:** 1 godzina

---

#### 🟡 P1-6: Usunąć UnitOfWork

**Cel:** DbContext już jest Unit of Work w EF Core

**Pliki do zmiany:**
- `src\Application\Abstractions\Data\IUnitOfWork.cs`
- `src\Infrastructure\Database\UnitOfWork.cs`
- `src\Infrastructure\DependencyInjection.cs`
- Wszystkie command handlers

**Kroki:**
1. Usunąć `IUnitOfWork` i `UnitOfWork` class
2. Zmienić command handlers:
   ```csharp
   // Z:
   await _unitOfWork.SaveChangesAsync(cancellationToken);
   
   // Na:
   await _context.SaveChangesAsync(cancellationToken);
   ```
3. Usunąć rejestrację z DI

**Argumentacja:**
- DbContext już implementuje Unit of Work pattern
- Redundant abstraction
- Mniej kodu do utrzymania

**Weryfikacja:**
- Build projektu
- Testy wszystkich command handlers

**Czas:** 1 godzina

---

#### 🟡 P1-7: Dodać IDateTimeProvider

**Cel:** Umożliwić testowanie logiki zależnej od czasu

**Pliki do utworzenia:**
- `src\Application\Abstractions\Time\IDateTimeProvider.cs`
- `src\Infrastructure\Time\DateTimeProvider.cs`

**Pliki do zmiany:**
- `src\Domain\SRS\SrsState.cs`
- `src\Domain\SRS\FlashcardReview.cs`
- `src\Application\LanguageAccounts\Commands\AddFlashcardReview\AddFlashcardReviewCommandHandler.cs`
- `src\Infrastructure\DependencyInjection.cs`

**Kroki:**
1. Utworzyć interfejs:
   ```csharp
   namespace Application.Abstractions.Time;
   
   public interface IDateTimeProvider
   {
       DateTime UtcNow { get; }
   }
   ```
2. Utworzyć implementation:
   ```csharp
   namespace Infrastructure.Time;
   
   public sealed class DateTimeProvider : IDateTimeProvider
   {
       public DateTime UtcNow => DateTime.UtcNow;
   }
   ```
3. Zarejestrować w DI:
   ```csharp
   services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
   ```
4. Zmienić domain entities:
   ```csharp
   public static SrsState CreateInitialState(Guid flashcardId, DateTime currentTime)
   {
       return new SrsState(flashcardId, interval: 0, easeFactor: 2.5, 
                          repetitions: 0, nextReviewDate: currentTime);
   }
   
   public void UpdateState(ReviewResult reviewResult, DateTime currentTime)
   {
       // ... logic ...
       NextReviewDate = currentTime.AddDays(Interval);
   }
   ```
5. Zmienić command handlers:
   ```csharp
   var review = FlashcardReview.Create(command.FlashcardId, 
                                        dateTimeProvider.UtcNow, reviewResult);
   ```

**Argumentacja:**
- Domain logic staje się testowalna
- Można testować edge cases (midnight, timezone)
- Możliwe stubowanie w testach

**Weryfikacja:**
- Build projektu
- Napisać unit tests dla SrsState z kontrolowanym czasem
- Testy command handlers

**Czas:** 2 godziny

---

#### 🟡 P1-8: Dodać Concurrency Control

**Cel:** Zapobiec race conditions na SrsState

**Pliki do zmiany:**
- `src\SharedKernel\Entity.cs`
- `src\Web.Api\Extensions\ExceptionHandlingMiddleware.cs` (lub utworzyć)
- Command handlers z retry logic (opcjonalnie)

**Kroki:**
1. Dodać do Entity:
   ```csharp
   public abstract class Entity
   {
       [Timestamp]
       public byte[]? RowVersion { get; protected set; }
   }
   ```
2. Dodać migration:
   ```bash
   dotnet ef migrations add AddConcurrencyTokens
   dotnet ef database update
   ```
3. Dodać exception handling:
   ```csharp
   catch (DbUpdateConcurrencyException ex)
   {
       var error = Error.Conflict("Concurrency.Conflict", 
           "The record was modified by another user. Please refresh and try again.");
       context.Response.StatusCode = StatusCodes.Status409Conflict;
       await context.Response.WriteAsJsonAsync(error);
   }
   ```

**Argumentacja:**
- Zapobiega silent data loss
- Industry standard dla concurrent updates
- EF Core obsługuje automatycznie

**Weryfikacja:**
- Build projektu
- Migration succeeds
- Test concurrency scenarios

**Czas:** 1 godzina

---

#### 🟢 P2-1: Dodać Authorization Specifications

**Cel:** Wydzielić authorization logic do osobnych specifications

**Pliki do utworzenia:**
- `src\Application\LanguageAccounts\Authorization\CanAccessFlashcardSpecification.cs`
- `src\Application\LanguageAccounts\Authorization\CanAccessLanguageAccountSpecification.cs`
- `src\Domain\Authorization\IAuthorizationSpecification.cs`

**Kroki:**
1. Utworzyć interfejs base:
   ```csharp
   public interface IAuthorizationSpecification<T>
   {
       Task<bool> IsSatisfiedByAsync(T entity, Guid userId, CancellationToken cancellationToken = default);
       Error UnauthorizedError { get; }
   }
   ```
2. Utworzyć specifications:
   ```csharp
   public sealed class CanAccessFlashcardSpecification(IFlashcardRepository repository)
       : IAuthorizationSpecification<Guid>
   {
       public async Task<bool> IsSatisfiedByAsync(Guid flashcardId, Guid userId, CancellationToken cancellationToken = default)
       {
           var flashcard = await repository.GetByIdWithCollectionAsync(flashcardId, cancellationToken);
           return flashcard?.FlashcardCollection?.LanguageAccount?.UserId == userId;
       }
       
       public Error UnauthorizedError => UserErrors.Unauthorized();
   }
   ```
3. Zmienić command handlers:
   ```csharp
   bool canAccess = await canAccessFlashcard.IsSatisfiedByAsync(command.FlashcardId, userContext.UserId, cancellationToken);
   if (!canAccess)
       return Result.Failure<Guid>(canAccessFlashcard.UnauthorizedError);
   ```

**Argumentacja:**
- Separation of concerns
- Reusable authorization logic
- Better testability
- Consistent authorization across handlers

**Weryfikacja:**
- Build projektu
- Unit tests dla specifications
- Integration tests dla command handlers

**Czas:** 4 godziny

---

#### 🟢 P2-2: Dodać Integration Tests

**Cel:** Testować aggregate invariants i end-to-end scenarios

**Pliki do utworzenia:**
- `tests\IntegrationTests\LanguageAccount\LanguageAccountAggregateTests.cs`
- `tests\IntegrationTests\Users\UserAggregateTests.cs`
- `tests\IntegrationTests\SRS\SrsStateTests.cs`

**Kroki:**
1. Utworzyć test base class z in-memory database
2. Napisać testy dla aggregate invariants:
   ```csharp
   [Fact]
   public async Task CreateFlashcardCollection_ShouldAddToLanguageAccount()
   {
       // Arrange
       var account = LanguageAccount.Create(userId, proficiencyLevel, language);
       
       // Act
       var collection = account.CreateCollection("Test Collection");
       
       // Assert
       account.FlashcardCollections.Should().Contain(collection);
   }
   
   [Fact]
   public void UpdateProficiencyLevel_ShouldNotAllowDowngrade()
   {
       // Arrange
       var account = LanguageAccount.Create(userId, ProficiencyLevel.B2, language);
       
       // Act & Assert
       account.Invoking(a => a.UpdateProficiencyLevel(ProficiencyLevel.A1))
           .Should().Throw<InvalidOperationException>();
   }
   ```
3. Napisać testy dla domain events:
   ```csharp
   [Fact]
   public async Task RegisterUser_ShouldRaiseUserRegisteredEvent()
   {
       // Arrange
       var user = User.Create(email, firstName, lastName, passwordHash);
       
       // Act
       await repository.Add(user);
       await unitOfWork.SaveChangesAsync();
       
       // Assert
       user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredDomainEvent);
   }
   ```

**Argumentacja:**
- Weryfikacja aggregate invariants
- Testowanie domain events
- Zapobieganie regresjom
- Dokumentacja zachowania systemu

**Weryfikacja:**
- Wszystkie testy pass
- Code coverage > 80% dla domain layer

**Czas:** 8 godzin

---

#### 🟢 P2-3: Dodać Unit Tests dla Domain Logic

**Cel:** Testować domain logic w izolacji

**Pliki do utworzenia:**
- `tests\Domain.Tests\SRS\SrsStateTests.cs`
- `tests\Domain.Tests\LanguageAccount\LanguageAccountTests.cs`
- `tests\Domain.Tests\Users\UserTests.cs`

**Kroki:**
1. Napisać testy dla SRS algorithm:
   ```csharp
   [Theory]
   [InlineData(ReviewResult.Again, 0, 2.3, 1)]
   [InlineData(ReviewResult.Know, 1, 2.55, 1)]
   [InlineData(ReviewResult.Easy, 1, 2.65, 3)]
   public void UpdateState_ShouldCalculateCorrectInterval(ReviewResult result, int expectedInterval, double expectedEaseFactor, int expectedRepetitions)
   {
       // Arrange
       var state = SrsState.CreateInitialState(flashcardId, fixedTime);
       var reviewResult = new ReviewResult(result);
       
       // Act
       state.UpdateState(reviewResult, fixedTime);
       
       // Assert
       state.Interval.Should().Be(expectedInterval);
       state.EaseFactor.Should().Be(expectedEaseFactor);
       state.Repetitions.Should().Be(expectedRepetitions);
   }
   ```
2. Napisać testy dla value objects:
   ```csharp
   [Theory]
   [InlineData("invalid-email")]
   [InlineData("test@")]
   [InlineData("@example.com")]
   public void Email_WithInvalidFormat_ShouldThrow(string invalidEmail)
   {
       // Act & Assert
       Assert.Throws<ArgumentException>(() => new Email(invalidEmail));
   }
   ```

**Argumentacja:**
- Testowanie domain logic w izolacji
- Szybkie testy (nie potrzebują bazy)
- Dokumentacja algorytmów
- Refactoring safety net

**Weryfikacja:**
- Wszystkie testy pass
- Code coverage > 90% dla domain logic

**Czas:** 8 godzin

---

#### 🟢 P3-1: Implementować Outbox Pattern (Opcjonalne)

**Cel:** Eventual consistency dla domain events w production

**Pliki do utworzenia:**
- `src\Domain\OutboxMessage.cs`
- `src\Infrastructure\Database\ApplicationDbContext.cs` (zmiany)
- `src\Infrastructure\Outbox\ProcessOutboxMessagesJob.cs`

**Kroki:**
1. Utworzyć OutboxMessage entity
2. Zmienić SaveChangesAsync aby zapisywać outbox messages
3. Utworzyć background service do przetwarzania outbox
4. Dodać retry logic dla failed messages

**Argumentacja:**
- Eventual consistency
- Event handlers nie blokują transakcji
- Retry mechanism dla failures
- Industry best practice

**Weryfikacja:**
- Integration tests dla outbox
- Performance tests
- Monitoring outbox processing

**Czas:** 16 godzin

---

#### 🟢 P3-2: Dodać Audit Trail (Opcjonalne)

**Cel:** Śledzenie wszystkich zmian w systemie

**Pliki do utworzenia:**
- `src\Domain\AuditLog.cs`
- `src\Infrastructure\Database\ApplicationDbContext.cs` (zmiany)

**Kroki:**
1. Utworzyć AuditLog entity
2. Automatycznie logować wszystkie zmiany w SaveChangesAsync
3. Dodać interfejs do query audit logs

**Argumentacja:**
- Compliance requirements
- Debugging
- Security
- Accountability

**Weryfikacja:**
- Integration tests
- Performance impact assessment

**Czas:** 8 godzin

---

#### 🟢 P3-3: Rozważyć Event Sourcing dla SRS (Opcjonalne)

**Cel:** Pełna historia zmian dla SRS algorithm

**Kroki:**
1. Zaprojektować event schema dla SRS
2. Zaimplementować Event Store
3. Zaimplementować projections
4. Zmigrować istniejące dane

**Argumentacja:**
- Pełna historia zmian
- Time travel debugging
- Audit trail
- Możliwość replay

**Weryfikacja:**
- Proof of concept
- Performance tests
- Migration plan

**Czas:** 40 godzin

---

### 10.3 Test Strategy

#### Unit Tests
- **Cel:** Testowanie domain logic w izolacji
- **Framework:** xUnit, FluentAssertions
- **Coverage target:** > 90% dla domain layer
- **Przykłady:**
  - SRS algorithm correctness
  - Value object validation
  - Aggregate invariants
  - Domain events

#### Integration Tests
- **Cel:** Testowanie aggregate invariants z prawdziwą bazą
- **Framework:** xUnit, EF Core In-Memory
- **Coverage target:** > 80% dla application layer
- **Przykłady:**
  - End-to-end command execution
  - Domain events propagation
  - Repository operations
  - Concurrency scenarios

#### End-to-End Tests
- **Cel:** Testowanie całego systemu
- **Framework:** Playwright lub SpecFlow
- **Coverage target:** Kluczowe user journeys
- **Przykłady:**
  - Rejestracja użytkownika
  - Tworzenie konta językowego
  - Dodawanie fiszki
  - Przeglądanie fiszek

---

### 10.4 Monitoring i Observability

Po wdrożeniu zmian, dodać:

1. **Structured Logging**
   - Serilog z sinks: Console, File, Seq (dev), Application Insights (prod)
   - Log levels: Debug (dev), Information (prod)
   - Correlation ID dla trace requests

2. **Metrics**
   - Prometheus metrics dla ASP.NET Core
   - Custom metrics: flashcards_reviewed_total, srs_algorithm_duration
   - Health checks: /health, /health/ready

3. **Distributed Tracing**
   - OpenTelemetry
   - Export do Jaeger (dev), Application Insights (prod)
   - Trace command handlers i domain events

---

### 10.5 Deployment Strategy

#### Development
1. Wdrożyć P0 zmiany lokalnie
2. Uruchomić wszystkie testy
3. Code review
4. Commit do branch `fix/critical-ddd-issues`

#### Staging
1. Merge do `main`
2. Deploy do staging
3. Run integration tests
4. Manual testing
5. Performance testing

#### Production
1. Blue-green deployment
2. Monitor metrics
3. Ready to rollback
4. Post-deployment verification

---

### 10.6 Risk Mitigation

| Ryzyko | Mitigation | Plan B |
|--------|------------|-------|
| Domain events timing failure | Wdrożyć P0-4 natychmiast | Outbox pattern (P3-1) |
| Concurrency issues | Wdrożyć P1-6 | Pessimistic locking |
| Performance regression | Benchmark przed/after | Caching layer |
| Data loss podczas migration | Backup przed migration | Rollback plan |
| Test coverage insufficient | Wymaganie >80% coverage | Manual testing |

---

### 10.7 Success Criteria

**Technical:**
- [ ] Wszystkie P0 zmiany wdrożone
- [ ] Wszystkie P1 zmiany wdrożone
- [ ] Test coverage > 80% (integration), > 90% (unit)
- [ ] Zero krytycznych bugs w production
- [ ] Performance regression < 5%

**Process:**
- [ ] Code review dla wszystkich zmian
- [ ] Documentation zaktualizowana
- [ ] Team code review session
- [ ] Knowledge sharing session

**Business:**
- [ ] Brak downtime podczas deployment
- [ ] Zero data loss
- [ ] User experience nie pogorszony
- [ ] System skalowalny dla 1000+ concurrent users

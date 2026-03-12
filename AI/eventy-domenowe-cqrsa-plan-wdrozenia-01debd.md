# Plan Wdrożenia Aplikacji z Eventami Domenowymi i CQRS

Ten dokument przedstawia szczegółowy plan wdrożenia aplikacji do nauki języków oparty na eventach domenowych, wzorcu CQRS i zdefiniowanej domenie.

## Eventy Domenowe w Systemie

### 1. User Management Context
- **UserRegistered** - użytkownik zarejestrowany
- **UserUpdated** - dane użytkownika zaktualizowane
- **TokensGranted** - przyznano tokeny użytkownikowi
- **TokensSpent** - zużyto tokeny
- **MonthlyTokenReset** - miesięczny reset tokenów

### 2. Language Learning Context
- **LanguageAccountCreated** - utworzono konto językowe
- **StudySessionCompleted** - zakończono sesję nauki
- **LearningSettingsUpdated** - zaktualizowano ustawienia nauki
- **WordAddedToVocabulary** - dodano słowo do słownictwa
- **VocabularyTypeChanged** - zmieniono typ słownictwa (aktywne/pasywne)

### 3. Flashcard System Context
- **FlashcardCreated** - utworzono fiszkę
- **FlashcardReviewed** - przejrzano fiszkę
- **FlashcardCollectionCreated** - utworzono kolekcję fiszek
- **FlashcardsGenerated** - wygenerowano fiszki przez AI
- **WordTranslationAdded** - dodano tłumaczenie słowa

### 4. Grammar Exercises Context
- **ExerciseCompleted** - ukończono ćwiczenie
- **ExerciseGenerated** - wygenerowano ćwiczenie
- **GrammarRuleCreated** - utworzono regułę gramatyczną
- **QuizGenerated** - wygenerowano quiz

### 5. Language Production Context
- **SpeakingSessionStarted** - rozpoczęto sesję mówienia
- **SpeakingSessionEvaluated** - oceniono sesję mówienia
- **SentenceCreated** - utworzono zdanie
- **EssaySubmitted** - przesłano esej
- **EssayAnalyzed** - przeanalizowano esej

### 6. AI Integration Context
- **AiContentGenerated** - wygenerowano treść przez AI
- **EvaluationRequested** - zgłoszono prośbę o ocenę
- **EvaluationCompleted** - ukończono ocenę
- **TokensConsumed** - zużyto tokeny
- **ContentQualityAssessed** - oceniono jakość treści

---

## Plan Wdrożenia Krok po Kroku

### Krok 1: Fundamenty Infrastruktury (Tydzień 1-2)

#### 1.1 Struktura Projektu
```
src/
├── Domain/
│   ├── Common/
│   │   ├── Events/
│   │   ├── ValueObjects/
│   │   └── Enums/
│   ├── UserManagement/
│   ├── LanguageLearning/
│   ├── FlashcardSystem/
│   ├── GrammarExercises/
│   ├── LanguageProduction/
│   └── AIIntegration/
├── Application/
│   ├── Commands/
│   ├── Queries/
│   ├── EventHandlers/
│   └── Services/
├── Infrastructure/
│   ├── Events/
│   │   ├── Dispatchers/
│   │   ├── Handlers/
│   │   └── Stores/
│   ├── Persistence/
│   └── ExternalServices/
└── API/
    ├── Controllers/
    └── DTOs/
```

#### 1.2 Podstawowe Klasy Eventów
```csharp
// Base domain event
public abstract class DomainEvent
{
    public Guid Id { get; }
    public DateTime OccurredOn { get; }
    public string EventType { get; }
}

// Event interface
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

// Event dispatcher interface
public interface IEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent);
}

// Event handler interface
public interface IEventHandler<T> where T : IDomainEvent
{
    Task HandleAsync(T domainEvent);
}
```

#### 1.3 Konfiguracja MediatR/CQRS
```csharp
// MediatR configuration
services.AddMediatR(typeof(Startup).Assembly);

// Event dispatcher implementation
public class MediatREventDispatcher : IEventDispatcher
{
    private readonly IMediator _mediator;
    
    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        await _mediator.Publish(domainEvent);
    }
}
```

### Krok 2: User Management Context (Tydzień 3-4)

#### 2.1 Encje i Agregaty
```csharp
// User aggregate root
public class User : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PasswordHash { get; private set; }
    
    private readonly List<LanguageAccount> _languageAccounts = new();
    public IReadOnlyCollection<LanguageAccount> LanguageAccounts => _languageAccounts.AsReadOnly();
    
    public static User Create(string email, string firstName, string lastName, string passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHash
        };
        
        user.AddDomainEvent(new UserRegistered(user.Id, email, firstName, lastName));
        return user;
    }
}

// TokenBalance entity
public class TokenBalance : Entity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int MonthlyTokens { get; private set; }
    public int UsedTokens { get; private set; }
    public DateTime ResetDate { get; private set; }
    
    public void GrantTokens(int amount, string reason)
    {
        MonthlyTokens += amount;
        AddDomainEvent(new TokensGranted(UserId, amount, reason));
    }
    
    public bool ConsumeTokens(int amount, string operation)
    {
        if (UsedTokens + amount > MonthlyTokens)
            return false;
            
        UsedTokens += amount;
        AddDomainEvent(new TokensSpent(UserId, amount, operation));
        return true;
    }
}
```

#### 2.2 Commandy i Query
```csharp
// Commands
public record RegisterUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password
) : IRequest<Guid>;

public record GrantTokensCommand(
    Guid UserId,
    int Amount,
    string Reason
) : IRequest<bool>;

// Queries
public record GetUserQuery(Guid UserId) : IRequest<UserDto>;
public record GetTokenBalanceQuery(Guid UserId) : IRequest<TokenBalanceDto>;
```

#### 2.3 Event Handlery
```csharp
// UserRegistered event handler
public class UserRegisteredHandler : INotificationHandler<UserRegistered>
{
    private readonly ITokenBalanceRepository _tokenRepository;
    
    public async Task Handle(UserRegistered notification, CancellationToken cancellationToken)
    {
        // Grant initial tokens to new user
        var tokenBalance = TokenBalance.Create(notification.UserId, 1000); // 1000 monthly tokens
        await _tokenRepository.AddAsync(tokenBalance);
    }
}

// TokensSpent event handler
public class TokensSpentHandler : INotificationHandler<TokensSpent>
{
    private readonly ITokenUsageRepository _usageRepository;
    
    public async Task Handle(TokensSpent notification, CancellationToken cancellationToken)
    {
        var usage = TokenUsageRecord.Create(
            notification.UserId,
            notification.Operation,
            notification.Amount
        );
        await _usageRepository.AddAsync(usage);
    }
}
```

### Krok 3: Language Learning Context (Tydzień 5-6)

#### 3.1 Encje i Agregaty
```csharp
// LanguageAccount aggregate root
public class LanguageAccount : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid KnownLanguageId { get; private set; }
    public Guid TargetLanguageId { get; private set; }
    public ProficiencyLevel ProficiencyLevel { get; private set; }
    
    private readonly List<Vocabulary> _vocabulary = new();
    public IReadOnlyCollection<Vocabulary> Vocabulary => _vocabulary.AsReadOnly();
    
    public static LanguageAccount Create(
        Guid userId,
        Guid knownLanguageId,
        Guid targetLanguageId,
        ProficiencyLevel proficiencyLevel
    )
    {
        var account = new LanguageAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            KnownLanguageId = knownLanguageId,
            TargetLanguageId = targetLanguageId,
            ProficiencyLevel = proficiencyLevel
        };
        
        account.AddDomainEvent(new LanguageAccountCreated(
            account.Id,
            userId,
            knownLanguageId,
            targetLanguageId,
            proficiencyLevel
        ));
        
        return account;
    }
    
    public void AddWordToVocabulary(Guid wordId, VocabularyType type)
    {
        var vocabulary = Vocabulary.Create(Id, wordId, type);
        _vocabulary.Add(vocabulary);
        
        AddDomainEvent(new WordAddedToVocabulary(Id, wordId, type));
    }
}

// Vocabulary entity
public class Vocabulary : Entity
{
    public Guid Id { get; private set; }
    public Guid LanguageAccountId { get; private set; }
    public Guid WordId { get; private set; }
    public VocabularyType Type { get; private set; }
    public MasteryLevel MasteryLevel { get; private set; }
    public DateTime LastReviewed { get; private set; }
    
    public static Vocabulary Create(Guid languageAccountId, Guid wordId, VocabularyType type)
    {
        return new Vocabulary
        {
            Id = Guid.NewGuid(),
            LanguageAccountId = languageAccountId,
            WordId = wordId,
            Type = type,
            MasteryLevel = MasteryLevel.Unknown,
            LastReviewed = DateTime.UtcNow
        };
    }
}
```

#### 3.2 Commandy i Query
```csharp
// Commands
public record CreateLanguageAccountCommand(
    Guid UserId,
    Guid KnownLanguageId,
    Guid TargetLanguageId,
    ProficiencyLevel ProficiencyLevel
) : IRequest<Guid>;

public record AddWordToVocabularyCommand(
    Guid LanguageAccountId,
    Guid WordId,
    VocabularyType Type
) : IRequest<bool>;

// Queries
public record GetLanguageAccountsQuery(Guid UserId) : IRequest<List<LanguageAccountDto>>;
public record GetVocabularyQuery(Guid LanguageAccountId) : IRequest<List<VocabularyDto>>;
```

#### 3.3 Event Handlery
```csharp
// LanguageAccountCreated handler
public class LanguageAccountCreatedHandler : INotificationHandler<LanguageAccountCreated>
{
    private readonly IFlashcardCollectionRepository _collectionRepository;
    private readonly IAiService _aiService;
    
    public async Task Handle(LanguageAccountCreated notification, CancellationToken cancellationToken)
    {
        // Create default flashcard collection
        var collection = FlashcardCollection.Create(
            notification.LanguageAccountId,
            "Default Collection",
            "Default collection for new words"
        );
        
        await _collectionRepository.AddAsync(collection);
        
        // Generate initial flashcards using AI
        await _aiService.GenerateInitialFlashcardsAsync(notification.LanguageAccountId);
    }
}
```

### Krok 4: Flashcard System Context (Tydzień 7-8)

#### 4.1 Encje i Agregaty
```csharp
// FlashcardCollection aggregate root
public class FlashcardCollection : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid LanguageAccountId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    
    private readonly List<Flashcard> _flashcards = new();
    public IReadOnlyCollection<Flashcard> Flashcards => _flashcards.AsReadOnly();
    
    public static FlashcardCollection Create(
        Guid languageAccountId,
        string title,
        string description
    )
    {
        var collection = new FlashcardCollection
        {
            Id = Guid.NewGuid(),
            LanguageAccountId = languageAccountId,
            Title = title,
            Description = description
        };
        
        collection.AddDomainEvent(new FlashcardCollectionCreated(collection.Id, languageAccountId, title));
        return collection;
    }
    
    public void AddFlashcard(Flashcard flashcard)
    {
        _flashcards.Add(flashcard);
        AddDomainEvent(new FlashcardCreated(flashcard.Id, Id, flashcard.WordTranslationId));
    }
}

// Flashcard entity
public class Flashcard : Entity
{
    public Guid Id { get; private set; }
    public Guid CollectionId { get; private set; }
    public Guid WordTranslationId { get; private set; }
    public string FrontTemplate { get; private set; }
    public string BackTemplate { get; private set; }
    public FlashcardType Type { get; private set; }
    public DifficultyLevel Difficulty { get; private set; }
    
    public static Flashcard Create(
        Guid collectionId,
        Guid wordTranslationId,
        string frontTemplate,
        string backTemplate,
        FlashcardType type,
        DifficultyLevel difficulty
    )
    {
        return new Flashcard
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            WordTranslationId = wordTranslationId,
            FrontTemplate = frontTemplate,
            BackTemplate = backTemplate,
            Type = type,
            Difficulty = difficulty
        };
    }
}
```

#### 4.2 Commandy i Query
```csharp
// Commands
public record CreateFlashcardCollectionCommand(
    Guid LanguageAccountId,
    string Title,
    string Description
) : IRequest<Guid>;

public record GenerateFlashcardsCommand(
    Guid LanguageAccountId,
    int Count,
    DifficultyLevel Difficulty
) : IRequest<List<Guid>>;

public record ReviewFlashcardCommand(
    Guid FlashcardId,
    Guid LanguageAccountId,
    ReviewResult Result
) : IRequest<bool>;

// Queries
public record GetFlashcardCollectionsQuery(Guid LanguageAccountId) : IRequest<List<FlashcardCollectionDto>>;
public record GetFlashcardsForReviewQuery(Guid LanguageAccountId, int Count) : IRequest<List<FlashcardDto>>;
```

#### 4.3 Event Handlery
```csharp
// FlashcardReviewed handler
public class FlashcardReviewedHandler : INotificationHandler<FlashcardReviewed>
{
    private readonly IVocabularyRepository _vocabularyRepository;
    private readonly IStudySessionRepository _sessionRepository;
    
    public async Task Handle(FlashcardReviewed notification, CancellationToken cancellationToken)
    {
        // Update vocabulary mastery level
        var vocabulary = await _vocabularyRepository.GetByWordAndAccountAsync(
            notification.WordId,
            notification.LanguageAccountId
        );
        
        if (vocabulary != null)
        {
            vocabulary.UpdateMasteryLevel(notification.Result);
            await _vocabularyRepository.UpdateAsync(vocabulary);
        }
        
        // Update study session statistics
        var session = await _sessionRepository.GetActiveSessionAsync(notification.LanguageAccountId);
        if (session != null)
        {
            session.AddFlashcardReview(notification.Result);
            await _sessionRepository.UpdateAsync(session);
        }
    }
}
```

### Krok 5: Event Sourcing i Event Store (Tydzień 9)

#### 5.1 Event Store Implementation
```csharp
// Event store interface
public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion);
    Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId);
    Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId, int fromVersion);
}

// Event store implementation
public class SqlEventStore : IEventStore
{
    private readonly DbContext _context;
    
    public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion)
    {
        foreach (var @event in events)
        {
            var eventDescriptor = new EventDescriptor
            {
                EventId = @event.EventId,
                AggregateId = aggregateId,
                EventType = @event.GetType().Name,
                EventData = JsonSerializer.Serialize(@event, @event.GetType()),
                Version = expectedVersion + 1,
                Timestamp = @event.OccurredOn
            };
            
            _context.EventDescriptors.Add(eventDescriptor);
        }
        
        await _context.SaveChangesAsync();
    }
}
```

#### 5.2 Aggregate Base Class with Event Sourcing
```csharp
public abstract class EventSourcedAggregateRoot : AggregateRoot
{
    private readonly List<IDomainEvent> _changes = new();
    
    public Guid Id { get; protected set; }
    public int Version { get; protected set; }
    
    public IReadOnlyCollection<IDomainEvent> GetUncommittedChanges() => _changes.AsReadOnly();
    public void MarkChangesAsCommitted() => _changes.Clear();
    
    protected void ApplyChange(IDomainEvent @event)
    {
        Apply(@event);
        _changes.Add(@event);
    }
    
    private void Apply(IDomainEvent @event)
    {
        // Use reflection to call Apply method for specific event type
        var method = GetType().GetMethod(
            "Apply",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { @event.GetType() },
            null
        );
        
        method?.Invoke(this, new object[] { @event });
        Version++;
    }
    
    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        foreach (var @event in history)
        {
            Apply(@event);
            Version++;
        }
    }
}
```

### Krok 6: AI Integration Context (Tydzień 10-11)

#### 6.1 Encje i Agregaty
```csharp
// AiGeneratedContent aggregate root
public class AiGeneratedContent : AggregateRoot
{
    public Guid Id { get; private set; }
    public ContentType ContentType { get; private set; }
    public Guid ContentId { get; private set; }
    public string GenerationPrompt { get; private set; }
    public string ModelUsed { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public int TokensUsed { get; private set; }
    
    public static AiGeneratedContent Create(
        ContentType contentType,
        Guid contentId,
        string generationPrompt,
        string modelUsed,
        int tokensUsed
    )
    {
        var content = new AiGeneratedContent
        {
            Id = Guid.NewGuid(),
            ContentType = contentType,
            ContentId = contentId,
            GenerationPrompt = generationPrompt,
            ModelUsed = modelUsed,
            GeneratedAt = DateTime.UtcNow,
            TokensUsed = tokensUsed
        };
        
        content.AddDomainEvent(new AiContentGenerated(
            content.Id,
            contentType,
            contentId,
            tokensUsed
        ));
        
        return content;
    }
}

// TokenUsageRecord entity
public class TokenUsageRecord : Entity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public OperationType OperationType { get; private set; }
    public int TokensUsed { get; private set; }
    public decimal Cost { get; private set; }
    public DateTime Timestamp { get; private set; }
    public Guid ContentId { get; private set; }
    public string ModelUsed { get; private set; }
    
    public static TokenUsageRecord Create(
        Guid userId,
        OperationType operationType,
        int tokensUsed,
        decimal cost,
        Guid contentId,
        string modelUsed
    )
    {
        return new TokenUsageRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OperationType = operationType,
            TokensUsed = tokensUsed,
            Cost = cost,
            Timestamp = DateTime.UtcNow,
            ContentId = contentId,
            ModelUsed = modelUsed
        };
    }
}
```

#### 6.2 Commandy i Query
```csharp
// Commands
public record GenerateFlashcardsCommand(
    Guid LanguageAccountId,
    int Count,
    DifficultyLevel Difficulty
) : IRequest<List<FlashcardDto>>;

public record EvaluateSentenceCommand(
    Guid LanguageAccountId,
    Guid WordId,
    string Sentence
) : IRequest<SentenceEvaluationDto>;

public record AnalyzeEssayCommand(
    Guid LanguageAccountId,
    string Topic,
    string Content,
    string AdditionalNotes
) : IRequest<EssayAnalysisDto>;

// Queries
public record GetTokenUsageQuery(Guid UserId, DateTime From, DateTime To) : IRequest<List<TokenUsageDto>>;
public record GetAiGeneratedContentQuery(Guid ContentId) : IRequest<AiGeneratedContentDto>;
```

#### 6.3 Event Handlery
```csharp
// AiContentGenerated handler
public class AiContentGeneratedHandler : INotificationHandler<AiContentGenerated>
{
    private readonly ITokenBalanceRepository _tokenRepository;
    private readonly ITokenUsageRepository _usageRepository;
    
    public async Task Handle(AiContentGenerated notification, CancellationToken cancellationToken)
    {
        // Determine user from content
        var userId = await GetUserIdFromContentAsync(notification.ContentId);
        
        // Consume tokens
        var tokenBalance = await _tokenRepository.GetByUserIdAsync(userId);
        if (tokenBalance != null)
        {
            var consumed = tokenBalance.ConsumeTokens(
                notification.TokensUsed,
                $"AI Content Generation: {notification.ContentType}"
            );
            
            if (!consumed)
            {
                throw new InsufficientTokensException("Not enough tokens for AI operation");
            }
        }
        
        // Record usage
        var usage = TokenUsageRecord.Create(
            userId,
            OperationType.FlashcardGeneration,
            notification.TokensUsed,
            CalculateCost(notification.TokensUsed),
            notification.ContentId,
            "gpt-4"
        );
        
        await _usageRepository.AddAsync(usage);
    }
}
```

### Krok 7: Projektowanie API (Tydzień 12)

#### 7.1 Kontrolery API
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpPost]
    public async Task<ActionResult<Guid>> Register(RegisterUserRequest request)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password
        );
        
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetUser), new { id = result }, result);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var query = new GetUserQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpPost("{id}/tokens/grant")]
    public async Task<ActionResult<bool>> GrantTokens(Guid id, GrantTokensRequest request)
    {
        var command = new GrantTokensCommand(id, request.Amount, request.Reason);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
```

#### 7.2 DTOs
```csharp
public record RegisterUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Password
);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    TokenBalanceDto TokenBalance
);

public record TokenBalanceDto(
    int MonthlyTokens,
    int UsedTokens,
    int AvailableTokens,
    DateTime ResetDate
);
```

### Krok 8: Testowanie (Tydzień 13-14)

#### 8.1 Unit Testy dla Command Handlerów
```csharp
public class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly RegisterUserCommandHandler _handler;
    
    [Fact]
    public async Task Handle_ValidUser_ShouldCreateUserAndPublishEvent()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com",
            "John",
            "Doe",
            "password123"
        );
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.NotEqual(Guid.Empty, result);
        
        // Verify user was created
        var user = await _userRepository.GetByIdAsync(result);
        Assert.NotNull(user);
        Assert.Equal("test@example.com", user.Email);
        
        // Verify event was published
        _eventDispatcher.Verify(x => x.DispatchAsync(It.IsAny<UserRegistered>()), Times.Once);
    }
}
```

#### 8.2 Integration Testy dla Event Handleriów
```csharp
public class UserRegisteredHandlerTests : IClassFixture<TestFixture>
{
    private readonly ITokenBalanceRepository _tokenRepository;
    private readonly UserRegisteredHandler _handler;
    
    [Fact]
    public async Task Handle_UserRegistered_ShouldCreateTokenBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = new UserRegistered(userId, "test@example.com", "John", "Doe");
        
        // Act
        await _handler.Handle(@event, CancellationToken.None);
        
        // Assert
        var tokenBalance = await _tokenRepository.GetByUserIdAsync(userId);
        Assert.NotNull(tokenBalance);
        Assert.Equal(1000, tokenBalance.MonthlyTokens); // Initial tokens
    }
}
```

### Krok 9: Optymalizacja i Monitoring (Tydzień 15-16)

#### 9.1 Event Bus z Retry Mechanizmem
```csharp
public class ResilientEventDispatcher : IEventDispatcher
{
    private readonly IEventDispatcher _innerDispatcher;
    private readonly ILogger<ResilientEventDispatcher> _logger;
    
    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            .FallbackAsync(async (exception, context) =>
            {
                _logger.LogError(exception, "Failed to dispatch event {EventId} after retries", domainEvent.EventId);
                await StoreFailedEventAsync(domainEvent);
            });
        
        await policy.ExecuteAsync(() => _innerDispatcher.DispatchAsync(domainEvent));
    }
}
```

#### 9.2 Event Metrics
```csharp
public class EventMetrics
{
    private readonly IMetrics _metrics;
    
    public void RecordEventPublished(string eventType)
    {
        _metrics.Counter("events_published")
            .WithTags("event_type", eventType)
            .Increment();
    }
    
    public void RecordEventProcessed(string eventType, TimeSpan processingTime)
    {
        _metrics.Histogram("event_processing_duration")
            .WithTags("event_type", eventType)
            .Observe(processingTime.TotalSeconds);
    }
}
```

### Krok 10: Deployment (Tydzień 17-18)

#### 10.1 Docker Configuration
```dockerfile
# API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Api/Api.csproj", "src/Api/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "src/Api/Api.csproj"
COPY . .
WORKDIR "/src/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
```

#### 10.2 Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: flashcards-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: flashcards-api
  template:
    metadata:
      labels:
        app: flashcards-api
    spec:
      containers:
      - name: api
        image: flashcards-api:latest
        ports:
        - containerPort: 8080
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
```

---

## Podsumowanie Planu Wdrożenia

### **Kluczowe Korzyści:**
1. **Scalability** - każdy kontekst może być skalowany niezależnie
2. **Maintainability** - jasne granice między bounded contexts
3. **Testability** - łatwe testowanie command/query handlerów
4. **Event-Driven Architecture** - luźne powiązania między komponentami
5. **CQRS** - optymalizacja operacji read/write

### **Ryzyka i Mitigacje:**
1. **Złożoność eventów** - start z prostymi eventami, ewoluuj w czasie
2. **Wydajność Event Store** - implementuj archiwizację starych eventów
3. **Debugowanie event-driven systemu** - dodaj comprehensive logging i tracing

### **Następne Kroki:**
1. Implementacja podstawowych eventów i handlerów
2. Dodanie Event Sourcing dla krytycznych agregatów
3. Implementacja eventual consistency patterns
4. Dodanie sag dla złożonych operacji międzykontekstowych

Ten plan zapewnia solidne podstawy dla zbudowania skalowalnej, maintainable aplikacji zgodnej z zasadami DDD i CQRS.

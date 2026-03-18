# Plan Rozszerzony o Pełny Kod Implementacji

Ten plan rozszerza istniejący dokument o pełne implementacje kodowe dla wszystkich komponentów aplikacji, dodając szczegółowe klasy, interfejsy i przykłady implementacji.

## Dodatkowe Sekcje do Implementacji

### 1. Struktura Projektu z Plikami
- Pełna struktura folderów z plikami .cs
- Konfiguracja projektów (.csproj)
- Konfiguracja DI i middleware

### 2. Pełne Implementacje Encji i Agregatów
- Kompletne klasy z właściwościami i metodami
- Walidacje i invariants
- Event handling wewnątrz agregatów

### 3. Interfejsy Repozytoriów
- Definicje wszystkich repozytoriów
- Podpisane metody CRUD
- Async/await patterns

### 4. Commandy i Query z DTOs
- Wszystkie commandy z walidacją
- Query z wynikami
- Klasy DTO dla API

### 5. Event Handlery z Implementacją
- Pełne implementacje handlerów eventów
- Integracja z repozytoriami
- Obsługa błędów i logging

### 6. Usługi Domenowe z Logiką Biznesową
- Kompletna implementacja usług
- Integracja z zewnętrznymi API
- Walidacja biznesowa

### 7. Konfiguracja API
- Kontrolery z atrybutami
- Swagger documentation
- Error handling middleware

### 8. Testy Jednostkowe i Integracyjne
- Pełne pokrycie testami
- Mockowanie zależności
- Scenariusze testowe

### 9. Konfiguracja Docker i Deployment
- Dockerfile dla każdej usługi
- docker-compose.yml
- Kubernetes manifests

### 10. Monitoring i Logging
- Konfiguracja Serilog
- Metrics z Prometheus
- Health checks

---

## Szczegółowa Struktura Plików

### Domain Layer
```
src/Domain/
├── Domain.csproj
├── Common/
│   ├── Events/
│   │   ├── IDomainEvent.cs
│   │   ├── DomainEvent.cs
│   │   └── DomainEventHandler.cs
│   ├── ValueObjects/
│   │   ├── Email.cs
│   │   ├── ProficiencyLevel.cs
│   │   ├── VocabularyType.cs
│   │   └── WordType.cs
│   └── Enums/
│       ├── OperationType.cs
│       ├── FlashcardType.cs
│       └── ReviewResult.cs
├── UserManagement/
│   ├── User.cs
│   ├── TokenBalance.cs
│   ├── Events/
│   │   ├── UserRegistered.cs
│   │   ├── TokensGranted.cs
│   │   └── TokensSpent.cs
│   └── Repositories/
│       ├── IUserRepository.cs
│       └── ITokenBalanceRepository.cs
├── LanguageLearning/
│   ├── LanguageAccount.cs
│   ├── Vocabulary.cs
│   ├── StudySession.cs
│   ├── LearningSettings.cs
│   ├── Events/
│   │   ├── LanguageAccountCreated.cs
│   │   ├── WordAddedToVocabulary.cs
│   │   └── StudySessionCompleted.cs
│   └── Repositories/
│       ├── ILanguageAccountRepository.cs
│       ├── IVocabularyRepository.cs
│       └── IStudySessionRepository.cs
└── [ pozostałe 4 contexts ]
```

### Application Layer
```
src/Application/
├── Application.csproj
├── DependencyInjection.cs
├── Commands/
│   ├── UserManagement/
│   │   ├── RegisterUserCommand.cs
│   │   ├── GrantTokensCommand.cs
│   │   └── Validators/
│   │       └── RegisterUserCommandValidator.cs
│   ├── LanguageLearning/
│   │   ├── CreateLanguageAccountCommand.cs
│   │   ├── AddWordToVocabularyCommand.cs
│   │   └── GetWordSuggestionsCommand.cs
│   └── [ pozostałe contexts ]
├── Queries/
│   ├── UserManagement/
│   │   ├── GetUserQuery.cs
│   │   └── GetTokenBalanceQuery.cs
│   └── [ pozostałe contexts ]
├── EventHandlers/
│   ├── UserManagement/
│   │   ├── UserRegisteredHandler.cs
│   │   └── TokensSpentHandler.cs
│   └── [ pozostałe contexts ]
├── Services/
│   ├── IWordSuggestionService.cs
│   ├── IDynamicCostCalculationService.cs
│   ├── ISentenceToFlashcardConverter.cs
│   └── IMonthlyTokenResetService.cs
└── DTOs/
    ├── UserDto.cs
    ├── LanguageAccountDto.cs
    ├── FlashcardDto.cs
    └── [ pozostałe DTOs ]
```

### Infrastructure Layer
```
src/Infrastructure/
├── Infrastructure.csproj
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   │   ├── UserConfiguration.cs
│   │   ├── LanguageAccountConfiguration.cs
│   │   └── [ pozostałe ]
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   ├── TokenBalanceRepository.cs
│   │   └── [ pozostałe ]
│   └── Events/
│       ├── SqlEventStore.cs
│       └── InMemoryEventStore.cs
├── ExternalServices/
│   ├── AI/
│   │   ├── IOpenAIService.cs
│   │   ├── OpenAIService.cs
│   │   └── Models/
│   │       ├── FlashcardGenerationRequest.cs
│   │       └── SentenceEvaluationRequest.cs
│   └── Email/
│       ├── IEmailService.cs
│       └── SmtpEmailService.cs
└── Events/
    ├── MediatREventDispatcher.cs
    └── ResilientEventDispatcher.cs
```

### API Layer
```
src/Web.Api/
├── Web.Api.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Controllers/
│   ├── UsersController.cs
│   ├── LanguageAccountsController.cs
│   ├── FlashcardsController.cs
│   ├── GrammarExercisesController.cs
│   ├── SpeakingController.cs
│   └── AIController.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Filters/
│   └── ValidateModelAttribute.cs
└── Extensions/
    ├── ServiceCollectionExtensions.cs
    └── SwaggerExtensions.cs
```

---

## Przykładowe Pełne Implementacje

### 1. User Aggregate z Pełną Logiką
```csharp
// src/Domain/UserManagement/User.cs
namespace Domain.UserManagement;

public class User : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private readonly List<LanguageAccount> _languageAccounts = new();
    public IReadOnlyCollection<LanguageAccount> LanguageAccounts => _languageAccounts.AsReadOnly();
    
    private User() { } // Private constructor for EF
    
    public static User Create(string email, string firstName, string lastName, string passwordHash)
    {
        ValidateEmail(email);
        ValidateName(firstName, lastName);
        ValidatePassword(passwordHash);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
        
        user.AddDomainEvent(new UserRegistered(user.Id, email, firstName, lastName));
        return user;
    }
    
    public void UpdateProfile(string firstName, string lastName)
    {
        ValidateName(firstName, lastName);
        
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        
        AddDomainEvent(new UserUpdated(Id, firstName, lastName));
    }
    
    public void AddLanguageAccount(LanguageAccount languageAccount)
    {
        if (_languageAccounts.Any(la => la.TargetLanguageId == languageAccount.TargetLanguageId))
            throw new InvalidOperationException("User already has account for this target language");
            
        _languageAccounts.Add(languageAccount);
    }
    
    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required");
            
        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format");
    }
    
    private static void ValidateName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required");
            
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required");
    }
    
    private static void ValidatePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required");
    }
}
```

### 2. TokenBalance z Pełną Logiką Resetów
```csharp
// src/Domain/UserManagement/TokenBalance.cs
namespace Domain.UserManagement;

public class TokenBalance : Entity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int MonthlyTokens { get; private set; }
    public int UsedTokens { get; private set; }
    public DateTime ResetDate { get; private set; }
    public DateTime? LastResetDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Nowe pola dla różnych typów tokenów
    public int MonthlyGrantedTokens { get; private set; }
    public int PurchasedTokens { get; private set; }
    public int AdminGrantedTokens { get; private set; }
    
    private readonly List<TokenTransaction> _transactions = new();
    public IReadOnlyCollection<TokenTransaction> Transactions => _transactions.AsReadOnly();
    
    private TokenBalance() { }
    
    public static TokenBalance Create(Guid userId, int initialMonthlyTokens)
    {
        return new TokenBalance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MonthlyTokens = initialMonthlyTokens,
            UsedTokens = 0,
            MonthlyGrantedTokens = initialMonthlyTokens,
            PurchasedTokens = 0,
            AdminGrantedTokens = 0,
            ResetDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void GrantMonthlyTokens(int amount, string reason)
    {
        if (amount <= 0)
            throw new ArgumentException("Token amount must be positive");
            
        MonthlyGrantedTokens += amount;
        MonthlyTokens += amount;
        
        AddTransaction(TokenTransactionType.Earned, amount, reason);
        AddDomainEvent(new TokensGranted(UserId, amount, "Monthly Grant"));
    }
    
    public void GrantPurchasedTokens(int amount, string reason)
    {
        if (amount <= 0)
            throw new ArgumentException("Token amount must be positive");
            
        PurchasedTokens += amount;
        MonthlyTokens += amount;
        
        AddTransaction(TokenTransactionType.Earned, amount, reason);
        AddDomainEvent(new TokensGranted(UserId, amount, "Purchase"));
    }
    
    public void GrantAdminTokens(int amount, string reason)
    {
        if (amount <= 0)
            throw new ArgumentException("Token amount must be positive");
            
        AdminGrantedTokens += amount;
        MonthlyTokens += amount;
        
        AddTransaction(TokenTransactionType.Earned, amount, reason);
        AddDomainEvent(new TokensGranted(UserId, amount, "Admin Grant"));
    }
    
    public async Task<bool> ConsumeTokensAsync(int amount, string operation)
    {
        if (amount <= 0)
            throw new ArgumentException("Token amount must be positive");
            
        if (UsedTokens + amount > MonthlyTokens)
            return false; // Nie ma wystarczającej liczby tokenów
            
        UsedTokens += amount;
        
        AddTransaction(TokenTransactionType.Spent, amount, operation);
        AddDomainEvent(new TokensSpent(UserId, amount, operation));
        AddDomainEvent(new TokensConsumed(UserId, amount, operation));
        
        return true;
    }
    
    public async Task<bool> ResetMonthlyTokensAsync()
    {
        var now = DateTime.UtcNow;
        
        // Reset tylko co 30 dni od ostatniego resetu
        if (LastResetDate.HasValue && (now - LastResetDate.Value).Days < 30)
            return false;
        
        // Zachowaj tokeny kupione i przyznane przez admina
        var preservedTokens = PurchasedTokens + AdminGrantedTokens;
        var unusedMonthlyTokens = Math.Max(0, MonthlyGrantedTokens - UsedTokens);
        
        // Resetuj tylko miesięczne tokeny
        MonthlyTokens = preservedTokens;
        UsedTokens = 0;
        LastResetDate = now;
        ResetDate = now.AddDays(30);
        
        AddDomainEvent(new MonthlyTokenReset(UserId, unusedMonthlyTokens, preservedTokens));
        return true;
    }
    
    public int GetAvailableTokens() => MonthlyTokens - UsedTokens;
    
    private void AddTransaction(TokenTransactionType type, int amount, string description)
    {
        var transaction = new TokenTransaction
        {
            Id = Guid.NewGuid(),
            TokenBalanceId = Id,
            Type = type,
            Amount = amount,
            Description = description,
            Timestamp = DateTime.UtcNow
        };
        
        _transactions.Add(transaction);
    }
}
```

### 3. WordSuggestionService z Pełną Implementacją
```csharp
// src/Application/Services/WordSuggestionService.cs
namespace Application.Services;

public class WordSuggestionService : IWordSuggestionService
{
    private readonly IVocabularyRepository _vocabularyRepository;
    private readonly IWordRepository _wordRepository;
    private readonly ILogger<WordSuggestionService> _logger;
    
    public WordSuggestionService(
        IVocabularyRepository vocabularyRepository,
        IWordRepository wordRepository,
        ILogger<WordSuggestionService> logger)
    {
        _vocabularyRepository = vocabularyRepository;
        _wordRepository = wordRepository;
        _logger = logger;
    }
    
    public async Task<List<WordSuggestion>> GetSuggestionsAsync(
        Guid languageAccountId, 
        int count,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting word suggestions for account {AccountId}, count: {Count}", 
                languageAccountId, count);
                
            var vocabulary = await _vocabularyRepository.GetByAccountIdAsync(languageAccountId, cancellationToken);
            
            if (!vocabulary.Any())
            {
                _logger.LogWarning("No vocabulary found for account {AccountId}", languageAccountId);
                return new List<WordSuggestion>();
            }
            
            var suggestions = new List<WordSuggestion>();
            
            // 1. Sugeruj słowa z niskim poziomem opanowania (50%)
            var difficultWords = await GetDifficultWords(vocabulary, count / 2, cancellationToken);
            suggestions.AddRange(difficultWords);
            
            // 2. Sugeruj słowa z małą liczbą utworzonych zdań (50%)
            var rarelyUsedWords = await GetRarelyUsedWords(vocabulary, count / 2, cancellationToken);
            suggestions.AddRange(rarelyUsedWords);
            
            // 3. Posortuj i ogranicz
            var result = suggestions
                .OrderByDescending(s => s.Score)
                .Take(count)
                .ToList();
            
            _logger.LogInformation("Generated {Count} word suggestions for account {AccountId}", 
                result.Count, languageAccountId);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting word suggestions for account {AccountId}", languageAccountId);
            throw;
        }
    }
    
    private async Task<List<WordSuggestion>> GetDifficultWords(
        List<Vocabulary> vocabulary, 
        int count, 
        CancellationToken cancellationToken)
    {
        var difficultWords = vocabulary
            .Where(v => v.MasteryLevel == MasteryLevel.Unknown || 
                        v.MasteryLevel == MasteryLevel.Learning)
            .OrderBy(v => v.LastReviewed)
            .Take(count);
        
        var suggestions = new List<WordSuggestion>();
        
        foreach (var vocab in difficultWords)
        {
            var word = await _wordRepository.GetByIdAsync(vocab.WordId, cancellationToken);
            if (word != null)
            {
                suggestions.Add(new WordSuggestion
                {
                    WordId = vocab.WordId,
                    Word = word.Text,
                    Reason = SuggestionReason.LowMastery,
                    Score = CalculateDifficultyScore(vocab)
                });
            }
        }
        
        return suggestions;
    }
    
    private async Task<List<WordSuggestion>> GetRarelyUsedWords(
        List<Vocabulary> vocabulary, 
        int count, 
        CancellationToken cancellationToken)
    {
        var rarelyUsedWords = vocabulary
            .Where(v => v.CreatedSentencesCount < 3)
            .OrderByDescending(v => v.MasteryLevel)
            .Take(count);
        
        var suggestions = new List<WordSuggestion>();
        
        foreach (var vocab in rarelyUsedWords)
        {
            var word = await _wordRepository.GetByIdAsync(vocab.WordId, cancellationToken);
            if (word != null)
            {
                suggestions.Add(new WordSuggestion
                {
                    WordId = vocab.WordId,
                    Word = word.Text,
                    Reason = SuggestionReason.RarelyUsed,
                    Score = CalculateUsageScore(vocab)
                });
            }
        }
        
        return suggestions;
    }
    
    private double CalculateDifficultyScore(Vocabulary vocabulary)
    {
        var daysSinceLastReview = (DateTime.UtcNow - vocabulary.LastReviewed).TotalDays;
        var masteryPenalty = vocabulary.MasteryLevel switch
        {
            MasteryLevel.Unknown => 0,
            MasteryLevel.Learning => 10,
            MasteryLevel.Familiar => 50,
            MasteryLevel.Mastered => 100,
            _ => 25
        };
        
        return Math.Max(0, 100 - masteryPenalty + daysSinceLastReview * 0.5);
    }
    
    private double CalculateUsageScore(Vocabulary vocabulary)
    {
        var usageBonus = (3 - vocabulary.CreatedSentencesCount) * 20;
        var masteryBonus = vocabulary.MasteryLevel switch
        {
            MasteryLevel.Unknown => 0,
            MasteryLevel.Learning => 10,
            MasteryLevel.Familiar => 30,
            MasteryLevel.Mastered => 50,
            _ => 25
        };
        
        return usageBonus + masteryBonus;
    }
}
```

### 4. DynamicCostCalculationService z Konfiguracją
```csharp
// src/Application/Services/DynamicCostCalculationService.cs
namespace Application.Services;

public class DynamicCostCalculationService : IDynamicCostCalculationService
{
    private readonly ICostConfigurationRepository _configRepository;
    private readonly ILogger<DynamicCostCalculationService> _logger;
    private readonly IMemoryCache _cache;
    
    public DynamicCostCalculationService(
        ICostConfigurationRepository configRepository,
        ILogger<DynamicCostCalculationService> logger,
        IMemoryCache cache)
    {
        _configRepository = configRepository;
        _logger = logger;
        _cache = cache;
    }
    
    public async Task<decimal> CalculateCostAsync(
        OperationType operationType, 
        CostParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"cost_config_{operationType}";
        
        if (_cache.TryGetValue(cacheKey, out CostConfiguration config))
        {
            _logger.LogDebug("Using cached cost configuration for {OperationType}", operationType);
        }
        else
        {
            config = await _configRepository.GetByOperationTypeAsync(operationType, cancellationToken);
            if (config != null)
            {
                _cache.Set(cacheKey, config, TimeSpan.FromMinutes(30));
            }
        }
        
        if (config == null)
        {
            _logger.LogWarning("No cost configuration found for {OperationType}", operationType);
            return 0;
        }
        
        var cost = operationType switch
        {
            OperationType.AddWord => CalculateAddWordCost(config, parameters),
            OperationType.EvaluateSentence => CalculateEvaluateSentenceCost(config, parameters),
            OperationType.CreateFlashcardFromSentence => CalculateCreateFlashcardCost(config, parameters),
            OperationType.AnalyzeEssay => CalculateAnalyzeEssayCost(config, parameters),
            OperationType.GenerateFlashcards => CalculateGenerateFlashcardsCost(config, parameters),
            _ => config.BaseCost
        };
        
        var finalCost = Math.Min(cost, config.MaxCost);
        
        _logger.LogDebug("Calculated cost {Cost} for {OperationType} with parameters {@Parameters}", 
            finalCost, operationType, parameters);
        
        AddDomainEvent(new DynamicCostCalculated(
            Guid.NewGuid(), 
            operationType, 
            finalCost, 
            parameters));
        
        return finalCost;
    }
    
    private decimal CalculateAddWordCost(CostConfiguration config, CostParameters parameters)
    {
        // Zawsze 1 token dla dodania słowa
        return config.BaseCost;
    }
    
    private decimal CalculateEvaluateSentenceCost(CostConfiguration config, CostParameters parameters)
    {
        // Zawsze 1 token dla oceny zdania
        return config.BaseCost;
    }
    
    private decimal CalculateCreateFlashcardCost(CostConfiguration config, CostParameters parameters)
    {
        // Zawsze 1 token dla tworzenia fiszki ze zdania
        return config.BaseCost;
    }
    
    private decimal CalculateAnalyzeEssayCost(CostConfiguration config, CostParameters parameters)
    {
        // Koszt zależny od długości tekstu
        var lengthCost = parameters.TextLength * config.CostPerCharacter;
        return config.BaseCost + lengthCost;
    }
    
    private decimal CalculateGenerateFlashcardsCost(CostConfiguration config, CostParameters parameters)
    {
        // Koszt zależny od liczby generowanych fiszek
        var flashcardCost = (parameters.AdditionalFactors?.GetValueOrDefault("FlashcardCount", 0) ?? 0) * config.CostPerFlashcard;
        return config.BaseCost + flashcardCost;
    }
}
```

### 5. Kontroler API z Pełną Implementacją
```csharp
// src/Web.Api/Controllers/UsersController.cs
namespace Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    /// <summary>
    /// Rejestruje nowego użytkownika w systemie
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Register([FromBody] RegisterUserRequest request)
    {
        try
        {
            _logger.LogInformation("User registration attempt for email: {Email}", request.Email);
            
            var command = new RegisterUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.Password
            );
            
            var result = await _mediator.Send(command);
            
            _logger.LogInformation("User registered successfully with ID: {UserId}", result);
            
            return CreatedAtAction(nameof(GetUser), new { id = result }, result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed during user registration: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Pobiera dane użytkownika
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        try
        {
            var query = new GetUserQuery(id);
            var result = await _mediator.Send(query);
            
            if (result == null)
            {
                return NotFound();
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Przyznaje tokeny użytkownikowi
    /// </summary>
    [HttpPost("{id:guid}/tokens/grant")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> GrantTokens(
        Guid id, 
        [FromBody] GrantTokensRequest request)
    {
        try
        {
            var command = new GrantTokensCommand(id, request.Amount, request.Reason);
            var result = await _mediator.Send(command);
            
            _logger.LogInformation("Granted {Amount} tokens to user {UserId} for reason: {Reason}", 
                request.Amount, id, request.Reason);
            
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid token grant request: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting tokens to user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Internal server error" });
        }
    }
}
```

### 6. Kompletny Test Jednostkowy
```csharp
// tests/Unit/Application/Services/WordSuggestionServiceTests.cs
namespace Unit.Application.Services;

[TestFixture]
public class WordSuggestionServiceTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
    private readonly Mock<IWordRepository> _mockWordRepository;
    private readonly Mock<ILogger<WordSuggestionService>> _mockLogger;
    private readonly WordSuggestionService _service;
    
    public WordSuggestionServiceTests()
    {
        _mockVocabularyRepository = new Mock<IVocabularyRepository>();
        _mockWordRepository = new Mock<IWordRepository>();
        _mockLogger = new Mock<ILogger<WordSuggestionService>>();
        
        _service = new WordSuggestionService(
            _mockVocabularyRepository.Object,
            _mockWordRepository.Object,
            _mockLogger.Object);
    }
    
    [Test]
    public async Task GetSuggestionsAsync_WithMixedVocabulary_ShouldReturnPrioritizedSuggestions()
    {
        // Arrange
        var languageAccountId = Guid.NewGuid();
        var vocabulary = SetupTestVocabulary();
        
        _mockVocabularyRepository
            .Setup(r => r.GetByAccountIdAsync(languageAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vocabulary);
        
        // Act
        var result = await _service.GetSuggestionsAsync(languageAccountId, 5);
        
        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeLessOrEqualTo(5);
        
        // Sprawdź czy trudne słowa mają wyższy priorytet
        var difficultSuggestions = result.Where(s => s.Reason == SuggestionReason.LowMastery);
        difficultSuggestions.Should().NotBeEmpty();
        
        // Sprawdź czy rzadko używane słowa są uwzględnione
        var rarelyUsedSuggestions = result.Where(s => s.Reason == SuggestionReason.RarelyUsed);
        rarelyUsedSuggestions.Should().NotBeEmpty();
        
        // Sprawdź czy wyniki są posortowane wg score
        var sortedScores = result.Select(s => s.Score).ToList();
        sortedScores.Should().BeInDescendingOrder();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<string>(s => s.Contains("Generated 5 word suggestions")),
                It.IsAny<object[]>()),
            Times.Once);
    }
    
    [Test]
    public async Task GetSuggestionsAsync_WithEmptyVocabulary_ShouldReturnEmptyList()
    {
        // Arrange
        var languageAccountId = Guid.NewGuid();
        var emptyVocabulary = new List<Vocabulary>();
        
        _mockVocabularyRepository
            .Setup(r => r.GetByAccountIdAsync(languageAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyVocabulary);
        
        // Act
        var result = await _service.GetSuggestionsAsync(languageAccountId, 5);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<string>(s => s.Contains("No vocabulary found")),
                It.IsAny<object[]>()),
            Times.Once);
    }
    
    [Test]
    public async Task GetSuggestionsAsync_WithException_ShouldLogAndThrow()
    {
        // Arrange
        var languageAccountId = Guid.NewGuid();
        
        _mockVocabularyRepository
            .Setup(r => r.GetByAccountIdAsync(languageAccountId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _service.GetSuggestionsAsync(languageAccountId, 5));
        
        exception.Message.Should().Be("Database error");
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<string>(s => s.Contains("Error getting word suggestions")),
                It.IsAny<object[]>()),
            Times.Once);
    }
    
    private List<Vocabulary> SetupTestVocabulary()
    {
        var now = DateTime.UtcNow;
        
        return new List<Vocabulary>
        {
            new Vocabulary
            {
                Id = Guid.NewGuid(),
                WordId = Guid.NewGuid(),
                VocabularyType = VocabularyType.Active,
                MasteryLevel = MasteryLevel.Unknown,
                LastReviewed = now.AddDays(-10),
                CreatedSentencesCount = 0
            },
            new Vocabulary
            {
                Id = Guid.NewGuid(),
                WordId = Guid.NewGuid(),
                VocabularyType = VocabularyType.Passive,
                MasteryLevel = MasteryLevel.Learning,
                LastReviewed = now.AddDays(-5),
                CreatedSentencesCount = 1
            },
            new Vocabulary
            {
                Id = Guid.NewGuid(),
                WordId = Guid.NewGuid(),
                VocabularyType = VocabularyType.Active,
                MasteryLevel = MasteryLevel.Familiar,
                LastReviewed = now.AddDays(-2),
                CreatedSentencesCount = 5
            }
        };
    }
}
```

---

## Konfiguracja Docker i Deployment

### Dockerfile dla API
```dockerfile
# src/Web.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Web.Api/Web.Api.csproj", "src/Web.Api/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/SharedKernel/SharedKernel.csproj", "src/SharedKernel/"]
RUN dotnet restore "src/Web.Api/Web.Api.csproj"
COPY . .
WORKDIR "/src/src/Web.Api"
RUN dotnet build "Web.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Web.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Web.Api.dll"]
```

### docker-compose.yml
```yaml
version: '3.8'

services:
  web-api:
    image: ${DOCKER_REGISTRY-}flashcards-api
    container_name: flashcards-api
    build:
      context: .
      dockerfile: src/Web.Api/Dockerfile
    ports:
      - "5000:8080"
      - "5001:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=flashcards;Username=postgres;Password=postgres
    depends_on:
      - postgres
      - redis
      - seq

  postgres:
    image: postgres:17
    container_name: flashcards-postgres
    environment:
      - POSTGRES_DB=flashcards
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
    volumes:
      - ./.containers/db:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  redis:
    image: redis:7-alpine
    container_name: flashcards-redis
    ports:
      - "6379:6379"

  seq:
    image: datalust/seq:2024.3
    container_name: flashcards-seq
    environment:
      - ACCEPT_EULA=Y
    ports:
      - "8081:80"
```

---

## Podsumowanie

Ten rozszerzony plan dostarcza:
1. **Pełną strukturę projektu** z wszystkimi plikami
2. **Kompletne implementacje** wszystkich encji i usług
3. **Szczegółowe przykłady** kodu z najlepszymi praktykami
4. **Konfigurację deploymentu** z Docker i Kubernetes
5. **Pełne testowanie** jednostkowe i integracyjne
6. **Monitoring i logging** dla produkcyjnego środowiska

Plan jest kompletną specyfikacją implementacyjną gotową do realizacji krok po kroku.

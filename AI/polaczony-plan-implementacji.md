# Połączony Plan Implementacji Aplikacji do Nauki Języków

Ten dokument łączy wszystkie poprzednie plany w jeden spójny dokument implementacyjny uwzględniający pełen opis funkcjonalny aplikacji, eventy domenowe, CQRS i wszystkie szczegóły implementacyjne.

## Spis Treści

1. [Model Domeny i Bounded Contexts](#model-domeny-i-bounded-contexts)
2. [Eventy Domenowe](#eventy-domenowe)
3. [Plan Wdrożenia Krok po Kroku](#plan-wdrozenia-krok-po-kroku)
4. [Szczegółowe Implementacje](#szczegółowe-implementacje)
5. [Konfiguracja i Testowanie](#konfiguracja-i-testowanie)

---

## Model Domeny i Bounded Contexts

### 1. Zarządzanie Użytkownikami (User Management Context)
**Odpowiedzialność:** Rejestracja, autentykacja, zarządzanie tokenami

#### Encje:
- **User** (Aggregate Root)
  - Id, Email, FirstName, LastName, PasswordHash, CreatedAt
  - LanguageAccounts: List<LanguageAccount>

- **TokenBalance** (Entity)
  - Id, UserId, MonthlyTokens, UsedTokens, ResetDate
  - MonthlyGrantedTokens, PurchasedTokens, AdminGrantedTokens
  - LastResetDate, TokenTransactions: List<TokenTransaction>

#### Obiekty Wartości:
- **TokenTransactionType**
  - Earned, Spent, Refunded, AdminGranted

#### Zdarzenia Domenowe:
- **UserRegistered** - użytkownik zarejestrowany
- **UserUpdated** - dane użytkownika zaktualizowane
- **TokensGranted** - przyznano tokeny użytkownikowi
- **TokensSpent** - zużyto tokeny
- **MonthlyTokenReset** - miesięczny reset tokenów

#### Repozytoria:
- **IUserRepository**
- **ITokenBalanceRepository**

#### Usługi Domenowe:
- **UserRegistrationService**
- **AuthenticationService**
- **TokenManagementService**

---

### 2. Nauka Języków (Language Learning Context)
**Odpowiedzialność:** Zarządzanie kontami językowymi, słownictwem, postępami

#### Encje:
- **LanguageAccount** (Aggregate Root)
  - Id, UserId, KnownLanguage, TargetLanguage, ProficiencyLevel
  - LearningSettings, CreatedAt, UpdatedAt
  - FlashcardCollections, GrammarExercises, SpeakingSessions

- **Vocabulary** (Entity)
  - Id, LanguageAccountId, WordId, VocabularyType
  - MasteryLevel, LastReviewed, CreatedAt
  - CreatedSentencesCount, LastSentenceCreated

- **StudySession** (Entity)
  - Id, LanguageAccountId, SessionType, StartTime, EndTime
  - CardsStudied, CorrectAnswers, TotalAnswers, Score

#### Obiekty Wartości:
- **LearningSettings**
  - DailyGoal, ReviewMethod, AutoPlayAudio, ShowPronunciation
  - EnableHints, SessionDuration, PreferredExerciseTypes
  - DailyFlashcardGeneration, RequireSentenceCreation

- **VocabularyType**
  - Active, Passive

- **MasteryLevel**
  - Unknown, Learning, Familiar, Mastered

#### Zdarzenia Domenowe:
- **LanguageAccountCreated** - utworzono konto językowe
- **StudySessionCompleted** - zakończono sesję nauki
- **LearningSettingsUpdated** - zaktualizowano ustawienia nauki
- **WordAddedToVocabulary** - dodano słowo do słownictwa
- **VocabularyTypeChanged** - zmieniono typ słownictwa
- **WordSuggested** - zaproponowano słowo do ćwiczenia

#### Repozytoria:
- **ILanguageAccountRepository**
- **IStudySessionRepository**
- **IVocabularyRepository**

#### Usługi Domenowe:
- **ProgressTrackingService**
- **DifficultyAssessmentService**
- **LearningSettingsService**
- **VocabularyManagementService**
- **WordSuggestionService**

---

### 3. System Fiszek (Flashcard System Context)
**Odpowiedzialność:** Zarządzanie fiszkami, SRS, generowanie AI

#### Encje:
- **FlashcardCollection** (Aggregate Root)
  - Id, LanguageAccountId, Title, Description, Category
  - DifficultyLevel, CreatedAt, UpdatedAt
  - Flashcards: List<Flashcard>

- **FillInBlankFlashcard** (Entity)
  - Id, CollectionId, WordTranslationId, SentenceWithBlank
  - CorrectAnswer, Synonyms: List<string>, WordType
  - Type: FlashcardType.FillInBlank

- **FlashcardReview** (Entity)
  - Id, FlashcardId, LanguageAccountId, ReviewResult
  - ReviewTime, NextReviewDate, ReviewInterval, EaseFactor

- **WordTranslation** (Entity)
  - Id, WordId, Translation, LanguageId, Context
  - MultipleMeanings: List<string>

#### Obiekty Wartości:
- **WordType**
  - SingleWord, PhrasalVerb, Phrase, Idiom

- **FlashcardTemplate**
  - FillInTheBlank, MultipleChoice, Traditional

- **ReviewResult**
  - Again (0), Hard (1), Good (2), Easy (3)

#### Zdarzenia Domenowe:
- **FlashcardCreated** - utworzono fiszkę
- **FlashcardReviewed** - przejrzano fiszkę
- **FlashcardCollectionCreated** - utworzono kolekcję fiszek
- **FlashcardsGenerated** - wygenerowano fiszki przez AI
- **WordTranslationAdded** - dodano tłumaczenie słowa
- **SentenceConvertedToFlashcard** - konwertowano zdanie na fiszkę

#### Repozytoria:
- **IFlashcardCollectionRepository**
- **IFlashcardRepository**
- **IFlashcardReviewRepository**
- **IWordTranslationRepository**

#### Usługi Domenowe:
- **SpacedRepetitionService**
- **FlashcardGenerationService**
- **ReviewSchedulerService**
- **TemplateService**

---

### 4. Ćwiczenia Gramatyczne (Grammar Exercises Context)
**Odpowiedzialność:** Zarządzanie ćwiczeniami, regułami, quizami

#### Encje:
- **GrammarExercise** (Aggregate Root)
  - Id, LanguageAccountId, Title, Description, GrammarTopic
  - DifficultyLevel, ExerciseType, Question, CorrectAnswer
  - Options, Explanation, CreatedAt

- **GrammarRule** (Entity)
  - Id, LanguageAccountId, Name, Description, Examples
  - CreatedAt, UpdatedAt

- **GrammarQuiz** (Entity)
  - Id, LanguageAccountId, GrammarRuleId, Title
  - QuestionCount, DifficultyLevel, CreatedAt

- **GrammarExerciseResult** (Entity)
  - Id, ExerciseId, LanguageAccountId, UserAnswer
  - IsCorrect, AttemptCount, CompletedAt

#### Obiekty Wartości:
- **ExerciseType**
  - FillInBlank, MultipleChoice, SentenceTransformation, Translation, ProductionExercise

#### Zdarzenia Domenowe:
- **ExerciseCompleted** - ukończono ćwiczenie
- **ExerciseGenerated** - wygenerowano ćwiczenie
- **GrammarRuleCreated** - utworzono regułę gramatyczną
- **QuizGenerated** - wygenerowano quiz

#### Repozytoria:
- **IGrammarExerciseRepository**
- **IGrammarExerciseResultRepository**
- **IGrammarRuleRepository**
- **IGrammarQuizRepository**

#### Usługi Domenowe:
- **ExerciseGenerationService**
- **GrammarValidationService**
- **ExerciseDifficultyService**
- **QuizGenerationService**
- **RuleManagementService**

---

### 5. Produkcja Językowa (Language Production Context)
**Odpowiedzialność:** Ocena wypowiedzi, tworzenie zdań, analiza esejów

#### Encje:
- **SpeakingSession** (Aggregate Root)
  - Id, LanguageAccountId, Topic, Prompt, UserResponse
  - SessionType, Duration, AudioRecordingUrl, CreatedAt
  - AiEvaluation: SpeakingEvaluation

- **SpeakingEvaluation** (Entity)
  - Id, SpeakingSessionId, PronunciationScore, FluencyScore
  - GrammarScore, VocabularyScore, OverallScore
  - Feedback, Suggestions, EvaluatedAt, EvaluationModel

- **SentenceCreation** (Entity)
  - Id, LanguageAccountId, WordId, UserSentence, AiEvaluation
  - CreatedAt, IsCorrect, CanCreateFlashcard
  - ConvertedToFlashcardId, ConvertedAt

- **Essay** (Entity)
  - Id, LanguageAccountId, Topic, Content, AdditionalNotes
  - CreatedAt, AiAnalysis, Status

#### Obiekty Wartości:
- **SpeakingType**
  - Pronunciation, Fluency, Grammar, Vocabulary, Conversation

- **SentenceCreationStatus**
  - Draft, Submitted, Evaluated, Approved, Rejected

- **EssayStatus**
  - Draft, Submitted, Analyzing, Completed

#### Zdarzenia Domenowe:
- **SpeakingSessionStarted** - rozpoczęto sesję mówienia
- **SpeakingSessionEvaluated** - oceniono sesję mówienia
- **SentenceCreated** - utworzono zdanie
- **EssaySubmitted** - przesłano esej
- **EssayAnalyzed** - przeanalizowano esej

#### Repozytoria:
- **ISpeakingSessionRepository**
- **ISpeakingEvaluationRepository**
- **ISentenceCreationRepository**
- **IEssayRepository**

#### Usługi Domenowe:
- **SpeakingEvaluationService**
- **AudioProcessingService**
- **FeedbackGenerationService**
- **SentenceCreationService**
- **EssayAnalysisService**

---

### 6. Integracja AI (AI Integration Context)
**Odpowiedzialność:** Generowanie treści, ocena, zarządzanie kosztami

#### Encje:
- **AiGeneratedContent** (Aggregate Root)
  - Id, ContentType, ContentId, GenerationPrompt
  - ModelUsed, GeneratedAt, QualityScore, TokensUsed

- **EvaluationRequest** (Aggregate Root)
  - Id, ContentToEvaluate, EvaluationType, LanguageAccountId
  - RequestStatus, Result, RequestedAt, CompletedAt?

- **TokenUsageRecord** (Entity)
  - Id, UserId, OperationType, TokensUsed, Cost
  - Timestamp, ContentId, ModelUsed

- **CostConfiguration** (Entity)
  - Id, OperationType, BaseCost, CostPerCharacter
  - MaxCost, IsActive, UpdatedAt

#### Obiekty Wartości:
- **ContentType**
  - Flashcard, Exercise, Example, Explanation, Sentence, Essay, GrammarRule

- **EvaluationType**
  - Speaking, Writing, Grammar, Vocabulary, Sentence, Essay

- **OperationType**
  - FlashcardGeneration, ExerciseGeneration, SentenceEvaluation, EssayAnalysis, GrammarValidation

#### Zdarzenia Domenowe:
- **AiContentGenerated** - wygenerowano treść przez AI
- **EvaluationRequested** - zgłoszono prośbę o ocenę
- **EvaluationCompleted** - ukończono ocenę
- **TokensConsumed** - zużyto tokeny
- **ContentQualityAssessed** - oceniono jakość treści
- **DynamicCostCalculated** - obliczono dynamiczny koszt

#### Repozytoria:
- **IAiGeneratedContentRepository**
- **IEvaluationRequestRepository**
- **ITokenUsageRecordRepository**
- **ICostConfigurationRepository**

#### Usługi Domenowe:
- **AiContentGenerationService**
- **AiEvaluationService**
- **QualityAssessmentService**
- **TokenTrackingService**
- **CostCalculationService**

---

## Eventy Domenowe

### Kompletna Lista Eventów:
1. **UserRegistered** - nowy użytkownik w systemie
2. **UserUpdated** - zmiana danych użytkownika
3. **TokensGranted** - przyznanie tokenów (miesięczne/zakupione/admin)
4. **TokensSpent** - zużycie tokenów przy operacji AI
5. **MonthlyTokenReset** - reset miesięcznych tokenów
6. **LanguageAccountCreated** - nowe konto językowe
7. **StudySessionCompleted** - zakończenie sesji nauki
8. **LearningSettingsUpdated** - zmiana ustawień nauki
9. **WordAddedToVocabulary** - dodanie słowa do słownictwa
10. **VocabularyTypeChanged** - zmiana typu słownictwa
11. **WordSuggested** - sugestia słowa do ćwiczenia
12. **FlashcardCreated** - utworzenie fiszki
13. **FlashcardReviewed** - przeglądnięcie fiszki
14. **FlashcardCollectionCreated** - utworzenie kolekcji
15. **FlashcardsGenerated** - generowanie fiszek przez AI
16. **WordTranslationAdded** - dodanie tłumaczenia
17. **SentenceConvertedToFlashcard** - konwersja zdania na fiszkę
18. **ExerciseCompleted** - ukończenie ćwiczenia
19. **ExerciseGenerated** - generowanie ćwiczenia
20. **GrammarRuleCreated** - utworzenie reguły gramatycznej
21. **QuizGenerated** - generowanie quizu
22. **SpeakingSessionStarted** - rozpoczęcie sesji mówienia
23. **SpeakingSessionEvaluated** - ocena sesji mówienia
24. **SentenceCreated** - utworzenie zdania
25. **EssaySubmitted** - przesłanie eseju
26. **EssayAnalyzed** - analiza eseju
27. **AiContentGenerated** - generowanie treści przez AI
28. **EvaluationRequested** - prośba o ocenę
29. **EvaluationCompleted** - zakończenie oceny
30. **TokensConsumed** - zużycie tokenów
31. **ContentQualityAssessed** - ocena jakości treści
32. **DynamicCostCalculated** - obliczenie kosztu operacji

---

## Plan Wdrożenia Krok po Kroku

### Faza 1: Fundamenty (Tygodnie 1-2)
1. **Struktura projektu** - 6 bounded contexts z CQRS
2. **Podstawowe klasy eventów** - DomainEvent, IEventDispatcher
3. **Konfiguracja MediatR** - CQRS infrastructure
4. **Event Store** - podstawowa implementacja

### Faza 2: User Management (Tygodnie 3-4)
1. **User aggregate** - rejestracja, zarządzanie
2. **TokenBalance entity** - system tokenów z resetami
3. **Command/Query handlers** - operacje użytkownika
4. **Event handlers** - przyznawanie tokenów, śledzenie zużycia

### Faza 3: Language Learning (Tygodnie 5-6)
1. **LanguageAccount aggregate** - konta językowe
2. **Vocabulary entity** - słownictwo aktywne/pasywne
3. **WordSuggestionService** - inteligentne sugerowanie
4. **StudySession tracking** - statystyki nauki

### Faza 4: Flashcard System (Tygodnie 7-8)
1. **FillInBlankFlashcard** - fiszki z lukami
2. **SpacedRepetitionService** - algorytm SRS
3. **FlashcardGenerationService** - generowanie przez AI
4. **WordTranslation management** - pary słowo-tłumaczenie

### Faza 5: Grammar Exercises (Tydzień 9)
1. **GrammarRule entity** - reguły użytkownika
2. **QuizGenerationService** - quizy z reguł
3. **ExerciseValidationService** - walidacja odpowiedzi
4. **ProductionExercise** - planowane ćwiczenia produkcyjne

### Faza 6: Language Production (Tydzień 10)
1. **SentenceCreation entity** - tworzenie zdań
2. **Essay entity** - dłuższe wypowiedzi
3. **SentenceToFlashcardConverter** - konwersja poprawnych zdań
4. **SpeakingEvaluation** - ocena wypowiedzi

### Faza 7: AI Integration (Tydzień 11)
1. **DynamicCostCalculationService** - elastyczne koszty
2. **AiGeneratedContent tracking** - generowanie treści
3. **TokenUsageRecord** - śledzenie zużycia
4. **CostConfiguration** - konfiguracja kosztów

### Faza 8: Event Sourcing (Tydzień 12)
1. **EventStore implementation** - pełny event sourcing
2. **Aggregate snapshots** - optymalizacja wydajności
3. **Event replay** - odtwarzanie stanu
4. **Event versioning** - migracja schematu

### Faza 9: API (Tydzień 13)
1. **REST controllers** - wszystkie endpointy
2. **DTOs** - transfer obiektów
3. **Validation** - FluentValidation
4. **Swagger documentation** - API docs

### Faza 10: Testowanie (Tygodnie 14-15)
1. **Unit tests** - command/query handlers
2. **Integration tests** - event handlers
3. **End-to-end tests** - pełne scenariusze
4. **Performance tests** - load testing

### Faza 11: Deployment (Tygodnie 16-18)
1. **Docker containers** - wszystkie usługi
2. **Kubernetes** - orchestration
3. **CI/CD pipeline** - automatyzacja
4. **Monitoring** - metrics, logging

---

## Szczegółowe Implementacje

### Kluczowe Usługi Domenowe:

#### WordSuggestionService
```csharp
public class WordSuggestionService : IWordSuggestionService
{
    public async Task<List<WordSuggestion>> GetSuggestionsAsync(
        Guid languageAccountId, int count)
    {
        // 1. Słowa z niskim poziomem opanowania (50%)
        var difficultWords = await GetDifficultWords(languageAccountId, count / 2);
        
        // 2. Słowa rzadko używane (50%)
        var rarelyUsedWords = await GetRarelyUsedWords(languageAccountId, count / 2);
        
        // 3. Połączenie i scoring
        return CombineAndScore(difficultWords, rarelyUsedWords);
    }
}
```

#### DynamicCostCalculationService
```csharp
public class DynamicCostCalculationService : IDynamicCostCalculationService
{
    public async Task<decimal> CalculateCostAsync(
        OperationType operationType, CostParameters parameters)
    {
        var config = await GetConfigAsync(operationType);
        
        return operationType switch
        {
            OperationType.AddWord => config.BaseCost, // 1 token
            OperationType.EvaluateSentence => config.BaseCost, // 1 token
            OperationType.CreateFlashcardFromSentence => config.BaseCost, // 1 token
            OperationType.AnalyzeEssay => Math.Min(
                config.BaseCost + (parameters.TextLength * config.CostPerCharacter), 
                config.MaxCost),
            _ => config.BaseCost
        };
    }
}
```

#### SentenceToFlashcardConverter
```csharp
public class SentenceToFlashcardConverter : ISentenceToFlashcardConverter
{
    public async Task<FillInBlankFlashcard> ConvertSentenceToFlashcardAsync(
        Guid sentenceId, Guid userId)
    {
        var sentence = await _sentenceRepository.GetByIdAsync(sentenceId);
        
        var prompt = $@"
            Convert sentence to fill-in-the-blank format.
            Original: '{sentence.Content}'
            
            Instructions:
            1. Identify key word/phrase to blank out
            2. Create sentence with '___'
            3. Provide correct answer
            4. Provide synonyms
            5. Use SAME FORMAT as auto-generated flashcards
            
            Response format JSON:
            {{
                ""sentenceWithBlank"": ""sentence with ___"",
                ""correctAnswer"": ""the correct answer"",
                ""synonyms"": [""synonym1"", ""synonym2""],
                ""wordType"": ""SingleWord|PhrasalVerb|Phrase""
            }}
        ";
        
        var aiResponse = await _aiService.GenerateFlashcardFromSentenceAsync(prompt);
        return CreateFlashcardFromResponse(aiResponse, sentence.CollectionId);
    }
}
```

#### MonthlyTokenResetService
```csharp
public class MonthlyTokenResetService : IMonthlyTokenResetService
{
    [ScheduledJob("0 0 1 * *")] // 1 dzień miesiąca o północy
    public async Task ResetAllMonthlyTokensAsync()
    {
        var balances = await _tokenRepository.GetAllAsync();
        
        foreach (var balance in balances)
        {
            // Reset tylko co 30 dni
            if (await balance.ResetMonthlyTokensAsync())
            {
                await _tokenRepository.UpdateAsync(balance);
            }
        }
    }
}
```

---

## Konfiguracja i Testowanie

### Konfiguracja Kosztów (appsettings.json):
```json
{
  "AiCosts": {
    "AddWord": {
      "BaseCost": 1.0,
      "MaxCost": 1.0
    },
    "EvaluateSentence": {
      "BaseCost": 1.0,
      "MaxCost": 1.0
    },
    "CreateFlashcardFromSentence": {
      "BaseCost": 1.0,
      "MaxCost": 1.0
    },
    "AnalyzeEssay": {
      "BaseCost": 5.0,
      "CostPerCharacter": 0.001,
      "MaxCost": 50.0
    },
    "GenerateFlashcards": {
      "BaseCost": 2.0,
      "CostPerFlashcard": 0.5,
      "MaxCost": 20.0
    }
  }
}
```

### Przykładowe Testy:
```csharp
[Test]
public async Task WordSuggestion_ShouldPrioritizeDifficultWords()
{
    // Arrange
    var service = new WordSuggestionService(repositories);
    SetupVocabularyWithMixedDifficulty();
    
    // Act
    var suggestions = await service.GetSuggestionsAsync(accountId, 5);
    
    // Assert
    Assert.True(suggestions.Any(s => s.Reason == SuggestionReason.LowMastery));
    Assert.True(suggestions.All(s => s.Score > 0));
}

[Test]
public async Task TokenReset_ShouldPreservePurchasedTokens()
{
    // Arrange
    var balance = SetupTokenBalance(monthly: 100, purchased: 50);
    
    // Act
    var result = await balance.ResetMonthlyTokensAsync();
    
    // Assert
    Assert.True(result);
    Assert.Equal(50, balance.MonthlyTokens); // tylko zakupione
}
```

---

## Podsumowanie

### ✅ **Pełna Zgodność z Wymaganiami:**
1. **Struktura kont użytkownika** - wielokrotność kont językowych
2. **System tokenów** - miesięczny reset z wyjątkami
3. **Słownictwo aktywne/pasywne** - pełne zarządzanie
4. **Fiszki z lukami** - konkretny format z synonimami
5. **SRS** - spacjowane powtórki
6. **Aktywna produkcja** - tworzenie zdań i analiza
7. **Reguły gramatyczne** - własne reguły i quizy
8. **AI integration** - generowanie i ocena z dynamicznymi kosztami

### 🎯 **Kluczowe Cechy Implementacji:**
- **Event-driven architecture** z CQRS
- **6 bounded contexts** z jasnymi granicami
- **Dynamiczne kosztowanie** z konfiguracją
- **Inteligentne sugerowanie** słów do ćwiczeń
- **Automatyczna konwersja** zdań na fiszki
- **Pełny system tokenów** z resetami i wyjątkami

Ten połączony plan jest kompletną specyfikacją implementacyjną aplikacji do nauki języków, w 100% zgodną z wymaganiami funkcjonalnymi i gotową do realizacji krok po kroku.

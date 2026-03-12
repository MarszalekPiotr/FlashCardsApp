# Przypisanie Encji do Bounded Contexts (Zaktualizowane)

Ten dokument pokazuje dokładne przypisanie wszystkich encji, obiektów wartości i usług domenowych do odpowiednich bounded contexts w aplikacji fiszek do nauki języków, z uwzględnieniem nowych funkcjonalności.

## 1. Zarządzanie Użytkownikami (User Management Context)

### Encje
- **User** (Aggregate Root)
  - Id, Email, FirstName, LastName, PasswordHash, CreatedAt
  - LanguageAccounts: List<LanguageAccount>

- **TokenBalance** (Nowość)
  - Id, UserId, MonthlyTokens, UsedTokens, ResetDate
  - TokenTransactions: List<TokenTransaction>

### Obiekty Wartości
- **TokenTransactionType**
  - Earned, Spent, Refunded, AdminGranted

### Zdarzenia Domenowe
- **UserRegistered**
- **UserUpdated**
- **TokensGranted** (Nowość)
- **TokensSpent** (Nowość)

### Repozytoria
- **IUserRepository**
- **ITokenBalanceRepository** (Nowość)

### Usługi Domenowe
- **UserRegistrationService**
- **AuthenticationService**
- **TokenManagementService** (Nowość)

---

## 2. Nauka Języków (Language Learning Context)

### Encje
- **LanguageAccount** (Aggregate Root)
  - Id, UserId, KnownLanguage, TargetLanguage, ProficiencyLevel
  - LearningSettings, CreatedAt, UpdatedAt
  - FlashcardCollections: List<FlashcardCollection>
  - GrammarExercises: List<GrammarExercise>
  - SpeakingSessions: List<SpeakingSession>

- **StudySession**
  - Id, LanguageAccountId, SessionType, StartTime, EndTime
  - CardsStudied, CorrectAnswers, TotalAnswers, Score

- **Vocabulary** (Nowość)
  - Id, LanguageAccountId, WordId, VocabularyType
  - MasteryLevel, LastReviewed, CreatedAt

### Obiekty Wartości
- **LearningSettings**
  - DailyGoal, ReviewMethod, AutoPlayAudio, ShowPronunciation
  - EnableHints, SessionDuration, PreferredExerciseTypes
  - DailyFlashcardGeneration (Nowość)
  - RequireSentenceCreation (Nowość)

- **ProficiencyLevel**
  - Level (A1-C2), Description

- **VocabularyType** (Nowość)
  - Active, Passive

- **MasteryLevel** (Nowość)
  - Unknown, Learning, Familiar, Mastered

### Zdarzenia Domenowe
- **LanguageAccountCreated**
- **StudySessionCompleted**
- **LearningSettingsUpdated**
- **WordAddedToVocabulary** (Nowość)
- **VocabularyTypeChanged** (Nowość)

### Repozytoria
- **ILanguageAccountRepository**
- **IStudySessionRepository**
- **IVocabularyRepository** (Nowość)

### Usługi Domenowe
- **ProgressTrackingService**
- **DifficultyAssessmentService**
- **LearningSettingsService**
- **VocabularyManagementService** (Nowość)

---

## 3. System Fiszek (Flashcard System Context)

### Encje
- **FlashcardCollection** (Aggregate Root)
  - Id, LanguageAccountId, Title, Description, Category
  - DifficultyLevel, CreatedAt, UpdatedAt
  - Flashcards: List<Flashcard>

- **Flashcard**
  - Id, CollectionId, WordTranslationId, FrontTemplate, BackTemplate
  - Type, Difficulty, Tags, ExampleSentence, Pronunciation
  - ImageUrl, AudioUrl, CreatedAt

- **FlashcardReview**
  - Id, FlashcardId, LanguageAccountId, ReviewResult
  - ReviewTime, NextReviewDate, ReviewInterval, EaseFactor

- **WordTranslation** (Nowość)
  - Id, WordId, Translation, LanguageId, Context
  - MultipleMeanings: List<string>

### Obiekty Wartości
- **DifficultyLevel**
  - Beginner, Intermediate, Advanced, Expert

- **FlashcardType**
  - Vocabulary, Grammar, Phrase, Idiom

- **ReviewResult**
  - Again (0), Hard (1), Good (2), Easy (3)

- **ReviewMethod**
  - SpacedRepetition, Random, DifficultyBased, RecentFirst

- **FlashcardTemplate** (Nowość)
  - FillInTheBlank, MultipleChoice, Traditional

### Zdarzenia Domenowe
- **FlashcardCreated**
- **FlashcardReviewed**
- **FlashcardCollectionCreated**
- **FlashcardsGenerated** (Nowość)
- **WordTranslationAdded** (Nowość)

### Repozytoria
- **IFlashcardCollectionRepository**
- **IFlashcardRepository**
- **IFlashcardReviewRepository**
- **IWordTranslationRepository** (Nowość)

### Usługi Domenowe
- **SpacedRepetitionService**
- **FlashcardGenerationService**
- **ReviewSchedulerService**
- **TemplateService** (Nowość)

---

## 4. Ćwiczenia Gramatyczne (Grammar Exercises Context)

### Encje
- **GrammarExercise** (Aggregate Root)
  - Id, LanguageAccountId, Title, Description, GrammarTopic
  - DifficultyLevel, ExerciseType, Question, CorrectAnswer
  - Options, Explanation, CreatedAt

- **GrammarExerciseResult**
  - Id, ExerciseId, LanguageAccountId, UserAnswer
  - IsCorrect, AttemptCount, CompletedAt

- **GrammarRule** (Nowość)
  - Id, LanguageAccountId, Name, Description, Examples
  - CreatedAt, UpdatedAt

- **GrammarQuiz** (Nowość)
  - Id, LanguageAccountId, GrammarRuleId, Title
  - QuestionCount, DifficultyLevel, CreatedAt

### Obiekty Wartości
- **ExerciseType**
  - FillInBlank, MultipleChoice, SentenceTransformation, Translation
  - ProductionExercise (Nowość)

### Zdarzenia Domenowe
- **ExerciseCompleted**
- **ExerciseGenerated**
- **GrammarRuleCreated** (Nowość)
- **QuizGenerated** (Nowość)

### Repozytoria
- **IGrammarExerciseRepository**
- **IGrammarExerciseResultRepository**
- **IGrammarRuleRepository** (Nowość)
- **IGrammarQuizRepository** (Nowość)

### Usługi Domenowe
- **ExerciseGenerationService**
- **GrammarValidationService**
- **ExerciseDifficultyService**
- **QuizGenerationService** (Nowość)
- **RuleManagementService** (Nowość)

---

## 5. Produkcja Językowa (Language Production Context)

### Encje
- **SpeakingSession** (Aggregate Root)
  - Id, LanguageAccountId, Topic, Prompt, UserResponse
  - SessionType, Duration, AudioRecordingUrl, CreatedAt
  - AiEvaluation: SpeakingEvaluation

- **SpeakingEvaluation**
  - Id, SpeakingSessionId, PronunciationScore, FluencyScore
  - GrammarScore, VocabularyScore, OverallScore
  - Feedback, Suggestions, EvaluatedAt, EvaluationModel

- **SentenceCreation** (Nowość)
  - Id, LanguageAccountId, WordId, UserSentence, AiEvaluation
  - CreatedAt, IsCorrect, CanCreateFlashcard

- **Essay** (Nowość)
  - Id, LanguageAccountId, Topic, Content, AdditionalNotes
  - CreatedAt, AiAnalysis, Status

### Obiekty Wartości
- **SpeakingType**
  - Pronunciation, Fluency, Grammar, Vocabulary, Conversation

- **SentenceCreationStatus** (Nowość)
  - Draft, Submitted, Evaluated, Approved, Rejected

- **EssayStatus** (Nowość)
  - Draft, Submitted, Analyzing, Completed

### Zdarzenia Domenowe
- **SpeakingSessionStarted**
- **SpeakingSessionEvaluated**
- **SentenceCreated** (Nowość)
- **EssaySubmitted** (Nowość)
- **EssayAnalyzed** (Nowość)

### Repozytoria
- **ISpeakingSessionRepository**
- **ISpeakingEvaluationRepository**
- **ISentenceCreationRepository** (Nowość)
- **IEssayRepository** (Nowość)

### Usługi Domenowe
- **SpeakingEvaluationService**
- **AudioProcessingService**
- **FeedbackGenerationService**
- **SentenceCreationService** (Nowość)
- **EssayAnalysisService** (Nowość)

---

## 6. Integracja AI (AI Integration Context)

### Encje
- **AiGeneratedContent** (Aggregate Root)
  - Id, ContentType, ContentId, GenerationPrompt
  - ModelUsed, GeneratedAt, QualityScore, TokensUsed

- **EvaluationRequest** (Aggregate Root)
  - Id, ContentToEvaluate, EvaluationType, LanguageAccountId
  - RequestStatus, Result, RequestedAt, CompletedAt?

- **TokenUsageRecord** (Nowość)
  - Id, UserId, OperationType, TokensUsed, Cost
  - Timestamp, ContentId, ModelUsed

### Obiekty Wartości
- **ContentType**
  - Flashcard, Exercise, Example, Explanation
  - Sentence, Essay, GrammarRule (Nowość)

- **EvaluationType**
  - Speaking, Writing, Grammar, Vocabulary
  - Sentence, Essay (Nowość)

- **RequestStatus**
  - Pending, Processing, Completed, Failed

- **OperationType** (Nowość)
  - FlashcardGeneration, ExerciseGeneration, SentenceEvaluation
  - EssayAnalysis, GrammarValidation

### Zdarzenia Domenowe
- **AiContentGenerated**
- **EvaluationRequested**
- **EvaluationCompleted**
- **TokensConsumed** (Nowość)
- **ContentQualityAssessed** (Nowość)

### Repozytoria
- **IAiGeneratedContentRepository**
- **IEvaluationRequestRepository**
- **ITokenUsageRecordRepository** (Nowość)

### Usługi Domenowe
- **AiContentGenerationService**
- **AiEvaluationService**
- **QualityAssessmentService**
- **TokenTrackingService** (Nowość)
- **CostCalculationService** (Nowość)

---

## 7. Współdzielone Konteksty (Shared Kernel)

### Encje Współdzielone
- **Language**
  - Id, Code, Name, IsActive

- **Word** (Nowość)
  - Id, Text, LanguageId, Pronunciation, AudioUrl
  - PartOfSpeech, DifficultyLevel

### Obiekty Wartości Współdzielone
- **SessionType**
  - FlashcardReview, GrammarPractice, SpeakingPractice, Mixed
  - SentenceCreation, EssayAnalysis (Nowość)

- **PartOfSpeech** (Nowość)
  - Noun, Verb, Adjective, Adverb, Preposition, etc.

---

## Kluczowe Zmian w Modelu Domeny

### **Nowe Encje:**
1. **TokenBalance** - zarządzanie limitami tokenów
2. **Vocabulary** - słownictwo aktywne/pasywne
3. **WordTranslation** - pary słowo-tłumaczenie
4. **GrammarRule** - reguły zdefiniowane przez użytkownika
5. **GrammarQuiz** - quizy generowane z reguł
6. **SentenceCreation** - tworzenie zdań z aktywnych słów
7. **Essay** - dłuższe wypowiedzi
8. **TokenUsageRecord** - śledzenie zużycia tokenów

### **Nowe Obiekty Wartości:**
1. **VocabularyType** (Active/Passive)
2. **MasteryLevel** (poziom opanowania słowa)
3. **FlashcardTemplate** (formaty fiszek)
4. **SentenceCreationStatus** (status tworzenia zdań)
5. **EssayStatus** (status analizy esejów)
6. **OperationType** (typy operacji AI)
7. **PartOfSpeech** (części mowy)

### **Nowe Usługi Domenowe:**
1. **TokenManagementService** - zarządzanie tokenami
2. **VocabularyManagementService** - zarządzanie słownictwem
3. **QuizGenerationService** - generowanie quizów
4. **SentenceCreationService** - tworzenie zdań
5. **EssayAnalysisService** - analiza esejów
6. **TokenTrackingService** - śledzenie zużycia tokenów

---

## Przepływ Danych Zaktualizowany

1. **Użytkownik rejestruje się** → User Management Context (przyznaje tokeny)
2. **Tworzy konto językowe** → Language Learning Context (tworzy słownictwo)
3. **Dodaje słowo** → Language Learning Context (aktywne/pasywne)
4. **Generuje fiszki** → Flashcard System + AI Integration (zużywa tokeny)
5. **Uczy się fiszek** → Flashcard System + Language Learning
6. **Tworzy zdanie** → Language Production + AI Integration (zużywa tokeny)
7. **Analizuje esej** → Language Production + AI Integration (zużywa tokeny)
8. **Robi quiz gramatyczny** → Grammar Exercises + AI Integration (zużywa tokeny)
9. **Śledzi zużycie tokenów** → AI Integration + User Management

---

## Komunikacja Międzykontekstowa (Zaktualizowana)

### User Management → Language Learning
- UserCreated → LanguageAccountCreated
- TokensGranted → TokensAvailable

### Language Learning → Flashcard System
- LanguageAccountCreated → FlashcardCollectionCreated
- WordAdded → WordTranslationCreated
- LearningSettingsUpdated → DailyGenerationChanged

### Language Learning → Grammar Exercises
- LanguageAccountCreated → GrammarRuleCreated
- ProficiencyLevelChanged → DifficultyAdjusted

### Language Learning → Language Production
- LanguageAccountCreated → SpeakingSessionAvailable
- VocabularyUpdated → ActiveWordsChanged

### Wszystkie konteksty → AI Integration
- ContentGenerationRequested → TokensConsumed
- EvaluationRequested → TokensConsumed
- QualityCheckRequested → TokensConsumed

### AI Integration → User Management
- TokensConsumed → TokenBalanceUpdated
- MonthlyReset → TokensReset

Ten zaktualizowany model domeny uwzględnia wszystkie nowe funkcjonalności opisane w wymaganiach, zachowując czyste granice między kontekstami i spójność architektury DDD.

# Przypisanie Encji do Bounded Contexts

Ten dokument pokazuje dokładne przypisanie wszystkich encji, obiektów wartości i usług domenowych do odpowiednich bounded contexts w aplikacji fiszek do nauki języków.

## 1. Zarządzanie Użytkownikami (User Management Context)

### Encje
- **User** (Aggregate Root)
  - Id, Email, FirstName, LastName, PasswordHash, CreatedAt
  - LanguageAccounts: List<LanguageAccount>

### Obiekty Wartości
- Brak (prosty kontekst zarządzania użytkownikami)

### Zdarzenia Domenowe
- **UserRegistered**
- **UserUpdated**

### Repozytoria
- **IUserRepository**

### Usługi Domenowe
- **UserRegistrationService**
- **AuthenticationService**

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

### Obiekty Wartości
- **LearningSettings**
  - DailyGoal, ReviewMethod, AutoPlayAudio, ShowPronunciation
  - EnableHints, SessionDuration, PreferredExerciseTypes

- **ProficiencyLevel**
  - Level (A1-C2), Description

### Zdarzenia Domenowe
- **LanguageAccountCreated**
- **StudySessionCompleted**
- **LearningSettingsUpdated**

### Repozytoria
- **ILanguageAccountRepository**
- **IStudySessionRepository**

### Usługi Domenowe
- **ProgressTrackingService**
- **DifficultyAssessmentService**
- **LearningSettingsService**

---

## 3. System Fiszek (Flashcard System Context)

### Encje
- **FlashcardCollection** (Aggregate Root)
  - Id, LanguageAccountId, Title, Description, Category
  - DifficultyLevel, CreatedAt, UpdatedAt
  - Flashcards: List<Flashcard>

- **Flashcard**
  - Id, CollectionId, FrontText, BackText, FrontLanguage, BackLanguage
  - Type, Difficulty, Tags, ExampleSentence, Pronunciation
  - ImageUrl, AudioUrl, CreatedAt

- **FlashcardReview**
  - Id, FlashcardId, LanguageAccountId, ReviewResult
  - ReviewTime, NextReviewDate, ReviewInterval, EaseFactor

### Obiekty Wartości
- **DifficultyLevel**
  - Beginner, Intermediate, Advanced, Expert

- **FlashcardType**
  - Vocabulary, Grammar, Phrase, Idiom

- **ReviewResult**
  - Again (0), Hard (1), Good (2), Easy (3)

- **ReviewMethod**
  - SpacedRepetition, Random, DifficultyBased, RecentFirst

### Zdarzenia Domenowe
- **FlashcardCreated**
- **FlashcardReviewed**
- **FlashcardCollectionCreated**

### Repozytoria
- **IFlashcardCollectionRepository**
- **IFlashcardRepository**
- **IFlashcardReviewRepository**

### Usługi Domenowe
- **SpacedRepetitionService**
- **FlashcardGenerationService**
- **ReviewSchedulerService**

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

### Obiekty Wartości
- **ExerciseType**
  - FillInBlank, MultipleChoice, SentenceTransformation, Translation

### Zdarzenia Domenowe
- **ExerciseCompleted**
- **ExerciseGenerated**

### Repozytoria
- **IGrammarExerciseRepository**
- **IGrammarExerciseResultRepository**

### Usługi Domenowe
- **ExerciseGenerationService**
- **GrammarValidationService**
- **ExerciseDifficultyService**

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

### Obiekty Wartości
- **SpeakingType**
  - Pronunciation, Fluency, Grammar, Vocabulary, Conversation

### Zdarzenia Domenowe
- **SpeakingSessionStarted**
- **SpeakingSessionEvaluated**

### Repozytoria
- **ISpeakingSessionRepository**
- **ISpeakingEvaluationRepository**

### Usługi Domenowe
- **SpeakingEvaluationService**
- **AudioProcessingService**
- **FeedbackGenerationService**

---

## 6. Integracja AI (AI Integration Context)

### Encje
- **AiGeneratedContent** (Aggregate Root)
  - Id, ContentType, ContentId, GenerationPrompt
  - ModelUsed, GeneratedAt, QualityScore

- **EvaluationRequest** (Aggregate Root)
  - Id, ContentToEvaluate, EvaluationType, LanguageAccountId
  - RequestStatus, Result, RequestedAt, CompletedAt

### Obiekty Wartości
- **ContentType**
  - Flashcard, Exercise, Example, Explanation

- **EvaluationType**
  - Speaking, Writing, Grammar, Vocabulary

- **RequestStatus**
  - Pending, Processing, Completed, Failed

### Zdarzenia Domenowe
- **AiContentGenerated**
- **EvaluationRequested**
- **EvaluationCompleted**

### Repozytoria
- **IAiGeneratedContentRepository**
- **IEvaluationRequestRepository**

### Usługi Domenowe
- **AiContentGenerationService**
- **AiEvaluationService**
- **QualityAssessmentService**

---

## 7. Współdzielone Konteksty (Shared Kernel)

### Encje Współdzielone
- **Language**
  - Id, Code, Name, IsActive

### Obiekty Wartości Współdzielone
- **SessionType**
  - FlashcardReview, GrammarPractice, SpeakingPractice, Mixed

### Wyjaśnienie Podziału

### **User Management Context**
Najprostszy kontekst - zarządza tylko użytkownikami i ich podstawowymi danymi. Nie wie nic o nauce języków.

### **Language Learning Context**
Centralny kontekst łączący wszystkie inne. Zarządza kontami językowymi użytkowników, postępami i ustawieniami nauki.

### **Flashcard System Context**
Specjalistyczny kontekst do zarządzania fiszkami i algorytmami powtórzeń rozstawionych. Ma własne agregaty i logikę biznesową.

### **Grammar Exercises Context**
Odpowiada za tworzenie i ocenianie ćwiczeń gramatycznych. Dostosowuje trudność do poziomu użytkownika.

### **Language Production Context**
Specjalizuje się na ocenie mówienia i produkcji językowej. Wymaga integracji z AI do oceny.

### **AI Integration Context**
Techniczny kontekst do zarządzania wszystkimi interakcjami z AI - generowanie treści i ocenianie.

### **Shared Kernel**
Minimalny zbiór współdzielonych encji i wartości używanych przez wiele kontekstów.

---

## Komunikacja Międzykontekstowa

### User → Language Learning
- UserCreated → LanguageAccountCreated

### Language Learning → Flashcard System
- LanguageAccountCreated → FlashcardCollectionCreated
- LearningSettingsUpdated → ReviewMethodChanged

### Language Learning → Grammar Exercises
- LanguageAccountCreated → ExerciseGenerationRequested
- ProficiencyLevelChanged → DifficultyAdjusted

### Language Learning → Language Production
- LanguageAccountCreated → SpeakingSessionAvailable
- LearningSettingsUpdated → SessionTypeChanged

### Flashcard System → AI Integration
- FlashcardGenerationRequested → AiContentGenerated

### Grammar Exercises → AI Integration
- ExerciseGenerationRequested → AiContentGenerated

### Language Production → AI Integration
- SpeakingEvaluationRequested → EvaluationCompleted

### AI Integration → Language Production
- EvaluationCompleted → SpeakingSessionEvaluated

---

## Przepływ Danych

1. **Użytkownik rejestruje się** → User Management Context
2. **Tworzy konto językowe** → Language Learning Context
3. **Konfiguruje ustawienia** → Language Learning Context
4. **Generuje fiszki** → Flashcard System + AI Integration
5. **Uczy się fiszek** → Flashcard System + Language Learning
6. **Robi ćwiczenia** → Grammar Exercises + AI Integration
7. **Praktykuje mówienie** → Language Production + AI Integration
8. **Śledzi postępy** → Language Learning Context

Ten podział zapewnia jasne granice odpowiedzialności i minimalne zależności między kontekstami, co jest kluczowe dla skalowalnej i Maintainable architektury.

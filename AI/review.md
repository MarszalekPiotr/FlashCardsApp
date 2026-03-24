# Review Planów Implementacji - Uwagi DDD

Data review: 2026-03-20

## Analiza ogólna

Przeanalizowano dwa plany implementacji pod kątem:
- Zgodności z zasadami DDD
- Prostoty projektu
- Podejścia iteracyjnego (core → rozszerzenia)

---

## Uwagi do pliku: `plan-rozszerony-o-kod-zrodlami-01debd.md`

### 🔴 Problemy krytyczne

#### 1. Brak definicji MVP/Core
Plan zakłada pełną implementację wszystkich komponentów od razu. Brak wyraźnego rozróżnienia między:
- **Core** (niezbędne minimum do działania)
- **Rozszerzenia** (dodatkowe funkcjonalności)

**Rekomendacja:** Zdefiniować MVP zawierające tylko:
- Rejestrację użytkownika
- Jedno konto językowe
- Podstawowe fiszki (bez AI)
- Prosty SRS

#### 2. Przedwczesna optymalizacja infrastruktury
Sekcje 9-10 (Docker, Kubernetes, Monitoring) są przesadzone dla początkowego projektu:
```
### 9. Konfiguracja Docker i Deployment
- Dockerfile dla każdej usługi
- docker-compose.yml
- Kubernetes manifests

### 10. Monitoring i Logging
- Konfiguracja Serilog
- Metrics z Prometheus
- Health checks
```

**Rekomendacja:** Usunąć Kubernetes i Prometheus z planu początkowego. Wystarczy prosty Docker Compose.

#### 3. Zbyt duża liczba serwisów domenowych na start
Plan zawiera 20+ serwisów domenowych:
- TokenManagementService
- VocabularyManagementService
- QuizGenerationService
- SentenceCreationService
- EssayAnalysisService
- TokenTrackingService
- CostCalculationService
- WordSuggestionService
- DynamicCostCalculationService
- SentenceToFlashcardConverter
- MonthlyTokenResetService
- ...i więcej

**Rekomendacja:** Na start wystarczy 3-5 serwisów:
- UserRegistrationService
- FlashcardService (z prostym SRS)
- AuthenticationService

#### 4. Event Sourcing jako obowiązkowy
Faza 8 wprowadza Event Sourcing:
```
### Faza 8: Event Sourcing (Tydzień 12)
1. EventStore implementation
2. Aggregate snapshots
3. Event replay
4. Event versioning
```

**Rekomendacja:** Event Sourcing to zaawansowana technika. Powinien być:
- Opcjonalny
- Rozważony dopiero po wdrożeniu MVP
- Nie w fazie początkowej

---

## Uwagi do pliku: `polaczony-plan-implementacji.md`

### 🔴 Problemy krytyczne

#### 1. Zbyt wiele Bounded Contexts na start
Plan zakłada 6 bounded contexts od początku:
1. User Management
2. Language Learning
3. Flashcard System
4. Grammar Exercises
5. Language Production
6. AI Integration

**Problem z DDD:** Zasada "start small" mówi, że należy zacząć od 1-2 kontekstów i rozwijać iteracyjnie.

**Rekomendacja MVP:**
```
Faza 1 (MVP):
├── User Management Context (rejestracja, auth)
└── Language Learning Context (konto językowe, fiszki, prosty SRS)

Faza 2 (Rozszerzenia):
├── Flashcard System Context (zaawansowane SRS, szablony)
└── AI Integration Context (generowanie fiszek)

Faza 3 (Zaawansowane):
├── Grammar Exercises Context
└── Language Production Context
```

#### 2. Przedwczesna złożoność modelu domenowego
Plan zawiera 32 zdarzenia domenowe na start:
```
### Kompletna Lista Eventów:
1. UserRegistered
2. UserUpdated
3. TokensGranted
4. TokensSpent
5. MonthlyTokenReset
...
32. DynamicCostCalculated
```

**Rekomendacja:** MVP powinno zawierać 5-8 eventów:
- UserRegistered
- LanguageAccountCreated
- FlashcardCreated
- FlashcardReviewed
- StudySessionCompleted

#### 3. Harmonogram 18 tygodni przed deployment
```
### Faza 11: Deployment (Tygodnie 16-18)
```

**Problem:** Zbyt długo bez działającego produktu. Użytkownik nie widzi wartości przez 4 miesiące.

**Rekomendacja:** Deployment po 4-6 tygodniach z MVP, potem iteracyjne rozszerzanie.

#### 4. Nadmiarowe encje w modelu
Niektóre encje są zbędne na start:
- **Essay** - zaawansowana funkcjonalność
- **GrammarQuiz** - można dodać później
- **TokenUsageRecord** - można uprościć do pola w TokenBalance
- **CostConfiguration** - przesada, wystarczą stałe

---

## Wspólne problemy obu planów

### 1. Brak priorytetyzacji funkcjonalności
Oba plany traktują wszystkie funkcje równoważnie. Brak pytań:
- Co jest niezbędne?
- Co jest "nice to have"?
- Co można odłożyć na później?

### 2. Zbyt dużo repozytoriów
Łącznie 15+ repozytoriów. Dla MVP wystarczy 4-5:
- IUserRepository
- ILanguageAccountRepository
- IFlashcardRepository
- IFlashcardReviewRepository

### 3. System tokenów jako obowiązkowy od razu
System tokenów jest złożony:
- TokenBalance z wieloma typami tokenów
- MonthlyTokenResetService
- CostCalculationService
- TokenUsageRecord

**Rekomendacja:** Tokeny wprowadzić w fazie 2, po działającym MVP.

### 4. AI Integration od początku
AI jest kosztowne i złożone. Wymaga:
- Integracji z zewnętrznym API
- Obsługi błędów sieciowych
- Fallbacki
- Śledzenia kosztów

**Rekomendacja:** Zacząć od ręcznie tworzonych fiszek. AI dodać w fazie 2.

---

## Proponowany plan iteracyjny (zgodny z DDD)

### Faza 1: Core MVP (Tygodnie 1-4)

**Bounded Contexts:**
- User Management (uproszczony)
- Language Learning (podstawowy)

**Encje:**
- User (Aggregate Root)
- LanguageAccount (Aggregate Root)
- Flashcard (Entity)
- FlashcardReview (Entity)

**Value Objects:**
- Email
- ProficiencyLevel
- ReviewResult

**Eventy domenowe:**
- UserRegistered
- LanguageAccountCreated
- FlashcardCreated
- FlashcardReviewed

**Repozytoria:**
- IUserRepository
- ILanguageAccountRepository
- IFlashcardRepository

**Deployment:** Po 4 tygodniach - działający produkt z podstawową wartością.

---

### Faza 2: Rozszerzenia Core (Tygodnie 5-8)

**Dodane Bounded Contexts:**
- Flashcard System (SRS, szablony)
- AI Integration (generowanie)

**Nowe encje:**
- FillInBlankFlashcard
- WordTranslation
- AiGeneratedContent

**Nowe funkcjonalności:**
- System tokenów (uproszczony)
- Generowanie fiszek przez AI
- Zaawansowane SRS

---

### Faza 3: Produkcja językowa (Tygodnie 9-12)

**Dodane Bounded Contexts:**
- Language Production

**Nowe encje:**
- SentenceCreation
- SpeakingSession

**Nowe funkcjonalności:**
- Tworzenie zdań
- Ocena przez AI
- Konwersja zdanie → fiszka

---

### Faza 4: Gramatyka (Tygodnie 13-16)

**Dodane Bounded Contexts:**
- Grammar Exercises

**Nowe encje:**
- GrammarRule
- GrammarQuiz
- GrammarExercise

---

### Faza 5: Zaawansowane (Opcjonalnie)

**Możliwe rozszerzenia:**
- Event Sourcing
- Kubernetes
- Monitoring (Prometheus)
- Essay analysis
- Zaawansowany system tokenów

---

## Kluczowe zasady DDD naruszone w planach

### 1. ⚠️ "Start with a small model"
Plany zaczynają od zbyt dużego modelu (6 kontekstów, 32 eventy).

### 2. ⚠️ "Focus on core domain"
Brak rozróżnienia między core a pomocniczymi funkcjonalnościami.

### 3. ⚠️ "Iterative refinement"
Plany są "waterfall" - wszystko zaplanowane na początku, brak iteracji.

### 4. ✅ "Bounded Contexts" - zachowane
Granice kontekstów są dobrze zdefiniowane.

### 5. ✅ "Ubiquitous Language" - zachowane
Język domenowy jest spójny i jasny.

### 6. ✅ "Aggregates" - zachowane
Agregaty są poprawnie zidentyfikowane z Aggregate Roots.

---

## Podsumowanie rekomendacji

### Co usunąć z planu początkowego:
1. ❌ Grammar Exercises Context
2. ❌ Language Production Context
3. ❌ Event Sourcing
4. ❌ Kubernetes
5. ❌ Prometheus/Monitoring
6. ❌ Essay entity
7. ❌ GrammarQuiz entity
8. ❌ TokenUsageRecord
9. ❌ CostConfiguration
10. ❌ DynamicCostCalculationService

### Co uprościć:
1. 🔧 System tokenów → prosty licznik w User
2. 🔧 AI Integration → opcjonalne w fazie 2
3. 🔧 6 kontekstów → 2 konteksty na start
4. 🔧 32 eventy → 5-8 eventów na start
5. 🔧 15+ repozytoriów → 4-5 repozytoriów na start

### Co zachować w MVP:
1. ✅ User Management Context (uproszczony)
2. ✅ Language Learning Context (podstawowy)
3. ✅ Rejestracja i autentykacja
4. ✅ Konta językowe
5. ✅ Podstawowe fiszki
6. ✅ Prosty SRS (bez zaawansowanych algorytmów)
7. ✅ Study sessions

---

## Zalecenia końcowe

1. **Stwórz definicję MVP** - co jest absolutnie niezbędne?
2. **Zacznij od 2 kontekstów** - User Management + Language Learning
3. **Deployment po 4 tygodniach** - działający produkt
4. **Dodawaj funkcjonalności iteracyjnie** - co 2-4 tygodnie
5. **AI jako opcja** - nie wymóg na start
6. **Tokeny w fazie 2** - po walidacji MVP
7. **Event Sourcing opcjonalny** - rozważyć tylko jeśli potrzebny

Podejście iteracyjne jest kluczowe dla sukcesu projektu. "Perfect is the enemy of good" - lepiej mieć działający prosty produkt po miesiącu niż niedokończony kompleksowy po czterech.

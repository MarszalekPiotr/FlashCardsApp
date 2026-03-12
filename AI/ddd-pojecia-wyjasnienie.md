# Wyjaśnienie Pojęć DDD w Planie Aplikacji Fiszek

Ten dokument wyjaśnia kluczowe pojęcia Domain-Driven Design (DDD) użyte w planie aplikacji fiszek do nauki języków.

## Czym jest Bounded Context?

### Definicja DDD
**Bounded Context** to granica wewnętrzna, w której dany model domeny ma określone, spójne znaczenie. Poza tą granicą ten sam termin może mieć inne znaczenie.

### W naszej aplikacji:

#### 1. Zarządzanie Użytkownikami (User Management Context)
- **Granica:** Tylko operacje związane z użytkownikami i tokenami
- **Spójność:** User znaczy tylko "konto użytkownika systemu", Token to "jednostka zużywania AI"
- **Dlaczego osobny kontekst:** Logika rejestracji, autentykacji i zarządzania tokenami jest niezależna od nauki języków
- **ZMIANA:** Dodano zarządzanie systemem tokenów dla operacji AI

#### 2. Nauka Języków (Language Learning Context)
- **Granica:** Cały proces nauki języka z podziałem na słownictwo aktywne/pasywne
- **Spójność:** LanguageAccount to "konto do nauki konkretnego języka", Vocabulary to "zbiór słów podzielony na aktywne/pasywne"
- **Dlaczego osobny kontekst:** Koordynuje wszystkie inne aspekty nauki, zarządza słownictwem i postępami
- **ZMIANA:** Dodano rozróżnienie słownictwa aktywnego i pasywnego oraz zarządzanie słowami

#### 3. System Fiszek (Flashcard System Context)
- **Granica:** Tylko logika fiszek i powtórek SRS z automatyczną generacją
- **Spójność:** Flashcard to "karta do nauki z algorytmem SRS", WordTranslation to "para słowo-tłumaczenie"
- **Dlaczego osobny kontekst:** Specjalistyczne algorytmy SRS i automatyczna generacja wymagają izolacji
- **ZMIANA:** Dodano automatyczną generację fiszek i format z lukami oraz synonimami

#### 4. Ćwiczenia Gramatyczne (Grammar Exercises Context)
- **Granica:** Logika ćwiczeń gramatycznych i zarządzanie regułami użytkownika
- **Spójność:** GrammarExercise to "zadanie gramatyczne z oceną", GrammarRule to "reguła zdefiniowana przez użytkownika"
- **Dlaczego osobny kontekst:** Specjalistyczna logika walidacji i generowania quizów na podstawie reguł
- **ZMIANA:** Dodano zarządzanie własnymi regułami gramatycznymi i generowanie quizów

#### 5. Produkcja Językowa (Language Production Context)
- **Granica:** Ocena wypowiedzi użytkownika i tworzenie zdań
- **Spójność:** SpeakingSession to "sesja mówienia oceniona przez AI", SentenceCreation to "tworzenie zdań z aktywnych słów", Essay to "dłuższa wypowiedź"
- **Dlaczego osobny kontekst:** Wymaga zaawansowanej integracji z AI do oceny różnych typów produkcji językowej
- **ZMIANA:** Dodano tworzenie zdań z aktywnych słów i analizę dłuższych wypowiedzi

#### 6. Integracja AI (AI Integration Context)
- **Granica:** Operacje związane z AI i zużywanie tokenów
- **Spójność:** AiGeneratedContent to "treść wygenerowana przez model AI", TokenUsage to "zapis zużycia tokenów"
- **Dlaczego osobny kontekst:** Techniczny kontekst do obsługi modeli ML i śledzenia kosztów
- **ZMIANA:** Dodano śledzenie zużycia tokenów i kontrolę kosztów operacji AI

---

## Czym jest Aggregate?

### Definicja DDD
**Aggregate** to klaster powiązanych obiektów domeny, który traktujemy jako jedną całość. Ma wyznaczonego **Aggregate Root** - główną encję kontrolującą dostęp.

### W naszej aplikacji:

#### User Aggregate (Context 1)
- **Aggregate Root:** User
- **Obiekty wewnątrz:** LanguageAccounts, TokenBalance
- **Invariant:** Użytkownik musi mieć unikalny email i nie może przekroczyć limitu tokenów
- **Operacje:** Rejestracja, aktualizacja danych, zarządzanie tokenami
- **ZMIANA:** Dodano TokenBalance do zarządzania miesięcznym limitem tokenów

#### LanguageAccount Aggregate (Context 2)
- **Aggregate Root:** LanguageAccount
- **Obiekty wewnątrz:** LearningSettings, StudySessions, Vocabulary
- **Invariant:** Konto musi mieć zdefiniowany język docelowy i słownictwo podzielone na aktywne/pasywne
- **Operacje:** Tworzenie konta, ustawianie celów, śledzenie postępów, zarządzanie słownictwem
- **ZMIANA:** Dodano Vocabulary do zarządzania słownictwem aktywnym i pasywnym

#### FlashcardCollection Aggregate (Context 3)
- **Aggregate Root:** FlashcardCollection
- **Obiekty wewnątrz:** Flashcards, WordTranslations
- **Invariant:** Kolekcja musi mieć co najmniej jedną fiszkę, fiszki muszą być w formacie z lukami
- **Operacje:** Dodawanie fiszek, przeglądanie, obliczanie powtórek, automatyczna generacja
- **ZMIANA:** Dodano WordTranslations i automatyczną generację fiszek w formacie z lukami

#### GrammarExercise Aggregate (Context 4)
- **Aggregate Root:** GrammarExercise
- **Obiekty wewnątrz:** GrammarExerciseResults, GrammarRules
- **Invariant:** Ćwiczenie musi mieć pytanie i poprawną odpowiedź, reguły muszą być zdefiniowane przez użytkownika
- **Operacje:** Tworzenie ćwiczeń, ocenianie odpowiedzi, zarządzanie regułami, generowanie quizów
- **ZMIANA:** Dodano GrammarRules i generowanie quizów na podstawie reguł użytkownika

#### SpeakingSession Aggregate (Context 5)
- **Aggregate Root:** SpeakingSession
- **Obiekty wewnątrz:** SpeakingEvaluation, SentenceCreations, Essays
- **Invariant:** Sesja musi mieć prompt i odpowiedź użytkownika, tworzenie zdań wymaga aktywnych słów
- **Operacje:** Rozpoczynanie sesji, ocenianie wypowiedzi, tworzenie zdań, analiza esejów
- **ZMIANA:** Dodano SentenceCreations i Essays do różnych typów produkcji językowej

#### AiGeneratedContent Aggregate (Context 6)
- **Aggregate Root:** AiGeneratedContent
- **Obiekty wewnątrz:** EvaluationRequests, TokenUsageRecords
- **Invariant:** Treść AI musi mieć zdefiniowany typ i jakość, każda operacja musi zużywać tokeny
- **Operacje:** Generowanie treści, ocenianie, zarządzanie jakością, śledzenie zużycia tokenów
- **ZMIANA:** Dodano TokenUsageRecords do śledzenia kosztów operacji AI

---

## Czym jest Context (w kontekście DDD)?

### Definicja DDD
**Context** (w pełnym brzmieniu Bounded Context) to zamknięty świat modelu domeny z własnym językiem (Ubiquitous Language) i regułami biznesowymi.

### W naszej aplikacji:

#### Context vs Bounded Context
- **Context** = skrót od **Bounded Context**
- Każdy context ma własny:
  - Model domeny
  - Język biznesowy (Ubiquitous Language)
  - Zasady integralności
  - Zespół deweloperski (potencjalnie)

#### Przykłady języka w różnych contextach:
- **User Management Context:** "User registered successfully"
- **Language Learning Context:** "Language account created for English learning"
- **Flashcard System Context:** "Flashcard scheduled for review in 3 days"

---

## Dlaczego Taki Podział?

### 1. **Single Responsibility Principle**
Każdy bounded context ma jedną, jasno zdefiniowaną odpowiedzialność:
- Context 1: "Zarządzaj użytkownikami i tokenami"
- Context 2: "Koordynuj naukę języków i słownictwem"
- Context 3: "Implementuj system fiszek SRS z automatyczną generacją"
- Context 4: "Zarządzaj ćwiczeniami gramatycznymi i regułami użytkownika"
- Context 5: "Oceniaj produkcję językową (zdania, esej, wypowiedzi)"
- Context 6: "Integruj usługi AI i śledź zużycie tokenów"
- **ZMIANA:** Zaktualizowano opisy kontekstów o nowe funkcjonalności

### 2. **Complexity Management**
Zamiast jednego gigantycznego modelu, mamy 6 mniejszych, zrozumiałych kontekstów.

### 3. **Team Autonomy**
Różne zespoły mogą pracować nad różnymi kontekstami bez konfliktów.

### 4. **Independent Deployment**
Każdy kontekst może być wdrażany niezależnie.

### 5. **Technology Diversity**
Różne konteksty mogą używać różnych technologii:
- AI Integration: Python + ML frameworks
- User Management: Standardowy web stack
- Flashcard System: Zoptymalizowana baza danych

---

## Mapowanie Kontekstów (Context Map)

### Relacje między kontekstami:

```
User Management → Language Learning (Customer/Supplier)
Language Learning → Flashcard System (Customer/Supplier)
Language Learning → Grammar Exercises (Customer/Supplier)
Language Learning → Language Production (Customer/Supplier)
Flashcard System → AI Integration (Customer/Supplier)
Grammar Exercises → AI Integration (Customer/Supplier)
Language Production → AI Integration (Customer/Supplier)
```

### Wyjaśnienie relacji:
- **Customer/Supplier:** Jeden kontekst (Customer) potrzebuje usług drugiego (Supplier)
- **Upstream:** Supplier (dostarcza dane/usługi)
- **Downstream:** Customer (używa danych/usług)

---

## Przykład Przepływu Międzykontekstowego

### Scenariusz: Nowy użytkownik zaczyna naukę

1. **User Management Context:**
   ```
   POST /api/users
   → UserRegistered event
   ```

2. **Language Learning Context:**
   ```
   Listens to UserRegistered
   → Creates default LanguageAccount
   → LanguageAccountCreated event
   ```

3. **Flashcard System Context:**
   ```
   Listens to LanguageAccountCreated
   → Creates default FlashcardCollection
   → Requests AI to generate basic flashcards
   ```

4. **AI Integration Context:**
   ```
   Receives generation request
   → Generates flashcards using ML model
   → Returns generated content
   ```

---

## Kluczowe Korzyści Tego Podziału

### 1. **Zrozumiałość**
- Każdy programista może zrozumieć jeden kontekst w izolacji
- Jasne granice odpowiedzialności

### 2. **Testowalność**
- Każdy kontekst może być testowany niezależnie
- Mockowanie zależności międzykontekstowych

### 3. **Skalowalność**
- Context 3 (Flashcard System) może być zoptymalizowany pod kątem wydajności
- Context 6 (AI Integration) może być skalowany niezależnie

### 4. **Elastyczność**
- Można zastąpić algorytm powtórek w Context 3 bez zmian w Context 2
- Można dodać nowy typ ćwiczeń w Context 4 bez wpływu na inne

### 5. **Bezpieczeństwo**
- Różne poziomy dostępu dla różnych kontekstów
- Izolacja danych wrażliwych

---

## Współdzielone Elementy (Shared Kernel)

### Co jest współdzielone:
- **Language** - podstawowa encja języka
- **Word** - jednostka słowa (może mieć wiele tłumaczeń)
- **VocabularyType** - enum (Active, Passive)
- **Podstawowe Value Objects** - enumy używane w wielu kontekstach
- **ZMIANA:** Dodano Word i VocabularyType jako nowe współdzielone encje

### Dlaczego minimalizm:
- Im mniej współdzielonych elementów, tym większa niezależność
- Współdzielone elementy tworzą dodatkowe zależności

---

## Podsumowanie

### **Bounded Context** = Granica spójności modelu
- Każdy ma własny język i reguły
- Izoluje złożoność

### **Aggregate** = Jednostka spójności danych
- Chroni integralność biznesową
- Kontroluje dostęp do obiektów wewnętrznych

### **Context** = Świat biznesowy
- Reprezentuje konkretny obszar działalności
- Ma swoich ekspertów domenowych

### **Dlaczego ten podział:**
- Zarządzanie złożonością
- Niezależny rozwój
- Lepsza testowalność
- Elastyczność w zmianach
- Skalowalność systemu

Ten podział pozwala budować dużą, złożoną aplikację kawałek po kawałku, zachowując kontrolę nad każdym aspektem systemu.

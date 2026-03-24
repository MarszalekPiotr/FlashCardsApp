# Plan Rozwoju Aplikacji - Zaawansowane Techniki Senior .NET Developera

Ten dokument opisuje plan rozwoju aplikacji fiszek poprzez wprowadzanie zaawansowanych technik programowania. Każda technika jest przedstawiona jako konkretna implementacja w aplikacji, co pozwala na praktyczne opanowanie umiejętności.

---

## Spis Treści

1. [Event Sourcing w Domenie](#1-event-sourcing-w-domenie)
2. [Azure Cloud Infrastructure](#2-azure-cloud-infrastructure)
3. [Distributed Cache z Redis](#3-distributed-cache-z-redis)
4. [Asynchronous Processing z Message Queues](#4-asynchronous-processing-z-message-queues)
5. [Skalowalność i Performance](#5-skalowalność-i-performance)
6. [Containerization i Kubernetes](#6-containerization-i-kubernetes)
7. [Observability i Monitoring](#7-observability-i-monitoring)
8. [Security i Authentication](#8-security-i-authentication)
9. [Advanced Testing Strategies](#9-advanced-testing-strategies)
10. [CI/CD i Infrastructure as Code](#10-cicd-i-infrastructure-as-code)

---

## 1. Event Sourcing w Domenie

### Opis implementacji
Zastąpienie tradycyjnego podejścia CRUD Event Sourcingiem dla kluczowych agregatów. Pozwoli to na pełną historię zmian, audit trail i możliwość odtwarzania stanu.

### Agregaty z Event Sourcing

#### User Aggregate
```
Events:
- UserRegistered (email, firstName, lastName, passwordHash)
- UserEmailChanged (oldEmail, newEmail)
- UserProfileUpdated (firstName, lastName)
- UserDeactivated (reason)
- UserReactivated

Projections:
- UserSummary (dla list użytkowników)
- UserActivityLog (dla audit)
- UserStatistics (dla dashboard)
```

#### TokenBalance Aggregate
```
Events:
- TokensGranted (userId, amount, reason, type)
- TokensConsumed (userId, amount, operation, correlationId)
- TokensRefunded (userId, amount, originalOperationId)
- MonthlyTokenReset (userId, preservedAmount, expiredAmount)
- TokensPurchased (userId, amount, paymentId)

Projections:
- TokenBalanceSummary (aktualny stan)
- TokenUsageHistory (historia zużycia)
- TokenCostReport (dla billing)
```

#### Flashcard Aggregate
```
Events:
- FlashcardCreated (front, back, languageAccountId)
- FlashcardReviewed (result, interval, easeFactor)
- FlashcardDifficultyAdjusted (newDifficulty)
- FlashcardArchived (reason)
- FlashcardRestored

Projections:
- FlashcardReviewHistory (dla SRS)
- FlashcardStatistics (dla progress tracking)
- SpacedRepetitionSchedule (dla scheduling)
```

### Implementacja techniczna

#### Event Store
```
Technologia: Azure Event Hubs + Azure Cosmos DB
- Event Hubs: append-only log dla eventów
- Cosmos DB: materialized views dla projections
- Snapshotting: co 100 eventów dla optymalizacji

Schema:
{
  "eventId": "guid",
  "aggregateId": "guid",
  "aggregateType": "string",
  "eventType": "string",
  "eventData": "json",
  "metadata": {
    "correlationId": "guid",
    "causationId": "guid",
    "userId": "guid",
    "timestamp": "datetime"
  },
  "version": "int"
}
```

#### Projections Engine
```
Real-time projections:
- Azure Functions triggered by Event Hubs
- Cosmos DB Change Feed dla projections updates
- Materialized views dla read models

Catch-up projections:
- Background worker dla rebuilding projections
- Idempotent handlers
- Checkpointing w Azure Blob Storage
```

### Korzyści dla aplikacji
- **Audit trail:** Każda operacja na tokenach jest zapisana
- **Time travel:** Możliwość odtworzenia stanu z dowolnego momentu
- **Debugging:** Łatwe śledzenie co się stało i kiedy
- **Compliance:** Pełna historia dla auditów

---

## 2. Azure Cloud Infrastructure

### Opis implementacji
Pełna migracja do Azure z wykorzystaniem managed services. Eliminacja self-hosted infrastructure.

### Azure Services Architecture

#### Compute
```
Azure App Service (Premium V3)
├── Production slot
├── Staging slot (blue-green deployment)
├── Auto-scaling (2-10 instances)
└── Availability zones

Azure Functions (Consumption Plan)
├── DailyFlashcardGenerator (Timer trigger)
├── TokenResetScheduler (Timer trigger)
├── EmailSender (Queue trigger)
├── AiEvaluationProcessor (Queue trigger)
└── FlashcardNotificationDispatcher (Event Hub trigger)
```

#### Data Storage
```
Azure SQL Database
├── Business Critical tier
├── Read replicas (2)
├── Geo-replication (secondary region)
├── Point-in-time restore (35 days)
└── Automatic tuning enabled

Azure Cosmos DB
├── API: Core (SQL)
├── Consistency: Session
├── Multi-region writes
├── Partition key: /languageAccountId
└── Collections:
    ├── Flashcards
    ├── Projections
    └── Events

Azure Blob Storage
├── Hot tier: audio files
├── Cool tier: old exports
├── Archive tier: audit logs
├── CDN enabled
└── Lifecycle management policies
```

#### Caching & Messaging
```
Azure Cache for Redis
├── Premium tier
├── Cluster mode enabled
├── Data persistence (AOF)
├── Geo-replication
└── VNET integration

Azure Service Bus
├── Premium tier
├── Topics: user-events, flashcard-events, ai-events
├── Queues: email-sending, flashcard-generation
├── Dead-letter queues
└── Scheduled messages
```

#### Security & Secrets
```
Azure Key Vault
├── Secrets: connection strings, API keys
├── Keys: encryption keys
├── Certificates: SSL/TLS
├── Soft-delete enabled
└── Purge protection

Azure Managed Identity
├── System-assigned dla App Service
├── User-assigned dla Functions
└── RBAC permissions
```

### Konfiguracja środowisk
My tutaj pozostaniemy sobie na development i skorzystamy raczej z darmowych rozwiązań
ale to jeszcze daleka droga do tego etapu więc sobie to jeszcze przedeskutujemy
#### Development
```
Resource Group: flashcards-dev-rg
├── App Service: Basic tier
├── SQL Database: Basic tier
├── Cosmos DB: Free tier
├── Redis: Basic tier
├── Service Bus: Standard tier
└── Storage: LRS

Cost target: $50-100/month
```

#### Staging
```
Resource Group: flashcards-staging-rg
├── App Service: Standard tier
├── SQL Database: Standard S2
├── Cosmos DB: 400 RU/s
├── Redis: Standard tier
├── Service Bus: Standard tier
└── Storage: GRS

Cost target: $200-300/month
```

#### Production
```
Resource Group: flashcards-prod-rg
├── App Service: Premium V3
├── SQL Database: Business Critical
├── Cosmos DB: 1000+ RU/s, multi-region
├── Redis: Premium with geo-replication
├── Service Bus: Premium
└── Storage: GZRS, CDN enabled

Cost target: $1000-2000/month
```

### Deployment Strategy
```
Azure DevOps / GitHub Actions
├── Infrastructure: ARM templates / Bicep
├── Application: Docker containers → App Service
├── Functions: Zip deploy
├── Database: EF Core migrations
└── Configuration: Azure App Configuration
```

---

## 3. Distributed Cache z Redis

### Opis implementacji
Wdrożenie distributed cache dla hot data i session management. Redis jako warstwa cache dla często używanych danych.

### Cache Scenarios

#### User Session Cache
```
Key pattern: session:{sessionId}
TTL: 24 hours
Data:
{
  "userId": "guid",
  "email": "string",
  "roles": ["string"],
  "tokenBalance": "int",
  "lastActivity": "datetime"
}

Invalidation:
- On token consumption
- On role change
- On logout
```

#### Flashcard Cache
```
Key pattern: flashcards:due:{languageAccountId}:{date}
TTL: 1 hour
Data: List of flashcard IDs due for review

Key pattern: flashcard:{flashcardId}
TTL: 10 minutes
Data: Complete flashcard object

Invalidation:
- On flashcard review
- On flashcard edit
- On SRS recalculation
```

#### Vocabulary Cache
```
Key pattern: vocabulary:active:{languageAccountId}
TTL: 30 minutes
Data: List of active vocabulary IDs

Key pattern: vocabulary:word:{wordId}
TTL: 1 hour
Data: Word details with translations

Invalidation:
- On word addition
- On vocabulary type change
- On mastery level update
```

#### Statistics Cache
```
Key pattern: stats:daily:{userId}:{date}
TTL: 5 minutes
Data: Daily study statistics

Key pattern: stats:weekly:{userId}:{week}
TTL: 1 hour
Data: Weekly progress summary

Invalidation:
- On study session completion
- On flashcard review
```

### Redis Patterns Implementation

#### Cache-Aside Pattern
```
Application flow:
1. Check Redis cache
2. If miss → query database
3. Store result in cache
4. Return data

Implementation:
- ICacheService interface
- Decorator pattern dla repositories
- Automatic serialization
```

#### Write-Through Pattern
```
Application flow:
1. Write to database
2. Update Redis cache
3. Return result

Use cases:
- User profile updates
- Token balance changes
- Flashcard modifications
```

#### Cache Invalidation
```
Strategies:
1. Time-based expiration (TTL)
2. Event-based invalidation
3. Tag-based invalidation (groups)

Implementation:
- Domain events trigger invalidation
- Background worker for tag cleanup
- Pub/Sub dla distributed invalidation
```

#### Distributed Locking
```
Use cases:
- Token consumption (prevent race conditions)
- Flashcard generation (prevent duplicates)
- Monthly token reset (single execution)
Tutaj też raczej nie będziemy się bawić lockami
ale pójdziemy w thread safe rozwiązania związanymi z kolekcjami
Implementation:
- Redlock algorithm
- Lock acquisition with TTL
- Automatic lock renewal
```

### Redis Cluster Configuration
```
Z tymi replikami to raczej pogadamy sobie po co ale nie będą one nam potrzebne
Primary nodes: 3
Replica nodes: 3
Sharding: Hash-based
Failover: Automatic

Memory: 6 GB per node
Max connections: 10,000
Persistence: AOF every second
```

---

## 4. Asynchronous Processing z Message Queues

### Opis implementacji
Wdrożenie asynchronicznego przetwarzania dla długotrwałych operacji. Oddzielenie user-facing operations od background processing.

### Message Flow Architecture

#### Flashcard Generation Flow
```
User Request
    ↓
API: POST /flashcards/generate
    ↓
Publish: FlashcardGenerationRequested
    ↓
Queue: flashcard-generation-queue
    ↓
Consumer: FlashcardGenerationWorker
    ↓
Process: AI generation (1-5 minutes)
    ↓
Publish: FlashcardsGenerated
    ↓
Queue: notification-queue
    ↓
Consumer: NotificationSender
    ↓
User: SignalR notification
```

#### AI Evaluation Flow
Tutaj raczej nie będziemy korzystać z azure funkcji ale spoko pogadać po co one są
```
User Request
    ↓
API: POST /sentences/evaluate
    ↓
Publish: SentenceEvaluationRequested
    ↓
Event Hub: ai-evaluation-hub
    ↓
Consumer: AiEvaluationProcessor (Azure Function)
    ↓
Process: OpenAI API call (5-30 seconds)
    ↓
Publish: SentenceEvaluated
    ↓
Update: Database
    ↓
User: SignalR notification
```

#### Email Sending Flow
```
System Event
    ↓
Publish: EmailSendingRequested
    ↓
Queue: email-sending-queue
    ↓
Consumer: EmailSenderWorker
    ↓
Process: SendGrid API call
    ↓
Publish: EmailSent
    ↓
Log: Audit trail
```

### Message Types & Queues
Zdecydowanie bym chciał na kolejkach i kolejnym temacie
z sagą i maszyną stanów spędzić więcej czasu ale
raczej pogadać po co niż koniecznie implememntować.
#### Azure Service Bus Queues
```
flashcard-generation-queue
├── Message: FlashcardGenerationRequested
├── TTL: 7 days
├── Lock duration: 5 minutes
├── Max delivery count: 3
└── Dead-letter on failure

email-sending-queue
├── Message: EmailSendingRequested
├── TTL: 14 days
├── Lock duration: 1 minute
├── Max delivery count: 5
└── Retry policy: exponential backoff

token-reset-queue
├── Message: MonthlyTokenResetScheduled
├── TTL: 30 days
├── Scheduled delivery
└── Single consumer
```

#### Azure Service Bus Topics
```
user-events-topic
├── Subscriptions:
│   ├── email-service-sub
│   ├── analytics-service-sub
│   └── notification-service-sub
└── Filters:
    ├── messageType = 'UserRegistered'
    └── messageType = 'UserDeactivated'

flashcard-events-topic
├── Subscriptions:
│   ├── statistics-service-sub
│   ├── notification-service-sub
│   └── search-index-sub
└── Filters:
    └── messageType IN ('FlashcardCreated', 'FlashcardReviewed')

ai-events-topic
├── Subscriptions:
│   ├── billing-service-sub
│   ├── quality-monitor-sub
│   └── analytics-service-sub
└── Filters:
    └── messageType = 'AiContentGenerated'
```

### Saga Implementation

#### Flashcard Generation Saga
```
States:
1. Started
2. TokensConsumed
3. Generating
4. Completed / Failed

Compensating Actions:
- On failure: Refund tokens
- On timeout: Cancel generation
- On quality issue: Regenerate

Implementation:
- MassTransit state machine
- Saga persistence in SQL
- Timeout handling
```

#### Token Purchase Saga
```
States:
1. PaymentInitiated
2. PaymentCompleted
3. TokensGranted
4. Completed / Failed

Compensating Actions:
- On payment failure: Notify user
- On grant failure: Refund payment

Integration:
- Stripe webhook
- Payment verification
- Idempotent token granting
```

### Error Handling

#### Retry Policies
```
Transient errors:
- Exponential backoff: 1s, 2s, 4s, 8s, 16s
- Max retries: 5
- Circuit breaker after 10 failures

Permanent errors:
- Move to dead-letter queue
- Alert operations team
- Manual investigation
```

#### Dead Letter Queue Processing
```
Monitoring:
- Azure Monitor alerts
- Daily report of failed messages
- Dashboard with failure reasons

Reprocessing:
- Manual review
- Fix and resubmit
- Archive after 30 days
```

---

## 5. Skalowalność i Performance

### Opis implementacji
Optymalizacja aplikacji pod kątem wysokiej wydajności i skalowalności. Target: 10,000 concurrent users, p95 latency < 200ms.
To będzie ciekawe :)
### Database Optimization

#### Indexing Strategy
```
Clustered indexes:
- Primary keys (default)

Non-clustered indexes:
- Flashcards: (LanguageAccountId, NextReviewDate) INCLUDE (Front, Back)
- Vocabulary: (LanguageAccountId, VocabularyType, MasteryLevel)
- TokenTransactions: (UserId, Timestamp) INCLUDE (Amount, Type)
- StudySessions: (LanguageAccountId, StartTime) INCLUDE (Score)

Filtered indexes:
- Active flashcards: WHERE IsActive = 1
- Due flashcards: WHERE NextReviewDate <= GETUTCDATE()
- Unprocessed messages: WHERE ProcessedAt IS NULL

Columnstore indexes:
- Analytics tables
- Audit logs
- Historical data
```

#### Query Optimization
```
N+1 problem elimination:
- EF Core: AsSplitQuery() dla complex queries
- Dapper: Multi-mapping dla joins
- Projection: Select only needed columns

Pagination:
- Keyset pagination (not OFFSET)
- Cursor-based dla infinite scroll
- Max page size: 100

Read replicas:
- All read queries → replica
- Write queries → primary
- Connection routing w connection string
```

#### Connection Pooling
```
Configuration:
- Min pool size: 10
- Max pool size: 100
- Connection timeout: 30s
- Connection lifetime: 300s

Monitoring:
- Active connections
- Idle connections
- Wait time for connection
```

### Application Performance

#### Caching Strategy
```
L1 Cache (In-Memory):
- Configuration data
- Reference data
- TTL: 5 minutes
- Size limit: 100 MB

L2 Cache (Redis):
- User sessions
- Frequently accessed data
- TTL: 1 hour
- Size limit: 5 GB

L3 Cache (CDN):
- Static assets
- API responses (public data)
- TTL: 24 hours
i Jeszcze hybdyrodwe z najnowszym .netem
```

#### Async Optimization
```
All I/O operations async:
- Database queries
- HTTP calls
- File operations
- Message queue operations

Parallel processing:
- Batch flashcard generation
- Parallel AI evaluations
- Bulk inserts

ConfigureAwait(false):
- All library code
- Non-UI applications
```

### Horizontal Scaling

To raczej zostawimy sobie na koniec
#### Stateless Design
```
Requirements:
- No in-memory session state
- No local file storage
- No singleton mutable state

Implementation:
- Session in Redis
- Files in Blob Storage
- Configuration in App Configuration
```

#### Auto-scaling Configuration
```
Azure App Service:
- Min instances: 2
- Max instances: 10
- Scale out: CPU > 70%
- Scale in: CPU < 30%
- Cooldown: 5 minutes

Azure Functions:
- Max instances: 200
- Scale out: Queue length > 100
- Scale in: Queue length < 10
```

#### Load Balancing
```
Azure Load Balancer:
- Health probe: /health
- Probe interval: 30s
- Unhealthy threshold: 3

Session affinity:
- Disabled (stateless)
- Client affinity via JWT
```

### Performance Targets

#### Latency
```
API endpoints:
- GET /flashcards: p95 < 100ms
- POST /flashcards: p95 < 200ms
- GET /statistics: p95 < 150ms
- POST /evaluate: p95 < 500ms (async)

Database queries:
- Simple queries: < 10ms
- Complex queries: < 50ms
- Batch operations: < 500ms
```
Do tego będziemy potrzebowali testów
#### Throughput
```
Read operations:
- 5,000 req/sec (cached)
- 1,000 req/sec (uncached)

Write operations:
- 500 req/sec (synchronous)
- 5,000 req/sec (asynchronous)

Background processing:
- 10,000 messages/sec
- 1,000 AI evaluations/sec
```

#### Resource Limits
```
CPU:
- App Service: 70% max sustained
- Functions: 80% max per instance

Memory:
- App Service: 80% max
- Redis: 70% max
- SQL: 80% max

Connections:
- Database: 100 max per instance
- Redis: 50 max per instance
- HTTP: 100 max per instance
```

---

## 6. Containerization i Kubernetes

### Opis implementacji
Konteneryzacja aplikacji i wdrożenie na Azure Kubernetes Service (AKS) dla zaawansowanego orkiestracji i skalowania.

### Docker Configuration

#### Multi-stage Dockerfile
```
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Security
RUN adduser --disabled-password --gecos '' appuser
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s \
  CMD curl -f http://localhost:5000/health || exit 1

EXPOSE 5000
ENTRYPOINT ["dotnet", "Web.Api.dll"]
```

#### Docker Compose (Development)
```
Services:
├── api (port 5000)
├── sqlserver (port 1433)
├── redis (port 6379)
├── rabbitmq (ports 5672, 15672)
├── seq (port 5341)
└── jaeger (ports 5775, 16686)
Tutaj pogadamy sobie o tym czemu te nasze kontenery beda w sieci i po co im volumeny
Networks:
- flashcards-network (bridge)

Volumes:
- sqlserver-data
- redis-data
- rabbitmq-data
```

### Kubernetes Architecture

#### Namespace Structure
```
Namespaces:
├── flashcards-production
├── flashcards-staging
├── flashcards-monitoring
└── flashcards-infrastructure
```

#### Deployments
```
api-deployment
├── Replicas: 3 (min), 10 (max)
├── Image: flashcardsacr.azurecr.io/api:v1.0
├── Resources:
│   ├── Requests: 256Mi memory, 250m CPU
│   └── Limits: 512Mi memory, 500m CPU
├── Probes:
│   ├── Liveness: /health
│   └── Readiness: /health/ready
└── Env from: ConfigMap, Secrets

background-worker-deployment
├── Replicas: 2 (min), 20 (max)
├── Image: flashcardsacr.azurecr.io/worker:v1.0
├── Resources:
│   ├── Requests: 512Mi memory, 500m CPU
│   └── Limits: 1Gi memory, 1000m CPU
└── HPA: Queue length > 100
```

#### Services
```
api-service
├── Type: LoadBalancer
├── Port: 80 → 5000
└── Annotations: Azure Load Balancer

redis-service
├── Type: ClusterIP
├── Port: 6379
└── Headless for StatefulSet

sqlserver-service
├── Type: ClusterIP
├── Port: 1433
└── Azure SQL Managed Instance
```

#### Ingress
```
api-ingress
├── Host: api.flashcards.com
├── TLS: Let's Encrypt (cert-manager)
├── Annotations:
│   ├── nginx.ingress.kubernetes.io/ssl-redirect: "true"
│   ├── nginx.ingress.kubernetes.io/rate-limit: "100"
│   └── nginx.ingress.kubernetes.io/cors-allow-origin: "*"
└── Paths:
    ├── / → api-service:80
    ├── /signalr → api-service:80 (websocket)
    └── /metrics → api-service:9090
```

#### Horizontal Pod Autoscaler
```
api-hpa
├── Min replicas: 3
├── Max replicas: 10
├── Metrics:
│   ├── CPU: target 70%
│   └── Memory: target 80%
└── Behavior:
    ├── Scale up: 100% per 60s
    └── Scale down: 50% per 120s

worker-hpa
├── Min replicas: 2
├── Max replicas: 20
├── Metrics:
│   ├── Queue length: target 100
│   └── CPU: target 80%
└── Custom metric: KEDA scaler
```

#### Monitoring Stack
```
Prometheus (kube-prometheus-stack)
├── Prometheus server
├── Alertmanager
├── Grafana
└── Node exporter

Grafana dashboards:
├── API performance
├── Kubernetes cluster
├── Redis metrics
├── SQL metrics
└── Custom business metrics
```

---

## 7. Observability i Monitoring

### Opis implementacji
Pełna obserwowalność aplikacji: structured logging, metrics, distributed tracing. Umożliwia szybkie diagnozowanie problemów.

### Logging Stack

#### Serilog Configuration
```
Sinks:
├── Console (development)
├── Azure Application Insights (production)
├── Seq (local development)
└── Elasticsearch (log aggregation)

Enrichers:
├── CorrelationId
├── UserId
├── TenantId (languageAccountId)
├── Environment
├── MachineName
└── ThreadId

Minimum levels:
├── Default: Information
├── Microsoft: Warning
├── System: Warning
└── Flashcards.*: Debug
```

#### Structured Log Events
```
User actions:
- UserLoggedIn (userId, email, ip)
- UserRegistered (userId, email, source)
- UserAction (userId, action, resourceId)

Business events:
- FlashcardCreated (flashcardId, userId, languageAccountId)
- TokensConsumed (userId, amount, operation)
- AiEvaluationCompleted (evaluationId, duration, model)

Performance events:
- SlowQuery (query, duration, parameters)
- CacheHit (key, ttl)
- CacheMiss (key, reason)

Error events:
- ExceptionOccurred (exception, stackTrace, context)
- ExternalServiceError (service, error, retryCount)
- ValidationFailed (request, errors)
```

### Metrics Stack

#### Prometheus Metrics
```
Counters:
- flashcards_created_total
- flashcards_reviewed_total
- tokens_consumed_total
- api_requests_total (by endpoint, method, status)
- errors_total (by type, endpoint)

Gauges:
- active_users_current
- flashcards_due_current
- token_balance (by user)
- queue_length (by queue)

Histograms:
- http_request_duration_seconds (by endpoint)
- flashcard_generation_duration_seconds
- ai_evaluation_duration_seconds
- database_query_duration_seconds

Summaries:
- request_size_bytes
- response_size_bytes
```

#### Custom Business Metrics
```
Learning metrics:
- daily_flashcards_reviewed
- weekly_study_time_minutes
- vocabulary_mastery_distribution
- srs_interval_distribution

Business metrics:
- daily_active_users
- monthly_recurring_revenue
- token_usage_by_operation
- ai_cost_by_operation
```

### Distributed Tracing
Tutaj Ci opowiem jakie problemy produkcyjne to rozwiazuje
#### OpenTelemetry Configuration
```
Instrumentation:
├── ASP.NET Core
├── HTTP client
├── SQL client
├── Redis client
└── MassTransit

Exporters:
├── Azure Monitor (production)
├── Jaeger (development)
└── OTLP (custom backend)

Sampling:
├── Production: 10% of requests
├── Errors: 100%
├── Slow requests: 100%
└── Business critical: 100%
```

#### Trace Correlation
```
Correlation flow:
1. API receives request with traceparent header
2. Extract trace context
3. Create new span for request
4. Propagate context to:
   - Database queries
   - HTTP calls
   - Message publishing
   - Redis operations
5. Export trace with all spans

Custom spans:
- FlashcardGeneration.GenerateBatch
- AiEvaluation.EvaluateSentence
- TokenManagement.ConsumeTokens
- SrsAlgorithm.CalculateNextReview
```

### Alerting

#### Azure Monitor Alerts
```
Availability:
- API health check fails > 3 times (5 min)
- Error rate > 1% (5 min)
- P95 latency > 500ms (5 min)

Resources:
- CPU > 80% (10 min)
- Memory > 85% (10 min)
- Disk > 90% (10 min)
- Database connections > 80% (5 min)

Business:
- Daily active users < threshold
- Token consumption anomaly
- AI cost spike > 200%

Actions:
- Email: operations team
- SMS: critical alerts
- Teams/Slack: all alerts
- Webhook: PagerDuty
```

#### Grafana Alerts
```
Alert rules:
├── HighErrorRate (error_rate > 0.01)
├── SlowResponseTime (p95 > 0.5s)
├── QueueBacklog (queue_length > 1000)
├── RedisMemoryHigh (memory > 80%)
└── DatabaseConnectionsLow (available < 20)

Notification channels:
├── Email
├── Slack
├── PagerDuty
└── Webhook
```

### Dashboards

#### Application Dashboard
```
Panels:
├── Request rate (req/s)
├── Error rate (%)
├── P50, P95, P99 latency
├── Active users
├── Flashcards created/reviewed
├── Tokens consumed
└── AI operations
```

#### Infrastructure Dashboard
```
Panels:
├── CPU usage (by pod)
├── Memory usage (by pod)
├── Network I/O
├── Disk I/O
├── Database connections
├── Redis memory/connections
└── Queue lengths
```

#### Business Dashboard
```
Panels:
├── Daily active users
├── Monthly active users
├── Flashcard completion rate
├── Vocabulary mastery progress
├── Token usage trends
├── AI cost breakdown
└── Revenue metrics
```

---

## 8. Security i Authentication

### Opis implementacji
Enterprise-grade security z OAuth 2.0, OpenID Connect, i comprehensive authorization. Zgodność z OWASP Top 10.

### Authentication Architecture

#### OAuth 2.0 / OpenID Connect
```
Identity Provider: Azure AD B2C
├── Sign-in flows:
│   ├── Email/password
│   ├── Social logins (Google, Facebook)
│   └── Custom policies
├── User flows:
│   ├── Sign-up
│   ├── Sign-in
│   ├── Password reset
│   └── Profile edit
└── Custom claims:
    ├── subscription_tier
    ├── token_balance
    └── preferred_language
```

#### JWT Token Structure
```
Header:
{
  "alg": "RS256",
  "typ": "JWT",
  "kid": "key-id"
}

Payload:
{
  "sub": "user-guid",
  "email": "user@example.com",
  "given_name": "John",
  "family_name": "Doe",
  "roles": ["User", "Premium"],
  "subscription_tier": "Premium",
  "token_balance": 500,
  "exp": 1234567890,
  "iat": 1234567890,
  "aud": "flashcards-api",
  "iss": "https://flashcards.b2clogin.com"
}
```

#### Token Lifecycle
```
Access token:
- Lifetime: 1 hour
- Refresh: Not allowed
- Revocation: On password change

Refresh token:
- Lifetime: 30 days
- Rotation: On every use
- Revocation: On logout, password change

ID token:
- Lifetime: 1 hour
- Used for: User info
- Validation: Same as access token
```

### Authorization Implementation

#### Role-Based Access Control (RBAC)
```
Roles:
├── User (default)
│   ├── Create flashcards
│   ├── Review flashcards
│   ├── Create sentences
│   └── View own profile
├── Premium
│   ├── All User permissions
│   ├── AI generation (unlimited)
│   ├── Advanced analytics
│   └── Priority support
└── Admin
    ├── All permissions
    ├── Manage users
    ├── Grant tokens
    └── View audit logs
```

#### Policy-Based Authorization
```
Policies:
├── CanGenerateFlashcards
│   └── Requirement: TokenBalance >= 10
├── CanAccessPremiumFeatures
│   └── Requirement: SubscriptionTier == Premium
├── CanManageUsers
│   └── Requirement: Role == Admin
├── CanAccessLanguageAccount
│   └── Requirement: Resource.UserId == CurrentUser.Id
└── CanConsumeTokens
    └── Requirement: TokenBalance >= requested amount
```

#### Resource-Based Authorization
```
Implementation:
- IAuthorizationService injection
- Custom authorization handlers
- Resource-based checks in controllers

Example:
[HttpGet("{id}")]
public async Task<ActionResult<LanguageAccount>> Get(Guid id)
{
    var account = await _repository.GetByIdAsync(id);
    
    var result = await _authorizationService.AuthorizeAsync(
        User, account, "CanAccessLanguageAccount");
    
    if (!result.Succeeded)
        return Forbid();
        
    return Ok(account);
}
```

### Security Measures

#### Input Validation
```
FluentValidation rules:
├── Email: valid format, not disposable domain
├── Password: min 8 chars, uppercase, lowercase, number, special
├── Flashcard content: max 500 chars, no HTML/JS
├── Sentence: max 1000 chars, language validation
└── All inputs: XSS sanitization, SQL injection prevention
```

#### Rate Limiting
```
Endpoints limits:
├── Authentication: 10 req/min
├── API general: 100 req/min
├── AI operations: 20 req/min
├── Flashcard generation: 5 req/hour
└── Admin operations: 30 req/min

Implementation:
- AspNetCoreRateLimit package
- Redis-backed distributed limits
- Sliding window algorithm
```

#### Security Headers
```
HTTP headers:
├── Strict-Transport-Security: max-age=31536000; includeSubDomains
├── X-Content-Type-Options: nosniff
├── X-Frame-Options: DENY
├── X-XSS-Protection: 1; mode=block
├── Content-Security-Policy: default-src 'self'
├── Referrer-Policy: strict-origin-when-cross-origin
└── Permissions-Policy: geolocation=(), microphone=()
```

#### CORS Policy
```
Allowed origins:
├── https://flashcards.com
├── https://app.flashcards.com
└── https://admin.flashcards.com

Allowed methods:
├── GET
├── POST
├── PUT
├── DELETE
└── OPTIONS

Allowed headers:
├── Authorization
├── Content-Type
├── X-Correlation-ID
└── X-Request-ID

Credentials: Allowed
Max age: 3600
```

### Data Protection

#### Encryption at Rest
```
Azure SQL: Transparent Data Encryption (TDE)
Azure Blob: Server-side encryption
Azure Redis: Encryption at rest
Azure Service Bus: Encryption at rest
Key Vault: HSM-backed encryption
```

#### Encryption in Transit
```
TLS 1.3: All connections
HTTPS: Enforced
Certificate: Managed by Azure
mTLS: Service-to-service (optional)
```

#### Sensitive Data Handling
```
PII fields:
├── Email: Encrypted in database
├── Name: Encrypted in database
├── IP address: Hashed in logs
└── Password: Hashed (never stored plain)

Implementation:
- Azure Key Vault encryption
- Data classification tags
- Access logging
```

---

## 9. Advanced Testing Strategies

### Opis implementacji
Kompleksowa strategia testowania: unit, integration, architecture, performance, security. Automatyzacja i CI/CD integration.

### Testing Pyramid

#### Unit Tests (70%)
```
Coverage target: 80%

Domain layer:
├── Entity behavior tests
├── Value object equality tests
├── Domain event tests
├── Invariant validation tests
└── Business rule tests

Application layer:
├── Command handler tests
├── Query handler tests
├── Validator tests
├── Mapper tests
└── Domain service tests

Tools:
├── xUnit / NUnit
├── FluentAssertions
├── Moq / NSubstitute
├── AutoFixture
└── Shouldly
```

#### Integration Tests (20%)
```
Coverage: API endpoints, database, external services

API integration:
├── Authentication flow
├── Authorization policies
├── CRUD operations
├── Error handling
└── Rate limiting

Database integration:
├── Repository operations
├── Transaction handling
├── Concurrency conflicts
├── Query performance
└── Migration tests

External services:
├── AI service integration
├── Email service integration
├── Payment integration
└── Message queue integration

Tools:
├── TestContainers
├── WebApplicationFactory
├── Respawn (database reset)
└── WireMock (external services)
```

#### E2E Tests (10%)
```
Critical user journeys:
├── User registration → verification → login
├── Create language account → add words → generate flashcards
├── Review flashcards → track progress
├── Create sentence → AI evaluation → create flashcard
└── Purchase tokens → verify balance

Tools:
├── Playwright
├── Cypress
└── SpecFlow (BDD)
```

### Architecture Tests

#### Dependency Rules
```
Domain layer:
├── No dependencies on Application
├── No dependencies on Infrastructure
├── No dependencies on API
└── No external package dependencies

Application layer:
├── Can depend on Domain
├── No dependencies on Infrastructure
├── No dependencies on API
└── Only MediatR, FluentValidation

Infrastructure layer:
├── Can depend on Domain
├── Can depend on Application (interfaces)
├── No dependencies on API
└── EF Core, Redis, external services

API layer:
├── Can depend on Application
├── No dependencies on Infrastructure
├── No dependencies on Domain
└── ASP.NET Core, Swagger
```

#### Naming Conventions
```
Entities: {Name} (e.g., User, Flashcard)
Value Objects: {Name} (e.g., Email, TokenAmount)
Domain Events: {Name}{PastTense} (e.g., UserRegistered)
Commands: {Verb}{Noun}Command (e.g., CreateFlashcardCommand)
Queries: Get{Noun}Query (e.g., GetFlashcardQuery)
Handlers: {Command/Query}Handler
Repositories: I{Entity}Repository
Services: I{Noun}Service
```

#### Design Pattern Tests
```
Aggregate roots:
├── Have public constructors
├── Raise domain events
├── Protect invariants
└── Control child entity access

Repositories:
├── Only for aggregate roots
├── Async methods
├── GetById, Add, Update, Delete
└── No business logic

Domain services:
├── Stateless
├── No persistence
├── Pure business logic
└── Single responsibility
```

### Performance Testing

#### Load Testing
```
Scenarios:
├── Normal load: 1000 users, 10 req/s
├── Peak load: 5000 users, 50 req/s
├── Stress test: 10000 users, 100 req/s
└── Soak test: 1000 users, 10 req/s, 24h

Metrics:
├── Response time (p50, p95, p99)
├── Throughput (req/s)
├── Error rate (%)
├── Resource utilization
└── Database connections

Tools:
├── k6
├── JMeter
├── Locust
└── Azure Load Testing
```

#### Benchmarking
```
Critical paths:
├── Flashcard retrieval (BenchmarkDotNet)
├── SRS calculation
├── Token consumption
├── AI evaluation
└── Cache hit/miss scenarios

Memory profiling:
├── Allocation patterns
├── GC pressure
├── Large object heap
└── Memory leaks

CPU profiling:
├── Hot paths
├── Lock contention
├── Thread pool starvation
└── Async state machine overhead
```

### Security Testing

#### SAST (Static Analysis)
```
Tools:
├── SonarQube
├── Security Code Scan
├── Roslyn Analyzers
└── OWASP Dependency Check

Checks:
├── SQL injection
├── XSS
├── CSRF
├── Hardcoded secrets
├── Weak cryptography
└── Insecure configurations
```

#### DAST (Dynamic Analysis)
```
Tools:
├── OWASP ZAP
├── Burp Suite
└── Azure Security Center

Checks:
├── Authentication bypass
├── Authorization flaws
├── Session management
├── Input validation
├── API security
└── Rate limiting effectiveness
```

#### Penetration Testing
```
Scope:
├── Authentication mechanisms
├── Authorization bypass
├── Token handling
├── Rate limiting
├── Input validation
└── API endpoints

Frequency:
├── Before major releases
├── After security updates
├── Quarterly (production)
└── After incident response
```

### Test Automation

#### CI/CD Integration
```
Pipeline stages:
├── Build → Run unit tests
├── Code coverage check (> 80%)
├── Architecture tests
├── Security scans
├── Integration tests (TestContainers)
├── Performance tests (smoke)
└── E2E tests (critical paths)

Quality gates:
├── Unit tests: must pass
├── Coverage: > 80%
├── Architecture: must pass
├── Security: no critical/high
├── Integration: must pass
└── Performance: p95 < 500ms
```

#### Test Data Management
```
Strategies:
├── Test data builders (builder pattern)
├── AutoFixture customization
├── Database snapshots (integration)
├── Data anonymization (production copies)
└── Seed data management

Cleanup:
├── Transaction rollback (integration)
├── Database truncation (E2E)
├── Isolated databases per test run
└── Parallel execution support
```

---

## 10. CI/CD i Infrastructure as Code

### Opis implementacji
W pełni zautomatyzowany pipeline CI/CD z Infrastructure as Code. Zero-downtime deployments i disaster recovery.
Tutaj się zastanawiam bo taką twarda infrastrukturą tylko okazjonalnie się zajmowałem.
### GitHub Actions Workflows

#### CI Workflow
```
Triggers:
├── Push to main/develop
└── Pull request to main

Jobs:
├── Build
│   ├── Restore dependencies
│   ├── Build solution
│   └── Publish artifacts
├── Test
│   ├── Unit tests
│   ├── Integration tests
│   ├── Architecture tests
│   └── Code coverage
├── Security
│   ├── SAST scan
│   ├── Dependency check
│   └── Secret scan
└── Quality
    ├── SonarQube analysis
    ├── Code formatting check
    └── Documentation check
```

#### CD Workflow (Staging)
```
Triggers:
└── Push to main

Jobs:
├── Build Docker image
│   ├── Build multi-arch image
│   ├── Scan for vulnerabilities
│   └── Push to ACR
├── Deploy infrastructure
│   ├── Validate ARM templates
│   ├── Deploy to staging RG
│   └── Apply configurations
├── Deploy application
│   ├── Deploy to staging slot
│   ├── Run smoke tests
│   └── Swap to production slot
└── Notify
    ├── Teams/Slack notification
    └── Update deployment dashboard
```

#### CD Workflow (Production)
```
Triggers:
└── Manual approval after staging

Jobs:
├── Approval gate
│   └── Require 2 approvers
├── Pre-deployment checks
│   ├── Staging health check
│   ├── Database migration preview
│   └── Rollback plan verification
├── Deploy infrastructure
│   ├── Blue-green deployment
│   ├── Database migrations
│   └── Configuration update
├── Deploy application
│   ├── Canary deployment (10%)
│   ├── Monitor for 10 minutes
│   ├── Full rollout (100%)
│   └── Rollback on failure
└── Post-deployment
    ├── Smoke tests
    ├── Performance baseline
    └── Documentation update
```

### Infrastructure as Code

#### Bicep Templates
```
Structure:
├── main.bicep (orchestrator)
├── modules/
│   ├── app-service.bicep
│   ├── sql-database.bicep
│   ├── cosmos-db.bicep
│   ├── redis-cache.bicep
│   ├── service-bus.bicep
│   ├── key-vault.bicep
│   └── monitoring.bicep
└── parameters/
    ├── dev.bicepparam
    ├── staging.bicepparam
    └── prod.bicepparam

Features:
├── Modular design
├── Parameter validation
├── Output references
├── Conditional deployment
└── Resource dependencies
```

#### ARM Template Example
```
Resource types:
├── Microsoft.Web/sites (App Service)
├── Microsoft.Sql/servers (SQL)
├── Microsoft.DocumentDB/databaseAccounts (Cosmos)
├── Microsoft.Cache/redis (Redis)
├── Microsoft.ServiceBus/namespaces (Service Bus)
├── Microsoft.KeyVault/vaults (Key Vault)
├── Microsoft.Insights/components (App Insights)
└── Microsoft.Network/dnsZones (DNS)

Configurations:
├── SKU selection by environment
├── Auto-scaling rules
├── Alert rules
├── Diagnostic settings
└── RBAC assignments
```

### Deployment Strategies

#### Blue-Green Deployment
```
Implementation:
├── Staging slot (blue)
├── Production slot (green)
├── Swap with preview
├── Automatic rollback
└── Traffic routing

Process:
1. Deploy to staging slot
2. Run smoke tests
3. Swap slots with preview
4. Verify in production
5. Complete swap or rollback
```

#### Canary Deployment
```
Implementation:
├── Traffic splitting (Azure Front Door)
├── Gradual rollout: 10% → 25% → 50% → 100%
├── Automated rollback on errors
└── A/B testing capability

Monitoring:
├── Error rate comparison
├── Latency comparison
├── Business metrics
└── User feedback
```

#### Feature Flags
```
Implementation:
├── Azure App Configuration
├── Feature filters:
│   ├── Percentage
│   ├── Targeting (users/groups)
│   ├── Time window
│   └── Custom
└── Dynamic configuration

Use cases:
├── Gradual feature rollout
├── A/B testing
├── Kill switches
└── Beta features
```

### Database Deployment

#### EF Core Migrations
```
Strategy:
├── Migrations in code repository
├── Automatic generation
├── Manual review before deployment
├── Idempotent scripts
└── Rollback scripts

Process:
1. Generate migration
2. Review SQL script
3. Test on local database
4. Apply to staging
5. Verify data integrity
6. Apply to production
7. Monitor for issues
```

#### Zero-Downtime Migrations
```
Principles:
├── Additive changes first
├── Backward compatible
├── Two-phase deployment
└── Cleanup after migration

Example:
Phase 1: Add new column (nullable)
Phase 2: Deploy code that uses new column
Phase 3: Populate column with data
Phase 4: Make column required
Phase 5: Remove old column
```

### Disaster Recovery

#### Backup Strategy
```
Database:
├── Azure SQL: Point-in-time restore (35 days)
├── Cosmos DB: Continuous backup (30 days)
├── Redis: AOF persistence + snapshots
└── Blob Storage: Geo-redundant (GRS)

Application:
├── Container images: Geo-replicated ACR
├── Configuration: Azure App Configuration
├── Secrets: Key Vault with soft-delete
└── Infrastructure: Bicep templates in Git
```

#### Recovery Procedures
```
RTO (Recovery Time Objective): 1 hour
RPO (Recovery Point Objective): 5 minutes

Scenarios:
├── Database failure: Failover to secondary
├── Region failure: Traffic to secondary region
├── Data corruption: Point-in-time restore
├── Security breach: Key rotation, credential reset
└── Code rollback: Previous container image

Automation:
├── Azure Site Recovery
├── Traffic Manager failover
├── Automated health checks
└── Runbook execution
```

### Monitoring & Alerting

#### Deployment Monitoring
```
Metrics:
├── Deployment duration
├── Rollback frequency
├── Success rate
├── Error rate post-deployment
└── Performance comparison

Alerts:
├── Deployment failed
├── Rollback triggered
├── Error rate spike
├── Latency degradation
└── Resource exhaustion
```

#### Cost Management
```
Budgets:
├── Development: $100/month
├── Staging: $300/month
├── Production: $2000/month
└── Alert at 80% of budget

Optimization:
├── Right-sizing recommendations
├── Unused resource detection
├── Reserved instances
├── Spot instances (workers)
└── Auto-scaling policies
```

---

## Podsumowanie - Roadmapa Implementacji

### Faza 1: Event Sourcing (Miesiąc 1-2)
- Implementacja Event Store
- Migracja User i TokenBalance aggregates
- Projections dla read models
- Snapshotting

### Faza 2: Azure Infrastructure (Miesiąc 3-4)
- Migracja do Azure SQL, Cosmos DB
- Azure Functions dla background processing
- Key Vault dla secrets
- Managed Identity

### Faza 3: Distributed Cache (Miesiąc 5)
- Redis Cache implementation
- Cache-aside pattern
- Distributed locking
- Session management

### Faza 4: Async Processing (Miesiąc 6)
- Azure Service Bus setup
- Message handlers
- Saga implementation
- Error handling

### Faza 5: Kubernetes (Miesiąc 7-8)
- AKS cluster setup
- Docker containerization
- Kubernetes manifests
- Auto-scaling

### Faza 6: Observability (Miesiąc 9)
- Structured logging
- Prometheus metrics
- Distributed tracing
- Dashboards

### Faza 7: Security (Miesiąc 10)
- Azure AD B2C integration
- Policy-based authorization
- Security hardening
- Penetration testing

### Faza 8: Testing & CI/CD (Miesiąc 11-12)
- Architecture tests
- Performance testing
- CI/CD pipelines
- Infrastructure as Code

---

## Szacowany koszt infrastruktury

### Development
- App Service Basic: $13
- SQL Basic: $5
- Cosmos DB Free: $0
- Redis Basic: $15
- Service Bus Standard: $10
- Storage: $5
- **Total: ~$50/month**

### Staging
- App Service Standard: $73
- SQL Standard S2: $75
- Cosmos DB 400 RU: $25
- Redis Standard: $40
- Service Bus Standard: $10
- Storage GRS: $10
- **Total: ~$250/month**

### Production
- App Service Premium V3: $180
- SQL Business Critical: $370
- Cosmos DB 1000 RU: $60
- Redis Premium: $100
- Service Bus Premium: $600
- Storage GZRS + CDN: $50
- Key Vault: $5
- App Insights: $30
- **Total: ~$1500/month**

---


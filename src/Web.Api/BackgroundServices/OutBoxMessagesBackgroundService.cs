using System.Collections.Concurrent;
using System.Text.Json;
using Application;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Storage;
using SharedKernel;

namespace Web.Api.BackgroundServices;


public class OutBoxMessagesBackgroundService : BackgroundService
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeDictionary = new();
    private static readonly ConcurrentDictionary<Type, Type> WrapperTypeDictionary = new();
    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IServiceProvider _serviceProvider;
    private const int MaxRetryCount = 5;

    public OutBoxMessagesBackgroundService(IServiceProvider serviceProvider, IDateTimeProvider dateTimeProvider)
    {
        _serviceProvider = serviceProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();

            // Use concrete ApplicationDbContext so we have access to OutboxMessageConsumers
            // (an infrastructure detail not exposed on IApplicationDbContext).
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            IEnumerable<OutboxMessage> outboxMessages = dbContext.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null && m.RetryCount < MaxRetryCount)
                .ToList();

            foreach (var outboxMessage in outboxMessages)
            {
                // ── Type resolution ───────────────────────────────────────────────
                Type? domainEventType = Type.GetType(outboxMessage.Type);
                if (domainEventType is null)
                {
                    outboxMessage.Error = $"Failed to get domain event type {outboxMessage.Type}";
                    outboxMessage.RetryCount += 1;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    continue;
                }

                IDomainEvent? domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(outboxMessage.Content, domainEventType);
                if (domainEvent is null)
                {
                    outboxMessage.Error = $"Failed to deserialize domain event of type {outboxMessage.Type}";
                    outboxMessage.RetryCount += 1;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    continue;
                }

                // ── Handler resolution ────────────────────────────────────────────
                Type handlerType = HandlerTypeDictionary.GetOrAdd(
                    domainEventType,
                    et => typeof(IDomainEventHandler<>).MakeGenericType(et));

                IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(handlerType);
                bool allHandlersSucceeded = true;

                foreach (object? handler in handlers)
                {
                    if (handler is null) continue;

                    string handlerTypeName = handler.GetType().FullName!;

                    // ── Idempotency check ─────────────────────────────────────────
                    bool alreadyProcessed = dbContext.OutboxMessageConsumers
                        .Any(c => c.OutboxMessageId == outboxMessage.Id && c.HandlerType == handlerTypeName);

                    if (alreadyProcessed) continue;

                    var handlerWrapper = HandlerWrapper.Create(handler, domainEventType);

                    // ── Explicit transaction per handler ──────────────────────────
                    // Guarantees that OutboxMessageConsumer and the handler's side effects
                    // are saved atomically. If SaveChanges fails after the handler succeeds,
                    // the transaction rolls back so the message is retried cleanly with no
                    // duplicate consumer record.
                    IDbContextTransaction transaction = await dbContext.BeginTransactionAsync(stoppingToken);
                    try
                    {
                        await handlerWrapper.Handle(domainEvent, stoppingToken);

                        dbContext.OutboxMessageConsumers.Add(new OutboxMessageConsumer
                        {
                            OutboxMessageId = outboxMessage.Id,
                            HandlerType = handlerTypeName,
                            ProcessedOnUtc = _dateTimeProvider.UtcNow
                        });

                        await dbContext.SaveChangesAsync(stoppingToken);
                        await transaction.CommitAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(stoppingToken);

                        allHandlersSucceeded = false;
                        outboxMessage.Error = $"Failed to process domain event of type {outboxMessage.Type}. Error: {ex.Message}";
                    }
                    finally
                    {
                        await transaction.DisposeAsync();
                    }
                }

                // ── Mark message processed / increment retry outside the per-handler tx ──
                if (allHandlersSucceeded)
                {
                    outboxMessage.ProcessedOnUtc = _dateTimeProvider.UtcNow;
                }
                else
                {
                    outboxMessage.RetryCount += 1;
                }
                await dbContext.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }


    private abstract class HandlerWrapper
    {
        public abstract Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken);

        public static HandlerWrapper Create(object handler, Type domainEventType)
        {
            Type wrapperType = WrapperTypeDictionary.GetOrAdd(
                domainEventType,
                et => typeof(HandlerWrapper<>).MakeGenericType(et));

            return (HandlerWrapper)Activator.CreateInstance(wrapperType, handler);
        }
    }

    private sealed class HandlerWrapper<T>(object handler) : HandlerWrapper where T : IDomainEvent
    {
        private readonly IDomainEventHandler<T> _handler = (IDomainEventHandler<T>)handler;

        public override async Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await _handler.Handle((T)domainEvent, cancellationToken);
        }
    }
}

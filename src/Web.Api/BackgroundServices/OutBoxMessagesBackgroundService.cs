using System.Collections.Concurrent;
using System.Text.Json;
using Application;
using Application.Abstractions.Data;
using Infrastructure.Database;
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
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            IEnumerable<OutboxMessage> outboxMessages = dbContext.OutboxMessages.Where(m => m.ProcessedOnUtc == null && m.RetryCount < MaxRetryCount).ToList();
            foreach (var outboxMessage in outboxMessages)
            {

                Type domainEventType = Type.GetType(outboxMessage.Type);

                if (domainEventType is null)
                {
                    outboxMessage.Error = $"Failed to get domain event type {outboxMessage.Type}";
                    outboxMessage.RetryCount += 1;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    continue;
                }
                IDomainEvent domainEvent = (IDomainEvent)JsonSerializer.Deserialize(outboxMessage.Content, domainEventType);

                if (domainEvent is null)
                {
                    outboxMessage.Error = $"Failed to deserialize domain event of type {outboxMessage.Type}";
                    outboxMessage.RetryCount += 1;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    continue;
                }
             

                Type handlerType = HandlerTypeDictionary.GetOrAdd(
                    domainEventType,
                    et => typeof(IDomainEventHandler<>).MakeGenericType(et));


                IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(handlerType);
                bool isSucceded = true;
                foreach (object? handler in handlers)
                {
                    if (handler is null)
                    {
                        continue;
                    }

                    string handlerTypeName = handler.GetType().FullName!;

                    bool alreadyProcessed = dbContext.OutboxMessageConsumers
                        .Any(c => c.OutboxMessageId == outboxMessage.Id && c.HandlerType == handlerTypeName);

                    if (alreadyProcessed)
                    {
                        continue;
                    }

                    var handlerWrapper = HandlerWrapper.Create(handler, domainEventType);

                    try
                    {
                        await handlerWrapper.Handle(domainEvent, stoppingToken);

                        dbContext.OutboxMessageConsumers.Add(new OutboxMessageConsumer
                        {
                            OutboxMessageId = outboxMessage.Id,
                            HandlerType = handlerTypeName,
                            ProcessedOnUtc = _dateTimeProvider.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        isSucceded = false;
                        outboxMessage.Error = $"Failed to process domain event of type {outboxMessage.Type}. Error: {ex.Message}";

                        continue;
                    }
                }
                if(isSucceded)
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

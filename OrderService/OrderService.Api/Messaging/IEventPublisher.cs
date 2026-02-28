namespace OrderService.Api.Messaging;

public interface IEventPublisher
{
    void Publish(OrderCreatedEvent evt);
}

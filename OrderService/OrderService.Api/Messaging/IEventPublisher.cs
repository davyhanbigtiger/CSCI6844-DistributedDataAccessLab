namespace OrderService.Api.Messaging;

public interface IEventPublisher
{
    void Publish<T>(T evt) where T : class;
}
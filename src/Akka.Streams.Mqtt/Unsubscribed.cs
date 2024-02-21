namespace Akka.Streams.Mqtt;

public record Unsubscribed(bool Success, Exception? Exception = null) : IEvent;

namespace Akka.Streams.Mqtt;

public record Subscribed(bool Success, Exception? Exception = null) : IEvent;

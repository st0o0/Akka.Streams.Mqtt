using Akka.Streams.Dsl;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;

namespace Akka.Streams.Mqtt;

public static class MqttSink
{
    public static Sink<IPublish, NotUsed> From(ManagedMqttClientOptions options, int maxBufferSize = 10)
        => Sink.FromGraph(new ManagedMqttSinkStage(options, maxBufferSize));
}

public record Unsubscribed(bool Success, Exception? Exception = null) : IEvent;
public record Subscribed(bool Success, Exception? Exception = null) : IEvent;
public record Message(MqttApplicationMessageReceivedEventArgs Event) : IEvent;
public record Connected(MqttClientConnectResult Result) : IStateChanged;
public record ConnectionFailed(MqttClientConnectResult Result, Exception Exception) : IStateChanged;
public record Disconnected(MqttClientConnectResult Result, Exception Exception, bool ClientWasConnected, MqttClientDisconnectReason Reason) : IStateChanged;
public interface IEvent : IMessage;
public interface IStateChanged : IMessage;
public interface IMessage;
public interface IOperation;
public interface IPublish : IOperation;

public record Subscribe(MqttTopicFilter filter) : IOperation;
public record Unsubscribe(MqttTopicFilter filter) : IOperation;
public record Publish(MqttApplicationMessage Message) : IPublish;
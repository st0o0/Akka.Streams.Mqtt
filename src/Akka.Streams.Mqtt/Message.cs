using MQTTnet.Client;

namespace Akka.Streams.Mqtt;

public record Message(MqttApplicationMessageReceivedEventArgs Event) : IEvent;

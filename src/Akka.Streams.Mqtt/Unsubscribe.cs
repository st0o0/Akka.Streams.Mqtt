using MQTTnet.Packets;

namespace Akka.Streams.Mqtt;

public record Unsubscribe(MqttTopicFilter filter) : IOperation;

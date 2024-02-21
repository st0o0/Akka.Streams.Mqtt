using MQTTnet.Packets;

namespace Akka.Streams.Mqtt;

public record Subscribe(MqttTopicFilter filter) : IOperation;

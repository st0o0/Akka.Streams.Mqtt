using MQTTnet;

namespace Akka.Streams.Mqtt;

public record Publish(MqttApplicationMessage Message) : IPublish;
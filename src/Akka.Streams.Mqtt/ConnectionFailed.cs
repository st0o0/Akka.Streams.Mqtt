using MQTTnet.Client;

namespace Akka.Streams.Mqtt;

public record ConnectionFailed(MqttClientConnectResult Result, Exception Exception) : IStateChanged;

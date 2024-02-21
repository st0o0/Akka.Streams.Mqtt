using MQTTnet.Client;

namespace Akka.Streams.Mqtt;

public record Connected(MqttClientConnectResult Result) : IStateChanged;

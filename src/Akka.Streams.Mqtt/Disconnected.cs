using MQTTnet.Client;

namespace Akka.Streams.Mqtt;

public record Disconnected(MqttClientConnectResult Result, Exception Exception, bool ClientWasConnected, MqttClientDisconnectReason Reason) : IStateChanged;

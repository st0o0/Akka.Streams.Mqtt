using Akka.Streams.Dsl;
using MQTTnet.Extensions.ManagedClient;

namespace Akka.Streams.Mqtt;

public static class MqttSink
{
    public static Sink<IPublish, NotUsed> From(ManagedMqttClientOptions options, int maxBufferSize = 10)
        => Sink.FromGraph(new ManagedMqttSinkStage(options, maxBufferSize));
}
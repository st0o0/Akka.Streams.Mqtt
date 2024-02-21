using Akka.Streams.Dsl;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;

namespace Akka.Streams.Mqtt;

public static class MqttSource
{
    public static Source<IMessage, NotUsed> FromConfigWithTopic(ManagedMqttClientOptions options, MqttTopicFilter mqttTopicFilter, int maxBufferSize = 10)
        => Source.FromGraph(new ManagedMqttSourceStage(options, [mqttTopicFilter], maxBufferSize));

    public static Source<IMessage, NotUsed> FromConfigWithTopics(ManagedMqttClientOptions options, ICollection<MqttTopicFilter> topics, int maxBufferSize = 10)
        => Source.FromGraph(new ManagedMqttSourceStage(options, topics, maxBufferSize));
}
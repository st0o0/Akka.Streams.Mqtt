using Akka.Streams.Dsl;
using Akka.Streams.Mqtt.Stages;
using MQTTnet.Extensions.ManagedClient;

namespace Akka.Streams.Mqtt;

public static class MqttFlow
{
    public static Flow<IOperation, IMessage, NotUsed> FlexiFlow(ManagedMqttClientOptions options, int maxBufferSize = 10)
        => Flow.FromGraph(new ManagedMqttFlowStage(options, maxBufferSize));
}

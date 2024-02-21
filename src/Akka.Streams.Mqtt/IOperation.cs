using MQTTnet;
using MQTTnet.Packets;

namespace Akka.Streams.Mqtt;

public interface IOperation
{
    public static Publish CreatePublish(Func<MqttApplicationMessageBuilder, MqttApplicationMessage> configure) => new(configure.Invoke(new MqttFactory().CreateApplicationMessageBuilder()));
    public static Subscribe CreateSubscribe(Func<MqttTopicFilterBuilder, MqttTopicFilter> configure) => new(configure.Invoke(new MqttFactory().CreateTopicFilterBuilder()));
    public static Unsubscribe CreateUnsubscribe(Func<MqttTopicFilterBuilder, MqttTopicFilter> configure) => new(configure.Invoke(new MqttFactory().CreateTopicFilterBuilder()));
}

using Akka.Streams.Stage;
using Akka.Streams.Supervision;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace Akka.Streams.Mqtt;

public class ManagedMqttSinkStage : GraphStage<SinkShape<IPublish>>
{
    public ManagedMqttSinkStage(ManagedMqttClientOptions options, int maxBufferSize = 10)
    {
        Inlet = new Inlet<IPublish>("ManagedMqttSinkStage.In");
        Shape = new SinkShape<IPublish>(Inlet);
        Options = options;
        MaxBufferSize = maxBufferSize;
    }

    public Inlet<IPublish> Inlet { get; init; }

    public override SinkShape<IPublish> Shape { get; }

    public ManagedMqttClientOptions Options { get; init; }
    public int MaxBufferSize { get; init; }

    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this, inheritedAttributes);

    sealed class Logic : InGraphStageLogic
    {
        private readonly Decider _decider;
        private readonly ManagedMqttSinkStage _stage;
        private readonly IManagedMqttClient _client;

        public Logic(ManagedMqttSinkStage stage, Attributes attributes) : base(stage.Shape)
        {
            _stage = stage;
            _decider = attributes.GetDeciderOrDefault();
            _client = new MqttFactory().CreateManagedMqttClient();

            SetHandler(stage.Inlet, this);
        }

        public override void PreStart()
        {
            _client.StartAsync(this._stage.Options).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    if (_decider(task.Exception) == Directive.Stop)
                    {
                        FailStage(task.Exception);
                    }
                }
            });

            Pull(_stage.Inlet);
            base.PreStart();
        }

        public override void PostStop()
        {
            _client.StopAsync().ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    if (_decider(task.Exception) == Directive.Stop)
                    {
                        FailStage(task.Exception);
                    }
                }
            });

            base.PostStop();
        }

        public override void OnPush()
        {
            if (_client.PendingApplicationMessagesCount >= this._stage.MaxBufferSize)
            {
                FailStage(new BufferOverflowException($"Max event buffer size {this._stage.MaxBufferSize}"));
            }

            if (Grab(_stage.Inlet) is not Publish item)
            {
                return;
            }

            _client.EnqueueAsync(item.Message).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    if (_decider(task.Exception) == Directive.Stop)
                    {
                        FailStage(task.Exception);
                    }
                }
            });
        }

        public Task ConnectingFailedHandler(ConnectingFailedEventArgs args)
        {
            if (args.ConnectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                FailStage(args.Exception);
            }

            return Task.CompletedTask;
        }
    }
}

using Akka.Streams.Stage;
using Akka.Streams.Supervision;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using Decider = Akka.Streams.Supervision.Decider;

namespace Akka.Streams.Mqtt;

public class ManagedMqttSourceStage : GraphStage<SourceShape<IMessage>>
{
    public ManagedMqttSourceStage(ManagedMqttClientOptions options, ICollection<MqttTopicFilter> topics, int maxBufferSize = 10)
    {
        Outlet = new Outlet<IMessage>("ManagedMqttSourceStage.Out");
        Shape = new SourceShape<IMessage>(Outlet);
        Options = options;
        Topics = topics;
        MaxBufferSize = maxBufferSize;
    }

    public ManagedMqttClientOptions Options { get; init; }
    public ICollection<MqttTopicFilter> Topics { get; init; }
    public int MaxBufferSize { get; init; }

    public Outlet<IMessage> Outlet { get; init; }

    public override SourceShape<IMessage> Shape { get; }

    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this, inheritedAttributes);

    sealed class Logic : OutGraphStageLogic
    {
        public readonly Queue<IMessage> _buffer = new();
        private readonly Decider _decider;
        private readonly ManagedMqttSourceStage _stage;
        private readonly IManagedMqttClient _client;

        public Logic(ManagedMqttSourceStage stage, Attributes attributes) : base(stage.Shape)
        {
            _stage = stage;
            _decider = attributes.GetDeciderOrDefault();
            _client = new MqttFactory().CreateManagedMqttClient();
            SetHandler(stage.Outlet, this);
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

            _client.SubscribeAsync(this._stage.Topics).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    if (_decider(task.Exception) == Directive.Stop)
                    {
                        FailStage(task.Exception);
                    }
                }
            });

            _client.ConnectedAsync += ConnectedHandler;
            _client.DisconnectedAsync += DisconnectedHandler;
            _client.ConnectingFailedAsync += ConnectingFailedHandler;
            _client.ApplicationMessageReceivedAsync += ReceivedMessageHandler;
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

            _client.ConnectedAsync -= ConnectedHandler;
            _client.DisconnectedAsync -= DisconnectedHandler;
            _client.ConnectingFailedAsync -= ConnectingFailedHandler;
            _client.ApplicationMessageReceivedAsync -= ReceivedMessageHandler;
            base.PostStop();
        }

        public override void OnPull()
        {
            if (IsAvailable(_stage.Outlet) && _buffer.Count > 0)
            {
                if (!_buffer.TryDequeue(out var item))
                {
                    return;
                }

                Push(_stage.Outlet, item);
            }
        }

        public override void OnDownstreamFinish(Exception cause)
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

            base.OnDownstreamFinish(cause);
        }

        private Task ReceivedMessageHandler(MqttApplicationMessageReceivedEventArgs args)
        {
            var item = new Message(args);
            if (IsAvailable(_stage.Outlet))
            {
                Push(_stage.Outlet, item);
            }
            else
            {
                _buffer.Enqueue(item);
                if (this._buffer.Count >= this._stage.MaxBufferSize)
                {
                    FailStage(new BufferOverflowException($"Max event buffer size {this._stage.MaxBufferSize}"));
                }
            }

            return Task.CompletedTask;
        }

        private Task ConnectedHandler(MqttClientConnectedEventArgs args)
        {
            _buffer.Enqueue(new Connected(args.ConnectResult));
            return Task.CompletedTask;
        }

        private Task DisconnectedHandler(MqttClientDisconnectedEventArgs args)
        {
            _buffer.Enqueue(new Disconnected(args.ConnectResult, args.Exception, args.ClientWasConnected, args.Reason));
            return Task.CompletedTask;
        }

        private Task ConnectingFailedHandler(ConnectingFailedEventArgs args)
        {
            _buffer.Enqueue(new ConnectionFailed(args.ConnectResult, args.Exception));
            return Task.CompletedTask;
        }
    }
}

public static class AttributesExtensions
{
    public static Decider GetDeciderOrDefault(this Attributes attributes)
    {
        var attr = attributes.GetAttribute<ActorAttributes.SupervisionStrategy>(null!);
        return attr != null ? attr.Decider : Deciders.StoppingDecider;
    }
}

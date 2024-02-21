using Akka.Streams.Stage;
using Akka.Streams.Supervision;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace Akka.Streams.Mqtt.Stages;

public partial class ManagedMqttFlowStage : GraphStage<FlowShape<IOperation, IMessage>>
{
    public ManagedMqttFlowStage(ManagedMqttClientOptions options, int maxBufferSize = 10)
    {
        Inlet = new Inlet<IOperation>("ManagedMqttFlowStage.In");
        Outlet = new Outlet<IMessage>("ManagedMqttFlowStage.Out");
        Shape = new FlowShape<IOperation, IMessage>(Inlet, Outlet);
        Options = options;
        MaxBufferSize = maxBufferSize;
    }

    public Inlet<IOperation> Inlet { get; }
    public Outlet<IMessage> Outlet { get; }

    public override FlowShape<IOperation, IMessage> Shape { get; }

    public ManagedMqttClientOptions Options { get; init; }
    public int MaxBufferSize { get; init; }

    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this, inheritedAttributes);

    sealed class Logic : InAndOutGraphStageLogic
    {
        private readonly Queue<IMessage> _buffer = new();
        private readonly Decider _decider;
        private readonly ManagedMqttFlowStage _stage;
        private readonly IManagedMqttClient _client;

        public Logic(ManagedMqttFlowStage stage, Attributes attributes) : base(stage.Shape)
        {
            _stage = stage;
            _decider = attributes.GetDeciderOrDefault();
            _client = new MqttFactory().CreateManagedMqttClient();

            SetHandler(stage.Inlet, this);
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

            _client.ConnectedAsync += ConnectedHandler;
            _client.DisconnectedAsync += DisconnectedHandler;
            _client.ConnectingFailedAsync += ConnectingFailedHandler;
            _client.ApplicationMessageReceivedAsync += ReceivedMessageHandler;

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
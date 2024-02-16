using Akka.Streams.Stage;

namespace Akka.Streams.Mqtt;

public class ManagedMqttFlowStage<TInput, TOutput> : GraphStage<FlowShape<TInput, TOutput>>
{

    public ManagedMqttFlowStage(string connectionString)
    {
        Inlet = new Inlet<TInput>("ManagedMqttFlow.in");
        Outlet = new Outlet<TOutput>("ManagedMqttFlow.out");
        Shape = new FlowShape<TInput, TOutput>(Inlet, Outlet);
    }

    public Inlet<TInput> Inlet { get; }
    public Outlet<TOutput> Outlet { get; }

    public override FlowShape<TInput, TOutput> Shape { get; }

    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => throw new NotImplementedException();

    sealed class Logic : GraphStageLogic
    {
        public Logic(int inCount, int outCount) : base(inCount, outCount)
        {
        }
    }
}

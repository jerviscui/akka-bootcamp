using Akka.Actor;

namespace ChartApp.Actors
{
    public class GatherMetrics
    {
    }

    public class Metric
    {
        public string Series { get; private set; }

        public float CounterValue { get; private set; }

        public Metric(string series, float counterValue)
        {
            Series = series;
            CounterValue = counterValue;
        }
    }

    public enum CounterType
    {
        Cpu,

        Memory,

        Disk
    }

    public class SubscribeCounter
    {
        public CounterType Counter { get; private set; }

        public IActorRef Subscriber { get; private set; }

        public SubscribeCounter(CounterType counter, IActorRef subscriber)
        {
            Counter = counter;
            Subscriber = subscriber;
        }
    }

    public class UnsubscribeCounter
    {
        public CounterType CounterType { get; private set; }

        public IActorRef Subscriber { get; private set; }

        public UnsubscribeCounter(CounterType counterType, IActorRef subscriber)
        {
            CounterType = counterType;
            Subscriber = subscriber;
        }
    }
}

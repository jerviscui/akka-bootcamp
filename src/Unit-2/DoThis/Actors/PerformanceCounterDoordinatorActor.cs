using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterDoordinatorActor : ReceiveActor
    {
        private static readonly Dictionary<CounterType, Func<PerformanceCounter>> CounterGenerators =
            new Dictionary<CounterType, Func<PerformanceCounter>>
            {
                { CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true) },
                { CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true) },
                { CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true) }
            };

        private static readonly Dictionary<CounterType, Func<Series>> CounterSeries =
            new Dictionary<CounterType, Func<Series>>
            {
                {
                    CounterType.Cpu,
                    () => new Series(CounterType.Cpu.ToString())
                        { ChartType = SeriesChartType.SplineArea, Color = Color.DarkGreen }
                },
                {
                    CounterType.Memory,
                    () => new Series(CounterType.Memory.ToString())
                        { ChartType = SeriesChartType.FastLine, Color = Color.MediumBlue }
                },
                {
                    CounterType.Disk,
                    () => new Series(CounterType.Disk.ToString())
                        { ChartType = SeriesChartType.SplineArea, Color = Color.DarkRed }
                }
            };

        private readonly IActorRef _chartingActor;

        private readonly Dictionary<CounterType, IActorRef> _counterActors;

        /// <inheritdoc />
        public PerformanceCounterDoordinatorActor(IActorRef chartingActor) : this(
            new Dictionary<CounterType, IActorRef>(), chartingActor)
        {
        }

        /// <inheritdoc />
        public PerformanceCounterDoordinatorActor(Dictionary<CounterType, IActorRef> counterActors,
            IActorRef chartingActor)
        {
            _counterActors = counterActors;
            _chartingActor = chartingActor;

            Receive<Watch>(watch =>
            {
                if (!_counterActors.ContainsKey(watch.CounterType))
                {
                    var counterActor = Context.ActorOf(Props.Create(() =>
                        new PerformanceCounterActor(watch.CounterType.ToString(),
                            CounterGenerators[watch.CounterType])));

                    _counterActors.Add(watch.CounterType, counterActor);
                }

                _chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[watch.CounterType].Invoke()));

                _counterActors[watch.CounterType].Tell(new SubscribeCounter(watch.CounterType, _chartingActor));
            });

            Receive<UnWatch>(unwatch =>
            {
                if (!_counterActors.ContainsKey(unwatch.CounterType))
                {
                    return;
                }

                _counterActors[unwatch.CounterType].Tell(new UnsubscribeCounter(unwatch.CounterType, _chartingActor));

                _chartingActor.Tell(new ChartingActor.RemoveSeries(unwatch.CounterType.ToString()));
            });
        }

        #region Message types

        public class Watch
        {
            public Watch(CounterType counterType)
            {
                CounterType = counterType;
            }

            public CounterType CounterType { get; private set; }
        }

        public class UnWatch
        {
            public UnWatch(CounterType counterType)
            {
                CounterType = counterType;
            }

            public CounterType CounterType { get; private set; }
        }

        #endregion
    }
}

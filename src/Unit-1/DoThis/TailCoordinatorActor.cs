using System;
using Akka.Actor;

namespace WinTail;

public class TailCoordinatorActor : UntypedActor
{
    /// <inheritdoc />
    protected override void OnReceive(object message)
    {
        if (message is StartTail start)
        {
            Context.ActorOf(Props.Create(() => new TailActor(start.ReporterActor, start.FilePath)));
        }
    }

    /// <inheritdoc />
    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(10, TimeSpan.FromSeconds(30),
            exception => exception switch
            {
                ArithmeticException _ => Directive.Resume,
                NotSupportedException _ => Directive.Stop,
                _ => Directive.Restart
            }
        );
    }

    #region Message types

    public class StartTail
    {
        public string FilePath { get; private set; }

        public IActorRef ReporterActor { get; private set; }

        public StartTail(string filePath, IActorRef reporterActor)
        {
            FilePath = filePath;
            ReporterActor = reporterActor;
        }
    }

    public class StopTail
    {
        public string FilePath { get; private set; }

        public StopTail(string filePath)
        {
            FilePath = filePath;
        }
    }

    #endregion
}



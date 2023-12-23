using Akka.Actor;

namespace WinTail
{
    #region Program

    internal class Program
    {
        public static ActorSystem MyActorSystem;

        private static void Main(string[] args)
        {
            // initialize MyActorSystem
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            var writerProps = Props.Create<ConsoleWriterActor>();
            var writerActor = MyActorSystem.ActorOf(writerProps, "consoleWriterActor");

            var validationProps = Props.Create(() => new ValidationActor(writerActor));
            var validationActor = MyActorSystem.ActorOf(validationProps, "validationActor");

            var readerProps = Props.Create<ConsoleReaderActor>(validationActor);
            var readerActor = MyActorSystem.ActorOf(readerProps, "consoleReaderActor");

            // tell console reader to begin
            readerActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }

    #endregion
}

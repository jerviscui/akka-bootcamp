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

            // time to make your first actors!
            // make consoleWriterActor using these props: Props.Create(() => new ConsoleWriterActor())
            var writerActor = MyActorSystem.ActorOf(Props.Create(() => new ConsoleWriterActor()));

            // make consoleReaderActor using these props: Props.Create(() => new ConsoleReaderActor(consoleWriterActor))
            var readerActor =
                MyActorSystem.ActorOf(Props.Create(() => new ConsoleReaderActor(writerActor)));

            // tell console reader to begin
            readerActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }

    #endregion
}

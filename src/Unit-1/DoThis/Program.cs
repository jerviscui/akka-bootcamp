using System;
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

            PrintInstructions();

            // time to make your first actors!
            // make consoleWriterActor using these props: Props.Create(() => new ConsoleWriterActor())
            var writerActor = MyActorSystem.ActorOf(Props.Create(() => new ConsoleWriterActor()));

            // make consoleReaderActor using these props: Props.Create(() => new ConsoleReaderActor(consoleWriterActor))
            var readerActor =
                MyActorSystem.ActorOf(Props.Create(() => new ConsoleReaderActor(writerActor)));

            // tell console reader to begin
            readerActor.Tell("start");

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }

        private static void PrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.Write("Some lines will appear as");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write(" red ");
            Console.ResetColor();
            Console.Write(" and others will appear as");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" green! ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }
    }

    #endregion
}

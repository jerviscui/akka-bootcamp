using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.Terminate"/>.
    /// </summary>
    internal class ConsoleReaderActor : UntypedActor
    {
        public const string ExitCommand = "exit";

        public const string StartCommand = "start";

        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                DoPrintInstructions();
            }

            GetAndValidateInput();
        }

        private void DoPrintInstructions()
        {
            Console.WriteLine("Please provide the URI of a log file on disk.\n");
        }

        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();

            if (!string.IsNullOrEmpty(message) && message.Equals(ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                Context.System.Terminate();
            }
            else
            {
                // otherwise, just send the message off for validation
                Context.ActorSelection("akka://MyActorSystem/user/validationActor").Tell(message);
            }
        }
    }
}

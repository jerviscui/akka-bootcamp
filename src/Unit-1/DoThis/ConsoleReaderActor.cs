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

        private readonly IActorRef _consoleWriterActor;

        public ConsoleReaderActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                DoPrintInstructions();
            }
            else if (message is Message.InputError error)
            {
                _consoleWriterActor.Tell(error);
            }

            GetAndValidateInput();
        }

        private void DoPrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.WriteLine("Some entries will pass validation, and some won't...\n\n");
            Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }

        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();

            if (string.IsNullOrEmpty(message))
            {
                Self.Tell(new Message.NullInputError("No input received."));
            }
            else if (message.Equals(ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                Context.System.Terminate();
            }
            else
            {
                var valid = IsValid(message);
                if (valid)
                {
                    _consoleWriterActor.Tell(new Message.InputSuccess("Thank you! Message was valid."));

                    Self.Tell(new Message.ContinueProcessing());
                }
                else
                {
                    Self.Tell(new Message.ValidationError("Invalid: input had odd number of characters."));
                }
            }
        }

        private bool IsValid(string message)
        {
            var valid = message.Length % 2 == 0;
            return valid;
        }
    }
}

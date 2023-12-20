using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for serializing message writes to the console.
    /// (write one message at a time, champ :)
    /// </summary>
    class ConsoleWriterActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            if (message is Message.InputError msg)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg.Error);
            }
            else if (message is Message.InputSuccess success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(success.Region);
            }
            else
            {
                Console.WriteLine(message);
            }

            Console.ResetColor();
        }
    }
}

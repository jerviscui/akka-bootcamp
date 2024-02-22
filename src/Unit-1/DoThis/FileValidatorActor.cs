using System.IO;
using Akka.Actor;

namespace WinTail;

public class FileValidatorActor : UntypedActor
{
    private readonly IActorRef _consoleWriterActor;

    /// <inheritdoc />
    public FileValidatorActor(IActorRef consoleWriterActor)
    {
        _consoleWriterActor = consoleWriterActor;
    }

    /// <inheritdoc />
    protected override void OnReceive(object message)
    {
        if (message is not string str)
        {
            return;
        }

        if (string.IsNullOrEmpty(str))
        {
            _consoleWriterActor.Tell(new Message.NullInputError("Input was blank.Please try again.\n"));

            Sender.Tell(new Message.ContinueProcessing());
        }
        else
        {
            if (IsFileUri(str))
            {
                _consoleWriterActor.Tell(new Message.InputSuccess($"Starting processing for {str}"));

                // start coordinator
                Context.ActorSelection("akka://MyActorSystem/user/tailCoordinatorActor").Tell(
                    new TailCoordinatorActor.StartTail(str, _consoleWriterActor));
            }
            else
            {
                _consoleWriterActor.Tell(new Message.ValidationError($"{str} is not an existing URI on disk."));

                Sender.Tell(new Message.ContinueProcessing());
            }
        }
    }

    private static bool IsFileUri(string path)
    {
        return File.Exists(path);
    }
}

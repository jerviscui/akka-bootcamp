using System;
using System.IO;
using System.Text;
using Akka.Actor;

namespace WinTail;

public class TailActor : UntypedActor
{
    private readonly string _filePath;

    private readonly IActorRef _reporterActor;

    private readonly FileObserver _observer;

    private readonly Stream _stream;

    private readonly StreamReader _reader;

    /// <inheritdoc />
    public TailActor(IActorRef reporterActor, string filePath)
    {
        _filePath = filePath;
        _reporterActor = reporterActor;

        var fullPath = Path.GetFullPath(filePath);
        _observer = new FileObserver(Self, fullPath);
        _observer.Start();

        _stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _reader = new StreamReader(_stream, Encoding.UTF8);

        var text = _reader.ReadToEnd();
        Self.Tell(new InitialRead(_filePath, text));
    }

    /// <inheritdoc />
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case FileWrite write:
                var text = _reader.ReadToEnd();
                if (!string.IsNullOrEmpty(text))
                {
                    _reporterActor.Tell(text);
                    // won't trigger father restart
                    //Unhandled(new Exception("SupervisorStrategy test"));
                    //Unhandled(null);

                    // will trigger father SupervisorStrategy() send Directive.Restart
                    //throw new Exception("who catch");
                }
                break;
            case FileError error:
                _reporterActor.Tell($"Tail error: {error.error}");
                break;
            case InitialRead read:
                _reporterActor.Tell(read.text);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    #region Message types

    public record FileWrite(string FileName);

    public record FileError(string fileName, string error);

    public record InitialRead(string fileName, string text);

    #endregion
}

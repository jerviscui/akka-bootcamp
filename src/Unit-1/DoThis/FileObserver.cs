using System;
using System.IO;
using Akka.Actor;

namespace WinTail;

public class FileObserver : IDisposable
{
    private readonly IActorRef _tailActor;

    private readonly string _absoluteFilePath;

    private FileSystemWatcher _watcher;

    private readonly string _fileDir;

    private readonly string _fileNameOnly;

    public FileObserver(IActorRef tailActor, string absoluteFilePath)
    {
        _tailActor = tailActor;
        _absoluteFilePath = absoluteFilePath;

        _fileDir = Path.GetDirectoryName(absoluteFilePath);
        _fileNameOnly = Path.GetFileName(absoluteFilePath);
    }

    public void Start()
    {
        _watcher = new FileSystemWatcher(_fileDir, _fileNameOnly);
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

        _watcher.Changed += (sender, args) =>
        {
            if (args.ChangeType == WatcherChangeTypes.Changed)
            {
                _tailActor.Tell(new TailActor.FileWrite(args.Name), ActorRefs.NoSender);
            }
        };
        _watcher.Error += (sender, args) =>
        {
            _tailActor.Tell(new TailActor.FileError(_fileNameOnly, args.GetException().Message),
                ActorRefs.NoSender);
        };

        _watcher.EnableRaisingEvents = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _watcher.Dispose();
    }
}

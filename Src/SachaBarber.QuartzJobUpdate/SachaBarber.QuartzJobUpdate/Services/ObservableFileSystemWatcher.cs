using System;
using System.IO;
using System.Reactive.Linq;

namespace SachaBarber.QuartzJobUpdate.Services
{
    public class ObservableFileSystemWatcher : IObservableFileSystemWatcher
    {
        private FileSystemWatcher _watcher;

        public void SetFile(FileSystemWatcher watcher)
        {
            _watcher = watcher;

            Changed = Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => _watcher.Changed += h, h => _watcher.Changed -= h)
                .Select(x => x.EventArgs);

            Renamed = Observable
                .FromEventPattern<RenamedEventHandler, RenamedEventArgs>(h => _watcher.Renamed += h, h => _watcher.Renamed -= h)
                .Select(x => x.EventArgs);

            Deleted = Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => _watcher.Deleted += h, h => _watcher.Deleted -= h)
                .Select(x => x.EventArgs);

            Errors = Observable
                .FromEventPattern<ErrorEventHandler, ErrorEventArgs>(h => _watcher.Error += h, h => _watcher.Error -= h)
                .Select(x => x.EventArgs);

            Created = Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => _watcher.Created += h, h => _watcher.Created -= h)
                .Select(x => x.EventArgs);

            All = Changed.Merge(Renamed).Merge(Deleted).Merge(Created);
            _watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }

        public IObservable<FileSystemEventArgs> Changed { get; private set; }
        public IObservable<RenamedEventArgs> Renamed { get; private set; }
        public IObservable<FileSystemEventArgs> Deleted { get; private set; }
        public IObservable<ErrorEventArgs> Errors { get; private set; }
        public IObservable<FileSystemEventArgs> Created { get; private set; }
        public IObservable<FileSystemEventArgs> All { get; private set; }
    }
}

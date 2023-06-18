using Temper.Database;

namespace Temper
{
    internal class TemperAgent
    {
        private string targetDirectory;
        private readonly AppDbContext db;

        public TemperAgent(string targetDirectory)
        {
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
            this.targetDirectory = targetDirectory;
            db = new();
        }

        internal async Task RunAsync(CancellationToken cancellationToken = default)
        {
            Task daily = StartWatcherAsync(targetDirectory, WatcherType.Daily, cancellationToken);
            Task weekly = StartWatcherAsync(targetDirectory, WatcherType.Weekly, cancellationToken);
            Task monthly = StartWatcherAsync(targetDirectory, WatcherType.Monthly, cancellationToken);
            Task remover = StartRecyclerAsync(cancellationToken);
            await Task.WhenAll(daily, weekly, monthly, remover);
        }

        private Task StartRecyclerAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    Console.WriteLine("Checking files was started at {0}", DateTime.Now);
                    int count = CheckDatabase();
                    Console.WriteLine("{0} files was checked at {1}", count, DateTime.Now);
                    await Task.Delay(60_000, cancellationToken);
                }
            }, cancellationToken);
        }

        private async Task StartWatcherAsync(string targetDirectory, WatcherType watcherType, CancellationToken cancellationToken = default)
        {
            string path = Path.Combine(targetDirectory, watcherType.ToString().ToLower());
            AddUntrackedFiles(path, watcherType);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            using FileSystemWatcher watcher = new(path);
            watcher.Created += (sender, args) => OnFileCreated(args, watcherType);
            watcher.Renamed += (sender, args) => OnFileCreated(args, watcherType);
            watcher.Changed += (sender, args) => OnFileCreated(args, watcherType);
            watcher.Deleted += (sender, args) => OnFileDeleted(args, watcherType);
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            await Task.Delay(-1, cancellationToken);
        }

        private void AddUntrackedFiles(string path, WatcherType watcherType)
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            int addedCounter = 0;
            foreach (var file in files)
            {
                var found = db.FileRecords.FirstOrDefault(x => x.FullName == file && x.WatcherType == watcherType);
                if (found == null)
                {
                    addedCounter++;
                    AddToDatabase(file, watcherType);
                }
            }
            Console.WriteLine("Directory {0} was scanned for {1} new files, total: {2}", path, addedCounter, files.Length);
        }

        private void OnFileCreated(FileSystemEventArgs args, WatcherType watcherType)
        {
            AddToDatabase(args.FullPath, watcherType);
        }

        private void OnFileDeleted(FileSystemEventArgs args, WatcherType watcherType)
        {
            DeleteFromDatabase(args.FullPath, watcherType);
        }

        private void DeleteFromDatabase(string fullName, WatcherType watcherType)
        {
            var found = db.FileRecords.FirstOrDefault(x => x.WatcherType == watcherType && x.FullName == fullName);
            if (found != null)
            {
                db.FileRecords.Remove(found);
                db.SaveChanges();
                Console.WriteLine("[{0}] Deleted file: {1}", fullName, watcherType);
            }
        }

        private void AddToDatabase(string fullName, WatcherType watcherType)
        {
            var found = db.FileRecords.FirstOrDefault(x => x.WatcherType == watcherType && x.FullName == fullName);
            if (found != null)
            {
                found.FullName = fullName;
                found.Created = DateTime.UtcNow;
            }
            else
            {
                found = new()
                {
                    WatcherType = watcherType,
                    Created = DateTime.UtcNow,
                    FullName = fullName
                };
                db.FileRecords.Add(found);
            }
            Console.WriteLine("[{0}] Added new file: {1}", fullName, watcherType);
            db.SaveChanges();
        }

        private int CheckDatabase()
        {
            var items = db.FileRecords.ToList();
            foreach (var item in items)
            {
                int hours = (int)item.WatcherType;
                TimeSpan diff = DateTime.UtcNow - item.Created;
                if (diff.TotalHours > hours)
                {
                    DeleteFile(item);
                }
            }
            return items.Count;
        }

        private void DeleteFile(FileRecord item)
        {
            try
            {
                if (Directory.Exists(item.FullName))
                {
                    Directory.Delete(item.FullName);
                }
                else
                {
                    File.Delete(item.FullName);
                }
                db.FileRecords.Remove(item);
                db.SaveChanges();
                Console.WriteLine($"[{item.WatcherType}] File deleted: {item.FullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{item.WatcherType}] Cannot delete file {item.FullName} - {ex.Message}");
            }
        }
    }
}
#nullable disable
namespace Temper.Database
{
    public class FileRecord
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime Created { get; set; }
        public WatcherType WatcherType { get; set; }
    }
}
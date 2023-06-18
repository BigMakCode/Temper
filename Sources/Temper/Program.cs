namespace Temper
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string defaultFolder = Path.Combine(Environment.CurrentDirectory, "temp");
            string targetDirectory = args.Length == 0 ? defaultFolder : args[0];
            TemperAgent agent = new(targetDirectory);
            await agent.RunAsync();
        }
    }
}
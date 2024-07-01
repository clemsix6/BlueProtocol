namespace SimpleRepeat;


internal static class Program
{
    private static void Main()
    {
        // Read the sentences from the file
        var sentences = File.ReadAllLines("sentences.txt");

        // Create a semaphore to control access to the console output
        var outputSemaphore = new Semaphore(1, 1);

        // Create a list of bots
        var bots = CreateBots(Config.BotCount, sentences, outputSemaphore);

        // Start the bots
        bots.ForEach(x => x.Start());

        // Wait for the bots to finish
        bots.ForEach(x => x.Thread.Join());
    }


    private static List<Bot> CreateBots(int count, string[] sentences, Semaphore outputSemaphore)
    {
        const string Ids = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var bots = new List<Bot>();

        for (var i = 0; i < count; i++)
        {
            var id = Ids[i].ToString();
            var color = (ConsoleColor) (i % 16 + 1);
            bots.Add(new Bot(id, Config.StartingPort + i, color, sentences, outputSemaphore));
        }

        return bots;
    }
}
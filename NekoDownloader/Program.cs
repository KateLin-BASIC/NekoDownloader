using System.CommandLine;
using Newtonsoft.Json.Linq;

var threadOption = new Option<int>(
    "--threads",
    getDefaultValue: () => 16,
    description: "Numbers of Thread");

var downloadPathOption = new Option<string>(
    "--path",
    getDefaultValue: () => "./Downloads",
    description: "Path of the folder where the image will be downloaded");

var nsfwOption = new Option<bool>(
    "--nsfw",
    getDefaultValue: () => false,
    description: "Determine whether the cat is naked or not (??).");

var rootCommand = new RootCommand
{
    threadOption,
    downloadPathOption,
    nsfwOption
};

rootCommand.Description = "Download Neko image from Internet!";

rootCommand.SetHandler((int threadCount, string downloadPath, bool nsfwOrNot) =>
{
    void DownloadNeko()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Task #{Task.CurrentId} (Thread: {Environment.CurrentManagedThreadId}) Started...");
        
        using (var client = new HttpClient())
        {
            string apiUrl = nsfwOrNot ? "https://nekos.life/api/lewd/neko" : "https://nekos.life/api/neko";
            var httpResponse = client.GetAsync(apiUrl).Result;
            var jsonObject = JObject.Parse(httpResponse.Content.ReadAsStringAsync().Result);
            var imageUrl = jsonObject.SelectToken("neko")?.ToString();
            var imageFilename = Path.GetFileName(new Uri(imageUrl ?? string.Empty).LocalPath);
            
            var directory = new DirectoryInfo(Path.GetFullPath(downloadPath));
            if (!directory.Exists)
            {
                Directory.CreateDirectory(directory.FullName);
            }

            var imageResponse = client.GetAsync(imageUrl).Result;
            using (var fileStream = new FileStream($"{Path.GetFullPath(downloadPath)}\\{imageFilename}", FileMode.Create, FileAccess.Write))
            {
                var stream = imageResponse.Content.ReadAsStreamAsync().Result;
                stream.CopyTo(fileStream);
            }
        }
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Task #{Task.CurrentId} (Thread: {Environment.CurrentManagedThreadId}) Done!");
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    var tasks = new List<Task>();
    for (int i = 0; i < threadCount; i++)
    {
        var task = new Task(DownloadNeko);
        task.Start();
        tasks.Add(task);
    }
    Task.WaitAll(tasks.ToArray());
    tasks.Clear();
    
}, threadOption, downloadPathOption, nsfwOption);

return rootCommand.Invoke(args);
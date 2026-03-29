using System;
using Medo;

Console.WriteLine($"User .: {Config.User.FileName}");
Console.WriteLine($"System: {Config.System.FileName}");
Console.WriteLine($"State : {Config.State.FileName}");
Console.WriteLine($"Recent: {Config.Recent.FileName}");

Console.WriteLine();
Console.WriteLine("Current recent files:");
if (Config.Recent.Files.Count > 0)
{
    foreach (var recentFile in Config.Recent.Files)
    {
        Console.WriteLine($"  {recentFile.Name}");
    }
}
else
{
    Console.WriteLine("  no recent files found");
}

Console.WriteLine();
var file = new FileInfo($"example{Random.Shared.Next(10)}.txt");
Console.WriteLine($"Adding {file.Name} to recent files");
Config.Recent.Files.Add(file);

Console.WriteLine();
Console.WriteLine("New recent files:");
if (Config.Recent.Files.Count > 0)
{
    foreach (var recentFile in Config.Recent.Files)
    {
        Console.WriteLine($"  {recentFile.Name}");
    }
}
else
{
    Console.WriteLine("  no recent files found");
}

using System;
using Medo;

Console.WriteLine($"User .: {Medo.Config.User.FileName}");
Console.WriteLine($"System: {Medo.Config.System.FileName}");
Console.WriteLine($"State : {Medo.Config.State.FileName}");
Console.WriteLine($"Recent: {Medo.Config.Recent.FileName}");

Console.WriteLine();
Console.WriteLine($"Last run: {Medo.Config.Read("LastRun", DateTime.MinValue)}");


Medo.Config.Write("LastRun", DateTime.Now);

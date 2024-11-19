using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Program
{
    // A thread-safe collection to keep track of threads
    private static readonly List<Thread> threads = new();

    static void Main(string[] args)
    {
        Console.WriteLine("Welcome to CommandThreader!");
        Console.Write("Enter the path to the commands file: ");
        string filePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("Invalid file path. Exiting...");
            return;
        }

        Console.Write("Enter the maximum number of threads to run concurrently: ");
        if (!int.TryParse(Console.ReadLine(), out int maxThreads) || maxThreads <= 0)
        {
            Console.WriteLine("Invalid thread count. Exiting...");
            return;
        }

        string[] commands = File.ReadAllLines(filePath);
        ThreadPool.SetMaxThreads(maxThreads, maxThreads);

        Console.WriteLine($"Starting to process {commands.Length} commands with up to {maxThreads} concurrent threads.");

        foreach (string command in commands)
        {
            if (string.IsNullOrWhiteSpace(command)) continue;

            ThreadPool.QueueUserWorkItem(state => RunCommand(command));
        }

        Console.WriteLine("Press any key to exit once all commands are processed...");
        Console.ReadKey();
    }

    private static void RunCommand(string command)
    {
        try
        {
            string[] parts = command.Split(' ', 2);
            string exePath = parts[0];
            string arguments = parts.Length > 1 ? parts[1] : string.Empty;

            Console.WriteLine($"Executing: {command}");
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"[Output from {exePath}]: {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"[Error from {exePath}]: {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command '{command}': {ex.Message}");
        }
    }
}
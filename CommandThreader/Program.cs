using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Program
{
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

        Console.WriteLine($"Starting to process {commands.Length} commands with up to {maxThreads} concurrent threads.");

        var semaphore = new SemaphoreSlim(maxThreads);
        var countdown = new CountdownEvent(commands.Length);

        foreach (string command in commands)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                countdown.Signal();
                continue;
            }

            semaphore.Wait(); // Wait for an available slot
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    RunCommand(command);
                }
                finally
                {
                    semaphore.Release(); // Release the slot after the command completes
                    countdown.Signal();  // Mark task as completed
                }
            });
        }

        // Wait for all threads to complete
        countdown.Wait();
        semaphore.Dispose();

        Console.WriteLine("All commands have been processed. Press any key to exit...");
        Console.ReadKey();
    }

    private static void RunCommand(string command)
    {
        try
        {
            // Split command using | as the delimiter
            string[] parts = command.Split('|');
            if (parts.Length < 3)
            {
                Console.WriteLine($"Invalid command format: {command}");
                return;
            }

            string exePath = parts[0].Trim();
            string arguments = parts[1].Trim();
            string visibility = parts[2].Trim().ToLower();

            // Validate executable path
            if (string.IsNullOrWhiteSpace(exePath))
            {
                Console.WriteLine($"Executable path is missing in: {command}");
                return;
            }

            // Determine whether the application window should be visible
            bool createNoWindow = visibility != "visible";

            Console.WriteLine($"Executing: {exePath} {arguments} (Visible: {!createNoWindow})");

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = createNoWindow
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
            process.WaitForExit(); // Wait for the process to terminate
            Console.WriteLine($"Process '{exePath}' completed with exit code {process.ExitCode}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command '{command}': {ex.Message}");
        }
    }
}
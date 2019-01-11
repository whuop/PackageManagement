using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Landfall.Processes
{
    public class ProcessRunner
    {
        public delegate void OnProcessExitedDelegate(object sender, EventArgs eventArgs);
        public delegate void OnProcessOutputDataReceivedDelegate(object sender, DataReceivedEventArgs eventArgs);
        public delegate void OnProcessErrorDataReceived(object sender, DataReceivedEventArgs eventArgs);

        public static async Task<int> RunAsync(string fileName, string args, string workingDirectory, OnProcessExitedDelegate onExited, OnProcessOutputDataReceivedDelegate onOutput, OnProcessErrorDataReceived onError)
        {
            using (var process = new Process
            {
                StartInfo =
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
                EnableRaisingEvents = true
            })
            {
                if (!string.IsNullOrEmpty(workingDirectory))
                    process.StartInfo.WorkingDirectory = workingDirectory;
                return await RunProcessAsync(process, onExited, onOutput, onError).ConfigureAwait(false);
            }
        }

        private static Task<int> RunProcessAsync(Process process, OnProcessExitedDelegate onExit, OnProcessOutputDataReceivedDelegate onOutput, OnProcessErrorDataReceived onError)
        {
            var tcs = new TaskCompletionSource<int>();

            process.Exited += (s, ea) =>
            {
                tcs.SetResult(process.ExitCode);
                onExit(s, ea);
            };

            process.OutputDataReceived += (s, ea) =>
            {
                onOutput(s, ea);
            };

            process.ErrorDataReceived += (s, ea) => 
            {
                onError(s, ea);
            };

            bool started = process.Start();
            if (!started)
            {
                throw new InvalidOperationException("Could not start process: " + process);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}



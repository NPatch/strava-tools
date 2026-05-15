using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace StravaTools.Utilities.Processes
{
    public class ProcessRunTask : IDisposable
    {
        public string FileName { get; set; }
        public string Arguments { get; set; }
        public DataReceivedEventHandler OnOutputReceived { get; set; }
        public DataReceivedEventHandler OnErrorReceived { get; set; }
        public List<string> OutputReceived { get; set; }
        public List<string> ErrorReceived { get; set; }

        public void Dispose()
        {
            OnOutputReceived = null;
            OnErrorReceived = null;
            OutputReceived.Clear();
            ErrorReceived.Clear();
            if (OutputReceived != null)
            {
                OutputReceived.Clear();
                OutputReceived = null;
            }

            if (ErrorReceived != null)
            {
                ErrorReceived.Clear();
                ErrorReceived = null;
            }            
        }
    }

    public class ProcessUtilities
    {
        public static async Task<int> RunProcessAsync(ProcessRunTask ptask, CancellationToken token = default)
        {
            Process pr = null;

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            void OnProcessExited(object sender, EventArgs e)
            {
                tcs.TrySetResult(true);
            }

            try
            {
                token.ThrowIfCancellationRequested();
                pr = new Process();

                pr.StartInfo = new ProcessStartInfo()
                {
                    FileName = ptask.FileName,
                    Arguments = ptask.Arguments,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                // Attach event handlers to read output and errors in real time
                pr.EnableRaisingEvents = true;
                if (ptask.OnOutputReceived != null)
                {
                    pr.OutputDataReceived += ptask.OnOutputReceived;
                }
                else
                {
                    ptask.OutputReceived = new List<string>();
                    pr.OutputDataReceived += (s, eargs) =>
                    {
                        string line = eargs.Data;
                        if (!string.IsNullOrEmpty(line))
                        {
                            ptask.OutputReceived.Add(line);
                        }
                    };
                }
                if (ptask.OnErrorReceived != null)
                {
                    pr.ErrorDataReceived += ptask.OnErrorReceived;
                }
                else
                {
                    ptask.ErrorReceived = new List<string>();
                    pr.ErrorDataReceived += (s, eargs) =>
                    {
                        string line = eargs.Data;
                        if (!string.IsNullOrEmpty(line))
                        {
                            ptask.ErrorReceived.Add(line);
                        }
                    };
                }

                pr.Exited += OnProcessExited;

                //Setting the Task to cancelled state
                token.Register(() =>
                {
                    try
                    {
                        if (!pr.HasExited)
                            pr.Kill(); // Forcefully kill process and child processes
                    }
                    catch { /* swallow exceptions like access denied */ }

                    tcs.TrySetCanceled(token);
                });


                Stopwatch stopwatch = Stopwatch.StartNew();
                pr.Start();

                // Begin asynchronous read of the output and error streams
                pr.BeginOutputReadLine();
                pr.BeginErrorReadLine();

                // Wait for the process to exit
                await tcs.Task;

                stopwatch.Stop();

                return pr.ExitCode;
            }
            catch (OperationCanceledException)
            {
                pr.Exited -= OnProcessExited;
                if (!pr.HasExited)
                    pr.Kill();
                throw;
            }
        }

#if FAKEWORK
        public static async Task<int> RunFakeWorkloadAsync(int seconds = 5, CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                for (int i = 0; i < seconds; i++)
                {
                    Log.Information($"Timeout in: {(5 - i)} seconds");
                    await Task.Delay(1000);
                    token.ThrowIfCancellationRequested();
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            return -1;
        }
#endif
    }
}

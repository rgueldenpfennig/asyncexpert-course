using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace TaskCompletionSourceExercises.Core
{
    public class AsyncTools
    {
        public static Task<string> RunProgramAsync(string path, string args = "")
        {
            var proc = new Process();
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            proc.StartInfo.Arguments = args;
            proc.StartInfo.FileName = path;
            proc.EnableRaisingEvents = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;

            proc.Exited += async (sender, eventArgs) =>
            {
                var p = (Process) sender;
                if (p.ExitCode != 0)
                {
                    var stdErr = await p.StandardError.ReadToEndAsync();
                    tcs.TrySetException(new Exception(stdErr));
                }
                else
                {
                    var stdOut = await p.StandardOutput.ReadToEndAsync();
                    tcs.TrySetResult(stdOut);
                }

                p.Dispose();
            };

            proc.Start();
            return tcs.Task;
        }
    }
}

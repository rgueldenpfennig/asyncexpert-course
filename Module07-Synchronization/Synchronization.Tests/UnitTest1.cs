using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Synchronization.Core;
using Xunit;

namespace Synchronization.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void NamedExclusiveScope_CanBeUsedSystemWide()
        {
#pragma warning disable CS0642 // Possible mistaken empty statement
            using (new NamedExclusiveScope(Guid.NewGuid().ToString(), true)) ;
#pragma warning restore CS0642 // Possible mistaken empty statement
        }

        [Fact]
        public async Task NamedExclusiveScope_WhenMultipleLocalExclusiveScopeInSameProcess_ThenSucceeds()
        {
            var tasks = Enumerable.Range(0, 5).Select(i => Task.Run(() =>
            {
                using (new NamedExclusiveScope("name", false))
                {
                    Thread.Sleep(300);
                    return DateTime.Now;
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            var minFinishTime = tasks.Select(t => t.Result).Min();
            var maxFinishTime = tasks.Select(t => t.Result).Max();
            var duration = (maxFinishTime - minFinishTime).TotalMilliseconds;

            Assert.True(duration >= 1200, "execution wasn't sequencial");
        }

        [Fact]
        public async Task NamedExclusiveScope_WhenMultipleLocalExclusiveScopesWithUniqueNamesInSameProcess_ThenSucceeds()
        {
            var tasks = Enumerable.Range(0, 5).Select(i =>
            {
                var index = i;
                return Task.Run(() =>
                {
                    using (new NamedExclusiveScope($"name {index}", false))
                    {
                        Thread.Sleep(300);
                        return DateTime.Now;
                    }
                });
            }).ToArray();

            await Task.WhenAll(tasks);

            var minFinishTime = tasks.Select(t => t.Result).Min();
            var maxFinishTime = tasks.Select(t => t.Result).Max();
            var duration = (maxFinishTime - minFinishTime).TotalMilliseconds;

            Assert.True(duration < 1200, "execution wasn't parallel");
        }

        [Fact]
        public async Task GivenExampleApp_WhenLocalExclusiveScope_ThenSucceeds()
        {
            var path = @"..\..\..\..\..\Synchronization\bin\x64\Debug\netcoreapp3.1\Synchronization.exe";

            var result = await RunProgramAsync(path, "name false");

            Assert.Equal("Hello world!\r\n", result);
        }

        [Fact]
        public async Task GivenExampleApp_WhenSingleGlobalExclusiveScope_ThenSucceeds()
        {
            var path = @"..\..\..\..\..\Synchronization\bin\x64\Debug\netcoreapp3.1\Synchronization.exe";

            var result = await RunProgramAsync(path, "name true");

            Assert.Equal("Hello world!\r\n", result);
        }

        [Fact]
        public async Task GivenExampleApp_WhenTwoLocalExclusiveScopesInDifferentProcesses_ThenSucceeds()
        {
            var scopeName = "someScopeName";
            var path = @"..\..\..\..\..\Synchronization\bin\x64\Debug\netcoreapp3.1\Synchronization.exe";
            var firstRunTask = RunProgramAsync(path, $"{scopeName} false");
            var exception = await Record.ExceptionAsync(async () =>
                await RunProgramAsync(path, $"{scopeName} false"));
            await firstRunTask;

            Assert.Null(exception);
        }

        [Fact]
        public async Task GivenExampleApp_WhenDoubleGlobalExclusiveScope_ThenThrows()
        {
            var scopeName = "someScopeName";
            var path = @"..\..\..\..\..\Synchronization\bin\x64\Debug\netcoreapp3.1\Synchronization.exe";
            var firstRunTask = RunProgramAsync(path, $"{scopeName} true");
            var exception = await Record.ExceptionAsync(async () =>
                await RunProgramAsync(path, $"{scopeName} true"));
            await firstRunTask;

            Assert.NotNull(exception);
            Assert.IsType<Exception>(exception);
            Assert.StartsWith($"Unhandled exception. System.InvalidOperationException: Unable to get a global lock {scopeName}.",
                exception.Message);

        }

        private static Task<string> RunProgramAsync(string path, string args = "")
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            var process = new Process();
            process.EnableRaisingEvents = true;
            process.StartInfo = new ProcessStartInfo(path, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            process.Exited += async (sender, eventArgs) =>
            {
                var senderProcess = sender as Process;
                if (senderProcess is null)
                    return;
                if (senderProcess.ExitCode != 0)
                {
                    var output = await process.StandardError.ReadToEndAsync();
                    tcs.SetException(new Exception(output));
                }
                else
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    tcs.SetResult(output);
                }
                process.Dispose();
            };
            process.Start();
            return tcs.Task;
        }
    }
}

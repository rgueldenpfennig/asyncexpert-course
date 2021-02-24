using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TaskCompletionSourceExercises.Core;
using Xunit;

namespace TaskCompletionSourceExercises.Tests
{
    public class AsyncToolsTests
    {
        [Fact]
        public async Task GivenExampleAppRequiringArguments_WhenNoArguments_ThenThrows()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var path = Path.Combine(location,  @"..\..\..\..\..\ExampleApp\bin\Debug\netcoreapp3.1\ExampleApp.exe");
            var exception = await Record.ExceptionAsync(async () =>
                await AsyncTools.RunProgramAsync(path));

            Assert.NotNull(exception);
            Assert.IsType<Exception>(exception);
            Assert.StartsWith("Unhandled exception. System.Exception: Missing program argument.", exception.Message);
        }

        [Fact]
        public async Task GivenExampleAppRequiringArguments_WhenHaveArguments_ThenSucceeds()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var path = Path.Combine(location,  @"..\..\..\..\..\ExampleApp\bin\Debug\netcoreapp3.1\ExampleApp.exe");
            var result = await AsyncTools.RunProgramAsync(path, "argument");

            Assert.Equal("Hello argument!\r\nGoodbye.\r\n", result);
        }
    }
}

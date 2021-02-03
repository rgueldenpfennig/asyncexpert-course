using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using ThreadPoolExercises.Core;
using Xunit;
using Xunit.Abstractions;

namespace ThreadPoolExercises.Tests
{
    public class ExecuteOnThreadTests
    {
        [Fact]
        public void RunningOnDifferentThreadTest()
        {
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var testThreadId = 0;
            bool? testThreadIsFromPool = null;

            ThreadingHelpers.ExecuteOnThread(
                () =>
                {
                    Thread.Sleep(100);
                    testThreadId = Thread.CurrentThread.ManagedThreadId;
                    testThreadIsFromPool = Thread.CurrentThread.IsThreadPoolThread;
                }, 1);

            Assert.NotEqual(0, testThreadId);
            Assert.NotEqual(mainThreadId, testThreadId);
            Assert.NotNull(testThreadIsFromPool);
            Assert.False(testThreadIsFromPool);
        }

        [Fact]
        public void ImmediateCancellationTest()
        {
            var errorActionMock = new Mock<Action<Exception>>();
            
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            ThreadingHelpers.ExecuteOnThread(
                () => Thread.Sleep(100),
                3,
                cts.Token,
                errorAction: errorActionMock.Object);

            errorActionMock.Verify(m 
                => m(It.IsAny<OperationCanceledException>()), Times.Once);
        }

        [Fact]
        public void TimeoutCancellationTest()
        {
            var errorActionMock = new Mock<Action<Exception>>();

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(200);

            ThreadingHelpers.ExecuteOnThread(
                () => Thread.Sleep(100),
                10,
                cts.Token,
                errorAction: errorActionMock.Object);

            errorActionMock.Verify(m 
                => m(It.IsAny<OperationCanceledException>()), Times.Once);
        }

        [Fact]
        public void ExceptionHandlingWhenErrorActionProvidedTest()
        {
            var errorActionMock = new Mock<Action<Exception>>();

            ThreadingHelpers.ExecuteOnThread(
                () => throw new NullReferenceException(), 
                10, 
                errorAction: errorActionMock.Object);

            errorActionMock.Verify(m 
                => m(It.IsAny<NullReferenceException>()), Times.Once);
        }

        [Fact]
        public void ExceptionHandlingWhenErrorActionMissing()
        {
            ThreadingHelpers.ExecuteOnThread(
                () => throw new NullReferenceException(),
                10);
            
            // This should simply not kill the test runner.
        }

        [Fact]
        public void ReferencePassingTest()
        {
            var data = new DataBag();

            ThreadingHelpers.ExecuteOnThread(
                () => data.X++, 
                10);

            Assert.Equal(10, data.X);
        }

        class DataBag
        {
            public int X;
        }
    }
}

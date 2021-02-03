using System;
using System.Threading;

namespace ThreadPoolExercises.Core
{
    public class ThreadingHelpers
    {
        public static void ExecuteOnThread(Action action, int repeats, CancellationToken token = default, Action<Exception>? errorAction = null)
        {
            // * Create a thread and execute there `action` given number of `repeats` - waiting for the execution!
            //   HINT: you may use `Join` to wait until created Thread finishes
            // * In a loop, check whether `token` is not cancelled
            // * If an `action` throws and exception (or token has been cancelled) - `errorAction` should be invoked (if provided)

            var thread = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < repeats; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        action();
                    }
                }
                catch (Exception ex)
                {
                    errorAction?.Invoke(ex);
                }

            });
            thread.IsBackground = true;
            thread.Start();
            thread.Join();
        }

        public static void ExecuteOnThreadPoolUnsafe(Action action, int repeats, CancellationToken token = default, Action<Exception>? errorAction = null)
        {
            // * Queue work item to a thread pool that executes `action` given number of `repeats` - waiting for the execution!
            //   HINT: you may use `AutoResetEvent` to wait until the queued work item finishes
            // * In a loop, check whether `token` is not cancelled
            // * If an `action` throws and exception (or token has been cancelled) - `errorAction` should be invoked (if provided)
            using (var are = new AutoResetEvent(false))
            {
                var state = new ValueTuple<Action, CancellationToken, Action<Exception>?, int, AutoResetEvent>(action, token, errorAction, repeats, are);

                ThreadPool.UnsafeQueueUserWorkItem(CallBack, state);

                are.WaitOne();
            }
        }
        
        public static void ExecuteOnThreadPool(Action action, int repeats, CancellationToken token = default, Action<Exception>? errorAction = null)
        {
            // * Queue work item to a thread pool that executes `action` given number of `repeats` - waiting for the execution!
            //   HINT: you may use `AutoResetEvent` to wait until the queued work item finishes
            // * In a loop, check whether `token` is not cancelled
            // * If an `action` throws and exception (or token has been cancelled) - `errorAction` should be invoked (if provided)
            using (var are = new AutoResetEvent(false))
            {
                var state = new ValueTuple<Action, CancellationToken, Action<Exception>?, int, AutoResetEvent>(action, token, errorAction, repeats, are);
        
                ThreadPool.QueueUserWorkItem(CallBack, state);
        
                are.WaitOne();
            }
        }

        private static void CallBack(object? state)
        {
            var (action, token, errorAction, repeats, autoResetEvent) = (ValueTuple<Action, CancellationToken, Action<Exception>?, int, AutoResetEvent>) state!;
            try
            {
                for (int i = 0; i < repeats; i++)
                {
                    token.ThrowIfCancellationRequested();
                    action();
                }
            }
            catch (Exception ex)
            {
                errorAction?.Invoke(ex);
            }
            finally
            {
                autoResetEvent.Set();
            }
        }
    }
}

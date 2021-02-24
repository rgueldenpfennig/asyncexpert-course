using System;
using System.Runtime.CompilerServices;

namespace AwaitableExercises.Core
{
    public static class BoolExtensions
    {
        public static BoolAwaiter GetAwaiter(this bool arg)
        {
            return new BoolAwaiter(arg);
        }
    }

    public class BoolAwaiter : INotifyCompletion
    {
        private readonly bool _arg;

        public BoolAwaiter(bool arg)
        {
            _arg = arg;
        }

        public bool IsCompleted => true;
        public bool GetResult() => _arg;
        public void OnCompleted(Action continuation) { }
    }
}

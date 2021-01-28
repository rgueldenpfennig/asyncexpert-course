using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Synchronization.Core
{
    /*
     * Implement very simple wrapper around Mutex or Semaphore (remember both implement WaitHandle) to
     * provide a exclusive region created by `using` clause.
     *
     * Created region may be system-wide or not, depending on the constructor parameter.
     *
     * Any try to get a second systemwide scope should throw an `System.InvalidOperationException` with `Unable to get a global lock {name}.`
     */
    public class NamedExclusiveScope : IDisposable
    {
        private bool _isDisposed;
        private readonly string _name;
        private readonly Semaphore _semaphore;

        private static readonly ConcurrentDictionary<string, NamedExclusiveScope> _instances =
            new ConcurrentDictionary<string, NamedExclusiveScope>();

        private int _count = 0;

        public NamedExclusiveScope(string name, bool isSystemWide)
        {
            _name = name;

            if (isSystemWide)
            {
                _semaphore = new Semaphore(1, 1, name, out bool createdNew);
                if (!createdNew) throw new InvalidOperationException($"Unable to get a global lock {name}.");

                _semaphore.WaitOne();
            }
            else
            {
                if (_instances.TryGetValue(name, out var instance))
                {
                    Interlocked.Increment(ref instance._count);
                    instance._semaphore.WaitOne();
                }
                else
                {
                    _semaphore = new Semaphore(1, 1);
                    instance = _instances.GetOrAdd(name, this);

                    if (ReferenceEquals(this, instance))
                    {
                        Interlocked.Increment(ref _count);
                        _semaphore.WaitOne();
                    }
                    else
                    {
                        _semaphore.Dispose();

                        Interlocked.Increment(ref instance._count);
                        instance._semaphore.WaitOne();
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_instances.TryGetValue(_name, out var instance))
                    {
                        Interlocked.Decrement(ref instance._count);
                        instance._semaphore.Release();
                        
                        if (Interlocked.CompareExchange(ref instance._count, 0, 0) == 0)
                        {
                            instance._semaphore.Dispose();
                        }
                    }
                    else
                    {
                        _semaphore.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _isDisposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~NamedExclusiveScope()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

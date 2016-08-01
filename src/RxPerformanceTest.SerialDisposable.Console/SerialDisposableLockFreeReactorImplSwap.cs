using System;
using System.Reactive.Disposables;
using System.Threading;

namespace RxPerformanceTest.SerialDisposable.Console
{
    public sealed class SerialDisposableLockFreeReactorImplSwap : ICancelable
    {
        private static readonly IDisposable DISPOSED = new Disposed();
        private IDisposable _current;

        /// <summary>
        /// Gets or sets the underlying disposable.
        /// </summary>
        /// <remarks>If the SerialDisposable has already been disposed, assignment to this property causes immediate disposal of the given disposable object. Assigning this property disposes the previous disposable object.</remarks>
        public IDisposable Disposable
        {
            get { return Volatile.Read(ref _current); }
            set { Set(ref _current, value); }
        }

        //Stolen from https://github.com/reactor/reactor-core-dotnet/blob/master/Reactor.Core/util/DisposableHelper.cs
        public void Dispose()
        {
            var c = Volatile.Read(ref _current);
            if (c != DISPOSED)
            {
                c = Interlocked.Exchange(ref _current, DISPOSED);
                if (c != DISPOSED)
                {
                    c?.Dispose();

                }
            }
        }

        public bool IsDisposed => ReferenceEquals(Volatile.Read(ref _current), DISPOSED);

        //Stolen from https://github.com/reactor/reactor-core-dotnet/blob/master/Reactor.Core/util/DisposableHelper.cs
        /// <summary>
        /// Atomically sets the contents of the target field, disposing the old
        /// valuue or disposes thenew IDisposable if the field contains the 
        /// disposed instance.
        /// </summary>
        /// <param name="d">The target field.</param>
        /// <param name="a">The new IDisposable instance</param>
        /// <returns>True if successful, false if the target contains the disposed instance</returns>
        private static bool Set(ref IDisposable d, IDisposable a)
        {
            var c = Volatile.Read(ref d);
            for (;;)
            {
                if (c == DISPOSED)
                {
                    a?.Dispose();
                    return false;
                }
                var b = Interlocked.CompareExchange(ref d, a, c);
                if (b == c)
                {
                    c?.Dispose();
                    return true;
                }
                c = b;
            }
        }

        /// <summary>
        /// The class representing a disposed IDisposable
        /// </summary>
        sealed class Disposed : IDisposable
        {
            public void Dispose()
            {
                // deliberately ignored
            }
        }
    }
}
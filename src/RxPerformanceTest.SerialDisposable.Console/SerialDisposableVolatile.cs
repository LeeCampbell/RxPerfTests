using System;
using System.Reactive.Disposables;

namespace RxPerformanceTest.SerialDisposable.Console
{
    //Like the Thread unsafe version, this shows the just using the volatile keyword will not solve any real problem. -LC
    public sealed class SerialDisposableVolatile : ICancelable
    {
        private volatile IDisposable _current;

        public SerialDisposableVolatile()
        {
        }

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets or sets the underlying disposable.
        /// </summary>
        /// <remarks>If the SerialDisposable has already been disposed, assignment to this property causes immediate disposal of the given disposable object. Assigning this property disposes the previous disposable object.</remarks>
        public IDisposable Disposable
        {
            get
            {
                return _current;
            }

            set
            {
                if (IsDisposed)
                {
                    value?.Dispose();
                }
                else
                {
                    var previous = _current;
                    _current = value;
                    previous?.Dispose();
                }

            }
        }

        /// <summary>
        /// Disposes the underlying disposable as well as all future replacements.
        /// </summary>
        public void Dispose()
        {
            IsDisposed = true;
            _current?.Dispose();
        }
    }
}
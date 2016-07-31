using System;
using System.Reactive.Disposables;
using System.Threading;

namespace RxPerformanceTest.SerialDisposable.Console
{
    public sealed class SerialDisposableImplSwap : ICancelable
    {
        private ISerialCancelable _current = new ActiveSerialCancelable();

        public SerialDisposableImplSwap()
        { }

        public bool IsDisposed => _current.IsDisposed;

        /// <summary>
        /// Gets or sets the underlying disposable.
        /// </summary>
        /// <remarks>If the SerialDisposable has already been disposed, assignment to this property causes immediate disposal of the given disposable object. Assigning this property disposes the previous disposable object.</remarks>
        public IDisposable Disposable
        {
            get { return _current.Disposable; }
            set { _current.Disposable = value; }
        }

        /// <summary>
        /// Disposes the underlying disposable as well as all future replacements.
        /// </summary>
        public void Dispose()
        {
            var old = Interlocked.Exchange(ref _current, DisposedSerialCancelable.Instance);
            old.Dispose();
        }

        private interface ISerialCancelable : ICancelable
        {
            IDisposable Disposable { get; set; }
        }

        private sealed class DisposedSerialCancelable : ISerialCancelable
        {
            public static readonly DisposedSerialCancelable Instance = new DisposedSerialCancelable();

            private DisposedSerialCancelable()
            { }
            public bool IsDisposed => true;

            public IDisposable Disposable
            {
                get { return null; }
                set { value?.Dispose(); }
            }

            public void Dispose()
            { }
        }
        private sealed class ActiveSerialCancelable : ISerialCancelable
        {
            private IDisposable _disposable;

            public bool IsDisposed => false;

            public IDisposable Disposable
            {
                get { return _disposable; }
                set
                {
                    var old = Interlocked.Exchange(ref _disposable, value);
                    old?.Dispose();
                }
            }

            public void Dispose()
            {
                _disposable?.Dispose();
            }
        }
    }
}
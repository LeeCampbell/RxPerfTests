using System;
using System.Reactive.Disposables;
using System.Threading;

namespace RxPerformanceTest.SerialDisposable.Console
{
    public sealed class SerialDisposableLockedImplSwap : ICancelable
    {
        private readonly object _gate = new object();
        private ISerialCancelable _current = new ActiveSerialCancelable();

        public SerialDisposableLockedImplSwap()
        {
        }

        public bool IsDisposed => Volatile.Read(ref _current).IsDisposed;

        /// <summary>
        /// Gets or sets the underlying disposable.
        /// </summary>
        /// <remarks>If the SerialDisposable has already been disposed, assignment to this property causes immediate disposal of the given disposable object. Assigning this property disposes the previous disposable object.</remarks>
        public IDisposable Disposable
        {
            get { return Volatile.Read(ref _current).Disposable; }
            set
            {
                lock (_gate)
                {
                    _current.Disposable = value;
                }
            }
        }

        /// <summary>
        /// Disposes the underlying disposable as well as all future replacements.
        /// </summary>
        public void Dispose()
        {
            lock (_gate)
            {
                //var old = _current;
                //_current = DisposedSerialCancelable.Instance;
                //old.Dispose();
                _current.Dispose();
                _current = DisposedSerialCancelable.Instance;
            }
        }

        private interface ISerialCancelable : ICancelable
        {
            IDisposable Disposable { get; set; }
        }

        private sealed class DisposedSerialCancelable : ISerialCancelable
        {
            public static readonly DisposedSerialCancelable Instance = new DisposedSerialCancelable();

            private DisposedSerialCancelable()
            {
            }

            public bool IsDisposed => true;

            public IDisposable Disposable
            {
                get { return null; }
                set { value?.Dispose(); }
            }

            public void Dispose()
            {
            }
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
                    //var old = _disposable;
                    //_disposable = value;
                    //old?.Dispose();
                    _disposable?.Dispose();
                    _disposable = value;
                }
            }

            public void Dispose()
            {
                _disposable?.Dispose();
            }
        }
    }
}
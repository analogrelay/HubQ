using System;

namespace Nux
{
    internal class Disposable : IDisposable
    {
        private readonly Action _action;

        public Disposable(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }

        public static IDisposable Create(Action action) => new Disposable(action);
    }
}
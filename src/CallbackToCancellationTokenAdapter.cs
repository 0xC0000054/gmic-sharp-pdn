////////////////////////////////////////////////////////////////////////
//
// This file is part of gmic-sharp-pdn, a library that extends
// gmic-sharp for use with Paint.NET Effect plugins.
//
// Copyright (c) 2020, 2021 Nicholas Hayes
//
// This file is licensed under the MIT License.
// See LICENSE.txt for complete licensing and attribution information.
//
////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using System;
using System.Threading;

namespace GmicSharpPdn
{
    internal sealed class CallbackToCancellationTokenAdapter : IDisposable
    {
        private readonly Func<bool> cancellationCallback;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Timer timer;
        private object timerCookie;
        private int inTimerCallback;
        private bool disposed;

        public CallbackToCancellationTokenAdapter(Func<bool> cancellationCallback)
        {
            if (cancellationCallback is null)
            {
                ExceptionUtil.ThrowArgumentNullException(nameof(cancellationCallback));
            }

            this.cancellationCallback = cancellationCallback;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.timerCookie = new object();
            this.timer = new Timer(OnTimerTick, this.timerCookie, 1000, 500);
            this.disposed = false;
        }

        public CancellationToken Token
        {
            get
            {
                if (this.disposed)
                {
                    ExceptionUtil.ThrowObjectDisposedException(nameof(CallbackToCancellationTokenAdapter));
                }

                return this.cancellationTokenSource.Token;
            }
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;

                this.timerCookie = null;
                this.timer?.Dispose();
                this.cancellationTokenSource?.Dispose();
            }
        }

        private void OnTimerTick(object state)
        {
            // The timer has been stopped.
            if (state != this.timerCookie)
            {
                return;
            }

            // Detect reentrant calls to this method.
            if (Interlocked.CompareExchange(ref this.inTimerCallback, 1, 0) != 0)
            {
                return;
            }

            if (this.cancellationCallback.Invoke())
            {
                try
                {
                    this.cancellationTokenSource?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Ignore it
                }
            }

            this.inTimerCallback = 0;
        }
    }
}

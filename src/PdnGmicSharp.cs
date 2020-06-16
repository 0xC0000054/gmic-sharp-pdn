////////////////////////////////////////////////////////////////////////
//
// This file is part of gmic-sharp-pdn, a library that extends
// gmic-sharp for use with Paint.NET Effect plugins.
//
// Copyright (c) 2020 Nicholas Hayes
//
// This file is licensed under the MIT License.
// See LICENSE.txt for complete licensing and attribution information.
//
////////////////////////////////////////////////////////////////////////

using GmicSharp;
using PaintDotNet;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GmicSharpPdn
{
    /// <summary>
    /// The class used to run G'MIC commands.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    /// <seealso cref="Disposable" />
    public sealed class PdnGmicSharp : Disposable
    {
        private Gmic<PdnGmicBitmap> gmic;
        private OutputImageCollection<PdnGmicBitmap> outputImages;
        private ManualResetEventSlim gmicDoneResetEvent;
        private Timer timer;
        private object timerCookie;
        private int inTimerCallback;
        private Func<bool> cancelPollFn;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdnGmicSharp"/> class.
        /// </summary>
        /// <exception cref="GmicException">Unable to create the G'MIC image list.</exception>
        public PdnGmicSharp()
        {
            this.gmic = new Gmic<PdnGmicBitmap>(PdnGmicBitmapOutputFactory.Instance)
            {
                HostName = "paintdotnet"
            };
            this.gmic.RunGmicCompleted += Gmic_RunGmicCompleted;
        }

        /// <summary>
        /// Gets the output images.
        /// </summary>
        /// <value>
        /// The output images.
        /// </value>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public IReadOnlyList<PdnGmicBitmap> OutputImages
        {
            get
            {
                VerifyNotDisposed();

                return this.outputImages;
            }
        }

        /// <summary>
        /// Gets a value indicating whether G'MIC was canceled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if G'MIC was canceled; otherwise, <c>false</c>.
        /// </value>
        public bool Canceled { get; private set; }

        /// <summary>
        /// Gets the error that occurred when running G'MIC.
        /// </summary>
        /// <value>
        /// The error that occurred when running G'MIC.
        /// </value>
        public Exception Error { get; private set; }

        /// <summary>
        /// Adds the input image.
        /// </summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bitmap"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        /// <exception cref="OutOfMemoryException">Insufficient memory to add the image.</exception>
        public void AddInputImage(PdnGmicBitmap bitmap)
        {
            this.gmic.AddInputImage(bitmap);
        }

        /// <summary>
        /// Adds the input image.
        /// </summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <param name="name">The bitmap name.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bitmap"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        /// <exception cref="OutOfMemoryException">Insufficient memory to add the image.</exception>
        public void AddInputImage(PdnGmicBitmap bitmap, string name)
        {
            VerifyNotDisposed();

            this.gmic.AddInputImage(bitmap, name);
        }

        /// <summary>
        /// Runs the specified G'MIC command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public void RunGmic(string command)
        {
            RunGmic(command, null);
        }

        /// <summary>
        /// Runs the specified G'MIC command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="cancelPollFn">The cancel poll function.</param>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public void RunGmic(string command, Func<bool> cancelPollFn)
        {
            VerifyNotDisposed();

            this.gmicDoneResetEvent?.Dispose();
            this.gmicDoneResetEvent = new ManualResetEventSlim();
            this.Canceled = false;
            this.Error = null;

            try
            {
                // Cleanup any previous cancellation poll fields.
                // Normally this would be done by the Gmic_GmicDone method, but Gmic_GmicDone
                // will never be called if this method throws an exception.
                StopCancellationTimer();

                if (this.outputImages != null)
                {
                    this.outputImages.Dispose();
                    this.outputImages = null;
                }

                if (cancelPollFn != null)
                {
                    if (cancelPollFn())
                    {
                        this.Canceled = true;
                        return;
                    }

                    this.cancelPollFn = cancelPollFn;
                    this.timerCookie = new object();
                    this.timer = new Timer(OnTimerTick, this.timerCookie, 1000, 500);
                }

                this.gmic.RunGmicAsync(command);

                // Wait until G'MIC finishes.
                this.gmicDoneResetEvent.Wait();
            }
            catch (Exception ex)
            {
                this.Error = ex;
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.timerCookie = null;
                this.cancelPollFn = null;

                if (this.gmic != null)
                {
                    this.gmic.Dispose();
                    this.gmic = null;
                }

                if (this.gmicDoneResetEvent != null)
                {
                    this.gmicDoneResetEvent.Dispose();
                    this.gmicDoneResetEvent = null;
                }

                if (this.outputImages != null)
                {
                    this.outputImages.Dispose();
                    this.outputImages = null;
                }

                if (this.timer != null)
                {
                    this.timer.Dispose();
                    this.timer = null;
                }
            }

            base.Dispose(disposing);
        }

        private void Gmic_RunGmicCompleted(object sender, RunGmicCompletedEventArgs<PdnGmicBitmap> e)
        {
            StopCancellationTimer();

            this.Canceled = e.Cancelled;
            this.Error = e.Error;
            this.outputImages = e.OutputImages;

            this.gmicDoneResetEvent?.Set();
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

            if (this.cancelPollFn?.Invoke() ?? false)
            {
                this.gmic.RunGmicAsyncCancel();
            }

            this.inTimerCallback = 0;
        }

        private void StopCancellationTimer()
        {
            this.timerCookie = null;
            this.cancelPollFn = null;

            if (this.timer != null)
            {
                this.timer.Dispose();
                this.timer = null;
            }
        }

        private void VerifyNotDisposed()
        {
            if (this.IsDisposed)
            {
                ExceptionUtil.ThrowObjectDisposedException(nameof(PdnGmicSharp));
            }
        }
    }
}

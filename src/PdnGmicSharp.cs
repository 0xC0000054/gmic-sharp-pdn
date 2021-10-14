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

using GmicSharp;
using PaintDotNet;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        /// Adds the input image.
        /// </summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bitmap"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        /// <exception cref="OutOfMemoryException">Insufficient memory to add the image.</exception>
        public void AddInputImage(PdnGmicBitmap bitmap)
        {
            VerifyNotDisposed();

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

            this.Canceled = false;
            this.Error = null;

            CallbackToCancellationTokenAdapter cancellationAdapter = null;
            try
            {
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

                    cancellationAdapter = new CallbackToCancellationTokenAdapter(cancelPollFn);
                }

                CancellationToken cancellationToken = cancellationAdapter?.Token ?? CancellationToken.None;

                Task<OutputImageCollection<PdnGmicBitmap>> task = this.gmic.RunGmicTaskAsync(command, cancellationToken);

                // Using WaitAny allows any exception that occurred
                // during the task execution to be examined.
                Task.WaitAny(task);

                if (task.IsFaulted)
                {
                    this.Error = task.Exception.GetBaseException();
                }
                else if (task.IsCanceled)
                {
                    this.Canceled = true;
                }
                else
                {
                    this.outputImages = task.Result;
                }
            }
            catch (Exception ex)
            {
                this.Error = ex;
            }
            finally
            {
                cancellationAdapter?.Dispose();
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
                if (this.gmic != null)
                {
                    this.gmic.Dispose();
                    this.gmic = null;
                }

                if (this.outputImages != null)
                {
                    this.outputImages.Dispose();
                    this.outputImages = null;
                }
            }

            base.Dispose(disposing);
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

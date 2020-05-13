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

namespace GmicSharpPdn
{
    /// <summary>
    /// A <see cref="GmicBitmap" /> that uses the Paint.NET <see cref="PaintDotNet.Surface" /> class.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    /// <seealso cref="GmicBitmap" />
    public sealed class PdnGmicBitmap : GmicBitmap
    {
#pragma warning disable IDE0032 // Use auto property
        private Surface surface;
#pragma warning restore IDE0032 // Use auto property
        private readonly bool hasTransparency;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdnGmicBitmap"/> class.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <exception cref="ArgumentNullException"><paramref name="surface"/> is null.</exception>
        public PdnGmicBitmap(Surface surface) : this(surface, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdnGmicBitmap"/> class.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="takeOwnership">
        /// If <c>true</c> the <see cref="PdnGmicBitmap"/> will take ownership of the surface,
        /// if <c>false</c> the surface will be cloned.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="surface"/> is null.</exception>
        public PdnGmicBitmap(Surface surface, bool takeOwnership)
        {
            if (surface is null)
            {
                ExceptionUtil.ThrowArgumentNullException(nameof(surface));
            }

            if (takeOwnership)
            {
                this.surface = surface;
            }
            else
            {
                this.surface = surface.Clone();
            }
            this.hasTransparency = HasTransparency(surface);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdnGmicBitmap"/> class.
        /// </summary>
        /// <param name="width">The surface width.</param>
        /// <param name="height">The surface height.</param>
        internal PdnGmicBitmap(int width, int height)
        {
            this.surface = new Surface(width, height);
            this.hasTransparency = true;
        }

        /// <summary>
        /// Gets the surface.
        /// </summary>
        /// <value>
        /// The surface.
        /// </value>
        public Surface Surface
        {
            get
            {
                VerifyNotDisposed();

                return this.surface;
            }
        }

        /// <summary>
        /// Gets the bitmap width.
        /// </summary>
        /// <value>
        /// The bitmap width.
        /// </value>
        public override int Width => this.surface.Width;

        /// <summary>
        /// Gets the bitmap height.
        /// </summary>
        /// <value>
        /// The bitmap height.
        /// </value>
        public override int Height => this.surface.Height;

        /// <summary>
        /// Gets the G'MIC pixel format.
        /// </summary>
        /// <returns>
        /// The G'MIC pixel format.
        /// </returns>
        public override GmicPixelFormat GetGmicPixelFormat()
        {
            return this.hasTransparency ? GmicPixelFormat.Bgra32 : GmicPixelFormat.Bgr32;
        }

        /// <summary>
        /// Locks the bitmap in memory for unsafe access to the pixel data.
        /// </summary>
        /// <returns>
        /// A <see cref="T:GmicSharp.GmicBitmapLock" /> instance.
        /// </returns>
        public override GmicBitmapLock Lock()
        {
            VerifyNotDisposed();

            return new GmicBitmapLock(this.surface.Scan0.Pointer, this.surface.Stride);
        }

        /// <summary>
        /// Unlocks the bitmap.
        /// </summary>
        public override void Unlock()
        {
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.surface != null)
                {
                    this.surface.Dispose();
                    this.surface = null;
                }
            }

            base.Dispose(disposing);
        }

        private static unsafe bool HasTransparency(Surface surface)
        {
            for (int y = 0; y < surface.Height; y++)
            {
                ColorBgra* ptr = surface.GetRowAddressUnchecked(y);
                ColorBgra* ptrEnd = ptr + surface.Width;

                while (ptr < ptrEnd)
                {
                    if (ptr->A < 255)
                    {
                        return true;
                    }

                    ptr++;
                }
            }

            return false;
        }

        private void VerifyNotDisposed()
        {
            if (this.surface == null)
            {
                ExceptionUtil.ThrowObjectDisposedException(nameof(PdnGmicBitmap));
            }
        }
    }
}

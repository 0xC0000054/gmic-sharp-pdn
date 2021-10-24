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
using System.Drawing;

namespace GmicSharpPdn
{
    /// <summary>
    /// A <see cref="GmicBitmap" /> that uses the Paint.NET <see cref="PaintDotNet.Surface" /> class.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    /// <seealso cref="GmicBitmap" />
    public sealed class PdnGmicBitmap : GmicBitmap
    {
        private Surface surface;

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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdnGmicBitmap"/> class.
        /// </summary>
        /// <param name="width">The surface width.</param>
        /// <param name="height">The surface height.</param>
        internal PdnGmicBitmap(int width, int height)
        {
            this.surface = new Surface(width, height);
        }

        private PdnGmicBitmap(PdnGmicBitmap cloneMe) => this.surface = cloneMe.surface.Clone();

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
        /// Creates a <see cref="PdnGmicBitmap"/> from the pixels in the selected area.
        /// </summary>
        /// <param name="effectEnvironmentParameters">The effect environment parameters.</param>
        /// <returns>A <see cref="PdnGmicBitmap"/> containing the pixels in the selected area.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="effectEnvironmentParameters"/> is null.
        /// </exception>
        public static PdnGmicBitmap FromSelection(PaintDotNet.Effects.EffectEnvironmentParameters effectEnvironmentParameters)
        {
            if (effectEnvironmentParameters is null)
            {
                ExceptionUtil.ThrowArgumentNullException(nameof(effectEnvironmentParameters));
            }

            Surface sourceSurface = effectEnvironmentParameters.SourceSurface;
            PdnRegion selection = effectEnvironmentParameters.GetSelectionAsPdnRegion();

            Rectangle selectionBounds = selection.GetBoundsInt();

            PdnGmicBitmap bitmap = new(selectionBounds.Width, selectionBounds.Height);
            bitmap.surface.CopySurface(sourceSurface, selection);

            return bitmap;
        }

        /// <summary>
        /// Gets the G'MIC pixel format.
        /// </summary>
        /// <returns>
        /// The G'MIC pixel format.
        /// </returns>
        public override GmicPixelFormat GetGmicPixelFormat()
        {
            AnalyzeSurfaceResult result = AnalyzeSurface();
            GmicPixelFormat gmicPixelFormat;

            if (result.HasTransparency)
            {
                gmicPixelFormat = result.IsGrayscale ? GmicPixelFormat.GrayAlpha16 : GmicPixelFormat.Rgba32;
            }
            else
            {
                gmicPixelFormat = result.IsGrayscale ? GmicPixelFormat.Gray8 : GmicPixelFormat.Rgb24;
            }

            return gmicPixelFormat;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A clone of this instance.</returns>
        internal PdnGmicBitmap Clone()
        {
            VerifyNotDisposed();

            return new PdnGmicBitmap(this);
        }

        /// <summary>
        /// Copies the pixel data from a G'MIC image that uses a gray-scale format into this instance.
        /// </summary>
        /// <param name="grayPlane">The gray plane.</param>
        /// <param name="planeStride">The plane stride.</param>
        protected override unsafe void CopyFromGmicImageGray(float* grayPlane, int planeStride)
        {
            for (int y = 0; y < this.surface.Height; y++)
            {
                float* src = grayPlane + (y * planeStride);
                ColorBgra* dst = this.surface.GetRowPointerUnchecked(y);

                for (int x = 0; x < this.surface.Width; x++)
                {
                    dst->R = dst->G = dst->B = GmicFloatToByte(*src);
                    dst->A = 255;

                    src++;
                    dst++;
                }
            }
        }

        /// <summary>
        /// Copies the pixel data from a G'MIC image that uses a gray-scale with alpha format into this instance.
        /// </summary>
        /// <param name="grayPlane">The gray plane.</param>
        /// <param name="alphaPlane">The alpha plane.</param>
        /// <param name="planeStride">The plane stride.</param>
        protected override unsafe void CopyFromGmicImageGrayAlpha(float* grayPlane, float* alphaPlane, int planeStride)
        {
            for (int y = 0; y < this.surface.Height; y++)
            {
                float* src = grayPlane + (y * planeStride);
                float* srcAlpha = alphaPlane + (y * planeStride);
                ColorBgra* dst = this.surface.GetRowPointerUnchecked(y);

                for (int x = 0; x < this.surface.Width; x++)
                {
                    dst->R = dst->G = dst->B = GmicFloatToByte(*src);
                    dst->A = GmicFloatToByte(*srcAlpha);

                    src++;
                    srcAlpha++;
                    dst++;
                }
            }
        }

        /// <summary>
        /// Copies the pixel data from a G'MIC image that uses a RGB format into this instance.
        /// </summary>
        /// <param name="redPlane">The red plane.</param>
        /// <param name="greenPlane">The green plane.</param>
        /// <param name="bluePlane">The blue plane.</param>
        /// <param name="planeStride">The plane stride.</param>
        protected override unsafe void CopyFromGmicImageRGB(float* redPlane, float* greenPlane, float* bluePlane, int planeStride)
        {
            for (int y = 0; y < this.surface.Height; y++)
            {
                float* srcR = redPlane + (y * planeStride);
                float* srcG = greenPlane + (y * planeStride);
                float* srcB = bluePlane + (y * planeStride);
                ColorBgra* dst = this.surface.GetRowPointerUnchecked(y);

                for (int x = 0; x < this.surface.Width; x++)
                {
                    dst->R = GmicFloatToByte(*srcR);
                    dst->G = GmicFloatToByte(*srcG);
                    dst->B = GmicFloatToByte(*srcB);
                    dst->A = 255;

                    srcR++;
                    srcG++;
                    srcB++;
                    dst++;
                }
            }
        }

        /// <summary>
        /// Copies the pixel data from a G'MIC image that uses a RGBA format into this instance.
        /// </summary>
        /// <param name="redPlane">The red plane.</param>
        /// <param name="greenPlane">The green plane.</param>
        /// <param name="bluePlane">The blue plane.</param>
        /// <param name="alphaPlane">The alpha plane.</param>
        /// <param name="planeStride">The plane stride.</param>
        protected override unsafe void CopyFromGmicImageRGBA(float* redPlane, float* greenPlane, float* bluePlane, float* alphaPlane, int planeStride)
        {
            for (int y = 0; y < this.surface.Height; y++)
            {
                float* srcR = redPlane + (y * planeStride);
                float* srcG = greenPlane + (y * planeStride);
                float* srcB = bluePlane + (y * planeStride);
                float* srcA = alphaPlane + (y * planeStride);
                ColorBgra* dst = this.surface.GetRowPointerUnchecked(y);

                for (int x = 0; x < this.surface.Width; x++)
                {
                    dst->R = GmicFloatToByte(*srcR);
                    dst->G = GmicFloatToByte(*srcG);
                    dst->B = GmicFloatToByte(*srcB);
                    dst->A = GmicFloatToByte(*srcA);

                    srcR++;
                    srcG++;
                    srcB++;
                    srcA++;
                    dst++;
                }
            }
        }

        /// <summary>
        /// Copies the pixel data from this instance into a G'MIC image that uses a gray-scale format.
        /// </summary>
        /// <param name="grayPlane">The gray plane.</param>
        /// <param name="planeStride">The plane stride.</param>
        protected override unsafe void CopyToGmicImageGray(float* grayPlane, int planeStride)
        {
            for (int y = 0; y < this.surface.Height; y++)
            {
                ColorBgra* src = this.surface.GetRowPointerUnchecked(y);
                float* dstGray = grayPlane + (y * planeStride);

                for (int x = 0; x < this.surface.Width; x++)
                {
                    *dstGray = ByteToGmicFloat(src->R);

                    dstGray++;
                    src++;
                }
            }
        }

        /// <summary>
        /// Copies the pixel data from this instance into a G'MIC image that uses a gray-scale with alpha format.
        /// </summary>
        /// <param name="grayPlane">The gray plane.</param>
        /// <param name="alphaPlane">The alpha plane.</param>
        /// <param name="planeStride">The plane stride.</param>
        protected override unsafe void CopyToGmicImageGrayAlpha(float* grayPlane, float* alphaPlane, int planeStride)
        {
            for (int y = 0; y < this.surface.Height; y++)
            {
                ColorBgra* src = this.surface.GetRowPointerUnchecked(y);
                float* dstGray = grayPlane + (y * planeStride);
                float* dstAlpha = alphaPlane + (y * planeStride);

                for (int x = 0; x < this.surface.Width; x++)
                {
                    *dstGray = ByteToGmicFloat(src->R);
                    *dstAlpha = ByteToGmicFloat(src->A);

                    dstGray++;
                    dstAlpha++;
                    src++;
                }
            }
        }

        /// <summary>
        /// Copies the pixel data from this instance into a G'MIC image that uses a RGB format.
        /// </summary>
        /// <param name="redPlane">The red plane.</param>
        /// <param name="greenPlane">The green plane.</param>
        /// <param name="bluePlane">The blue plane.</param>
        /// <param name="planeStride">The plane stride.</param>
        protected override unsafe void CopyToGmicImageRGB(float* redPlane, float* greenPlane, float* bluePlane, int planeStride)
        {
            for (int y = 0; y < this.surface.Height; y++)
            {
                ColorBgra* src = this.surface.GetRowPointerUnchecked(y);
                float* dstR = redPlane + (y * planeStride);
                float* dstG = greenPlane + (y * planeStride);
                float* dstB = bluePlane + (y * planeStride);

                for (int x = 0; x < this.surface.Width; x++)
                {
                    *dstR = ByteToGmicFloat(src->R);
                    *dstG = ByteToGmicFloat(src->G);
                    *dstB = ByteToGmicFloat(src->B);

                    dstR++;
                    dstG++;
                    dstB++;
                    src++;
                }
            }
        }

        /// <summary>
        /// Copies the pixel data from this instance into a G'MIC image that uses a RGBA format.
        /// </summary>
        /// <param name="redPlane">The red plane.</param>
        /// <param name="greenPlane">The green plane.</param>
        /// <param name="bluePlane">The blue plane.</param>
        /// <param name="alphaPlane">The alpha plane.</param>
        /// <param name="planeStride">The plane stride.</param>
        protected override unsafe void CopyToGmicImageRGBA(float* redPlane, float* greenPlane, float* bluePlane, float* alphaPlane, int planeStride)
        {
            for (int y = 0; y < this.surface.Height; y++)
            {
                ColorBgra* src = this.surface.GetRowPointerUnchecked(y);
                float* dstR = redPlane + (y * planeStride);
                float* dstG = greenPlane + (y * planeStride);
                float* dstB = bluePlane + (y * planeStride);
                float* dstA = alphaPlane + (y * planeStride);

                for (int x = 0; x < this.surface.Width; x++)
                {
                    *dstR = ByteToGmicFloat(src->R);
                    *dstG = ByteToGmicFloat(src->G);
                    *dstB = ByteToGmicFloat(src->B);
                    *dstA = ByteToGmicFloat(src->A);


                    dstR++;
                    dstG++;
                    dstB++;
                    dstA++;
                    src++;
                }
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
                if (this.surface != null)
                {
                    this.surface.Dispose();
                    this.surface = null;
                }
            }

            base.Dispose(disposing);
        }

        private unsafe AnalyzeSurfaceResult AnalyzeSurface()
        {
            bool hasTransparency = false;
            bool isGrayscale = true;

            for (int y = 0; y < this.surface.Height; y++)
            {
                ColorBgra* ptr = this.surface.GetRowPointerUnchecked(y);
                ColorBgra* ptrEnd = ptr + this.surface.Width;

                while (ptr < ptrEnd)
                {
                    if (ptr->A < 255)
                    {
                        hasTransparency = true;
                    }

                    if (!(ptr->R == ptr->G && ptr->G == ptr->B))
                    {
                        isGrayscale = false;
                    }

                    ptr++;
                }
            }

            return new AnalyzeSurfaceResult(hasTransparency, isGrayscale);
        }

        private void VerifyNotDisposed()
        {
            if (this.surface == null)
            {
                ExceptionUtil.ThrowObjectDisposedException(nameof(PdnGmicBitmap));
            }
        }

        private readonly struct AnalyzeSurfaceResult
        {
            public AnalyzeSurfaceResult(bool hasTransparency, bool isGrayscale)
            {
                this.HasTransparency = hasTransparency;
                this.IsGrayscale = isGrayscale;
            }

            public bool HasTransparency { get; }

            public bool IsGrayscale { get; }
        }
    }
}

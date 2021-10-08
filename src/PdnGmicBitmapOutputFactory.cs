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

namespace GmicSharpPdn
{
    internal sealed class PdnGmicBitmapOutputFactory : IGmicOutputImageFactory<PdnGmicBitmap>
    {
        private PdnGmicBitmapOutputFactory()
        {
        }

        public static PdnGmicBitmapOutputFactory Instance { get; } = new PdnGmicBitmapOutputFactory();

        public PdnGmicBitmap Create(int width, int height, GmicPixelFormat gmicPixelFormat)
        {
            // The gmicPixelFormat parameter is ignored because
            // Paint.NET Surfaces are always the same format (32-bit BGRA).
            return new PdnGmicBitmap(width, height);
        }
    }
}

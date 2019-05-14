// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright � Andrew Kirillov, 2005-2014
// aforge.net@gmail.com
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Dilatation operator from Mathematical Morphology.
    /// </summary>
    /// 
    /// <remarks><para>The filter assigns maximum value of surrounding pixels to each pixel of
    /// the result image. Surrounding pixels, which should be processed, are specified by
    /// structuring element: 1 - to process the neighbor, -1 - to skip it.</para>
    /// 
    /// <para>The filter especially useful for binary image processing, where it allows to grow
    /// separate objects or join objects.</para>
    /// 
    /// <para>For processing image with 3x3 structuring element, there are different optimizations
    /// available, like <see cref="Dilatation3x3"/> and <see cref="BinaryDilatation3x3"/>.</para>
    /// 
    /// <para>The filter accepts 8 and 16 bpp grayscale images and 24 and 48 bpp
    /// color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// Dilatation filter = new Dilatation( );
    /// // apply the filter
    /// filter.Apply( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample12.png" width="320" height="240" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/dilatation.png" width="320" height="240" />
    /// </remarks>
    /// 
    /// <seealso cref="Erosion"/>
    /// <seealso cref="Closing"/>
    /// <seealso cref="Opening"/>
    /// <seealso cref="Dilatation3x3"/>
    /// <seealso cref="BinaryDilatation3x3"/>
    /// 
    public class Dilatation : BaseUsingCopyPartialFilter
    {
        // structuring element
        private short[,] se = new short[3, 3] { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } };
        private int size = 3;

        // private format translation dictionary
        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>( );

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations
        {
            get { return this.formatTranslations; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dilatation"/> class.
        /// </summary>
        /// 
        /// <remarks><para>Initializes new instance of the <see cref="Dilatation"/> class using
        /// default structuring element - 3x3 structuring element with all elements equal to 1.
        /// </para></remarks>
        /// 
        public Dilatation( )
        {
            // initialize format translation dictionary
            this.formatTranslations[PixelFormat.Format8bppIndexed]    = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format24bppRgb]       = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format16bppGrayScale] = PixelFormat.Format16bppGrayScale;
            this.formatTranslations[PixelFormat.Format48bppRgb]       = PixelFormat.Format48bppRgb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dilatation"/> class.
        /// </summary>
        /// 
        /// <param name="se">Structuring element.</param>
        /// 
        /// <remarks><para>Structuring elemement for the dilatation morphological operator
        /// must be square matrix with odd size in the range of [3, 99].</para></remarks>
        /// 
        /// <exception cref="ArgumentException">Invalid size of structuring element.</exception>
        /// 
        public Dilatation( short[,] se )
            : this( )
        {
            var s = se.GetLength( 0 );

            // check structuring element size
            if ( ( s != se.GetLength( 1 ) ) || ( s < 3 ) || ( s > 99 ) || ( s % 2 == 0 ) )
                throw new ArgumentException( "Invalid size of structuring element." );

            this.se = se;
            this.size = s;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// 
        /// <param name="sourceData">Source image data.</param>
        /// <param name="destinationData">Destination image data.</param>
        /// <param name="rect">Image rectangle for processing by the filter.</param>
        /// 
        protected override unsafe void ProcessFilter( UnmanagedImage sourceData, UnmanagedImage destinationData, Rectangle rect )
        {
            var pixelFormat = sourceData.PixelFormat;

            // processing start and stop X,Y positions
            var startX  = rect.Left;
            var startY  = rect.Top;
            var stopX   = startX + rect.Width;
            var stopY   = startY + rect.Height;

            // structuring element's radius
            var r = this.size >> 1;

            // flag to indicate if at least one pixel for the given structuring element was found
            bool foundSomething;

            if ( ( pixelFormat == PixelFormat.Format8bppIndexed ) || ( pixelFormat == PixelFormat.Format24bppRgb ) )
            {
                var pixelSize = ( pixelFormat == PixelFormat.Format8bppIndexed ) ? 1 : 3;

                var dstStride = destinationData.Stride;
                var srcStride = sourceData.Stride;

                // base pointers
                var baseSrc = (byte*) sourceData.ImageData.ToPointer( );
                var baseDst = (byte*) destinationData.ImageData.ToPointer( );

                // allign pointers by X
                baseSrc += ( startX * pixelSize );
                baseDst += ( startX * pixelSize );

                if ( pixelFormat == PixelFormat.Format8bppIndexed )
                {
                    // grayscale image

                    // compute each line
                    for ( var y = startY; y < stopY; y++ )
                    {
                        var src = baseSrc + y * srcStride;
                        var dst = baseDst + y * dstStride;

                        byte max, v;

                        // loop and array indexes
                        int t, ir, jr, i, j;

                        // for each pixel
                        for ( var x = startX; x < stopX; x++, src++, dst++ )
                        {
                            max = 0;
                            foundSomething = false;

                            // for each structuring element's row
                            for ( i = 0; i < this.size; i++ )
                            {
                                ir = i - r;
                                t = y + ir;

                                // skip row
                                if ( t < startY )
                                    continue;
                                // break
                                if ( t >= stopY )
                                    break;

                                // for each structuring slement's column
                                for ( j = 0; j < this.size; j++ )
                                {
                                    jr = j - r;
                                    t = x + jr;

                                    // skip column
                                    if ( t < startX )
                                        continue;
                                    if ( t < stopX )
                                    {
                                        if ( this.se[i, j] == 1 )
                                        {
                                            foundSomething = true;
                                            // get new MAX value
                                            v = src[ir * srcStride + jr];
                                            if ( v > max )
                                                max = v;
                                        }
                                    }
                                }
                            }
                            // result pixel
                            *dst = ( foundSomething ) ? max : *src;
                        }
                    }
                }
                else
                {
                    // 24 bpp color image

                    // compute each line
                    for ( var y = startY; y < stopY; y++ )
                    {
                        var src = baseSrc + y * srcStride;
                        var dst = baseDst + y * dstStride;

                        byte maxR, maxG, maxB, v;
                        byte* p;

                        // loop and array indexes
                        int t, ir, jr, i, j;

                        // for each pixel
                        for ( var x = startX; x < stopX; x++, src += 3, dst += 3 )
                        {
                            maxR = maxG = maxB = 0;
                            foundSomething = false;

                            // for each structuring element's row
                            for ( i = 0; i < this.size; i++ )
                            {
                                ir = i - r;
                                t = y + ir;

                                // skip row
                                if ( t < startY )
                                    continue;
                                // break
                                if ( t >= stopY )
                                    break;

                                // for each structuring element's column
                                for ( j = 0; j < this.size; j++ )
                                {
                                    jr = j - r;
                                    t = x + jr;

                                    // skip column
                                    if ( t < startX )
                                        continue;
                                    if ( t < stopX )
                                    {
                                        if ( this.se[i, j] == 1 )
                                        {
                                            foundSomething = true;
                                            // get new MAX values
                                            p = &src[ir * srcStride + jr * 3];

                                            // red
                                            v = p[RGB.R];
                                            if ( v > maxR )
                                                maxR = v;

                                            // green
                                            v = p[RGB.G];
                                            if ( v > maxG )
                                                maxG = v;

                                            // blue
                                            v = p[RGB.B];
                                            if ( v > maxB )
                                                maxB = v;
                                        }
                                    }
                                }
                            }
                            // result pixel
                            if ( foundSomething )
                            {
                                dst[RGB.R] = maxR;
                                dst[RGB.G] = maxG;
                                dst[RGB.B] = maxB;
                            }
                            else
                            {
                                dst[RGB.R] = src[RGB.R];
                                dst[RGB.G] = src[RGB.G];
                                dst[RGB.B] = src[RGB.B];
                            }
                        }
                    }
                }
            }
            else
            {
                var pixelSize = ( pixelFormat == PixelFormat.Format16bppGrayScale ) ? 1 : 3;

                var dstStride = destinationData.Stride / 2;
                var srcStride = sourceData.Stride / 2;

                // base pointers
                var baseSrc = (ushort*) sourceData.ImageData.ToPointer( );
                var baseDst = (ushort*) destinationData.ImageData.ToPointer( );

                // allign pointers by X
                baseSrc += ( startX * pixelSize );
                baseDst += ( startX * pixelSize );

                if ( pixelFormat == PixelFormat.Format16bppGrayScale )
                {
                    // 16 bpp grayscale image

                    // compute each line
                    for( var y = startY; y < stopY; y++ )
                    {
                        var src = baseSrc + y * srcStride;
                        var dst = baseDst + y * dstStride;

                        ushort max, v;

                        // loop and array indexes
                        int t, ir, jr, i, j;

                        // for each pixel
                        for ( var x = startX; x < stopX; x++, src++, dst++ )
                        {
                            max = 0;
                            foundSomething = false;

                            // for each structuring element's row
                            for ( i = 0; i < this.size; i++ )
                            {
                                ir = i - r;
                                t = y + ir;

                                // skip row
                                if ( t < startY )
                                    continue;
                                // break
                                if ( t >= stopY )
                                    break;

                                // for each structuring slement's column
                                for ( j = 0; j < this.size; j++ )
                                {
                                    jr = j - r;
                                    t = x + jr;

                                    // skip column
                                    if ( t < startX )
                                        continue;
                                    if ( t < stopX )
                                    {
                                        if ( this.se[i, j] == 1 )
                                        {
                                            foundSomething = true;
                                            // get new MAX value
                                            v = src[ir * srcStride + jr];
                                            if ( v > max )
                                                max = v;
                                        }
                                    }
                                }
                            }
                            // result pixel
                            *dst = ( foundSomething ) ? max : *src;
                        }
                    }
                }
                else
                {
                    // 48 bpp color image

                    // compute each line
                    for( var y = startY; y < stopY; y++ )
                    {
                        var src = baseSrc + y * srcStride;
                        var dst = baseDst + y * dstStride;

                        ushort maxR, maxG, maxB, v;
                        ushort* p;

                        // loop and array indexes
                        int t, ir, jr, i, j;

                        // for each pixel
                        for ( var x = startX; x < stopX; x++, src += 3, dst += 3 )
                        {
                            maxR = maxG = maxB = 0;
                            foundSomething = false;

                            // for each structuring element's row
                            for ( i = 0; i < this.size; i++ )
                            {
                                ir = i - r;
                                t = y + ir;

                                // skip row
                                if ( t < startY )
                                    continue;
                                // break
                                if ( t >= stopY )
                                    break;

                                // for each structuring element's column
                                for ( j = 0; j < this.size; j++ )
                                {
                                    jr = j - r;
                                    t = x + jr;

                                    // skip column
                                    if ( t < startX )
                                        continue;
                                    if ( t < stopX )
                                    {
                                        if ( this.se[i, j] == 1 )
                                        {
                                            foundSomething = true;
                                            // get new MAX values
                                            p = &src[ir * srcStride + jr * 3];

                                            // red
                                            v = p[RGB.R];
                                            if ( v > maxR )
                                                maxR = v;

                                            // green
                                            v = p[RGB.G];
                                            if ( v > maxG )
                                                maxG = v;

                                            // blue
                                            v = p[RGB.B];
                                            if ( v > maxB )
                                                maxB = v;
                                        }
                                    }
                                }
                            }
                            // result pixel
                            if ( foundSomething )
                            {
                                dst[RGB.R] = maxR;
                                dst[RGB.G] = maxG;
                                dst[RGB.B] = maxB;
                            }
                            else
                            {
                                dst[RGB.R] = src[RGB.R];
                                dst[RGB.G] = src[RGB.G];
                                dst[RGB.B] = src[RGB.B];
                            }
                        }
                    }
                }
            }
        }
    }
}

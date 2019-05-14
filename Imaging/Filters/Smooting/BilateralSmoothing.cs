﻿// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2012
// contacts@aforgenet.com
//
// Original implementation by Maxim Saplin, 2012
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using M = System.Math;

    /// <summary>
    /// Bilateral filter implementation - edge preserving smoothing and noise reduction that uses chromatic and spatial factors.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>Bilateral filter conducts "selective" Gaussian smoothing of areas of same color (domains) which removes noise and contrast artifacts
    /// while preserving sharp edges.</para>
    /// 
    /// <para>Two major parameters <see cref="SpatialFactor"/> and <see cref="ColorFactor"/> define the result of the filter. 
    /// By changing these parameters you may achieve either only noise reduction with little change to the
    /// image or get nice looking effect to the entire image.</para>
    ///
    /// <para>Although the filter can use parallel processing large <see cref="KernelSize"/> values
    /// (greater than 25) on high resolution images may decrease speed of processing. Also on high
    /// resolution images small <see cref="KernelSize"/> values (less than 9) may not provide noticeable
    /// results.</para>
    /// 
    /// <para>More details on the algorithm can be found by following this
    /// <a href="http://saplin.blogspot.com/2012/01/bilateral-image-filter-edge-preserving.html">link</a>.</para>
    /// 
    /// <para>The filter accepts 8 bpp grayscale images and 24/32 bpp color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// BilateralSmoothing filter = new BilateralSmoothing( );
    /// filter.KernelSize    = 7;
    /// filter.SpatialFactor = 10;
    /// filter.ColorFactor   = 60;
    /// filter.ColorPower    = 0.5;
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample13.png" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/bilateral.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    public class BilateralSmoothing : BaseUsingCopyPartialFilter
    {
        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>( );

        private const int maxKernelSize = 255;
        private const int colorsCount = 256;

        private int kernelSize = 9;
        private double spatialFactor = 10;
        private double spatialPower = 2;
        private double colorFactor = 50;
        private double colorPower = 2;

        private bool spatialPropertiesChanged = true;
        private bool colorPropertiesChanged = true;

        private bool limitKernelSize = true;
        private bool enableParallelProcessing = false;

        /// <summary>
        /// Specifies if exception must be thrown in the case a large
        /// <see cref="KernelSize">kernel size</see> is used which may lead
        /// to significant performance issues.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>Default value is set to <see langword="true"/>.</para>
        /// </remarks>
        /// 
        public bool LimitKernelSize
        {
            get { return this.limitKernelSize; }
            set { this.limitKernelSize = value; }
        }

        /// <summary>
        /// Enable or not parallel processing on multi-core CPUs.
        /// </summary>
        /// 
        /// <remarks><para>If the property is set to <see langword="true"/>, then this image processing
        /// routine will run in parallel on the systems with multiple core/CPUs. The <see cref="AForge.Parallel.For"/>
        /// is used to make it parallel.</para>
        /// 
        /// <para>Default value is set to <see langword="false"/>.</para>
        /// </remarks>
        /// 
        public bool EnableParallelProcessing
        {
            get { return this.enableParallelProcessing; }
            set { this.enableParallelProcessing = value; }
        }

        /// <summary>
        /// Size of a square for limiting surrounding pixels that take part in calculations, [3, 255].
        /// </summary>
        /// 
        /// <remarks><para>The greater the value the more is the general power of the filter. Small values
        /// (less than 9) on high resolution images (3000 pixels wide) do not give significant results.
        /// Large values increase the number of calculations and degrade performance.</para>
        /// 
        /// <para><note>The value of this property must be an odd integer in the [3, 255] range if
        /// <see cref="LimitKernelSize"/> is set to <see langword="false"/> or in the [3, 25] range
        /// otherwise.</note></para>
        /// 
        /// <para>Default value is set to <b>9</b>.</para>
        /// </remarks>
        /// 
        /// <exception cref="ArgumentOutOfRangeException">The specified value is out of range (see
        /// eception message for details).</exception>
        /// <exception cref="ArgumentException">The value of this must be an odd integer.</exception>
        /// 
        public int KernelSize
        {
            get
            {
                return this.kernelSize;
            }
            set
            {
                if ( value > maxKernelSize )
                {
                    throw new ArgumentOutOfRangeException( "Maximum allowed value of KernelSize property is " + maxKernelSize.ToString( ) );
                }
                if ( ( this.limitKernelSize ) && ( value > 25 ) )
                {
                    throw new ArgumentOutOfRangeException( "KernerlSize is larger then 25. Time for applying is significant and may lead to application freezing. In order to use any KernelSize value set property 'LimitKernelSize' to false." );
                }
                if ( value < 3 )
                {
                    throw new ArgumentOutOfRangeException( "KernelSize must be greater than 3" );
                }
                if ( value % 2 == 0 )
                {
                    throw new ArgumentException( "KernerlSize must be an odd integer." );
                }

                this.kernelSize = value;
            }
        }

        /// <summary>
        /// Determines smoothing power within a color domain (neighbor pixels of similar color), >= 1.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>Default value is set to <b>10</b>.</para>
        /// </remarks>
        /// 
        public double SpatialFactor
        {
            get
            {
                return this.spatialFactor;
            }
            set
            {
                this.spatialFactor = Math.Max( 1, value );
                this.spatialPropertiesChanged = true;
            }
        }

        /// <summary>
        /// Exponent power, used in Spatial function calculation, >= 1.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>Default value is set to <b>2</b>.</para>
        /// </remarks>
        /// 
        public double SpatialPower
        {
            get
            {
                return this.spatialPower;
            }
            set
            {
                this.spatialPower = Math.Max( 1, value );
                this.spatialPropertiesChanged = true;
            }
        }

        /// <summary>
        /// Determines the variance of color for a color domain, >= 1.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>Default value is set to <b>50</b>.</para>
        /// </remarks>
        /// 
        public double ColorFactor
        {
            get
            {
                return this.colorFactor;
            }
            set
            {
                this.colorFactor = Math.Max( 1, value );
                this.colorPropertiesChanged = true;
            }
        }

        /// <summary>
        /// Exponent power, used in Color function calculation, >= 1.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>Default value is set to <b>2</b>.</para>
        /// </remarks>
        /// 
        public double ColorPower
        {
            get
            {
                return this.colorPower;
            }
            set
            {
                this.colorPower = Math.Max( 1, value );
                this.colorPropertiesChanged = true;
            }
        }

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        /// 
        /// <remarks><para>See <see cref="IFilterInformation.FormatTranslations"/>
        /// documentation for additional information.</para></remarks>
        ///
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations
        {
            get { return this.formatTranslations; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BilateralSmoothing"/> class.
        /// </summary>
        /// 
        public BilateralSmoothing( )
        {
            this.formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format24bppRgb]    = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppRgb]    = PixelFormat.Format32bppRgb;
            this.formatTranslations[PixelFormat.Format32bppArgb]   = PixelFormat.Format32bppArgb;
        }

        private double[,] spatialFunc;
        private double[,] colorFunc;

        // For performance improvements Color and Spatial functions are recalculated prior to filter execution and put into 2 dimensional arrays
        private void InitSpatialFunc( )
        {
            if ( ( this.spatialFunc == null ) || ( this.spatialFunc.Length != this.kernelSize * this.kernelSize ) ||
                 ( this.spatialPropertiesChanged ) )
            {
                if ( ( this.spatialFunc == null ) || ( this.spatialFunc.Length != this.kernelSize * this.kernelSize ) )
                {
                    this.spatialFunc = new double[this.kernelSize , this.kernelSize];
                }

                var kernelRadius = this.kernelSize / 2;

                for ( var i = 0; i < this.kernelSize; i++ )
                {
                    var ti  = i - kernelRadius;
                    var ti2 = ti * ti;

                    for ( var k = 0; k < this.kernelSize; k++ )
                    {
                        var tk = k - kernelRadius;
                        var tk2 = tk * tk;

                        this.spatialFunc[i, k] = M.Exp( -0.5 * M.Pow( M.Sqrt( ( ti2 + tk2 ) / this.spatialFactor ), this.spatialPower ) );
                    }
                }

                this.spatialPropertiesChanged = false;
            }
        }

        // For performance improvements Color and Spatial functions are recalculated prior to filter execution and put into 2 dimensional arrays
        private void InitColorFunc( )
        {
            if ( ( this.colorFunc == null ) || ( this.colorPropertiesChanged ) )
            {
                if ( this.colorFunc == null )
                {
                    this.colorFunc = new double[colorsCount, colorsCount];
                }

                for ( var i = 0; i < colorsCount; i++ )
                {
                    for ( var k = 0; k < colorsCount; k++ )
                    {
                        this.colorFunc[i, k] = M.Exp( -0.5 * ( M.Pow( M.Abs( i - k ) / this.colorFactor , this.colorPower ) ) );
                    }
                }

                this.colorPropertiesChanged = false;
            }
        }

        private void InitFilter( )
        {
            this.InitSpatialFunc( );
            this.InitColorFunc( );
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// 
        /// <param name="source">Source image data.</param>
        /// <param name="destination">Destination image data.</param>
        /// <param name="rect">Image rectangle for processing by the filter.</param>
        /// 
        protected override unsafe void ProcessFilter( UnmanagedImage source, UnmanagedImage destination, Rectangle rect )
        {
            var kernelHalf = this.kernelSize / 2;

            this.InitFilter( );

            if ( ( rect.Width <= this.kernelSize ) || ( rect.Height <= this.kernelSize ) )
            {
                this.ProcessWithEdgeChecks( source, destination, rect );
            }
            else
            {
                var safeArea = rect;
                safeArea.Inflate( -kernelHalf, -kernelHalf );

                if ( ( Environment.ProcessorCount > 1 ) && ( this.enableParallelProcessing ) )
                {
                    this.ProcessWithoutChecksParallel( source, destination, safeArea );
                }
                else
                {
                    this.ProcessWithoutChecks( source, destination, safeArea );
                }

                // top
                this.ProcessWithEdgeChecks( source, destination,
                    new Rectangle( rect.Left, rect.Top, rect.Width, kernelHalf ) );
                // bottom
                this.ProcessWithEdgeChecks( source, destination,
                    new Rectangle( rect.Left, rect.Bottom - kernelHalf, rect.Width, kernelHalf ) );
                // left
                this.ProcessWithEdgeChecks( source, destination,
                    new Rectangle( rect.Left, rect.Top + kernelHalf, kernelHalf, rect.Height - kernelHalf * 2 ) );
                // right
                this.ProcessWithEdgeChecks( source, destination,
                    new Rectangle( rect.Right - kernelHalf, rect.Top + kernelHalf, kernelHalf, rect.Height - kernelHalf  * 2 ) );
            }
        }

        // Perform parallel image processing without checking pixels' coordinates to make sure those are in bounds
        private unsafe void ProcessWithoutChecksParallel( UnmanagedImage source, UnmanagedImage destination, Rectangle rect )
        {
            var startX = rect.Left;
            var startY = rect.Top;
            var stopX  = rect.Right;
            var stopY  = rect.Bottom;

            var pixelSize = System.Drawing.Image.GetPixelFormatSize( source.PixelFormat ) / 8;
            var kernelHalf = this.kernelSize / 2;
            var bytesInKernelRow = this.kernelSize * pixelSize;

            var srcStride = source.Stride;
            var dstStride = destination.Stride;

            var srcOffset = srcStride - rect.Width * pixelSize;
            var dstOffset = dstStride - rect.Width * pixelSize;

            // offset of the first kernel's pixel
            var srcKernelFistPixelOffset = kernelHalf * ( srcStride + pixelSize );
            // offset to move to the next kernel's pixel after processing one kernel's row
            var srcKernelOffset = srcStride - bytesInKernelRow;

            var srcBase = (byte*) source.ImageData.ToPointer( );
            var dstBase = (byte*) destination.ImageData.ToPointer( );

            // allign pointers to the left most pixel in the first row
            srcBase += startX * pixelSize;
            dstBase += startX * pixelSize;

            if ( pixelSize > 1 )
            {
                Parallel.For( startY, stopY, delegate( int y )
                {
                    var src = srcBase + y * srcStride;
                    var dst = dstBase + y * dstStride;

                    byte srcR, srcG, srcB;
                    byte srcR0, srcG0, srcB0;
                    byte* srcPixel;

                    int tx, ty;

                    double sCoefR, sCoefG, sCoefB, sMembR, sMembG, sMembB, coefR, coefG, coefB;

                    for ( var x = startX; x < stopX; x++, src += pixelSize, dst += pixelSize )
                    {
                        // lower right corner - to start processing from that point
                        srcPixel = src + srcKernelFistPixelOffset;

                        sCoefR = 0;
                        sCoefG = 0;
                        sCoefB = 0;
                        sMembR = 0;
                        sMembG = 0;
                        sMembB = 0;

                        srcR0 = src[RGB.R];
                        srcG0 = src[RGB.G];
                        srcB0 = src[RGB.B];

                        // move from lower right to upper left corner
                        ty = this.kernelSize;
                        while ( ty != 0 )
                        {
                            ty--;

                            tx = this.kernelSize;
                            while ( tx != 0 )
                            {
                                tx--;

                                srcR = srcPixel[RGB.R];
                                srcG = srcPixel[RGB.G];
                                srcB = srcPixel[RGB.B];

                                coefR = this.spatialFunc[tx, ty] * this.colorFunc[srcR, srcR0];
                                coefG = this.spatialFunc[tx, ty] * this.colorFunc[srcG, srcG0];
                                coefB = this.spatialFunc[tx, ty] * this.colorFunc[srcB, srcB0];

                                sCoefR += coefR;
                                sCoefG += coefG;
                                sCoefB += coefB;

                                sMembR += coefR * srcR;
                                sMembG += coefG * srcG;
                                sMembB += coefB * srcB;

                                srcPixel -= pixelSize;
                            }

                            srcPixel -= srcKernelOffset;
                        }

                        dst[RGB.R] = (byte) ( sMembR / sCoefR );
                        dst[RGB.G] = (byte) ( sMembG / sCoefG );
                        dst[RGB.B] = (byte) ( sMembB / sCoefB );
                    }
                } );
            }
            else
            {
                // 8bpp grayscale images
                Parallel.For( startY, stopY, delegate( int y )
                {
                    var src = srcBase + y * srcStride;
                    var dst = dstBase + y * dstStride;

                    byte srcC;
                    byte srcC0;
                    byte* srcPixel;
                    double sCoefC, sMembC, coefC;

                    int tx, ty;

                    for ( var x = startX; x < stopX; x++, src++, dst++ )
                    {
                        // lower right corner - to start processing from that point
                        srcPixel = src + srcKernelFistPixelOffset;

                        sCoefC = 0;
                        sMembC = 0;

                        srcC0 = *src;

                        // move from lower right to upper left corner
                        ty = this.kernelSize;
                        while ( ty != 0 )
                        {
                            ty--;

                            tx = this.kernelSize;
                            while ( tx != 0 )
                            {
                                tx--;

                                srcC = *( srcPixel );
                                coefC = this.spatialFunc[tx, ty] * this.colorFunc[srcC, srcC0];

                                sCoefC += coefC;
                                sMembC += coefC * srcC;

                                srcPixel -= pixelSize;
                            }

                            srcPixel -= srcKernelOffset;
                        }

                        *dst = (byte) ( sMembC / sCoefC );
                    }
                } );
            }
        }

        // Perform image processing without checking pixels' coordinates to make sure those are in bounds
        private unsafe void ProcessWithoutChecks( UnmanagedImage source, UnmanagedImage destination, Rectangle rect )
        {
            var startX = rect.Left;
            var startY = rect.Top;
            var stopX  = rect.Right;
            var stopY  = rect.Bottom;

            var pixelSize = System.Drawing.Image.GetPixelFormatSize( source.PixelFormat ) / 8;
            var kernelHalf = this.kernelSize / 2;
            var bytesInKernelRow = this.kernelSize * pixelSize;

            var srcStride = source.Stride;
            var dstStride = destination.Stride;

            var srcOffset = srcStride - rect.Width * pixelSize;
            var dstOffset = dstStride - rect.Width * pixelSize;

            // offset of the first kernel's pixel
            var srcKernelFistPixelOffset = kernelHalf * ( srcStride + pixelSize );
            // offset to move to the next kernel's pixel after processing one kernel's row
            var srcKernelOffset = srcStride - bytesInKernelRow;

            int tx, ty;

            var src = (byte*) source.ImageData.ToPointer( );
            var dst = (byte*) destination.ImageData.ToPointer( );

            // allign pointers to the first pixel to process
            src += startY * srcStride + startX * pixelSize;
            dst += startY * dstStride + startX * pixelSize;

            if ( pixelSize > 1 )
            {
                byte srcR, srcG, srcB;
                byte srcR0, srcG0, srcB0;
                byte* srcPixel;

                double sCoefR, sCoefG, sCoefB, sMembR, sMembG, sMembB, coefR, coefG, coefB;

                for ( var y = startY; y < stopY; y++ )
                {
                    for ( var x = startX; x < stopX; x++, src += pixelSize, dst += pixelSize )
                    {
                        // lower right corner - to start processing from that point
                        srcPixel = src + srcKernelFistPixelOffset;

                        sCoefR = 0;
                        sCoefG = 0;
                        sCoefB = 0;
                        sMembR = 0;
                        sMembG = 0;
                        sMembB = 0;

                        srcR0 = src[RGB.R];
                        srcG0 = src[RGB.G];
                        srcB0 = src[RGB.B];

                        // move from lower right to upper left corner
                        ty = this.kernelSize;
                        while ( ty != 0 )
                        {
                            ty--;

                            tx = this.kernelSize;
                            while ( tx != 0 )
                            {
                                tx--;

                                srcR = srcPixel[RGB.R];
                                srcG = srcPixel[RGB.G];
                                srcB = srcPixel[RGB.B];

                                coefR = this.spatialFunc[tx, ty] * this.colorFunc[srcR, srcR0];
                                coefG = this.spatialFunc[tx, ty] * this.colorFunc[srcG, srcG0];
                                coefB = this.spatialFunc[tx, ty] * this.colorFunc[srcB, srcB0];

                                sCoefR += coefR;
                                sCoefG += coefG;
                                sCoefB += coefB;

                                sMembR += coefR * srcR;
                                sMembG += coefG * srcG;
                                sMembB += coefB * srcB;

                                srcPixel -= pixelSize;
                            }

                            srcPixel -= srcKernelOffset;
                        }

                        dst[RGB.R] = (byte) ( sMembR / sCoefR );
                        dst[RGB.G] = (byte) ( sMembG / sCoefG );
                        dst[RGB.B] = (byte) ( sMembB / sCoefB );
                    }
                    src += srcOffset;
                    dst += dstOffset;
                }
            }
            else
            {
                // 8bpp grayscale images
                byte srcC;
                byte srcC0;
                byte* srcPixel;
                double sCoefC, sMembC, coefC;

                for ( var y = startY; y < stopY; y++ )
                {
                    for ( var x = startX; x < stopX; x++, src++, dst++ )
                    {
                        // lower right corner - to start processing from that point
                        srcPixel = src + srcKernelFistPixelOffset;

                        sCoefC = 0;
                        sMembC = 0;

                        srcC0 = *src;

                        // move from lower right to upper left corner
                        ty = this.kernelSize;
                        while ( ty != 0 )
                        {
                            ty--;

                            tx = this.kernelSize;
                            while ( tx != 0 )
                            {
                                tx--;

                                srcC = *( srcPixel );
                                coefC = this.spatialFunc[tx, ty] * this.colorFunc[srcC, srcC0];

                                sCoefC += coefC;
                                sMembC += coefC * srcC;

                                srcPixel -= pixelSize;
                            }

                            srcPixel -= srcKernelOffset;
                        }

                        *dst = (byte) ( sMembC / sCoefC );
                    }
                    src += srcOffset;
                    dst += dstOffset;
                }
            }
        }

        // Perform image processing with checking pixels' coordinates to make sure those are in bounds
        private unsafe void ProcessWithEdgeChecks( UnmanagedImage source, UnmanagedImage destination, Rectangle rect )
        {
            var width  = source.Width;
            var height = source.Height;

            var startX = rect.Left;
            var startY = rect.Top;
            var stopX  = rect.Right;
            var stopY  = rect.Bottom;

            var pixelSize = System.Drawing.Image.GetPixelFormatSize( source.PixelFormat ) / 8;
            var kernelHalf = this.kernelSize / 2;
            var bytesInKernelRow = this.kernelSize * pixelSize;

            var srcStride = source.Stride;
            var dstStride = destination.Stride;

            var srcOffset = srcStride - rect.Width * pixelSize;
            var dstOffset = dstStride - rect.Width * pixelSize;

            // offset of the first kernel's pixel
            var srcKernelFistPixelOffset = kernelHalf * ( srcStride + pixelSize );
            // offset to move to the next kernel's pixel after processing one kernel's row
            var srcKernelOffset = srcStride - bytesInKernelRow;

            int rx, ry;
            int tx, ty;

            var src = (byte*) source.ImageData.ToPointer( );
            var dst = (byte*) destination.ImageData.ToPointer( );

            // allign pointers to the first pixel to process
            src += startY * srcStride + startX * pixelSize;
            dst += startY * dstStride + startX * pixelSize;

            if ( pixelSize > 1 )
            {
                // color images
                byte srcR, srcG, srcB;
                byte srcR0, srcG0, srcB0;
                byte* srcPixel;

                double sCoefR, sCoefG, sCoefB, sMembR, sMembG, sMembB, coefR, coefG, coefB;

                for ( var y = startY; y < stopY; y++ )
                {
                    for ( var x = startX; x < stopX; x++, src += pixelSize, dst += pixelSize )
                    {
                        // lower right corner - to start processing from that point
                        srcPixel = src + srcKernelFistPixelOffset;

                        sCoefR = 0;
                        sCoefG = 0;
                        sCoefB = 0;
                        sMembR = 0;
                        sMembG = 0;
                        sMembB = 0;

                        srcR0 = src[RGB.R];
                        srcG0 = src[RGB.G];
                        srcB0 = src[RGB.B];

                        // move from lower right to upper left corner
                        ty = this.kernelSize;
                        while ( ty != 0 )
                        {
                            ty--;
                            ry = ty - kernelHalf;

                            if ( ( ry + y >= height ) || ( ry + y < 0 ) ) // bounds check
                            {
                                srcPixel -= srcStride;
                                continue;
                            }

                            tx = this.kernelSize;
                            while ( tx != 0 )
                            {
                                tx--;
                                rx = tx - kernelHalf;

                                if ( ( rx + x >= width ) || ( rx + x < 0 ) ) // bounds check
                                {
                                    srcPixel -= pixelSize;
                                    continue;
                                }

                                srcR = srcPixel[RGB.R];
                                srcG = srcPixel[RGB.G];
                                srcB = srcPixel[RGB.B];

                                coefR = this.spatialFunc[tx, ty] * this.colorFunc[srcR, srcR0];
                                coefG = this.spatialFunc[tx, ty] * this.colorFunc[srcG, srcG0];
                                coefB = this.spatialFunc[tx, ty] * this.colorFunc[srcB, srcB0];

                                sCoefR += coefR;
                                sCoefG += coefG;
                                sCoefB += coefB;

                                sMembR += coefR * srcR;
                                sMembG += coefG * srcG;
                                sMembB += coefB * srcB;

                                srcPixel -= pixelSize;
                            }

                            srcPixel -= srcKernelOffset;
                        }

                        dst[RGB.R] = (byte) ( sMembR / sCoefR );
                        dst[RGB.G] = (byte) ( sMembG / sCoefG );
                        dst[RGB.B] = (byte) ( sMembB / sCoefB );
                    }

                    src += srcOffset;
                    dst += dstOffset;
                }
            }
            else
            {
                // 8bpp grayscale images
                byte srcC;
                byte srcC0;
                byte* srcPixel;
                double sCoefC, sMembC, coefC;

                for ( var y = startY; y < stopY; y++ )
                {
                    for ( var x = startX; x < stopX; x++, src++, dst++ )
                    {
                        // lower right corner - to start processing from that point
                        srcPixel = src + srcKernelFistPixelOffset;

                        sCoefC = 0;
                        sMembC = 0;

                        srcC0 = *src;

                        // move from lower right to upper left corner
                        ty = this.kernelSize;
                        while ( ty != 0 )
                        {
                            ty--;
                            ry = (int) ( ty - kernelHalf );

                            if ( ( ry + y >= height ) || ( ry + y < 0 ) ) // bounds check
                            {
                                srcPixel -= srcStride;
                                continue;
                            }

                            tx = this.kernelSize;
                            while ( tx != 0 )
                            {
                                tx--;
                                rx = (int) ( tx - kernelHalf );

                                if ( ( rx + x >= source.Width ) || ( rx + x < 0 ) ) // bounds check
                                {
                                    srcPixel -= pixelSize;
                                    continue;
                                }

                                srcC = *( srcPixel );
                                coefC = this.spatialFunc[tx, ty] * this.colorFunc[srcC, srcC0];

                                sCoefC += coefC;
                                sMembC += coefC * srcC;

                                srcPixel -= pixelSize;
                            }

                            srcPixel -= srcKernelOffset;
                        }

                        *dst = (byte) ( sMembC / sCoefC );
                    }
                    src += srcOffset;
                    dst += dstOffset;
                }
            }
        }
    }
}

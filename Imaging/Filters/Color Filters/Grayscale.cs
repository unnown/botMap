// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright � AForge.NET, 2005-2011
// contacts@aforgenet.com
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Base class for image grayscaling.
    /// </summary>
    /// 
    /// <remarks><para>This class is the base class for image grayscaling. Other
    /// classes should inherit from this class and specify <b>RGB</b>
    /// coefficients used for color image conversion to grayscale.</para>
    /// 
    /// <para>The filter accepts 24, 32, 48 and 64 bpp color images and produces
    /// 8 (if source is 24 or 32 bpp image) or 16 (if source is 48 or 64 bpp image)
    /// bpp grayscale image.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create grayscale filter (BT709)
    /// Grayscale filter = new Grayscale( 0.2125, 0.7154, 0.0721 );
    /// // apply the filter
    /// Bitmap grayImage = filter.Apply( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/grayscale.jpg" width="480" height="361" />
    /// </remarks>
    ///
    /// <seealso cref="GrayscaleBT709"/>
    /// <seealso cref="GrayscaleRMY"/>
    /// <seealso cref="GrayscaleY"/>
    ///
    public class Grayscale : BaseFilter
    {
        /// <summary>
        /// Set of predefined common grayscaling algorithms, which have aldready initialized
        /// grayscaling coefficients.
        /// </summary>
        public static class CommonAlgorithms
        {
            /// <summary>
            /// Grayscale image using BT709 algorithm.
            /// </summary>
            /// 
            /// <remarks><para>The instance uses <b>BT709</b> algorithm to convert color image
            /// to grayscale. The conversion coefficients are:
            /// <list type="bullet">
            /// <item>Red: 0.2125;</item>
            /// <item>Green: 0.7154;</item>
            /// <item>Blue: 0.0721.</item>
            /// </list></para>
            /// 
            /// <para>Sample usage:</para>
            /// <code>
            /// // apply the filter
            /// Bitmap grayImage = Grayscale.CommonAlgorithms.BT709.Apply( image );
            /// </code>
            /// </remarks>
            /// 
            public static readonly Grayscale BT709 = new Grayscale( 0.2125, 0.7154, 0.0721 );

            /// <summary>
            /// Grayscale image using R-Y algorithm.
            /// </summary>
            /// 
            /// <remarks><para>The instance uses <b>R-Y</b> algorithm to convert color image
            /// to grayscale. The conversion coefficients are:
            /// <list type="bullet">
            /// <item>Red: 0.5;</item>
            /// <item>Green: 0.419;</item>
            /// <item>Blue: 0.081.</item>
            /// </list></para>
            /// 
            /// <para>Sample usage:</para>
            /// <code>
            /// // apply the filter
            /// Bitmap grayImage = Grayscale.CommonAlgorithms.RMY.Apply( image );
            /// </code>
            /// </remarks>
            /// 
            public static readonly Grayscale RMY = new Grayscale( 0.5000, 0.4190, 0.0810 );

            /// <summary>
            /// Grayscale image using Y algorithm.
            /// </summary>
            /// 
            /// <remarks><para>The instance uses <b>Y</b> algorithm to convert color image
            /// to grayscale. The conversion coefficients are:
            /// <list type="bullet">
            /// <item>Red: 0.299;</item>
            /// <item>Green: 0.587;</item>
            /// <item>Blue: 0.114.</item>
            /// </list></para>
            /// 
            /// <para>Sample usage:</para>
            /// <code>
            /// // apply the filter
            /// Bitmap grayImage = Grayscale.CommonAlgorithms.Y.Apply( image );
            /// </code>
            /// </remarks>
            /// 
            public static readonly Grayscale Y = new Grayscale( 0.2990, 0.5870, 0.1140 );
        }

        // RGB coefficients for grayscale transformation

        /// <summary>
        /// Portion of red channel's value to use during conversion from RGB to grayscale.
        /// </summary>
        public readonly double RedCoefficient;
        /// <summary>
        /// Portion of green channel's value to use during conversion from RGB to grayscale.
        /// </summary>
        public readonly double GreenCoefficient;
        /// <summary>
        /// Portion of blue channel's value to use during conversion from RGB to grayscale.
        /// </summary>
        public readonly double BlueCoefficient;

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
        /// Initializes a new instance of the <see cref="Grayscale"/> class.
        /// </summary>
        /// 
        /// <param name="cr">Red coefficient.</param>
        /// <param name="cg">Green coefficient.</param>
        /// <param name="cb">Blue coefficient.</param>
        /// 
        public Grayscale( double cr, double cg, double cb )
        {
            this.RedCoefficient   = cr;
            this.GreenCoefficient = cg;
            this.BlueCoefficient  = cb;

            // initialize format translation dictionary
            this.formatTranslations[PixelFormat.Format24bppRgb]  = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format32bppRgb]  = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format48bppRgb]  = PixelFormat.Format16bppGrayScale;
            this.formatTranslations[PixelFormat.Format64bppArgb] = PixelFormat.Format16bppGrayScale;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// 
        /// <param name="sourceData">Source image data.</param>
        /// <param name="destinationData">Destination image data.</param>
        /// 
        protected override unsafe void ProcessFilter( UnmanagedImage sourceData, UnmanagedImage destinationData )
        {
            // get width and height
            var width  = sourceData.Width;
            var height = sourceData.Height;
            var srcPixelFormat = sourceData.PixelFormat;

            if (
                ( srcPixelFormat == PixelFormat.Format24bppRgb ) ||
                ( srcPixelFormat == PixelFormat.Format32bppRgb ) ||
                ( srcPixelFormat == PixelFormat.Format32bppArgb ) )
            {
                var pixelSize = ( srcPixelFormat == PixelFormat.Format24bppRgb ) ? 3 : 4;
                var srcOffset = sourceData.Stride - width * pixelSize;
                var dstOffset = destinationData.Stride - width;

                var rc = (int) ( 0x10000 * this.RedCoefficient );
                var gc = (int) ( 0x10000 * this.GreenCoefficient );
                var bc = (int) ( 0x10000 * this.BlueCoefficient );

                // make sure sum of coefficients equals to 0x10000
                while ( rc + gc + bc < 0x10000 )
                {
                    bc++;
                }

                // do the job
                var src = (byte*) sourceData.ImageData.ToPointer( );
                var dst = (byte*) destinationData.ImageData.ToPointer( );

                // for each line
                for ( var y = 0; y < height; y++ )
                {
                    // for each pixel
                    for ( var x = 0; x < width; x++, src += pixelSize, dst++ )
                    {
                        *dst = (byte) ( ( rc * src[RGB.R] + gc * src[RGB.G] + bc * src[RGB.B] ) >> 16 );
                    }
                    src += srcOffset;
                    dst += dstOffset;
                }
            }
            else
            {
                var pixelSize = ( srcPixelFormat == PixelFormat.Format48bppRgb ) ? 3 : 4;
                var srcBase = (byte*) sourceData.ImageData.ToPointer( );
                var dstBase = (byte*) destinationData.ImageData.ToPointer( );
                var srcStride = sourceData.Stride;
                var dstStride = destinationData.Stride;

                // for each line
                for ( var y = 0; y < height; y++ )
                {
                    var src = (ushort*) ( srcBase + y * srcStride );
                    var dst = (ushort*) ( dstBase + y * dstStride );

                    // for each pixel
                    for ( var x = 0; x < width; x++, src += pixelSize, dst++ )
                    {
                        *dst = (ushort) ( this.RedCoefficient * src[RGB.R] + this.GreenCoefficient * src[RGB.G] + this.BlueCoefficient * src[RGB.B] );
                    }
                }
            }
        }
    }
}

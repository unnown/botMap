// AForge Image Processing Library
// AForge.NET framework
//
// Copyright � Andrew Kirillov, 2005-2009
// andrew.kirillov@aforgenet.com
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Threshold binarization.
    /// </summary>
    /// 
    /// <remarks><para>The filter does image binarization using specified threshold value. All pixels
    /// with intensities equal or higher than threshold value are converted to white pixels. All other
    /// pixels with intensities below threshold value are converted to black pixels.</para>
    /// 
    /// <para>The filter accepts 8 and 16 bpp grayscale images for processing.</para>
    /// 
    /// <para><note>Since the filter can be applied as to 8 bpp and to 16 bpp images,
    /// the <see cref="ThresholdValue"/> value should be set appropriately to the pixel format.
    /// In the case of 8 bpp images the threshold value is in the [0, 255] range, but in the case
    /// of 16 bpp images the threshold value is in the [0, 65535] range.</note></para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// Threshold filter = new Threshold( 100 );
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/grayscale.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/threshold.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    public class Threshold : BaseInPlacePartialFilter
    {
        /// <summary>
        /// Threshold value.
        /// </summary>
        protected int threshold = 128;

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
        /// Threshold value.
        /// </summary>
        /// 
        /// <remarks>Default value is set to <b>128</b>.</remarks>
        /// 
        public int ThresholdValue
        {
            get { return this.threshold; }
            set { this.threshold = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Threshold"/> class.
        /// </summary>
        /// 
        public Threshold( )
        {
            // initialize format translation dictionary
            this.formatTranslations[PixelFormat.Format8bppIndexed]    = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format16bppGrayScale] = PixelFormat.Format16bppGrayScale;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Threshold"/> class.
        /// </summary>
        /// 
        /// <param name="threshold">Threshold value.</param>
        /// 
        public Threshold( int threshold )
            : this( )
        {
            this.threshold = threshold;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// 
        /// <param name="image">Source image data.</param>
        /// <param name="rect">Image rectangle for processing by the filter.</param>
        /// 
        protected override unsafe void ProcessFilter( UnmanagedImage image, Rectangle rect )
        {
            var startX  = rect.Left;
            var startY  = rect.Top;
            var stopX   = startX + rect.Width;
            var stopY   = startY + rect.Height;

            if ( image.PixelFormat == PixelFormat.Format8bppIndexed )
            {
                var offset = image.Stride - rect.Width;

                // do the job
                var ptr = (byte*) image.ImageData.ToPointer( );

                // allign pointer to the first pixel to process
                ptr += ( startY * image.Stride + startX );

                // for each line	
                for ( var y = startY; y < stopY; y++ )
                {
                    // for each pixel
                    for ( var x = startX; x < stopX; x++, ptr++ )
                    {
                        *ptr = (byte) ( ( *ptr >= this.threshold ) ? 255 : 0 );
                    }
                    ptr += offset;
                }
            }
            else
            {
                var basePtr = (byte*) image.ImageData.ToPointer( ) + startX * 2;
                var stride = image.Stride;

                // for each line	
                for ( var y = startY; y < stopY; y++ )
                {
                    var ptr = (ushort*) ( basePtr + stride * y );

                    // for each pixel
                    for ( var x = startX; x < stopX; x++, ptr++ )
                    {
                        *ptr = (ushort) ( ( *ptr >= this.threshold ) ? 65535 : 0 );
                    }
                }
            }
        }
    }
}

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
    /// Morph filter.
    /// </summary>
    /// 
    /// <remarks><para>The filter combines two images by taking
    /// <see cref="SourcePercent">specified percent</see> of pixels' intensities from source
    /// image and the rest from overlay image. For example, if the
    /// <see cref="SourcePercent">source percent</see> value is set to 0.8, then each pixel
    /// of the result image equals to <b>0.8 * source + 0.2 * overlay</b>, where <b>source</b>
    /// and <b>overlay</b> are corresponding pixels' values in source and overlay images.</para>
    /// 
    /// <para>The filter accepts 8 bpp grayscale and 24 bpp color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// Morph filter = new Morph( overlayImage );
    /// filter.SourcePercent = 0.75;
    /// // apply the filter
    /// Bitmap resultImage = filter.Apply( sourceImage );
    /// </code>
    /// 
    /// <para><b>Source image:</b></para>
    /// <img src="img/imaging/sample6.png" width="320" height="240" />
    /// <para><b>Overlay image:</b></para>
    /// <img src="img/imaging/sample7.png" width="320" height="240" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/morph.png" width="320" height="240" />
    /// </remarks>
    /// 
    public class Morph : BaseInPlaceFilter2
    {
        private double	sourcePercent = 0.50;

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
        /// Percent of source image to keep, [0, 1].
        /// </summary>
        /// 
        /// <remarks><para>The property specifies the percentage of source pixels' to take. The
        /// rest is taken from an overlay image.</para></remarks>
        /// 
        public double SourcePercent
        {
            get { return this.sourcePercent; }
            set { this.sourcePercent = Math.Max( 0.0, Math.Min( 1.0, value ) ); }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Morph"/> class.
        /// </summary>
        public Morph( )
        {
            this.InitFormatTranslations( );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Morph"/> class.
        /// </summary>
        /// 
        /// <param name="overlayImage">Overlay image.</param>
        /// 
        public Morph( Bitmap overlayImage )
            : base( overlayImage )
        {
            this.InitFormatTranslations( );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Morph"/> class.
        /// </summary>
        /// 
        /// <param name="unmanagedOverlayImage">Unmanaged overlay image.</param>
        /// 
        public Morph( UnmanagedImage unmanagedOverlayImage )
            : base( unmanagedOverlayImage )
        {
            this.InitFormatTranslations( );
        }

        // Initialize format translation dictionary
        private void InitFormatTranslations( )
        {
            this.formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format24bppRgb]    = PixelFormat.Format24bppRgb;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// 
        /// <param name="image">Source image data.</param>
        /// <param name="overlay">Overlay image data.</param>
        ///
        protected override unsafe void ProcessFilter( UnmanagedImage image, UnmanagedImage overlay )
        {
            // get image dimension
            var width  = image.Width;
            var height = image.Height;

            // initialize other variables
            var pixelSize = ( image.PixelFormat == PixelFormat.Format8bppIndexed ) ? 1 : 3;
            var lineSize  = width * pixelSize;
            var offset    = image.Stride - lineSize;
            var ovrOffset = overlay.Stride - lineSize;
            // percentage of overlay image
            var q = 1.0 - this.sourcePercent;

            // do the job
            var ptr = (byte*) image.ImageData.ToPointer( );
            var ovr = (byte*) overlay.ImageData.ToPointer( );

            // for each line
            for ( var y = 0; y < height; y++ )
            {
                // for each pixel
                for ( var x = 0; x < lineSize; x++, ptr++, ovr++ )
                {
                    *ptr = (byte) ( ( this.sourcePercent * ( *ptr ) ) + ( q * ( *ovr ) ) );
                }
                ptr += offset;
                ovr += ovrOffset;
            }
        }
    }
}

// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © Andrew Kirillov, 2005-2009
// andrew.kirillov@aforgenet.com
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Blobs filtering by size.
    /// </summary>
    /// 
    /// <remarks><para>The filter performs filtering of blobs by their size in the specified
    /// source image - all blobs, which are smaller or bigger then specified limits, are
    /// removed from the image.</para>
    /// 
    /// <para><note>The image processing filter treats all none black pixels as objects'
    /// pixels and all black pixel as background.</note></para>
    /// 
    /// <para>The filter accepts 8 bpp grayscale images and 24/32
    /// color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// BlobsFiltering filter = new BlobsFiltering( );
    /// // configure filter
    /// filter.CoupledSizeFiltering = true;
    /// filter.MinWidth  = 70;
    /// filter.MinHeight = 70;
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample2.jpg" width="320" height="240" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/blobs_filtering.jpg" width="320" height="240" />
    /// </remarks>
    /// 
    /// <seealso cref="BlobCounter"/>
    /// <seealso cref="BlobCounterBase"/>
    ///
    public class BlobsFiltering : BaseInPlaceFilter
    {
        private BlobCounter blobCounter = new BlobCounter( );

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
        /// Specifies if size filetering should be coupled or not.
        /// </summary>
        /// 
        /// <remarks><para>See documentation for <see cref="BlobCounterBase.CoupledSizeFiltering"/> property
        /// of <see cref="BlobCounterBase"/> class for more information.</para></remarks>
        /// 
        public bool CoupledSizeFiltering
        {
            get { return this.blobCounter.CoupledSizeFiltering; }
            set { this.blobCounter.CoupledSizeFiltering = value; }
        }

        /// <summary>
        /// Minimum allowed width of blob.
        /// </summary>
        /// 
        public int MinWidth
        {
            get { return this.blobCounter.MinWidth; }
            set { this.blobCounter.MinWidth = value; }
        }

        /// <summary>
        /// Minimum allowed height of blob.
        /// </summary>
        /// 
        public int MinHeight
        {
            get { return this.blobCounter.MinHeight; }
            set { this.blobCounter.MinHeight = value; }
        }

        /// <summary>
        /// Maximum allowed width of blob.
        /// </summary>
        /// 
        public int MaxWidth
        {
            get { return this.blobCounter.MaxWidth; }
            set { this.blobCounter.MaxWidth = value; }
        }

        /// <summary>
        /// Maximum allowed height of blob.
        /// </summary>
        /// 
        public int MaxHeight
        {
            get { return this.blobCounter.MaxHeight; }
            set { this.blobCounter.MaxHeight = value; }
        }

        /// <summary>
        /// Custom blobs' filter to use.
        /// </summary>
        /// 
        /// <remarks><para>See <see cref="BlobCounterBase.BlobsFilter"/> for information
        /// about custom blobs' filtering routine.</para></remarks>
        /// 
        public IBlobsFilter BlobsFilter
        {
            get { return this.blobCounter.BlobsFilter; }
            set { this.blobCounter.BlobsFilter = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobsFiltering"/> class.
        /// </summary>
        /// 
        public BlobsFiltering( )
        {
            this.blobCounter.FilterBlobs = true;
            this.blobCounter.MinWidth    = 1;
            this.blobCounter.MinHeight   = 1;
            this.blobCounter.MaxWidth    = int.MaxValue;
            this.blobCounter.MaxHeight   = int.MaxValue;

            this.formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format24bppRgb] = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format32bppArgb;
            this.formatTranslations[PixelFormat.Format32bppPArgb] = PixelFormat.Format32bppPArgb;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobsFiltering"/> class.
        /// </summary>
        /// 
        /// <param name="minWidth">Minimum allowed width of blob.</param>
        /// <param name="minHeight">Minimum allowed height of blob.</param>
        /// <param name="maxWidth">Maximum allowed width of blob.</param>
        /// <param name="maxHeight">Maximum allowed height of blob.</param>
        /// 
        /// <remarks>This constructor creates an instance of <see cref="BlobsFiltering"/> class
        /// with <see cref="CoupledSizeFiltering"/> property set to <b>false</b>.</remarks>
        /// 
        public BlobsFiltering( int minWidth, int minHeight, int maxWidth, int maxHeight )
            : this( minWidth, minHeight, maxWidth, maxHeight, false ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobsFiltering"/> class.
        /// </summary>
        /// 
        /// <param name="minWidth">Minimum allowed width of blob.</param>
        /// <param name="minHeight">Minimum allowed height of blob.</param>
        /// <param name="maxWidth">Maximum allowed width of blob.</param>
        /// <param name="maxHeight">Maximum allowed height of blob.</param>
        /// <param name="coupledSizeFiltering">Specifies if size filetering should be coupled or not.</param>
        /// 
        /// <remarks><para>For information about coupled filtering mode see documentation for
        /// <see cref="BlobCounterBase.CoupledSizeFiltering"/> property of <see cref="BlobCounterBase"/>
        /// class.</para></remarks>
        /// 
        public BlobsFiltering( int minWidth, int minHeight, int maxWidth, int maxHeight, bool coupledSizeFiltering )
            : this( )
        {
            this.blobCounter.MinWidth  = minWidth;
            this.blobCounter.MinHeight = minHeight;
            this.blobCounter.MaxWidth  = maxWidth;
            this.blobCounter.MaxHeight = maxHeight;
            this.blobCounter.CoupledSizeFiltering = coupledSizeFiltering;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobsFiltering"/> class.
        /// </summary>
        /// 
        /// <param name="blobsFilter">Custom blobs' filtering routine to use
        /// (see <see cref="BlobCounterBase.BlobsFilter"/>).</param>
        ///
        public BlobsFiltering( IBlobsFilter blobsFilter ) : this( )
        {
            this.blobCounter.BlobsFilter = blobsFilter;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// 
        /// <param name="image">Source image data.</param>
        ///
        protected override unsafe void ProcessFilter( UnmanagedImage image )
        {
            // use blob counter to build objects map and filter them
            this.blobCounter.ProcessImage( image );
            var objectsMap = this.blobCounter.ObjectLabels;

            // get image width and height
            var width  = image.Width;
            var height = image.Height;

            // do the job
            var ptr = (byte*) image.ImageData.ToPointer( );

            if ( image.PixelFormat == PixelFormat.Format8bppIndexed )
            {
                var offset = image.Stride - width;

                for ( int y = 0, p = 0; y < height; y++ )
                {
                    for ( var x = 0; x < width; x++, ptr++, p++ )
                    {
                        if ( objectsMap[p] == 0 )
                        {
                            *ptr = 0;
                        }
                    }
                    ptr += offset;
                }
            }
            else
            {
                var pixelSize = Bitmap.GetPixelFormatSize( image.PixelFormat ) / 8;
                var offset = image.Stride - width * pixelSize;

                for ( int y = 0, p = 0; y < height; y++ )
                {
                    for ( var x = 0; x < width; x++, ptr += pixelSize, p++ )
                    {
                        if ( objectsMap[p] == 0 )
                        {
                            ptr[RGB.R] = ptr[RGB.G] = ptr[RGB.B] = 0;
                        }
                    }
                    ptr += offset;
                }
            }
        }
    }
}

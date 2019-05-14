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
    /// Connected components labeling.
    /// </summary>
    /// 
    /// <remarks><para>The filter performs labeling of objects in the source image. It colors
    /// each separate object using different color. The image processing filter treats all none
    /// black pixels as objects' pixels and all black pixel as background.</para>
    /// 
    /// <para>The filter accepts 8 bpp grayscale images and 24/32 bpp color images and produces
    /// 24 bpp RGB image.</para>
    ///
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// ConnectedComponentsLabeling filter = new ConnectedComponentsLabeling( );
    /// // apply the filter
    /// Bitmap newImage = filter.Apply( image );
    /// // check objects count
    /// int objectCount = filter.ObjectCount;
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample2.jpg" width="320" height="240" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/labeling.jpg" width="320" height="240" />
    /// </remarks>
    /// 
    public class ConnectedComponentsLabeling : BaseFilter
    {
        // Color table for coloring objects
        private static Color[] colorTable = new Color[]
		{
			Color.Red,		Color.Green,	Color.Blue,			Color.Yellow,
			Color.Violet,	Color.Brown,	Color.Olive,		Color.Cyan,

			Color.Magenta,	Color.Gold,		Color.Indigo,		Color.Ivory,
			Color.HotPink,	Color.DarkRed,	Color.DarkGreen,	Color.DarkBlue,

			Color.DarkSeaGreen,	Color.Gray,	Color.DarkKhaki,	Color.DarkGray,
			Color.LimeGreen, Color.Tomato,	Color.SteelBlue,	Color.SkyBlue,

			Color.Silver,	Color.Salmon,	Color.SaddleBrown,	Color.RosyBrown,
            Color.PowderBlue, Color.Plum,	Color.PapayaWhip,	Color.Orange
		};

        // private format translation dictionary
        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>( );

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations
        {
            get { return this.formatTranslations; }
        }

        // blob counter
        private BlobCounterBase blobCounter = new BlobCounter( );

        /// <summary>
        /// Blob counter used to locate separate blobs.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to set blob counter to use for blobs' localization.</para>
        /// 
        /// <para>Default value is set to <see cref="BlobCounter"/>.</para>
        /// </remarks>
        /// 
        public BlobCounterBase BlobCounter
        {
            get { return this.blobCounter; }
            set { this.blobCounter = value; }
        }

        /// <summary>
        /// Colors used to color the binary image.
        /// </summary>
        public static Color[] ColorTable
        {
            get { return colorTable; }
            set { colorTable = value; }
        }

        /// <summary>
        /// Specifies if blobs should be filtered.
        /// </summary>
        /// 
        /// <remarks><para>See documentation for <see cref="BlobCounterBase.FilterBlobs"/> property
        /// of <see cref="BlobCounterBase"/> class for more information.</para></remarks>
        /// 
        public bool FilterBlobs
        {
            get { return this.blobCounter.FilterBlobs; }
            set { this.blobCounter.FilterBlobs = value; }
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
        /// Objects count.
        /// </summary>
        /// 
        /// <remarks>The amount of objects found in the last processed image.</remarks>
        /// 
        public int ObjectCount
        {
            get { return this.blobCounter.ObjectsCount; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectedComponentsLabeling"/> class.
        /// </summary>
        /// 
        public ConnectedComponentsLabeling( )
        {
            // initialize format translation dictionary
            this.formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format24bppRgb]    = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppArgb]   = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppPArgb]  = PixelFormat.Format24bppRgb;
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
            // process the image
            this.blobCounter.ProcessImage( sourceData );

            // get object labels
            var labels = this.blobCounter.ObjectLabels;

            // get width and height
            var width  = sourceData.Width;
            var height = sourceData.Height;

            var dstOffset = destinationData.Stride - width * 3;

            // do the job
            var dst = (byte*) destinationData.ImageData.ToPointer( );
            var p = 0;

            // for each row
            for ( var y = 0; y < height; y++ )
            {
                // for each pixel
                for ( var x = 0; x < width; x++, dst += 3, p++ )
                {
                    if ( labels[p] != 0 )
                    {
                        var c = colorTable[( labels[p] - 1 ) % colorTable.Length];

                        dst[RGB.R] = c.R;
                        dst[RGB.G] = c.G;
                        dst[RGB.B] = c.B;
                    }
                }
                dst += dstOffset;
            }
        }
    }
}

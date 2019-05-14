// AForge Image Processing Library
// AForge.NET framework
//
// Copyright � Andrew Kirillov, 2005-2008
// andrew.kirillov@gmail.com
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Binarization with thresholds matrix.
    /// </summary>
    /// 
    /// <remarks><para>Idea of the filter is the same as idea of <see cref="Threshold"/> filter -
    /// change pixel value to white, if its intensity is equal or higher than threshold value, or
    /// to black otherwise. But instead of using single threshold value for all pixel, the filter
    /// uses matrix of threshold values. Processing image is divided to adjacent windows of matrix
    /// size each. For pixels binarization inside of each window, corresponding threshold values are
    /// used from specified threshold matrix.</para>
    /// 
    /// <para>The filter accepts 8 bpp grayscale images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create binarization matrix
    /// byte[,] matrix = new byte[4, 4]
    /// {
    ///     {  95, 233, 127, 255 },
    ///     { 159,  31, 191,  63 },
    ///     { 111, 239,  79, 207 },
    ///     { 175,  47, 143,  15 }
    /// };
    /// // create filter
    /// OrderedDithering filter = new OrderedDithering( matrix );
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/grayscale.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/ordered_dithering.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    /// <seealso cref="BayerDithering"/>
    /// 
    public class OrderedDithering : BaseInPlacePartialFilter
    {
        private int rows = 4;
        private int cols = 4;

        private byte[,] matrix = new byte[4, 4]
		{
			{  15, 143,  47, 175 },
			{ 207,  79, 239, 111 },
			{  63, 191,  31, 159 },
			{ 255, 127, 223,  95 }
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

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDithering"/> class.
        /// </summary>
        /// 
        public OrderedDithering( )
        {
            // initialize format translation dictionary
            this.formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDithering"/> class.
        /// </summary>
        /// 
        /// <param name="matrix">Thresholds matrix.</param>
        /// 
        public OrderedDithering( byte[,] matrix )
            : this( )
        {
            this.rows = matrix.GetLength( 0 );
            this.cols = matrix.GetLength( 1 );

            this.matrix = matrix;
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
            var offset  = image.Stride - rect.Width;

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
                    *ptr = (byte) ( ( *ptr <= this.matrix[( y % this.rows ), ( x % this.cols )] ) ? 0 : 255 );
                }
                ptr += offset;
            }
        }
    }
}

// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright � Andrew Kirillov, 2005-2009
// andrew.kirillov@aforgenet.com
//

namespace AForge.Imaging
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Collections.Generic;

    /// <summary>
    /// Moravec corners detector.
    /// </summary>
    /// 
    /// <remarks><para>The class implements Moravec corners detector. For information about algorithm's
    /// details its <a href="http://www.cim.mcgill.ca/~dparks/CornerDetector/mainMoravec.htm">description</a>
    /// should be studied.</para>
    /// 
    /// <para><note>Due to limitations of Moravec corners detector (anisotropic response, etc.) its usage is limited
    /// to certain cases only.</note></para>
    /// 
    /// <para>The class processes only grayscale 8 bpp and color 24/32 bpp images.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create corner detector's instance
    /// MoravecCornersDetector mcd = new MoravecCornersDetector( );
    /// // process image searching for corners
    /// List&lt;IntPoint&gt; corners = scd.ProcessImage( image );
    /// // process points
    /// foreach ( IntPoint corner in corners )
    /// {
    ///     // ... 
    /// }
    /// </code>
    /// </remarks>
    /// 
    /// <seealso cref="SusanCornersDetector"/>
    /// 
    public class MoravecCornersDetector : ICornersDetector
    {
        // window size
        private int windowSize = 3;
        // threshold which is used to filter interest points
        private int threshold = 500;

        /// <summary>
        /// Window size used to determine if point is interesting, [3, 15].
        /// </summary>
        /// 
        /// <remarks><para>The value specifies window size, which is used for initial searching of
        /// corners candidates and then for searching local maximums.</para>
        /// 
        /// <para>Default value is set to <b>3</b>.</para>
        /// </remarks>
        /// 
        /// <exception cref="ArgumentException">Setting value is not odd.</exception>
        /// 
        public int WindowSize
        {
            get { return this.windowSize; }
            set
            {
                // check if value is odd
                if ( ( value & 1 ) == 0 )
                    throw new ArgumentException( "The value shoule be odd." );

                this.windowSize = Math.Max( 3, Math.Min( 15, value ) );
            }
        }

        /// <summary>
        /// Threshold value, which is used to filter out uninteresting points.
        /// </summary>
        /// 
        /// <remarks><para>The value is used to filter uninteresting points - points which have value below
        /// specified threshold value are treated as not corners candidates. Increasing this value decreases
        /// the amount of detected point.</para>
        /// 
        /// <para>Default value is set to <b>500</b>.</para>
        /// </remarks>
        /// 
        public int Threshold
        {
            get { return this.threshold; }
            set { this.threshold = value; }
        }

        private static int[] xDelta = new int[8] { -1, 0, 1, 1, 1, 0, -1, -1 };
        private static int[] yDelta = new int[8] { -1, -1, -1, 0, 1, 1, 1, 0 };

        /// <summary>
        /// Initializes a new instance of the <see cref="MoravecCornersDetector"/> class.
        /// </summary>
        public MoravecCornersDetector( ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoravecCornersDetector"/> class.
        /// </summary>
        /// 
        /// <param name="threshold">Threshold value, which is used to filter out uninteresting points.</param>
        /// 
        public MoravecCornersDetector( int threshold ) :
            this( threshold, 3 ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoravecCornersDetector"/> class.
        /// </summary>
        /// 
        /// <param name="threshold">Threshold value, which is used to filter out uninteresting points.</param>
        /// <param name="windowSize">Window size used to determine if point is interesting.</param>
        /// 
        public MoravecCornersDetector( int threshold, int windowSize )
        {
            this.Threshold = threshold;
            this.WindowSize = windowSize;
        }

        /// <summary>
        /// Process image looking for corners.
        /// </summary>
        /// 
        /// <param name="image">Source image to process.</param>
        /// 
        /// <returns>Returns array of found corners (X-Y coordinates).</returns>
        /// 
        /// <exception cref="UnsupportedImageFormatException">The source image has incorrect pixel format.</exception>
        /// 
        public List<IntPoint> ProcessImage( Bitmap image )
        {
            // check image format
            if (
                ( image.PixelFormat != PixelFormat.Format8bppIndexed ) &&
                ( image.PixelFormat != PixelFormat.Format24bppRgb ) &&
                ( image.PixelFormat != PixelFormat.Format32bppRgb ) &&
                ( image.PixelFormat != PixelFormat.Format32bppArgb )
                )
            {
                throw new UnsupportedImageFormatException( "Unsupported pixel format of the source image." );
            }

            // lock source image
            var imageData = image.LockBits(
                new Rectangle( 0, 0, image.Width, image.Height ),
                ImageLockMode.ReadOnly, image.PixelFormat );

            List<IntPoint> corners;

            try
            {
                // process the image
                corners = this.ProcessImage( new UnmanagedImage( imageData ) );
            }
            finally
            {
                // unlock image
                image.UnlockBits( imageData );
            }

            return corners;
        }

        /// <summary>
        /// Process image looking for corners.
        /// </summary>
        /// 
        /// <param name="imageData">Source image data to process.</param>
        /// 
        /// <returns>Returns array of found corners (X-Y coordinates).</returns>
        /// 
        /// <exception cref="UnsupportedImageFormatException">The source image has incorrect pixel format.</exception>
        /// 
        public List<IntPoint> ProcessImage( BitmapData imageData )
        {
            return this.ProcessImage( new UnmanagedImage( imageData ) );
        }

        /// <summary>
        /// Process image looking for corners.
        /// </summary>
        /// 
        /// <param name="image">Unmanaged source image to process.</param>
        /// 
        /// <returns>Returns array of found corners (X-Y coordinates).</returns>
        ///
        /// <exception cref="UnsupportedImageFormatException">The source image has incorrect pixel format.</exception>
        /// 
        public List<IntPoint> ProcessImage( UnmanagedImage image )
        {
            // check image format
            if (
                ( image.PixelFormat != PixelFormat.Format8bppIndexed ) &&
                ( image.PixelFormat != PixelFormat.Format24bppRgb ) &&
                ( image.PixelFormat != PixelFormat.Format32bppRgb ) &&
                ( image.PixelFormat != PixelFormat.Format32bppArgb )
                )
            {
                throw new UnsupportedImageFormatException( "Unsupported pixel format of the source image." );
            }

            // get source image size
            var width  = image.Width;
            var height = image.Height;
            var stride = image.Stride;
            var pixelSize = Bitmap.GetPixelFormatSize( image.PixelFormat ) / 8;
            // window radius
            var windowRadius = this.windowSize / 2;

            // offset
            var offset = stride - this.windowSize * pixelSize;

            // create moravec cornerness map
            var moravecMap = new int[height, width];

            // do the job
            unsafe
            {
                var ptr = (byte*) image.ImageData.ToPointer( );

                // for each row
                for ( int y = windowRadius, maxY = height - windowRadius; y < maxY; y++ )
                {
                    // for each pixel
                    for ( int x = windowRadius, maxX = width - windowRadius; x < maxX; x++ )
                    {
                        var minSum = int.MaxValue;

                        // go through 8 possible shifting directions
                        for ( var k = 0; k < 8; k++ )
                        {
                            // calculate center of shifted window
                            var sy = y + yDelta[k];
                            var sx = x + xDelta[k];

                            // check if shifted window is within the image
                            if (
                                ( sy < windowRadius ) || ( sy >= maxY ) ||
                                ( sx < windowRadius ) || ( sx >= maxX )
                            )
                            {
                                // skip this shifted window
                                continue;
                            }

                            var sum = 0;

                            var ptr1 = ptr + ( y - windowRadius )  * stride + ( x - windowRadius )  * pixelSize;
                            var ptr2 = ptr + ( sy - windowRadius ) * stride + ( sx - windowRadius ) * pixelSize;

                            // for each windows' rows
                            for ( var i = 0; i < this.windowSize; i++ )
                            {
                                // for each windows' pixels
                                for ( int j = 0, maxJ = this.windowSize * pixelSize; j < maxJ; j++, ptr1++, ptr2++ )
                                {
                                    var dif = *ptr1 - *ptr2;
                                    sum += dif * dif;
                                }
                                ptr1 += offset;
                                ptr2 += offset;
                            }

                            // check if the sum is mimimal
                            if ( sum < minSum )
                            {
                                minSum = sum;
                            }
                        }

                        // threshold the minimum sum
                        if ( minSum < this.threshold )
                        {
                            minSum = 0;
                        }

                        moravecMap[y, x] = minSum;
                    }
                }
            }

            // collect interesting points - only those points, which are local maximums
            var cornersList = new List<IntPoint>( );

            // for each row
            for ( int y = windowRadius, maxY = height - windowRadius; y < maxY; y++ )
            {
                // for each pixel
                for ( int x = windowRadius, maxX = width - windowRadius; x < maxX; x++ )
                {
                    var currentValue = moravecMap[y, x];

                    // for each windows' rows
                    for ( var i = -windowRadius; ( currentValue != 0 ) && ( i <= windowRadius ); i++ )
                    {
                        // for each windows' pixels
                        for ( var j = -windowRadius; j <= windowRadius; j++ )
                        {
                            if ( moravecMap[y + i, x + j] > currentValue )
                            {
                                currentValue = 0;
                                break;
                            }
                        }
                    }

                    // check if this point is really interesting
                    if ( currentValue != 0 )
                    {
                        cornersList.Add( new IntPoint( x, y ) );
                    }
                }
            }

            return cornersList;
        }
    }
}

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
    /// Flood filling with mean color starting from specified point.
    /// </summary>
    /// 
    /// <remarks><para>The filter performs image's area filling (4 directional) starting
    /// from the <see cref="StartingPoint">specified point</see>. It fills
    /// the area of the pointed color, but also fills other colors, which
    /// are similar to the pointed within specified <see cref="Tolerance">tolerance</see>.
    /// The area is filled using its mean color.
    /// </para>
    /// 
    /// <para>The filter is similar to <see cref="PointedColorFloodFill"/> filter, but instead
    /// of filling the are with specified color, it fills the area with its mean color. This means
    /// that this is a two pass filter - first pass is to calculate the mean value and the second pass is to
    /// fill the area. Unlike to <see cref="PointedColorFloodFill"/> filter, this filter has nothing
    /// to do in the case if zero <see cref="Tolerance">tolerance</see> is specified.</para>
    /// 
    /// <para>The filter accepts 8 bpp grayscale images and 24 bpp
    /// color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// PointedMeanFloodFill filter = new PointedMeanFloodFill( );
    /// // configre the filter
    /// filter.Tolerance = Color.FromArgb( 150, 92, 92 );
    /// filter.StartingPoint = new IntPoint( 150, 100 );
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/pointed_mean_fill.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    /// <seealso cref="PointedColorFloodFill"/>
    /// 
    public unsafe class PointedMeanFloodFill : BaseInPlacePartialFilter
    {
        // map of pixels, which are already checked by the flood fill algorithm
        private bool[,] checkedPixels;

        // set of variables (which describe image property and min/max color) to avoid passing them
        // recursively as parameters
        byte* scan0;      // pointer to first image line
        int stride;     // size of image's line
        int startX;     // X1 of bounding rectangle
        int stopX;      // Y1 of bounding rectangle
        int startY;     // X2 of bounding rectangle (including)
        int stopY;      // Y2 of bounding rectangle (including)

        // min/max colors
        byte minR, maxR;      // min/max Red
        byte minG, maxG;      // min/max Green (Gray) color
        byte minB, maxB;      // min/max Blue

        // mean color
        int meanR, meanG, meanB;
        int pixelsCount = 0;

        // starting point to fill from
        private IntPoint startingPoint = new IntPoint( 0, 0 );
        // filling tolerance
        private Color tolerance = Color.FromArgb( 16, 16, 16 );

        // format translation dictionary
        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>( );

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
        /// Flood fill tolerance.
        /// </summary>
        /// 
        /// <remarks><para>The tolerance value determines the level of similarity between
        /// colors to fill and the pointed color. If the value is set to zero, then the
        /// filter does nothing, since the filling area contains only one color and its
        /// filling with mean is meaningless.</para>
        /// 
        /// <para>The tolerance value is specified as <see cref="System.Drawing.Color"/>,
        /// where each component (R, G and B) represents tolerance for the corresponding
        /// component of color. This allows to set different tolerances for red, green
        /// and blue components.</para>
        /// 
        /// <para>Default value is set to <b>(16, 16, 16)</b>.</para>
        /// </remarks>
        /// 
        public Color Tolerance
        {
            get { return this.tolerance; }
            set { this.tolerance = value; }
        }

        /// <summary>
        /// Point to start filling from.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to set the starting point, where filling is
        /// started from.</para>
        /// 
        /// <remarks>Default value is set to <b>(0, 0)</b>.</remarks>
        /// </remarks>
        /// 
        public IntPoint StartingPoint
        {
            get { return this.startingPoint; }
            set { this.startingPoint = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointedMeanFloodFill"/> class.
        /// </summary>
        /// 
        public PointedMeanFloodFill( )
        {
            // initialize format translation dictionary
            this.formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format24bppRgb]    = PixelFormat.Format24bppRgb;
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
            // skip, if there is nothing to fill
            if ( !rect.Contains(this.startingPoint.X, this.startingPoint.Y ) || ( this.tolerance == Color.Black ) )
                return;

            // save bounding rectangle
            this.startX = rect.Left;
            this.startY = rect.Top;
            this.stopX  = rect.Right - 1;
            this.stopY  = rect.Bottom - 1;

            // save image properties
            this.scan0 = (byte*) image.ImageData.ToPointer( );
            this.stride = image.Stride;

            // create map of visited pixels
            this.checkedPixels = new bool[image.Height, image.Width];

            this.pixelsCount = this.meanR = this.meanG = this.meanB = 0;

            if ( image.PixelFormat == PixelFormat.Format8bppIndexed )
            {
                var startColor= *( (byte*)this.CoordsToPointerGray(this.startingPoint.X, this.startingPoint.Y ) );
                this.minG = (byte) ( Math.Max(   0, startColor - this.tolerance.G ) );
                this.maxG = (byte) ( Math.Min( 255, startColor + this.tolerance.G ) );

                this.LinearFloodFill4Gray(this.startingPoint.X, this.startingPoint.Y );

                // calculate mean value
                this.meanG /= this.pixelsCount;
                var fillG = (byte)this.meanG;

                // do fill with the mean
                var src = (byte*) image.ImageData.ToPointer( );
                // allign pointer to the first pixel to process
                src += ( this.startY * this.stride + this.startX );

                var offset = this.stride - rect.Width;

                // for each line	
                for ( var y = this.startY; y <= this.stopY; y++ )
                {
                    // for each pixel
                    for ( var x = this.startX; x <= this.stopX; x++, src++ )
                    {
                        if ( this.checkedPixels[y, x] )
                        {
                            *src = fillG; 
                        }
                    }
                    src += offset;
                }
            }
            else
            {
                var startColor= (byte*)this.CoordsToPointerRGB(this.startingPoint.X, this.startingPoint.Y );

                this.minR = (byte) ( Math.Max(   0, startColor[RGB.R] - this.tolerance.R ) );
                this.maxR = (byte) ( Math.Min( 255, startColor[RGB.R] + this.tolerance.R ) );
                this.minG = (byte) ( Math.Max(   0, startColor[RGB.G] - this.tolerance.G ) );
                this.maxG = (byte) ( Math.Min( 255, startColor[RGB.G] + this.tolerance.G ) );
                this.minB = (byte) ( Math.Max(   0, startColor[RGB.B] - this.tolerance.B ) );
                this.maxB = (byte) ( Math.Min( 255, startColor[RGB.B] + this.tolerance.B ) );

                this.LinearFloodFill4RGB(this.startingPoint.X, this.startingPoint.Y );

                // calculate mean value
                this.meanR /= this.pixelsCount;
                this.meanG /= this.pixelsCount;
                this.meanB /= this.pixelsCount;

                var fillR = (byte)this.meanR;
                var fillG = (byte)this.meanG;
                var fillB = (byte)this.meanB;

                // do fill with the mean
                var src = (byte*) image.ImageData.ToPointer( );
                // allign pointer to the first pixel to process
                src += ( this.startY * this.stride + this.startX * 3);

                var offset = this.stride - rect.Width * 3;

                // for each line	
                for ( var y = this.startY; y <= this.stopY; y++ )
                {
                    // for each pixel
                    for ( var x = this.startX; x <= this.stopX; x++, src += 3 )
                    {
                        if ( this.checkedPixels[y, x] )
                        {
                            src[RGB.R] = fillR;
                            src[RGB.G] = fillG;
                            src[RGB.B] = fillB;
                        }
                    }
                    src += offset;
                }
            }
        }

        // Liner flood fill in 4 directions for grayscale images
        private unsafe void LinearFloodFill4Gray( int x, int y )
        {
            // get image pointer for current (X, Y)
            var p = (byte*)this.CoordsToPointerGray( x, y );

            // find left end of line to fill
            var leftLineEdge = x;
            var ptr = p;

            while ( true )
            {
                // sum value of the current pixel
                this.meanG += *ptr;
                this.pixelsCount++;
                // mark the pixel as checked
                this.checkedPixels[y, leftLineEdge] = true;

                leftLineEdge--;
                ptr -= 1;

                // check if we need to stop on the edge of image or color area
                if ( ( leftLineEdge < this.startX ) || ( this.checkedPixels[y, leftLineEdge] ) || ( !this.CheckGrayPixel( *ptr ) ) )
                    break;

            }
            leftLineEdge++;

            // find right end of line to fill
            var rightLineEdge = x + 1;
            ptr = p + 1;

            // while we don't need to stop on the edge of image or color area
            while ( !( rightLineEdge > this.stopX || ( this.checkedPixels[y, rightLineEdge] ) || ( !this.CheckGrayPixel( *ptr ) ) ) )
            {
                // sum value of the current pixel
                this.meanG += *ptr;
                this.pixelsCount++;
                // mark the pixel as checked
                this.checkedPixels[y, rightLineEdge] = true;

                rightLineEdge++;
                ptr += 1;

            }
            rightLineEdge--;


            // loop to go up and down
            ptr = (byte*)this.CoordsToPointerGray( leftLineEdge, y );
            for ( var i = leftLineEdge; i <= rightLineEdge; i++, ptr++ )
            {
                // go up
                if ( ( y > this.startY ) && ( !this.checkedPixels[y - 1, i] ) && ( this.CheckGrayPixel( *( ptr - this.stride ) ) ) )
                    this.LinearFloodFill4Gray( i, y - 1 );
                // go down
                if ( ( y < this.stopY ) && ( !this.checkedPixels[y + 1, i] ) && ( this.CheckGrayPixel( *( ptr + this.stride ) ) ) )
                    this.LinearFloodFill4Gray( i, y + 1 );
            }
        }

        // Liner flood fill in 4 directions for RGB
        private unsafe void LinearFloodFill4RGB( int x, int y )
        {
            // get image pointer for current (X, Y)
            var p = (byte*)this.CoordsToPointerRGB( x, y );

            // find left end of line to fill
            var leftLineEdge = x;
            var ptr = p;

            while ( true )
            {
                // sum value of the current pixel
                this.meanR += ptr[RGB.R];
                this.meanG += ptr[RGB.G];
                this.meanB += ptr[RGB.B];
                this.pixelsCount++;
                // mark the pixel as checked
                this.checkedPixels[y, leftLineEdge] = true;

                leftLineEdge--;
                ptr -= 3;

                // check if we need to stop on the edge of image or color area
                if ( ( leftLineEdge < this.startX ) || ( this.checkedPixels[y, leftLineEdge] ) || ( !this.CheckRGBPixel( ptr ) ) )
                    break;

            }
            leftLineEdge++;

            // find right end of line to fill
            var rightLineEdge = x + 1;
            ptr = p + 3;

            // while we don't need to stop on the edge of image or color area
            while ( !( rightLineEdge > this.stopX || ( this.checkedPixels[y, rightLineEdge] ) || ( !this.CheckRGBPixel( ptr ) ) ) )
            {
                // sum value of the current pixel
                this.meanR += ptr[RGB.R];
                this.meanG += ptr[RGB.G];
                this.meanB += ptr[RGB.B];
                this.pixelsCount++;
                // mark the pixel as checked
                this.checkedPixels[y, rightLineEdge] = true;

                rightLineEdge++;
                ptr += 3;
            }
            rightLineEdge--;


            // loop to go up and down
            ptr = (byte*)this.CoordsToPointerRGB( leftLineEdge, y );
            for ( var i = leftLineEdge; i <= rightLineEdge; i++, ptr += 3 )
            {
                // go up
                if ( ( y > this.startY ) && ( !this.checkedPixels[y - 1, i] ) && ( this.CheckRGBPixel( ptr - this.stride ) ) )
                    this.LinearFloodFill4RGB( i, y - 1 );
                // go down
                if ( ( y < this.stopY ) && ( !this.checkedPixels[y + 1, i] ) && ( this.CheckRGBPixel( ptr + this.stride ) ) )
                    this.LinearFloodFill4RGB( i, y + 1 );
            }
        }

        // Check if pixel equals to the starting color within required tolerance
        private unsafe bool CheckGrayPixel( byte pixel )
        {
            return ( pixel >= this.minG ) && ( pixel <= this.maxG );
        }

        // Check if pixel equals to the starting color within required tolerance
        private unsafe bool CheckRGBPixel( byte* pixel )
        {
            return  ( pixel[RGB.R] >= this.minR ) && ( pixel[RGB.R] <= this.maxR ) &&
                    ( pixel[RGB.G] >= this.minG ) && ( pixel[RGB.G] <= this.maxG ) &&
                    ( pixel[RGB.B] >= this.minB ) && ( pixel[RGB.B] <= this.maxB );
        }

        // Convert image coordinate to pointer for Grayscale images
        private byte* CoordsToPointerGray( int x, int y )
        {
            return this.scan0 + ( this.stride * y ) + x;
        }

        // Convert image coordinate to pointer for RGB images
        private byte* CoordsToPointerRGB( int x, int y )
        {
            return this.scan0 + ( this.stride * y ) + x * 3;
        }
    }
}

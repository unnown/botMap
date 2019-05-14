// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2005-2010
// contacts@aforgenet.com
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Flood filling with specified color starting from specified point.
    /// </summary>
    /// 
    /// <remarks><para>The filter performs image's area filling (4 directional) starting
    /// from the <see cref="StartingPoint">specified point</see>. It fills
    /// the area of the pointed color, but also fills other colors, which
    /// are similar to the pointed within specified <see cref="Tolerance">tolerance</see>.
    /// The area is filled using <see cref="FillColor">specified fill color</see>.
    /// </para>
    /// 
    /// <para>The filter accepts 8 bpp grayscale images and 24 bpp
    /// color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// PointedColorFloodFill filter = new PointedColorFloodFill( );
    /// // configure the filter
    /// filter.Tolerance = Color.FromArgb( 150, 92, 92 );
    /// filter.FillColor = Color.FromArgb( 255, 255, 255 );
    /// filter.StartingPoint = new IntPoint( 150, 100 );
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/pointed_color_fill.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    /// <seealso cref="PointedMeanFloodFill"/>
    /// 
    public unsafe class PointedColorFloodFill : BaseInPlacePartialFilter
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

        // fill color
        byte fillR, fillG, fillB;

        // starting point to fill from
        private IntPoint startingPoint = new IntPoint( 0, 0 );
        // filling tolerance
        private Color tolerance = Color.FromArgb( 0, 0, 0 );

        // format translation dictionary
        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>( );

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations
        {
            get { return this.formatTranslations; }
        }

        /// <summary>
        /// Flood fill tolerance.
        /// </summary>
        /// 
        /// <remarks><para>The tolerance value determines which colors to fill. If the
        /// value is set to 0, then only color of the <see cref="StartingPoint">pointed pixel</see>
        /// is filled. If the value is not 0, then other colors may be filled as well,
        /// which are similar to the color of the pointed pixel within the specified
        /// tolerance.</para>
        /// 
        /// <para>The tolerance value is specified as <see cref="System.Drawing.Color"/>,
        /// where each component (R, G and B) represents tolerance for the corresponding
        /// component of color. This allows to set different tolerances for red, green
        /// and blue components.</para>
        /// </remarks>
        /// 
        public Color Tolerance
        {
            get { return this.tolerance; }
            set { this.tolerance = value; }
        }

        /// <summary>
        /// Fill color.
        /// </summary>
        /// 
        /// <remarks><para>The fill color is used to fill image's area starting from the
        /// <see cref="StartingPoint">specified point</see>.</para>
        /// 
        /// <para>For grayscale images the color needs to be specified with all three
        /// RGB values set to the same value, (128, 128, 128) for example.</para>
        /// 
        /// <para>Default value is set to <b>black</b>.</para>
        /// </remarks>
        /// 
        public Color FillColor
        {
            get { return Color.FromArgb(this.fillR , this.fillG , this.fillB ); }
            set
            {
                this.fillR = value.R;
                this.fillG = value.G;
                this.fillB = value.B;
            }
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
        /// Initializes a new instance of the <see cref="PointedColorFloodFill"/> class.
        /// </summary>
        /// 
        public PointedColorFloodFill( )
        {
            // initialize format translation dictionary
            this.formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format24bppRgb]    = PixelFormat.Format24bppRgb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointedColorFloodFill"/> class.
        /// </summary>
        /// 
        /// <param name="fillColor">Fill color.</param>
        /// 
        public PointedColorFloodFill( Color fillColor )
            : this( )
        {
            this.FillColor = fillColor;
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
            if ( !rect.Contains(this.startingPoint.X, this.startingPoint.Y ) )
                return;

            // save bounding rectangle
            this.startX = rect.Left;
            this.startY = rect.Top;
            this.stopX  = rect.Right - 1;
            this.stopY  = rect.Bottom - 1;

            // save image properties
            this.scan0 = (byte*) image.ImageData.ToPointer( );
            this.stride = image.Stride;

            // create map visited pixels
            this.checkedPixels = new bool[image.Height, image.Width];

            if ( image.PixelFormat == PixelFormat.Format8bppIndexed )
            {
                var startColor = *( (byte*)this.CoordsToPointerGray(this.startingPoint.X, this.startingPoint.Y ) );
                this.minG = (byte) ( Math.Max(   0, startColor - this.tolerance.G ) );
                this.maxG = (byte) ( Math.Min( 255, startColor + this.tolerance.G ) );

                this.LinearFloodFill4Gray(this.startingPoint );
            }
            else
            {
                var startColor = (byte*)this.CoordsToPointerRGB(this.startingPoint.X, this.startingPoint.Y );

                this.minR = (byte) ( Math.Max(   0, startColor[RGB.R] - this.tolerance.R ) );
                this.maxR = (byte) ( Math.Min( 255, startColor[RGB.R] + this.tolerance.R ) );
                this.minG = (byte) ( Math.Max(   0, startColor[RGB.G] - this.tolerance.G ) );
                this.maxG = (byte) ( Math.Min( 255, startColor[RGB.G] + this.tolerance.G ) );
                this.minB = (byte) ( Math.Max(   0, startColor[RGB.B] - this.tolerance.B ) );
                this.maxB = (byte) ( Math.Min( 255, startColor[RGB.B] + this.tolerance.B ) );

                this.LinearFloodFill4RGB(this.startingPoint );
            }
        }

        // Liner flood fill in 4 directions for grayscale images
        private unsafe void LinearFloodFill4Gray( IntPoint startingPoint )
        {
            var points = new Queue<IntPoint>( );
            points.Enqueue( startingPoint );

            while ( points.Count > 0 )
            {
                var point = points.Dequeue( );

                var x = point.X;
                var y = point.Y;

                // get image pointer for current (X, Y)
                var p = (byte*)this.CoordsToPointerGray( x, y );

                // find left end of line to fill
                var leftLineEdge = x;
                var ptr = p;

                while ( true )
                {
                    // fill current pixel
                    *ptr = this.fillG;
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
                var rightLineEdge = x;
                ptr = p;

                while ( true )
                {
                    // fill current pixel
                    *ptr = this.fillG;
                    // mark the pixel as checked
                    this.checkedPixels[y, rightLineEdge] = true;

                    rightLineEdge++;
                    ptr += 1;

                    // check if we need to stop on the edge of image or color area
                    if ( rightLineEdge > this.stopX || ( this.checkedPixels[y, rightLineEdge] ) || ( !this.CheckGrayPixel( *ptr ) ) )
                        break;
                }
                rightLineEdge--;


                // loop to go up and down
                ptr = (byte*)this.CoordsToPointerGray( leftLineEdge, y );

                var upperPointIsQueued = false;
                var lowerPointIsQueued = false;
                var upperY = y - 1;
                var lowerY = y + 1;

                for ( var i = leftLineEdge; i <= rightLineEdge; i++, ptr++ )
                {
                    // go up
                    if ( ( y > this.startY ) && ( !this.checkedPixels[y - 1, i] ) && ( this.CheckGrayPixel( *( ptr - this.stride ) ) ) )
                    {
                        if ( !upperPointIsQueued )
                        {
                            points.Enqueue( new IntPoint( i, upperY ) );
                            upperPointIsQueued = true;
                        }
                    }
                    else
                    {
                        upperPointIsQueued = false;
                    }

                    // go down
                    if ( ( y < this.stopY ) && ( !this.checkedPixels[y + 1, i] ) && ( this.CheckGrayPixel( *( ptr + this.stride ) ) ) )
                    {
                        if ( !lowerPointIsQueued )
                        {
                            points.Enqueue( new IntPoint( i, lowerY ) );
                            lowerPointIsQueued = true;
                        }
                    }
                    else
                    {
                        lowerPointIsQueued = false;
                    }
                }
            }
        }

        // Liner flood fill in 4 directions for RGB
        private unsafe void LinearFloodFill4RGB( IntPoint startPoint )
        {
            var points = new Queue<IntPoint>( );
            points.Enqueue(this.startingPoint );

            while ( points.Count > 0 )
            {
                var point = points.Dequeue( );

                var x = point.X;
                var y = point.Y;

                // get image pointer for current (X, Y)
                var p = (byte*)this.CoordsToPointerRGB( x, y );

                // find left end of line to fill
                var leftLineEdge = x;
                var ptr = p;

                while ( true )
                {
                    // fill current pixel
                    ptr[RGB.R] = this.fillR;
                    ptr[RGB.G] = this.fillG;
                    ptr[RGB.B] = this.fillB;
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
                var rightLineEdge = x;
                ptr = p;

                while ( true )
                {
                    // fill current pixel
                    ptr[RGB.R] = this.fillR;
                    ptr[RGB.G] = this.fillG;
                    ptr[RGB.B] = this.fillB;
                    // mark the pixel as checked
                    this.checkedPixels[y, rightLineEdge] = true;

                    rightLineEdge++;
                    ptr += 3;

                    // check if we need to stop on the edge of image or color area
                    if ( rightLineEdge > this.stopX || ( this.checkedPixels[y, rightLineEdge] ) || ( !this.CheckRGBPixel( ptr ) ) )
                        break;
                }
                rightLineEdge--;

                // loop to go up and down
                ptr = (byte*)this.CoordsToPointerRGB( leftLineEdge, y );

                var upperPointIsQueued = false;
                var lowerPointIsQueued = false;
                var upperY = y - 1;
                var lowerY = y + 1;

                for ( var i = leftLineEdge; i <= rightLineEdge; i++, ptr += 3 )
                {
                    // go up
                    if ( ( y > this.startY ) && ( !this.checkedPixels[upperY, i] ) && ( this.CheckRGBPixel( ptr - this.stride ) ) )
                    {
                        if ( !upperPointIsQueued )
                        {
                            points.Enqueue( new IntPoint( i, upperY ) );
                            upperPointIsQueued = true;
                        }
                    }
                    else
                    {
                        upperPointIsQueued = false;
                    }

                    // go down
                    if ( ( y < this.stopY ) && ( !this.checkedPixels[lowerY, i] ) && ( this.CheckRGBPixel( ptr + this.stride ) ) )
                    {
                        if ( !lowerPointIsQueued )
                        {
                            points.Enqueue( new IntPoint( i, lowerY ) );
                            lowerPointIsQueued = true;
                        }
                    }
                    else
                    {
                        lowerPointIsQueued = false;
                    }
                }
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

// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright � Andrew Kirillov, 2005-2010
// andrew.kirillov@aforgenet.com
//

namespace AForge.Imaging
{
    using System;
    using System.Collections;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Hough circle.
    /// </summary>
    /// 
    /// <remarks>Represents circle of Hough transform.</remarks>
    /// 
    /// <seealso cref="HoughCircleTransformation"/>
    /// 
    public class HoughCircle : IComparable
    {
        /// <summary>
        /// Circle center's X coordinate.
        /// </summary>
        public readonly int X;

        /// <summary>
        /// Circle center's Y coordinate.
        /// </summary>
        public readonly int Y;

        /// <summary>
        /// Circle's radius.
        /// </summary>
        public readonly int Radius;

        /// <summary>
        /// Line's absolute intensity.
        /// </summary>
        public readonly short Intensity;

        /// <summary>
        /// Line's relative intensity.
        /// </summary>
        public readonly double RelativeIntensity;

        /// <summary>
        /// Initializes a new instance of the <see cref="HoughCircle"/> class.
        /// </summary>
        /// 
        /// <param name="x">Circle's X coordinate.</param>
        /// <param name="y">Circle's Y coordinate.</param>
        /// <param name="radius">Circle's radius.</param>
        /// <param name="intensity">Circle's absolute intensity.</param>
        /// <param name="relativeIntensity">Circle's relative intensity.</param>
        /// 
        public HoughCircle( int x, int y, int radius, short intensity, double relativeIntensity )
        {
            this.X = x;
            this.Y = y;
            this.Radius = radius;
            this.Intensity = intensity;
            this.RelativeIntensity = relativeIntensity;
        }

        /// <summary>
        /// Compare the object with another instance of this class.
        /// </summary>
        /// 
        /// <param name="value">Object to compare with.</param>
        /// 
        /// <returns><para>A signed number indicating the relative values of this instance and <b>value</b>: 1) greater than zero - 
        /// this instance is greater than <b>value</b>; 2) zero - this instance is equal to <b>value</b>;
        /// 3) greater than zero - this instance is less than <b>value</b>.</para>
        /// 
        /// <para><note>The sort order is descending.</note></para></returns>
        /// 
        /// <remarks>
        /// <para><note>Object are compared using their <see cref="Intensity">intensity</see> value.</note></para>
        /// </remarks>
        /// 
        public int CompareTo( object value )
        {
            return ( -this.Intensity.CompareTo( ( (HoughCircle) value ).Intensity ) );
        }
    }

    /// <summary>
    /// Hough circle transformation.
    /// </summary>
    ///
    /// <remarks><para>The class implements Hough circle transformation, which allows to detect
    /// circles of specified radius in an image.</para>
    /// 
    /// <para>The class accepts binary images for processing, which are represented by 8 bpp grayscale images.
    /// All black pixels (0 pixel's value) are treated as background, but pixels with different value are
    /// treated as circles' pixels.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// HoughCircleTransformation circleTransform = new HoughCircleTransformation( 35 );
    /// // apply Hough circle transform
    /// circleTransform.ProcessImage( sourceImage );
    /// Bitmap houghCirlceImage = circleTransform.ToBitmap( );
    /// // get circles using relative intensity
    /// HoughCircle[] circles = circleTransform.GetCirclesByRelativeIntensity( 0.5 );
    /// 
    /// foreach ( HoughCircle circle in circles )
    /// {
    ///     // ...
    /// }
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample8.jpg" width="400" height="300" />
    /// <para><b>Hough circle transformation image:</b></para>
    /// <img src="img/imaging/hough_circles.jpg" width="400" height="300" />
    /// </remarks>
    /// 
    /// <seealso cref="HoughLineTransformation"/>
    /// 
    public class HoughCircleTransformation
    {
        // circle radius to detect
        private int radiusToDetect;

        // Hough map
        private short[,] houghMap;
        private short maxMapIntensity = 0;

        // Hough map's width and height
        private int width;
        private int height;

        private int localPeakRadius = 4;
        private short minCircleIntensity = 10;
        private ArrayList circles = new ArrayList( );

        /// <summary>
        /// Minimum circle's intensity in Hough map to recognize a circle.
        /// </summary>
        ///
        /// <remarks><para>The value sets minimum intensity level for a circle. If a value in Hough
        /// map has lower intensity, then it is not treated as a circle.</para>
        /// 
        /// <para>Default value is set to <b>10</b>.</para></remarks>
        ///
        public short MinCircleIntensity
        {
            get { return this.minCircleIntensity; }
            set { this.minCircleIntensity = value; }
        }

        /// <summary>
        /// Radius for searching local peak value.
        /// </summary>
        /// 
        /// <remarks><para>The value determines radius around a map's value, which is analyzed to determine
        /// if the map's value is a local maximum in specified area.</para>
        /// 
        /// <para>Default value is set to <b>4</b>. Minimum value is <b>1</b>. Maximum value is <b>10</b>.</para></remarks>
        /// 
        public int LocalPeakRadius
        {
            get { return this.localPeakRadius; }
            set { this.localPeakRadius = Math.Max( 1, Math.Min( 10, value ) ); }
        }

        /// <summary>
        /// Maximum found intensity in Hough map.
        /// </summary>
        /// 
        /// <remarks><para>The property provides maximum found circle's intensity.</para></remarks>
        /// 
        public short MaxIntensity
        {
            get { return this.maxMapIntensity; }
        }

        /// <summary>
        /// Found circles count.
        /// </summary>
        /// 
        /// <remarks><para>The property provides total number of found circles, which intensity is higher (or equal to),
        /// than the requested <see cref="MinCircleIntensity">minimum intensity</see>.</para></remarks>
        /// 
        public int CirclesCount
        {
            get { return this.circles.Count; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HoughCircleTransformation"/> class.
        /// </summary>
        /// 
        /// <param name="radiusToDetect">Circles' radius to detect.</param>
        /// 
        public HoughCircleTransformation( int radiusToDetect )
		{
            this.radiusToDetect = radiusToDetect;
		}

        /// <summary>
        /// Process an image building Hough map.
        /// </summary>
        /// 
        /// <param name="image">Source image to process.</param>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the source image.</exception>
        /// 
        public void ProcessImage( Bitmap image )
        {
            // check image format
            if ( image.PixelFormat != PixelFormat.Format8bppIndexed )
            {
                throw new UnsupportedImageFormatException( "Unsupported pixel format of the source image." );
            }

            // lock source image
            var imageData = image.LockBits(
                new Rectangle( 0, 0, image.Width, image.Height ),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed );

            try
            {
                // process the image
                this.ProcessImage( new UnmanagedImage( imageData ) );
            }
            finally
            {
                // unlock image
                image.UnlockBits( imageData );
            }
        }

        /// <summary>
        /// Process an image building Hough map.
        /// </summary>
        /// 
        /// <param name="imageData">Source image data to process.</param>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the source image.</exception>
        /// 
        public void ProcessImage( BitmapData imageData )
        {
            this.ProcessImage( new UnmanagedImage( imageData ) );
        }

        /// <summary>
        /// Process an image building Hough map.
        /// </summary>
        /// 
        /// <param name="image">Source unmanaged image to process.</param>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the source image.</exception>
        /// 
        public void ProcessImage( UnmanagedImage image )
        {
            if ( image.PixelFormat != PixelFormat.Format8bppIndexed )
            {
                throw new UnsupportedImageFormatException( "Unsupported pixel format of the source image." );
            }

            // get source image size
            this.width  = image.Width;
            this.height = image.Height;

            var srcOffset = image.Stride - this.width;

            // allocate Hough map of the same size like image
            this.houghMap = new short[this.height , this.width];

            // do the job
            unsafe
            {
                var src = (byte*) image.ImageData.ToPointer( );

                // for each row
                for ( var y = 0; y < this.height; y++ )
                {
                    // for each pixel
                    for ( var x = 0; x < this.width; x++, src++ )
                    {
                        if ( *src != 0 )
                        {
                            this.DrawHoughCircle( x, y );
                        }
                    }
                    src += srcOffset;
                }
            }

            // find max value in Hough map
            this.maxMapIntensity = 0;
            for ( var i = 0; i < this.height; i++ )
            {
                for ( var j = 0; j < this.width; j++ )
                {
                    if ( this.houghMap[i, j] > this.maxMapIntensity )
                    {
                        this.maxMapIntensity = this.houghMap[i, j];
                    }
                }
            }

            this.CollectCircles( );
        }

        /// <summary>
        /// �onvert Hough map to bitmap. 
        /// </summary>
        /// 
        /// <returns>Returns 8 bppp grayscale bitmap, which shows Hough map.</returns>
        /// 
        /// <exception cref="ApplicationException">Hough transformation was not yet done by calling
        /// ProcessImage() method.</exception>
        /// 
        public Bitmap ToBitmap( )
        {
            // check if Hough transformation was made already
            if ( this.houghMap == null )
            {
                throw new ApplicationException( "Hough transformation was not done yet." );
            }

            var width = this.houghMap.GetLength( 1 );
            var height = this.houghMap.GetLength( 0 );

            // create new image
            var image = AForge.Imaging.Image.CreateGrayscaleImage( width, height );

            // lock destination bitmap data
            var imageData = image.LockBits(
                new Rectangle( 0, 0, width, height ),
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed );

            var offset = imageData.Stride - width;
            var scale = 255.0f / this.maxMapIntensity;

            // do the job
            unsafe
            {
                var dst = (byte*) imageData.Scan0.ToPointer( );

                for ( var y = 0; y < height; y++ )
                {
                    for ( var x = 0; x < width; x++, dst++ )
                    {
                        *dst = (byte) System.Math.Min( 255, (int) ( scale * this.houghMap[y, x] ) );
                    }
                    dst += offset;
                }
            }

            // unlock destination images
            image.UnlockBits( imageData );

            return image;
        }

        /// <summary>
        /// Get specified amount of circles with highest intensity.
        /// </summary>
        /// 
        /// <param name="count">Amount of circles to get.</param>
        /// 
        /// <returns>Returns arrary of most intesive circles. If there are no circles detected,
        /// the returned array has zero length.</returns>
        /// 
        public HoughCircle[] GetMostIntensiveCircles( int count )
        {
            // lines count
            var n = Math.Min( count, this.circles.Count );

            // result array
            var dst = new HoughCircle[n];
            this.circles.CopyTo( 0, dst, 0, n );

            return dst;
        }

        /// <summary>
        /// Get circles with relative intensity higher then specified value.
        /// </summary>
        /// 
        /// <param name="minRelativeIntensity">Minimum relative intesity of circles.</param>
        /// 
        /// <returns>Returns arrary of most intesive circles. If there are no circles detected,
        /// the returned array has zero length.</returns>
        /// 
        public HoughCircle[] GetCirclesByRelativeIntensity( double minRelativeIntensity )
        {
            int count = 0, n = this.circles.Count;

            while ( ( count < n ) && ( ( (HoughCircle)this.circles[count] ).RelativeIntensity >= minRelativeIntensity ) )
                count++;

            return this.GetMostIntensiveCircles( count );
        }


        // Collect circles with intesities greater or equal then specified
        private void CollectCircles( )
        {
            short intensity;
            bool foundGreater;

            // clean circles collection
            this.circles.Clear( );

			// for each Y coordinate
            for ( var y = 0; y < this.height; y++ )
            {
                // for each X coordinate
                for ( var x = 0; x < this.width; x++ )
                {
                    // get current value
                    intensity = this.houghMap[y, x];

                    if ( intensity < this.minCircleIntensity )
                        continue;

                    foundGreater = false;

					// check neighboors
                    for ( int ty = y - this.localPeakRadius, tyMax = y + this.localPeakRadius; ty < tyMax; ty++ )
                    {
                        // continue if the coordinate is out of map
                        if ( ty < 0 )
                            continue;
                        // break if it is not local maximum or coordinate is out of map
                        if ( ( foundGreater == true ) || ( ty >= this.height ) )
                            break;

                        for ( int tx = x - this.localPeakRadius, txMax = x + this.localPeakRadius; tx < txMax; tx++ )
                        {
                            // continue or break if the coordinate is out of map
                            if ( tx < 0 )
                                continue;
                            if ( tx >= this.width )
                                break;

                            // compare the neighboor with current value
                            if ( this.houghMap[ty, tx] > intensity )
                            {
                                foundGreater = true;
                                break;
                            }
                        }
                    }

                    // was it local maximum ?
                    if ( !foundGreater )
                    {
                        // we have local maximum
                        this.circles.Add( new HoughCircle( x, y, this.radiusToDetect , intensity, (double) intensity / this.maxMapIntensity ) );
                    }
                }
            }

            this.circles.Sort( );
        }

        // Draw Hough circle:
        // http://www.cs.unc.edu/~mcmillan/comp136/Lecture7/circle.html
        //
        // TODO: more optimizations of circle drawing could be done.
        //
        private void DrawHoughCircle( int xCenter, int yCenter )
        {
            var x = 0;
            var y = this.radiusToDetect;
            var p = ( 5 - this.radiusToDetect * 4 ) / 4;

            this.SetHoughCirclePoints( xCenter, yCenter, x, y );

            while ( x < y )
            {
                x++;
                if ( p < 0 )
                {
                    p += 2 * x + 1;
                }
                else
                {
                    y--;
                    p += 2 * ( x - y ) + 1;
                }
                this.SetHoughCirclePoints( xCenter, yCenter, x, y );
            }
        }

        // Set circle points
        private void SetHoughCirclePoints( int cx, int cy, int x, int y )
        {
            if ( x == 0 )
            {
                this.SetHoughPoint( cx, cy + y );
                this.SetHoughPoint( cx, cy - y );
                this.SetHoughPoint( cx + y, cy );
                this.SetHoughPoint( cx - y, cy );
            }
            else if ( x == y )
            {
                this.SetHoughPoint( cx + x, cy + y );
                this.SetHoughPoint( cx - x, cy + y );
                this.SetHoughPoint( cx + x, cy - y );
                this.SetHoughPoint( cx - x, cy - y );
            }
            else if ( x < y )
            {
                this.SetHoughPoint( cx + x, cy + y );
                this.SetHoughPoint( cx - x, cy + y );
                this.SetHoughPoint( cx + x, cy - y );
                this.SetHoughPoint( cx - x, cy - y );
                this.SetHoughPoint( cx + y, cy + x );
                this.SetHoughPoint( cx - y, cy + x );
                this.SetHoughPoint( cx + y, cy - x );
                this.SetHoughPoint( cx - y, cy - x );
            }
        }

        // Set point
        private void SetHoughPoint( int x, int y )
        {
            if ( ( x >= 0 ) && ( y >= 0 ) && ( x < this.width ) && ( y < this.height ) )
            {
                this.houghMap[y, x]++;
            }
        }
    }
}

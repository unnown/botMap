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
    /// Mirroring filter.
    /// </summary>
    /// 
    /// <remarks><para>The filter mirrors image around X and/or Y axis (horizontal and vertical
    /// mirroring).</para>
    /// 
    /// <para>The filter accepts 8 bpp grayscale images and 24 bpp
    /// color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// Mirror filter = new Mirror( false, true );
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/mirror.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    public class Mirror : BaseInPlacePartialFilter
    {
        private bool mirrorX = false;
        private bool mirrorY = false;

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
        /// Specifies if mirroring should be done for X axis (horizontal mirroring).
        /// </summary>
        /// 
        public bool MirrorX
        {
            get { return this.mirrorX; }
            set { this.mirrorX = value; }
        }

        /// <summary>
        /// Specifies if mirroring should be done for Y axis (vertical mirroring).
        /// </summary>
        /// 
        public bool MirrorY
        {
            get { return this.mirrorY; }
            set { this.mirrorY = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mirror"/> class.
        /// </summary>
        /// 
        /// <param name="mirrorX">Specifies if mirroring should be done for X axis.</param>
        /// <param name="mirrorY">Specifies if mirroring should be done for Y axis</param>
        /// 
        public Mirror( bool mirrorX, bool mirrorY )
        {
            this.mirrorX = mirrorX;
            this.MirrorY = mirrorY;

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
            var pixelSize = ( image.PixelFormat == PixelFormat.Format8bppIndexed ) ? 1 : 3;

            var width   = rect.Width;
            var height  = rect.Height;

            // processing start and stop X,Y positions
            var startY  = rect.Top;
            var stopY   = startY + height;
            var startX  = rect.Left;
            var stopX   = startX + width;
            var startXInBytes = startX * pixelSize;
            var stopXInBytes  = stopX * pixelSize;

            var stride = image.Stride;

            // perform Y mirroring
            if ( this.mirrorY )
            {
                // first pointer - points to the first pixel in line
                var ptr1 = (byte*) image.ImageData.ToPointer( );
                ptr1 += ( startY * stride + startX * pixelSize );
                // second pointer - points to the last pixel in line
                var ptr2 = (byte*) image.ImageData.ToPointer( );
                ptr2 += ( startY * stride + ( stopX - 1 ) * pixelSize );

                // offsets
                var offset1 = stride - ( width >> 1 ) * pixelSize;
                var offset2 = stride + ( width >> 1 ) * pixelSize;

                // temporary value for swapping
                byte v;

                if ( image.PixelFormat == PixelFormat.Format8bppIndexed )
                {
                    // grayscale mirroring

                    // for each line
                    for ( var y = startY; y < stopY; y++ )
                    {
                        // for each pixel
                        for ( int x = startX, halfStopX = startX + ( width >> 1 ); x < halfStopX; x++, ptr1++, ptr2-- )
                        {
                            // swap values
                            v = *ptr1;
                            *ptr1 = *ptr2;
                            *ptr2 = v;
                        }
                        ptr1 += offset1;
                        ptr2 += offset2;
                    }
                }
                else
                {
                    // color mirroring

                    // for each line
                    for ( var y = startY; y < stopY; y++ )
                    {
                        // for each pixel
                        for ( int x = startX, halfStopX = startX + ( width >> 1 ); x < halfStopX; x++, ptr1 += 3, ptr2 -= 3 )
                        {
                            // swap Red
                            v = ptr1[RGB.R];
                            ptr1[RGB.R] = ptr2[RGB.R];
                            ptr2[RGB.R] = v;

                            // swap Green
                            v = ptr1[RGB.G];
                            ptr1[RGB.G] = ptr2[RGB.G];
                            ptr2[RGB.G] = v;

                            // swap Blue
                            v = ptr1[RGB.B];
                            ptr1[RGB.B] = ptr2[RGB.B];
                            ptr2[RGB.B] = v;
                        }
                        ptr1 += offset1;
                        ptr2 += offset2;
                    }
                }
            }

            // perform X mirroring
            if ( this.mirrorX )
            {
                var offset = stride - rect.Width * pixelSize;

                // first pointer - points to the first line
                var ptr1 = (byte*) image.ImageData.ToPointer( );
                ptr1 += ( startY * stride + startX * pixelSize );
                // second pointer - points to the last line
                var ptr2 = (byte*) image.ImageData.ToPointer( );
                ptr2 += ( ( stopY - 1 ) * stride + startX * pixelSize );

                // temporary value for swapping
                byte v;

                // for each line
                for ( int y = startY, halfStopY = startY + ( height >> 1 ); y < halfStopY; y++ )
                {
                    // for each pixel
                    for ( var x = startXInBytes; x < stopXInBytes; x++, ptr1++, ptr2++ )
                    {
                        // swap values
                        v = *ptr1;
                        *ptr1 = *ptr2;
                        *ptr2 = v;
                    }
                    ptr1 += offset;
                    ptr2 += offset - stride - stride;
                }
            }
        }
    }
}

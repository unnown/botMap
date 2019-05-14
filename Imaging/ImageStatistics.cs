// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2005-2011
// contacts@aforgenet.com
//

namespace AForge.Imaging
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using AForge.Math;

    /// <summary>
    /// Gather statistics about image in RGB color space.
    /// </summary>
    /// 
    /// <remarks><para>The class is used to accumulate statistical values about images,
    /// like histogram, mean, standard deviation, etc. for each color channel in RGB color
    /// space.</para>
    /// 
    /// <para>The class accepts 8 bpp grayscale and 24/32 bpp color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // gather statistics
    /// ImageStatistics stat = new ImageStatistics( image );
    /// // get red channel's histogram
    /// Histogram red = stat.Red;
    /// // check mean value of red channel
    /// if ( red.Mean > 128 )
    /// {
    ///     // do further processing
    /// }
    /// </code>
    /// </remarks>
    /// 
    /// <seealso cref="AForge.Math.Histogram"/>
    /// 
    public class ImageStatistics
    {
        private Histogram red;
        private Histogram green;
        private Histogram blue;
        private Histogram gray;

        private Histogram redWithoutBlack;
        private Histogram greenWithoutBlack;
        private Histogram blueWithoutBlack;
        private Histogram grayWithoutBlack;

        private int pixels;
        private int pixelsWithoutBlack;

        /// <summary>
        /// Histogram of red channel.
        /// </summary>
        /// 
        /// <remarks><para><note>The property is valid only for color images
        /// (see <see cref="IsGrayscale"/> property).</note></para></remarks>
        /// 
        public Histogram Red
        {
            get
            {
                if ( this.red == null )
                {
                    throw new InvalidImagePropertiesException( ExceptionMessage.ColorHistogramException );
                }
                return this.red;
            }
        }

        /// <summary>
        /// Histogram of green channel.
        /// </summary>
        /// 
        /// <remarks><para><note>The property is valid only for color images
        /// (see <see cref="IsGrayscale"/> property).</note></para></remarks>
        /// 
        public Histogram Green
        {
            get
            {
                if ( this.green == null )
                {
                    throw new InvalidImagePropertiesException( ExceptionMessage.ColorHistogramException );
                }
                return this.green;
            }
        }

        /// <summary>
        /// Histogram of blue channel.
        /// </summary>
        /// 
        /// <remarks><para><note>The property is valid only for color images
        /// (see <see cref="IsGrayscale"/> property).</note></para></remarks>
        /// 
        public Histogram Blue
        {
            get
            {
                if ( this.blue == null )
                {
                    throw new InvalidImagePropertiesException( ExceptionMessage.ColorHistogramException );
                }
                return this.blue;
            }
        }

        /// <summary>
        /// Histogram of gray channel.
        /// </summary>
        /// 
        /// <remarks><para><note>The property is valid only for grayscale images
        /// (see <see cref="IsGrayscale"/> property).</note></para></remarks>
        /// 
        public Histogram Gray
        {
            get
            {
                if ( this.gray == null )
                {
                    throw new InvalidImagePropertiesException( ExceptionMessage.GrayHistogramException );
                }
                return this.gray;
            }
        }

        /// <summary>
        /// Histogram of red channel excluding black pixels.
        /// </summary>
        /// 
        /// <remarks><para>The property keeps statistics about red channel, which
        /// excludes all black pixels, what affects mean, standard deviation, etc.</para>
        /// 
        /// <para><note>The property is valid only for color images
        /// (see <see cref="IsGrayscale"/> property).</note></para></remarks>
        /// 
        public Histogram RedWithoutBlack
        {
            get
            {
                if ( this.redWithoutBlack == null )
                {
                    throw new InvalidImagePropertiesException( ExceptionMessage.ColorHistogramException );
                }
                return this.redWithoutBlack;
            }
        }

        /// <summary>
        /// Histogram of green channel excluding black pixels.
        /// </summary>
        /// 
        /// <remarks><para>The property keeps statistics about green channel, which
        /// excludes all black pixels, what affects mean, standard deviation, etc.</para>
        /// 
        /// <para><note>The property is valid only for color images
        /// (see <see cref="IsGrayscale"/> property).</note></para></remarks>
        /// 
        public Histogram GreenWithoutBlack
        {
            get
            {
                if ( this.greenWithoutBlack == null )
                {
                    throw new InvalidImagePropertiesException( ExceptionMessage.ColorHistogramException );
                }
                return this.greenWithoutBlack;
            }
        }

        /// <summary>
        /// Histogram of blue channel excluding black pixels
        /// </summary>
        /// 
        /// <remarks><para>The property keeps statistics about blue channel, which
        /// excludes all black pixels, what affects mean, standard deviation, etc.</para>
        /// 
        /// <para><note>The property is valid only for color images
        /// (see <see cref="IsGrayscale"/> property).</note></para></remarks>
        /// 
        public Histogram BlueWithoutBlack
        {
            get
            {
                if ( this.blueWithoutBlack == null )
                {
                    throw new InvalidImagePropertiesException( ExceptionMessage.ColorHistogramException );
                }
                return this.blueWithoutBlack;
            }
        }

        /// <summary>
        /// Histogram of gray channel channel excluding black pixels.
        /// </summary>
        /// 
        /// <remarks><para>The property keeps statistics about gray channel, which
        /// excludes all black pixels, what affects mean, standard deviation, etc.</para>
        /// 
        /// <para><note>The property is valid only for grayscale images
        /// (see <see cref="IsGrayscale"/> property).</note></para></remarks>
        /// 
        public Histogram GrayWithoutBlack
        {
            get
            {
                if ( this.grayWithoutBlack == null )
                {
                    throw new InvalidImagePropertiesException( ExceptionMessage.GrayHistogramException );
                }
                return this.grayWithoutBlack;
            }
        }

        /// <summary>
        /// Total pixels count in the processed image.
        /// </summary>
        /// 
        public int PixelsCount
        {
            get { return this.pixels; }
        }

        /// <summary>
        /// Total pixels count in the processed image excluding black pixels.
        /// </summary>
        /// 
        public int PixelsCountWithoutBlack
        {
            get { return this.pixelsWithoutBlack; }
        }

        /// <summary>
        /// Value wich specifies if the processed image was color or grayscale.
        /// </summary>
        /// 
        /// <remarks><para>If the value is set to <see langword="true"/> then <see cref="Gray"/>
        /// property should be used to get statistics information about image. Otherwise
        /// <see cref="Red"/>, <see cref="Green"/> and <see cref="Blue"/> properties should be used
        /// for color images.</para></remarks>
        /// 
        public bool IsGrayscale
        {
            get { return ( this.gray != null ); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageStatistics"/> class.
        /// </summary>
        /// 
        /// <param name="image">Image to gather statistics about.</param>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Source pixel format is not supported.</exception>
        /// 
        public ImageStatistics( Bitmap image )
        {
            this.CheckSourceFormat( image.PixelFormat );

            // lock bitmap data
            var imageData = image.LockBits(
                new Rectangle( 0, 0, image.Width, image.Height ),
                ImageLockMode.ReadOnly, image.PixelFormat );

            try
            {
                // gather statistics
                unsafe
                {

                    this.ProcessImage( new UnmanagedImage( imageData ), null, 0 );
                }
            }
            finally
            {
                // unlock image
                image.UnlockBits( imageData );
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageStatistics"/> class.
        /// </summary>
        /// 
        /// <param name="image">Image to gather statistics about.</param>
        /// <param name="mask">Mask image which specifies areas to collect statistics for.</param>
        /// 
        /// <remarks><para>The mask image must be a grayscale/binary (8bpp) image of the same size as the
        /// specified source image, where black pixels (value 0) correspond to areas which should be excluded
        /// from processing. So statistics is calculated only for pixels, which are none black in the mask image.
        /// </para></remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Source pixel format is not supported.</exception>
        /// <exception cref="ArgumentException">Mask image must be 8 bpp grayscale image.</exception>
        /// <exception cref="ArgumentException">Mask must have the same size as the source image to get statistics for.</exception>
        /// 
        public ImageStatistics( Bitmap image, Bitmap mask )
        {
            this.CheckSourceFormat( image.PixelFormat );
            this.CheckMaskProperties( mask.PixelFormat, new Size( mask.Width, mask.Height ), new Size( image.Width, image.Height ) );

            // lock bitmap and mask data
            var imageData = image.LockBits(
                new Rectangle( 0, 0, image.Width, image.Height ),
                ImageLockMode.ReadOnly, image.PixelFormat );
            var maskData = mask.LockBits(
                new Rectangle( 0, 0, mask.Width, mask.Height ),
                ImageLockMode.ReadOnly, mask.PixelFormat );

            try
            {
                // gather statistics
                unsafe
                {
                    this.ProcessImage( new UnmanagedImage( imageData ), (byte*) maskData.Scan0.ToPointer( ), maskData.Stride );
                }
            }
            finally
            {
                // unlock images
                image.UnlockBits( imageData );
                mask.UnlockBits( maskData );
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageStatistics"/> class.
        /// </summary>
        /// 
        /// <param name="image">Image to gather statistics about.</param>
        /// <param name="mask">Mask array which specifies areas to collect statistics for.</param>
        /// 
        /// <remarks><para>The mask array must be of the same size as the specified source image, where 0 values
        /// correspond to areas which should be excluded from processing. So statistics is calculated only for pixels,
        /// which have none zero corresponding value in the mask.
        /// </para></remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Source pixel format is not supported.</exception>
        /// <exception cref="ArgumentException">Mask must have the same size as the source image to get statistics for.</exception>
        /// 
        public ImageStatistics( Bitmap image, byte[,] mask )
        {
            this.CheckSourceFormat( image.PixelFormat );
            this.CheckMaskProperties( PixelFormat.Format8bppIndexed,
                new Size( mask.GetLength( 1 ), mask.GetLength( 0 ) ), new Size( image.Width, image.Height ) );

            // lock bitmap data
            var imageData = image.LockBits(
                new Rectangle( 0, 0, image.Width, image.Height ),
                ImageLockMode.ReadOnly, image.PixelFormat );

            try
            {
                // gather statistics
                unsafe
                {
                    fixed ( byte* maskPtr = mask )
                    {
                        this.ProcessImage( new UnmanagedImage( imageData ), maskPtr, mask.GetLength( 1 ) );
                    }
                }
            }
            finally
            {
                // unlock image
                image.UnlockBits( imageData );
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageStatistics"/> class.
        /// </summary>
        /// 
        /// <param name="image">Unmanaged image to gather statistics about.</param>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Source pixel format is not supported.</exception>
        /// 
        public ImageStatistics( UnmanagedImage image )
        {
            this.CheckSourceFormat( image.PixelFormat );
            unsafe
            {
                this.ProcessImage( image, null, 0 );
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageStatistics"/> class.
        /// </summary>
        /// 
        /// <param name="image">Image to gather statistics about.</param>
        /// <param name="mask">Mask image which specifies areas to collect statistics for.</param>
        /// 
        /// <remarks><para>The mask image must be a grayscale/binary (8bpp) image of the same size as the
        /// specified source image, where black pixels (value 0) correspond to areas which should be excluded
        /// from processing. So statistics is calculated only for pixels, which are none black in the mask image.
        /// </para></remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Source pixel format is not supported.</exception>
        /// <exception cref="ArgumentException">Mask image must be 8 bpp grayscale image.</exception>
        /// <exception cref="ArgumentException">Mask must have the same size as the source image to get statistics for.</exception>
        /// 
        public ImageStatistics( UnmanagedImage image, UnmanagedImage mask )
        {
            this.CheckSourceFormat( image.PixelFormat );
            this.CheckMaskProperties( mask.PixelFormat, new Size( mask.Width, mask.Height ), new Size( image.Width, image.Height ) );

            unsafe
            {
                this.ProcessImage( image, (byte*) mask.ImageData.ToPointer( ), mask.Stride );
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageStatistics"/> class.
        /// </summary>
        /// 
        /// <param name="image">Image to gather statistics about.</param>
        /// <param name="mask">Mask array which specifies areas to collect statistics for.</param>
        /// 
        /// <remarks><para>The mask array must be of the same size as the specified source image, where 0 values
        /// correspond to areas which should be excluded from processing. So statistics is calculated only for pixels,
        /// which have none zero corresponding value in the mask.
        /// </para></remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Source pixel format is not supported.</exception>
        /// <exception cref="ArgumentException">Mask must have the same size as the source image to get statistics for.</exception>
        /// 
        public ImageStatistics( UnmanagedImage image, byte[,] mask )
        {
            this.CheckSourceFormat( image.PixelFormat );
            this.CheckMaskProperties( PixelFormat.Format8bppIndexed,
                new Size( mask.GetLength( 1 ), mask.GetLength( 0 ) ), new Size( image.Width, image.Height ) );

            unsafe
            {
                fixed ( byte* maskPtr = mask )
                {
                    this.ProcessImage( image, maskPtr, mask.GetLength( 1 ) );
                }
            }
        }

        // Gather statistics for the specified image
        private unsafe void ProcessImage( UnmanagedImage image, byte* mask, int maskLineSize )
        {
            // get image dimension
            var width  = image.Width;
            var height = image.Height;

            this.pixels = this.pixelsWithoutBlack = 0;

            this.red = this.green = this.blue = this.gray = null;
            this.redWithoutBlack = this.greenWithoutBlack = this.blueWithoutBlack = this.grayWithoutBlack = null;

            var maskOffset = maskLineSize - width;

            // check pixel format
            if ( image.PixelFormat == PixelFormat.Format8bppIndexed )
            {
                // alloc arrays
                var g   = new int[256];
                var gwb = new int[256];

                byte value;
                var  offset = image.Stride - width;

                // do the job
                var p = (byte*) image.ImageData.ToPointer( );

                if ( mask == null )
                {
                    // for each pixel
                    for ( var y = 0; y < height; y++ )
                    {
                        // for each pixel
                        for ( var x = 0; x < width; x++, p++ )
                        {
                            // get pixel value
                            value = *p;

                            g[value]++;
                            this.pixels++;

                            if ( value != 0 )
                            {
                                gwb[value]++;
                                this.pixelsWithoutBlack++;
                            }
                        }
                        p += offset;
                    }
                }
                else
                {
                    // for each pixel
                    for ( var y = 0; y < height; y++ )
                    {
                        // for each pixel
                        for ( var x = 0; x < width; x++, p++, mask++ )
                        {
                            if ( *mask == 0 )
                                continue;

                            // get pixel value
                            value = *p;

                            g[value]++;
                            this.pixels++;

                            if ( value != 0 )
                            {
                                gwb[value]++;
                                this.pixelsWithoutBlack++;
                            }
                        }
                        p += offset;
                        mask += maskOffset;
                    }
                }

                // create historgram for gray level
                this.gray = new Histogram( g );
                this.grayWithoutBlack = new Histogram( gwb );
            }
            else
            {
                // alloc arrays
                var	r = new int[256];
                var	g = new int[256];
                var	b = new int[256];

                var	rwb = new int[256];
                var	gwb = new int[256];
                var	bwb = new int[256];

                byte rValue, gValue, bValue;
                var  pixelSize = ( image.PixelFormat == PixelFormat.Format24bppRgb ) ? 3 : 4;
                var  offset = image.Stride - width * pixelSize;

                // do the job
                var p = (byte*) image.ImageData.ToPointer( );

                if ( mask == null )
                {
                    // for each line
                    for ( var y = 0; y < height; y++ )
                    {
                        // for each pixel
                        for ( var x = 0; x < width; x++, p += pixelSize )
                        {
                            // get pixel values
                            rValue = p[RGB.R];
                            gValue = p[RGB.G];
                            bValue = p[RGB.B];

                            r[rValue]++;
                            g[gValue]++;
                            b[bValue]++;
                            this.pixels++;

                            if ( ( rValue != 0 ) || ( gValue != 0 ) || ( bValue != 0 ) )
                            {
                                rwb[rValue]++;
                                gwb[gValue]++;
                                bwb[bValue]++;
                                this.pixelsWithoutBlack++;
                            }
                        }
                        p += offset;
                    }
                }
                else
                {
                    // for each line
                    for ( var y = 0; y < height; y++ )
                    {
                        // for each pixel
                        for ( var x = 0; x < width; x++, p += pixelSize, mask++ )
                        {
                            if ( *mask == 0 )
                                continue;

                            // get pixel values
                            rValue = p[RGB.R];
                            gValue = p[RGB.G];
                            bValue = p[RGB.B];

                            r[rValue]++;
                            g[gValue]++;
                            b[bValue]++;
                            this.pixels++;

                            if ( ( rValue != 0 ) || ( gValue != 0 ) || ( bValue != 0 ) )
                            {
                                rwb[rValue]++;
                                gwb[gValue]++;
                                bwb[bValue]++;
                                this.pixelsWithoutBlack++;
                            }
                        }
                        p += offset;
                        mask += maskOffset;
                    }
                }

                // create histograms
                this.red   = new Histogram( r );
                this.green = new Histogram( g );
                this.blue  = new Histogram( b );

                this.redWithoutBlack   = new Histogram( rwb );
                this.greenWithoutBlack = new Histogram( gwb );
                this.blueWithoutBlack  = new Histogram( bwb );
            }
        }

        // Check pixel format of the source image
        private void CheckSourceFormat( PixelFormat pixelFormat )
        {
            if (
                ( pixelFormat != PixelFormat.Format8bppIndexed ) &&
                ( pixelFormat != PixelFormat.Format24bppRgb ) &&
                ( pixelFormat != PixelFormat.Format32bppRgb ) &&
                ( pixelFormat != PixelFormat.Format32bppArgb ) )
            {
                throw new UnsupportedImageFormatException( "Source pixel format is not supported." );
            }
        }

        private void CheckMaskProperties( PixelFormat maskFormat, Size maskSize, Size sourceImageSize )
        {
            if ( maskFormat != PixelFormat.Format8bppIndexed )
            {
                throw new ArgumentException( "Mask image must be 8 bpp grayscale image." );
            }

            if ( ( maskSize.Width != sourceImageSize.Width ) || ( maskSize.Height != sourceImageSize.Height ) )
            {
                throw new ArgumentException( "Mask must have the same size as the source image to get statistics for." );
            }
        }
    }
}

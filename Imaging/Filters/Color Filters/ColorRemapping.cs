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
    using AForge;

    /// <summary>
    /// Color remapping.
    /// </summary>
    /// 
    /// <remarks><para>The filter allows to remap colors of the image. Unlike <see cref="LevelsLinear"/> filter
    /// the filter allow to do non-linear remapping. For each pixel of specified image the filter changes
    /// its values (value of each color plane) to values, which are stored in remapping arrays by corresponding
    /// indexes. For example, if pixel's RGB value equals to (32, 96, 128), the filter will change it to
    /// (<see cref="RedMap"/>[32], <see cref="GreenMap"/>[96], <see cref="BlueMap"/>[128]).</para>
    /// 
    /// <para>The filter accepts 8 bpp grayscale and 24/32 bpp color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create map
    /// byte[] map = new byte[256];
    /// for ( int i = 0; i &lt; 256; i++ )
    /// {
    ///     map[i] = (byte) Math.Min( 255, Math.Pow( 2, (double) i / 32 ) );
    /// }
    /// // create filter
    /// ColorRemapping filter = new ColorRemapping( map, map, map );
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/color_remapping.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    public class ColorRemapping : BaseInPlacePartialFilter
    {
        // color maps
        private byte[] redMap;
        private byte[] greenMap;
        private byte[] blueMap;
        private byte[] grayMap;

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
        /// Remapping array for red color plane.
        /// </summary>
        /// 
        /// <remarks><para>The remapping array should contain 256 remapping values. The remapping occurs
        /// by changing pixel's red value <b>r</b> to <see cref="RedMap"/>[r].</para></remarks>
        /// 
        /// <exception cref="ArgumentException">A map should be array with 256 value.</exception>
        /// 
        public byte[] RedMap
        {
            get { return this.redMap; }
            set
            {
                // check the map
                if ( ( value == null ) || ( value.Length != 256 ) )
                    throw new ArgumentException( "A map should be array with 256 value." );

                this.redMap = value;
            }
        }

        /// <summary>
        /// Remapping array for green color plane.
        /// </summary>
        /// 
        /// <remarks><para>The remapping array should contain 256 remapping values. The remapping occurs
        /// by changing pixel's green value <b>g</b> to <see cref="GreenMap"/>[g].</para></remarks>
        /// 
        /// <exception cref="ArgumentException">A map should be array with 256 value.</exception>
        /// 
        public byte[] GreenMap
        {
            get { return this.greenMap; }
            set
            {
                // check the map
                if ( ( value == null ) || ( value.Length != 256 ) )
                    throw new ArgumentException( "A map should be array with 256 value." );

                this.greenMap = value;
            }
        }

        /// <summary>
        /// Remapping array for blue color plane.
        /// </summary>
        /// 
        /// <remarks><para>The remapping array should contain 256 remapping values. The remapping occurs
        /// by changing pixel's blue value <b>b</b> to <see cref="BlueMap"/>[b].</para></remarks>
        /// 
        /// <exception cref="ArgumentException">A map should be array with 256 value.</exception>
        /// 
        public byte[] BlueMap
        {
            get { return this.blueMap; }
            set
            {
                // check the map
                if ( ( value == null ) || ( value.Length != 256 ) )
                    throw new ArgumentException( "A map should be array with 256 value." );

                this.blueMap = value;
            }
        }

        /// <summary>
        /// Remapping array for gray color.
        /// </summary>
        /// 
        /// <remarks><para>The remapping array should contain 256 remapping values. The remapping occurs
        /// by changing pixel's value <b>g</b> to <see cref="GrayMap"/>[g].</para>
        /// 
        /// <para>The gray map is for grayscale images only.</para></remarks>
        /// 
        /// <exception cref="ArgumentException">A map should be array with 256 value.</exception>
        /// 
        public byte[] GrayMap
        {
            get { return this.grayMap; }
            set
            {
                // check the map
                if ( ( value == null ) || ( value.Length != 256 ) )
                    throw new ArgumentException( "A map should be array with 256 value." );

                this.grayMap = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRemapping"/> class.
        /// </summary>
        /// 
        /// <remarks>Initializes the filter without any remapping. All
        /// pixel values are mapped to the same values.</remarks>
        /// 
        public ColorRemapping( )
        {
            this.redMap      = new byte[256];
            this.greenMap    = new byte[256];
            this.blueMap     = new byte[256];
            this.grayMap     = new byte[256];

            // fill the maps
            for ( var i = 0; i < 256; i++ )
            {
                this.redMap[i] = this.greenMap[i] = this.blueMap[i] = this.grayMap[i] = (byte) i;
            }

            this.formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format24bppRgb]    = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppRgb]    = PixelFormat.Format32bppRgb;
            this.formatTranslations[PixelFormat.Format32bppArgb]   = PixelFormat.Format32bppArgb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRemapping"/> class.
        /// </summary>
        /// 
        /// <param name="redMap">Red map.</param>
        /// <param name="greenMap">Green map.</param>
        /// <param name="blueMap">Blue map.</param>
        /// 
        public ColorRemapping( byte[] redMap, byte[] greenMap, byte[] blueMap ) : this( )
        {
            this.RedMap      = redMap;
            this.GreenMap    = greenMap;
            this.BlueMap     = blueMap;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRemapping"/> class.
        /// </summary>
        /// 
        /// <param name="grayMap">Gray map.</param>
        /// 
        /// <remarks>This constructor is supposed for grayscale images.</remarks>
        /// 
        public ColorRemapping( byte[] grayMap ) : this( )
        {
            this.GrayMap = grayMap;
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
            var pixelSize = Image.GetPixelFormatSize( image.PixelFormat ) / 8;

            // processing start and stop X,Y positions
            var startX  = rect.Left;
            var startY  = rect.Top;
            var stopX   = startX + rect.Width;
            var stopY   = startY + rect.Height;
            var offset  = image.Stride - rect.Width * pixelSize;

            // do the job
            var ptr = (byte*) image.ImageData.ToPointer( );

            // allign pointer to the first pixel to process
            ptr += ( startY * image.Stride + startX * pixelSize );

            if ( image.PixelFormat == PixelFormat.Format8bppIndexed )
            {
                // grayscale image
                for ( var y = startY; y < stopY; y++ )
                {
                    for ( var x = startX; x < stopX; x++, ptr++ )
                    {
                        // gray
                        *ptr = this.grayMap[*ptr];
                    }
                    ptr += offset;
                }
            }
            else
            {
                // RGB image
                for ( var y = startY; y < stopY; y++ )
                {
                    for ( var x = startX; x < stopX; x++, ptr += pixelSize )
                    {
                        // red
                        ptr[RGB.R] = this.redMap[ptr[RGB.R]];
                        // green
                        ptr[RGB.G] = this.greenMap[ptr[RGB.G]];
                        // blue
                        ptr[RGB.B] = this.blueMap[ptr[RGB.B]];
                    }
                    ptr += offset;
                }
            }
        }
    }
}

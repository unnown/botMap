﻿// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © Andrew Kirillov, 2005-2009
// andrew.kirillov@aforgenet.com
//
// Copyright © Frank Nagl, 2008-2009
// admin@franknagl.de
// www.franknagl.de
//
namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Simple posterization of an image.
    /// </summary>
    /// 
    /// <remarks><para>The class implements simple <a href="http://en.wikipedia.org/wiki/Posterization">posterization</a> of an image by splitting
    /// each color plane into adjacent areas of the <see cref="PosterizationInterval">specified size</see>. After the process
    /// is done, each color plane will contain maximum of 256/<see cref="PosterizationInterval">PosterizationInterval</see> levels.
    /// For example, if grayscale image is posterized with posterization interval equal to 64,
    /// then result image will contain maximum of 4 tones. If color image is posterized with the
    /// same posterization interval, then it will contain maximum of 4<sup>3</sup>=64 colors.
    /// See <see cref="FillingType"/> property to get information about the way how to control
    /// color used to fill posterization areas.</para>
    /// 
    /// <para>Posterization is a process in photograph development which converts normal photographs
    /// into an image consisting of distinct, but flat, areas of different tones or colors.</para>
    ///
    /// <para>The filter accepts 8 bpp grayscale and 24/32 bpp color images.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// SimplePosterization filter = new SimplePosterization( );
    /// // process image
    /// filter.ApplyInPlace( sourceImage );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/posterization.png" width="480" height="361" />
    /// </remarks>
    /// 
    public class SimplePosterization : BaseInPlacePartialFilter
    {
        /// <summary>
        /// Enumeration of possible types of filling posterized areas.
        /// </summary>
        public enum PosterizationFillingType
        {
            /// <summary>
            /// Fill area with minimum color's value.
            /// </summary>
            Min,
            /// <summary>
            /// Fill area with maximum color's value.
            /// </summary>
            Max,
            /// <summary>
            /// Fill area with average color's value.
            /// </summary>
            Average
        }
        
        byte posterizationInterval = 64;
        PosterizationFillingType fillingType = PosterizationFillingType.Average;

        /// <summary>
        /// Posterization interval, which specifies size of posterization areas.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies size of adjacent posterization areas
        /// for each color plane. The value has direct effect on the amount of colors
        /// in the result image. For example, if grayscale image is posterized with posterization
        /// interval equal to 64, then result image will contain maximum of 4 tones. If color
        /// image is posterized with same posterization interval, then it will contain maximum
        /// of 4<sup>3</sup>=64 colors.</para>
        /// 
        /// <para>Default value is set to <b>64</b>.</para>
        /// </remarks>
        /// 
        public byte PosterizationInterval
        {
            get { return this.posterizationInterval; }
            set { this.posterizationInterval = value; }
        }

        /// <summary>
        /// Posterization filling type.
        /// </summary>
        /// 
        /// <remarks><para>The property controls the color, which is used to substitute
        /// colors within the same posterization interval - minimum, maximum or average value.
        /// </para>
        /// 
        /// <para>Default value is set to <see cref="PosterizationFillingType.Average"/>.</para>
        /// </remarks>
        /// 
        public PosterizationFillingType FillingType
        {
            get { return this.fillingType; }
            set { this.fillingType = value; }
        }

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
        /// Initializes a new instance of the <see cref="SimplePosterization"/> class.
        /// </summary>
        public SimplePosterization( )
        {
            // initialize format translation dictionary
            this.formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format24bppRgb]    = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format32bppRgb]    = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format32bppArgb]   = PixelFormat.Format8bppIndexed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimplePosterization"/> class.
        /// </summary>
        /// 
        /// <param name="fillingType">Specifies <see cref="FillingType">filling type</see> of posterization areas.</param>
        /// 
        public SimplePosterization( PosterizationFillingType fillingType ) : this ( )
        {
            this.fillingType = fillingType;
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
            // get pixel size
            var pixelSize = Image.GetPixelFormatSize( image.PixelFormat ) / 8;

            var startX  = rect.Left;
            var startY  = rect.Top;
            var stopX   = startX + rect.Width;
            var stopY   = startY + rect.Height;
            var offset  = image.Stride - rect.Width * pixelSize;

            // calculate posterization offset
            var posterizationOffset = ( this.fillingType == PosterizationFillingType.Min ) ?
                0 : ( ( this.fillingType == PosterizationFillingType.Max ) ?
                this.posterizationInterval - 1 : this.posterizationInterval / 2 );

            // calculate mapping array
            var map = new byte[256];

            for ( var i = 0; i < 256; i++ )
            {
                map[i] = (byte) Math.Min( 255, ( i / this.posterizationInterval ) * this.posterizationInterval + posterizationOffset );
            }

            // do the job
            var ptr = (byte*) image.ImageData.ToPointer( );

            // allign pointer to the first pixel to process
            ptr += ( startY * image.Stride + startX * pixelSize );

            // check image format
            if ( image.PixelFormat == PixelFormat.Format8bppIndexed )
            {
                // for each line
                for ( var y = startY; y < stopY; y++ )
                {
                    // for each pixel in line
                    for ( var x = startX; x < stopX; x++, ptr++ )
                    {
                        *ptr = map[*ptr];
                    }
                    ptr += offset;
                }
            }
            else
            {
                // for each line
                for ( var y = startY; y < stopY; y++ )
                {
                    // for each pixel in line
                    for ( var x = startX; x < stopX; x++, ptr += pixelSize )
                    {
                        ptr[RGB.R] = map[ptr[RGB.R]];
                        ptr[RGB.G] = map[ptr[RGB.G]];
                        ptr[RGB.B] = map[ptr[RGB.B]];
                    }
                    ptr += offset;
                }
            }
        }
    }
}

// AForge Image Processing Library
// AForge.NET framework
//
// Copyright © AForge.NET, 2007-2011
// contacts@aforgenet.com
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using AForge;

    /// <summary>
    /// Color filtering in YCbCr color space.
    /// </summary>
    /// 
    /// <remarks><para>The filter operates in <b>YCbCr</b> color space and filters
    /// pixels, which color is inside/outside of the specified YCbCr range - 
    /// it keeps pixels with colors inside/outside of the specified range and fills the
    /// rest with <see cref="FillColor">specified color</see>.</para>
    /// 
    /// <para>The filter accepts 24 and 32 bpp color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// YCbCrFiltering filter = new YCbCrFiltering( );
    /// // set color ranges to keep
    /// filter.Cb = new Range( -0.2f, 0.0f );
    /// filter.Cr = new Range( 0.26f, 0.5f );
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/ycbcr_filtering.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    /// <seealso cref="ColorFiltering"/>
    /// <seealso cref="HSLFiltering"/>
    /// 
    public class YCbCrFiltering : BaseInPlacePartialFilter
    {
        private Range yRange  = new Range( 0.0f, 1.0f );
        private Range cbRange = new Range( -0.5f, 0.5f );
        private Range crRange = new Range( -0.5f, 0.5f );

        private float fillY  = 0.0f;
        private float fillCb = 0.0f;
        private float fillCr = 0.0f;
        private bool  fillOutsideRange = true;

        private bool updateY = true;
        private bool updateCb = true;
        private bool updateCr = true;

        // private format translation dictionary
        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>( );

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations
        {
            get { return this.formatTranslations; }
        }

        #region Public properties

        /// <summary>
        /// Range of Y component, [0, 1].
        /// </summary>
        /// 
        public Range Y
        {
            get { return this.yRange; }
            set { this.yRange = value; }
        }

        /// <summary>
        /// Range of Cb component, [-0.5, 0.5].
        /// </summary>
        /// 
        public Range Cb
        {
            get { return this.cbRange; }
            set { this.cbRange = value; }
        }

        /// <summary>
        /// Range of Cr component, [-0.5, 0.5].
        /// </summary>
        /// 
        public Range Cr
        {
            get { return this.crRange; }
            set { this.crRange = value; }
        }

        /// <summary>
        /// Fill color used to fill filtered pixels.
        /// </summary>
        public YCbCr FillColor
        {
            get { return new YCbCr(this.fillY , this.fillCb , this.fillCr ); }
            set
            {
                this.fillY = value.Y;
                this.fillCb = value.Cb;
                this.fillCr = value.Cr;
            }
        }

        /// <summary>
        /// Determines, if pixels should be filled inside or outside specified
        /// color range.
        /// </summary>
        /// 
        /// <remarks><para>Default value is set to <see langword="true"/>, which means
        /// the filter removes colors outside of the specified range.</para></remarks>
        /// 
        public bool FillOutsideRange
        {
            get { return this.fillOutsideRange; }
            set { this.fillOutsideRange = value; }
        }

        /// <summary>
        /// Determines, if Y value of filtered pixels should be updated.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies if Y channel of filtered pixels should be
        /// updated with value from <see cref="FillColor">fill color</see> or not.</para>
        /// 
        /// <para>Default value is set to <see langword="true"/>.</para></remarks>
        /// 
        public bool UpdateY
        {
            get { return this.updateY; }
            set { this.updateY = value; }
        }

        /// <summary>
        /// Determines, if Cb value of filtered pixels should be updated.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies if Cb channel of filtered pixels should be
        /// updated with value from <see cref="FillColor">fill color</see> or not.</para>
        /// 
        /// <para>Default value is set to <see langword="true"/>.</para></remarks>
        /// 
        public bool UpdateCb
        {
            get { return this.updateCb; }
            set { this.updateCb = value; }
        }

        /// <summary>
        /// Determines, if Cr value of filtered pixels should be updated.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies if Cr channel of filtered pixels should be
        /// updated with value from <see cref="FillColor">fill color</see> or not.</para>
        /// 
        /// <para>Default value is set to <see langword="true"/>.</para></remarks>
        /// 
        public bool UpdateCr
        {
            get { return this.updateCr; }
            set { this.updateCr = value; }
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="YCbCrFiltering"/> class.
        /// </summary>
        public YCbCrFiltering( )
        {
            this.formatTranslations[PixelFormat.Format24bppRgb]  = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppRgb]  = PixelFormat.Format32bppRgb;
            this.formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format32bppArgb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YCbCrFiltering"/> class.
        /// </summary>
        /// 
        /// <param name="yRange">Range of Y component.</param>
        /// <param name="cbRange">Range of Cb component.</param>
        /// <param name="crRange">Range of Cr component.</param>
        /// 
        public YCbCrFiltering( Range yRange, Range cbRange, Range crRange ) :
            this( )
        {
            this.yRange  = yRange;
            this.cbRange = cbRange;
            this.crRange = crRange;
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
            var pixelSize = ( image.PixelFormat == PixelFormat.Format24bppRgb ) ? 3 : 4;

            var startX  = rect.Left;
            var startY  = rect.Top;
            var stopX   = startX + rect.Width;
            var stopY   = startY + rect.Height;
            var offset  = image.Stride - rect.Width * pixelSize;

            var rgb = new RGB( );
            var ycbcr = new YCbCr( );

            bool updated;

            // do the job
            var ptr = (byte*) image.ImageData.ToPointer( );

            // allign pointer to the first pixel to process
            ptr += ( startY * image.Stride + startX * pixelSize );

            // for each row
            for ( var y = startY; y < stopY; y++ )
            {
                // for each pixel
                for ( var x = startX; x < stopX; x++, ptr += pixelSize )
                {
                    updated   = false;
                    rgb.Red   = ptr[RGB.R];
                    rgb.Green = ptr[RGB.G];
                    rgb.Blue  = ptr[RGB.B];

                    // convert to YCbCr
                    AForge.Imaging.YCbCr.FromRGB( rgb, ycbcr );

                    // check YCbCr values
                    if (
                        ( ycbcr.Y >= this.yRange.Min )   && ( ycbcr.Y <= this.yRange.Max ) &&
                        ( ycbcr.Cb >= this.cbRange.Min ) && ( ycbcr.Cb <= this.cbRange.Max ) &&
                        ( ycbcr.Cr >= this.crRange.Min ) && ( ycbcr.Cr <= this.crRange.Max )
                        )
                    {
                        if ( !this.fillOutsideRange )
                        {
                            if ( this.updateY ) ycbcr.Y   = this.fillY;
                            if ( this.updateCb ) ycbcr.Cb = this.fillCb;
                            if ( this.updateCr ) ycbcr.Cr = this.fillCr;

                            updated = true;
                        }
                    }
                    else
                    {
                        if ( this.fillOutsideRange )
                        {
                            if ( this.updateY ) ycbcr.Y   = this.fillY;
                            if ( this.updateCb ) ycbcr.Cb = this.fillCb;
                            if ( this.updateCr ) ycbcr.Cr = this.fillCr;

                            updated = true;
                        }
                    }

                    if ( updated )
                    {
                        // convert back to RGB
                        AForge.Imaging.YCbCr.ToRGB( ycbcr, rgb );

                        ptr[RGB.R] = rgb.Red;
                        ptr[RGB.G] = rgb.Green;
                        ptr[RGB.B] = rgb.Blue;
                    }
                }
                ptr += offset;
            }
        }
    }
}

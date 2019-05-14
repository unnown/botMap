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
    /// Color filtering in HSL color space.
    /// </summary>
    /// 
    /// <remarks><para>The filter operates in <b>HSL</b> color space and filters
    /// pixels, which color is inside/outside of the specified HSL range -
    /// it keeps pixels with colors inside/outside of the specified range and fills the
    /// rest with <see cref="FillColor">specified color</see>.</para>
    /// 
    /// <para>The filter accepts 24 and 32 bpp color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// HSLFiltering filter = new HSLFiltering( );
    /// // set color ranges to keep
    /// filter.Hue = new IntRange( 335, 0 );
    /// filter.Saturation = new Range( 0.6f, 1 );
    /// filter.Luminance = new Range( 0.1f, 1 );
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/hsl_filtering.jpg" width="480" height="361" />
    /// 
    /// <para>Sample usage with saturation update only:</para>
    /// <code>
    /// // create filter
    /// HSLFiltering filter = new HSLFiltering( );
    /// // configure the filter
    /// filter.Hue = new IntRange( 340, 20 );
    /// filter.UpdateLuminance = false;
    /// filter.UpdateHue = false;
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/hsl_filtering2.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    /// <seealso cref="ColorFiltering"/>
    /// <seealso cref="YCbCrFiltering"/>
    /// 
    public class HSLFiltering : BaseInPlacePartialFilter
    {
        private IntRange hue = new IntRange( 0, 359 );
        private Range saturation = new Range( 0.0f, 1.0f );
        private Range luminance = new Range( 0.0f, 1.0f );

        private int   fillH = 0;
        private float fillS = 0.0f;
        private float fillL = 0.0f;
        private bool  fillOutsideRange = true;

        private bool updateH = true;
        private bool updateS = true;
        private bool updateL = true;

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
        /// Range of hue component, [0, 359].
        /// </summary>
        /// 
        /// <remarks><note>Because of hue values are cycled, the minimum value of the hue
        /// range may have bigger integer value than the maximum value, for example [330, 30].</note></remarks>
        /// 
        public IntRange Hue
        {
            get { return this.hue; }
            set { this.hue = value; }
        }

        /// <summary>
        /// Range of saturation component, [0, 1].
        /// </summary>
        public Range Saturation
        {
            get { return this.saturation; }
            set { this.saturation = value; }
        }

        /// <summary>
        /// Range of luminance component, [0, 1].
        /// </summary>
        public Range Luminance
        {
            get { return this.luminance; }
            set { this.luminance = value; }
        }

        /// <summary>
        /// Fill color used to fill filtered pixels.
        /// </summary>
        public HSL FillColor
        {
            get { return new HSL(this.fillH , this.fillS , this.fillL ); }
            set
            {
                this.fillH = value.Hue;
                this.fillS = value.Saturation;
                this.fillL = value.Luminance;
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
        /// Determines, if hue value of filtered pixels should be updated.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies if hue of filtered pixels should be
        /// updated with value from <see cref="FillColor">fill color</see> or not.</para>
        /// 
        /// <para>Default value is set to <see langword="true"/>.</para></remarks>
        /// 
        public bool UpdateHue
        {
            get { return this.updateH; }
            set { this.updateH = value; }
        }

        /// <summary>
        /// Determines, if saturation value of filtered pixels should be updated.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies if saturation of filtered pixels should be
        /// updated with value from <see cref="FillColor">fill color</see> or not.</para>
        /// 
        /// <para>Default value is set to <see langword="true"/>.</para></remarks>
        /// 
        public bool UpdateSaturation
        {
            get { return this.updateS; }
            set { this.updateS = value; }
        }

        /// <summary>
        /// Determines, if luminance value of filtered pixels should be updated.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies if luminance of filtered pixels should be
        /// updated with value from <see cref="FillColor">fill color</see> or not.</para>
        /// 
        /// <para>Default value is set to <see langword="true"/>.</para></remarks>
        /// 
        public bool UpdateLuminance
        {
            get { return this.updateL; }
            set { this.updateL = value; }
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="HSLFiltering"/> class.
        /// </summary>
        public HSLFiltering( )
        {
            this.formatTranslations[PixelFormat.Format24bppRgb]  = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppRgb]  = PixelFormat.Format32bppRgb;
            this.formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format32bppArgb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HSLFiltering"/> class.
        /// </summary>
        /// 
        /// <param name="hue">Range of hue component.</param>
        /// <param name="saturation">Range of saturation component.</param>
        /// <param name="luminance">Range of luminance component.</param>
        /// 
        public HSLFiltering( IntRange hue, Range saturation, Range luminance ) :
            this( )
        {
            this.hue = hue;
            this.saturation = saturation;
            this.luminance = luminance;
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
            var hsl = new HSL( );

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

                    // convert to HSL
                    AForge.Imaging.HSL.FromRGB( rgb, hsl );

                    // check HSL values
                    if (
                        ( hsl.Saturation >= this.saturation.Min ) && ( hsl.Saturation <= this.saturation.Max ) &&
                        ( hsl.Luminance >= this.luminance.Min ) && ( hsl.Luminance <= this.luminance.Max ) &&
                        (
                        ( ( this.hue.Min < this.hue.Max ) && ( hsl.Hue >= this.hue.Min ) && ( hsl.Hue <= this.hue.Max ) ) ||
                        ( ( this.hue.Min > this.hue.Max ) && ( ( hsl.Hue >= this.hue.Min ) || ( hsl.Hue <= this.hue.Max ) ) )
                        )
                        )
                    {
                        if ( !this.fillOutsideRange )
                        {
                            if ( this.updateH ) hsl.Hue = this.fillH;
                            if ( this.updateS ) hsl.Saturation = this.fillS;
                            if ( this.updateL ) hsl.Luminance = this.fillL;

                            updated = true;
                        }
                    }
                    else
                    {
                        if ( this.fillOutsideRange )
                        {
                            if ( this.updateH ) hsl.Hue = this.fillH;
                            if ( this.updateS ) hsl.Saturation = this.fillS;
                            if ( this.updateL ) hsl.Luminance = this.fillL;

                            updated = true;
                        }
                    }

                    if ( updated )
                    {
                        // convert back to RGB
                        AForge.Imaging.HSL.ToRGB( hsl, rgb );

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

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
    /// Linear correction of YCbCr channels.
    /// </summary>
    /// 
    /// <remarks><para>The filter operates in <b>YCbCr</b> color space and provides
    /// with the facility of linear correction of its channels - mapping specified channels'
    /// input ranges to specified output ranges.</para>
    /// 
    /// <para>The filter accepts 24 and 32 bpp color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create filter
    /// YCbCrLinear filter = new YCbCrLinear( );
    /// // configure the filter
    /// filter.InCb = new Range( -0.276f, 0.163f );
    /// filter.InCr = new Range( -0.202f, 0.500f );
    /// // apply the filter
    /// filter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/ycbcr_linear.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    /// <seealso cref="HSLLinear"/>
    /// <seealso cref="YCbCrLinear"/>
    /// 
    public class YCbCrLinear : BaseInPlacePartialFilter
    {
        private Range inY  = new Range(  0.0f, 1.0f );
        private Range inCb = new Range( -0.5f, 0.5f );
        private Range inCr = new Range( -0.5f, 0.5f );

        private Range outY  = new Range(  0.0f, 1.0f );
        private Range outCb = new Range( -0.5f, 0.5f );
        private Range outCr = new Range( -0.5f, 0.5f );

        #region Public Propertis

        /// <summary>
        /// Y component's input range.
        /// </summary>
        /// 
        /// <remarks>Y component is measured in the range of [0, 1].</remarks>
        ///
        public Range InY
        {
            get { return this.inY; }
            set { this.inY = value; }
        }

        /// <summary>
        /// Cb component's input range.
        /// </summary>
        /// 
        /// <remarks>Cb component is measured in the range of [-0.5, 0.5].</remarks>
        ///
        public Range InCb
        {
            get { return this.inCb; }
            set { this.inCb = value; }
        }

        /// <summary>
        /// Cr component's input range.
        /// </summary>
        /// 
        /// <remarks>Cr component is measured in the range of [-0.5, 0.5].</remarks>
        ///
        public Range InCr
        {
            get { return this.inCr; }
            set { this.inCr = value; }
        }

        /// <summary>
        /// Y component's output range.
        /// </summary>
        /// 
        /// <remarks>Y component is measured in the range of [0, 1].</remarks>
        ///
        public Range OutY
        {
            get { return this.outY; }
            set { this.outY = value; }
        }

        /// <summary>
        /// Cb component's output range.
        /// </summary>
        /// 
        /// <remarks>Cb component is measured in the range of [-0.5, 0.5].</remarks>
        ///
        public Range OutCb
        {
            get { return this.outCb; }
            set { this.outCb = value; }
        }

        /// <summary>
        /// Cr component's output range.
        /// </summary>
        /// 
        /// <remarks>Cr component is measured in the range of [-0.5, 0.5].</remarks>
        ///
        public Range OutCr
        {
            get { return this.outCr; }
            set { this.outCr = value; }
        }

        #endregion

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
        /// Initializes a new instance of the <see cref="YCbCrLinear"/> class.
        /// </summary>
        /// 
        public YCbCrLinear( )
        {
            this.formatTranslations[PixelFormat.Format24bppRgb]  = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppRgb]  = PixelFormat.Format32bppRgb;
            this.formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format32bppArgb;
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

            var startX  = rect.Left;
            var startY  = rect.Top;
            var stopX   = startX + rect.Width;
            var stopY   = startY + rect.Height;
            var offset  = image.Stride - rect.Width * pixelSize;

            var     rgb = new RGB( );
            var   ycbcr = new YCbCr( );

            float ky  = 0, by  = 0;
            float kcb = 0, bcb = 0;
            float kcr = 0, bcr = 0;

            // Y line parameters
            if ( this.inY.Max != this.inY.Min )
            {
                ky = ( this.outY.Max - this.outY.Min ) / ( this.inY.Max - this.inY.Min );
                by = this.outY.Min - ky * this.inY.Min;
            }
            // Cb line parameters
            if ( this.inCb.Max != this.inCb.Min )
            {
                kcb = ( this.outCb.Max - this.outCb.Min ) / ( this.inCb.Max - this.inCb.Min );
                bcb = this.outCb.Min - kcb * this.inCb.Min;
            }
            // Cr line parameters
            if ( this.inCr.Max != this.inCr.Min )
            {
                kcr = ( this.outCr.Max - this.outCr.Min ) / ( this.inCr.Max - this.inCr.Min );
                bcr = this.outCr.Min - kcr * this.inCr.Min;
            }

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
                    rgb.Red     = ptr[RGB.R];
                    rgb.Green   = ptr[RGB.G];
                    rgb.Blue    = ptr[RGB.B];

                    // convert to YCbCr
                    AForge.Imaging.YCbCr.FromRGB( rgb, ycbcr );

                    // correct Y
                    if ( ycbcr.Y >= this.inY.Max )
                        ycbcr.Y = this.outY.Max;
                    else if ( ycbcr.Y <= this.inY.Min )
                        ycbcr.Y = this.outY.Min;
                    else
                        ycbcr.Y = ky * ycbcr.Y + by;

                    // correct Cb
                    if ( ycbcr.Cb >= this.inCb.Max )
                        ycbcr.Cb = this.outCb.Max;
                    else if ( ycbcr.Cb <= this.inCb.Min )
                        ycbcr.Cb = this.outCb.Min;
                    else
                        ycbcr.Cb = kcb * ycbcr.Cb + bcb;

                    // correct Cr
                    if ( ycbcr.Cr >= this.inCr.Max )
                        ycbcr.Cr = this.outCr.Max;
                    else if ( ycbcr.Cr <= this.inCr.Min )
                        ycbcr.Cr = this.outCr.Min;
                    else
                        ycbcr.Cr = kcr * ycbcr.Cr + bcr;

                    // convert back to RGB
                    AForge.Imaging.YCbCr.ToRGB( ycbcr, rgb );

                    ptr[RGB.R] = rgb.Red;
                    ptr[RGB.G] = rgb.Green;
                    ptr[RGB.B] = rgb.Blue;
                }
                ptr += offset;
            }
        }
    }
}

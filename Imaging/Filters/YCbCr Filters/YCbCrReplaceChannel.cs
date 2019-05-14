// AForge Image Processing Library
// AForge.NET framework
//
// Copyright � AForge.NET, 2007-2011
// contacts@aforgenet.com
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Replace channel of YCbCr color space.
    /// </summary>
    /// 
    /// <remarks><para>Replaces specified YCbCr channel of color image with
    /// specified grayscale imge.</para>
    /// 
    /// <para>The filter is quite useful in conjunction with <see cref="YCbCrExtractChannel"/> filter
    /// (however may be used alone in some cases). Using the <see cref="YCbCrExtractChannel"/> filter
    /// it is possible to extract one of YCbCr channel, perform some image processing with it and then
    /// put it back into the original color image.</para>
    /// 
    /// <para>The filter accepts 24 and 32 bpp color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create YCbCrExtractChannel filter for channel extracting
    /// YCbCrExtractChannel extractFilter = new YCbCrExtractChannel(
    ///                                     YCbCr.CbIndex );
    /// // extract Cb channel
    /// Bitmap cbChannel = extractFilter.Apply( image );
    /// // invert the channel
    /// Invert invertFilter = new Invert( );
    /// invertFilter.ApplyInPlace( cbChannel );
    /// // put the channel back into the source image
    /// YCbCrReplaceChannel replaceFilter = new YCbCrReplaceChannel(
    ///                                     YCbCr.CbIndex, cbChannel );
    /// replaceFilter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/ycbcr_replace_channel.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    /// <seealso cref="YCbCrExtractChannel"/>
    /// 
    public class YCbCrReplaceChannel : BaseInPlacePartialFilter
    {
        private short channel = YCbCr.YIndex;
        private Bitmap channelImage;
        private UnmanagedImage unmanagedChannelImage;

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
        /// YCbCr channel to replace.
        /// </summary>
        /// 
        /// <remarks><para>Default value is set to <see cref="YCbCr.YIndex"/> (Y channel).</para></remarks>
        /// 
        /// <exception cref="ArgumentException">Invalid channel was specified.</exception>
        /// 
        public short Channel
        {
            get { return this.channel; }
            set
            {
                if (
                    ( value != YCbCr.YIndex ) &&
                    ( value != YCbCr.CbIndex ) &&
                    ( value != YCbCr.CrIndex )
                    )
                {
                    throw new ArgumentException( "Invalid YCbCr channel was specified." );
                }
                this.channel = value;
            }
        }

        /// <summary>
        /// Grayscale image to use for channel replacement.
        /// </summary>
        /// 
        /// <remarks>
        /// <para><note>Setting this property will clear the <see cref="UnmanagedChannelImage"/> property -
        /// only one channel image is allowed: managed or unmanaged.</note></para>
        /// </remarks>
        /// 
        /// <exception cref="InvalidImagePropertiesException">Channel image should be 8bpp indexed image (grayscale).</exception>
        /// 
        public Bitmap ChannelImage
        {
            get { return this.channelImage; }
            set
            {
                if ( value != null )
                {
                    // check for valid format
                    if ( value.PixelFormat != PixelFormat.Format8bppIndexed )
                        throw new InvalidImagePropertiesException( "Channel image should be 8bpp indexed image (grayscale)." );
                }

                this.channelImage = value;
                this.unmanagedChannelImage = null;
            }
        }

        /// <summary>
        /// Unmanaged grayscale image to use for channel replacement.
        /// </summary>
        /// 
        /// <remarks>
        /// <para><note>Setting this property will clear the <see cref="ChannelImage"/> property -
        /// only one channel image is allowed: managed or unmanaged.</note></para>
        /// </remarks>
        /// 
        /// <exception cref="InvalidImagePropertiesException">Channel image should be 8bpp indexed image (grayscale).</exception>
        /// 
        public UnmanagedImage UnmanagedChannelImage
        {
            get { return this.unmanagedChannelImage; }
            set
            {
                if ( value != null )
                {
                    // check for valid format
                    if ( value.PixelFormat != PixelFormat.Format8bppIndexed )
                        throw new InvalidImagePropertiesException( "Channel image should be 8bpp indexed image (grayscale)." );
                }

                this.channelImage = null;
                this.unmanagedChannelImage = value;
            }
        }

        // private constructor
        private YCbCrReplaceChannel( )
        {
            // initialize format translation dictionary
            this.formatTranslations[PixelFormat.Format24bppRgb]  = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppRgb]  = PixelFormat.Format32bppRgb;
            this.formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format32bppArgb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YCbCrReplaceChannel"/> class.
        /// </summary>
        /// 
        /// <param name="channel">YCbCr channel to replace.</param>
        /// 
        public YCbCrReplaceChannel( short channel ) : this( )
        {
            this.Channel = channel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YCbCrReplaceChannel"/> class.
        /// </summary>
        /// 
        /// <param name="channel">YCbCr channel to replace.</param>
        /// <param name="channelImage">Channel image to use for replacement.</param>
        /// 
        public YCbCrReplaceChannel( short channel, Bitmap channelImage ) : this( )
        {
            this.Channel = channel;
            this.ChannelImage = channelImage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YCbCrReplaceChannel"/> class.
        /// </summary>
        /// 
        /// <param name="channel">YCbCr channel to replace.</param>
        /// <param name="channelImage">Unmanaged channel image to use for replacement.</param>
        /// 
        public YCbCrReplaceChannel( short channel, UnmanagedImage channelImage ) : this( )
        {
            this.Channel = channel;
            this.UnmanagedChannelImage = channelImage;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// 
        /// <param name="image">Source image data.</param>
        /// <param name="rect">Image rectangle for processing by the filter.</param>
        /// 
        /// <exception cref="NullReferenceException">Channel image was not specified.</exception>
        /// <exception cref="InvalidImagePropertiesException">Channel image size does not match source
        /// image size.</exception>
        ///
        protected override unsafe void ProcessFilter( UnmanagedImage image, Rectangle rect )
        {
            if ( ( this.channelImage == null ) && ( this.unmanagedChannelImage == null ) )
            {
                throw new NullReferenceException( "Channel image was not specified." );
            }

            var pixelSize = Image.GetPixelFormatSize( image.PixelFormat ) / 8;

            var width   = image.Width;
            var height  = image.Height;
            var startX  = rect.Left;
            var startY  = rect.Top;
            var stopX   = startX + rect.Width;
            var stopY   = startY + rect.Height;
            var offset  = image.Stride - rect.Width * pixelSize;

            BitmapData chData = null;
            // pointer to channel's data
            byte* ch;
            // channel's image stride
            var chStride = 0;

            // check channel's image type
            if ( this.channelImage != null )
            {
                // check channel's image dimension
                if ( ( width != this.channelImage.Width ) || ( height != this.channelImage.Height ) )
                    throw new InvalidImagePropertiesException( "Channel image size does not match source image size." );

                // lock channel image
                chData = this.channelImage.LockBits(
                    new Rectangle( 0, 0, width, height ),
                    ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed );

                ch = (byte*) chData.Scan0.ToPointer( );
                chStride = chData.Stride;
            }
            else
            {
                // check channel's image dimension
                if ( ( width != this.unmanagedChannelImage.Width ) || ( height != this.unmanagedChannelImage.Height ) )
                    throw new InvalidImagePropertiesException( "Channel image size does not match source image size." );

                ch = (byte*)this.unmanagedChannelImage.ImageData;
                chStride = this.unmanagedChannelImage.Stride;
            }

            var     offsetCh = chStride - rect.Width;
            var     rgb = new RGB( );
            var   ycbcr = new YCbCr( );

            // do the job
            var dst = (byte*) image.ImageData.ToPointer( );

            // allign pointer to the first pixel to process
            dst += ( startY * image.Stride + startX * pixelSize );
            ch += ( startY * chStride + startX );

            // for each line
            for ( var y = startY; y < stopY; y++ )
            {
                // for each pixel
                for ( var x = startX; x < stopX; x++, dst += pixelSize, ch++ )
                {
                    rgb.Red     = dst[RGB.R];
                    rgb.Green   = dst[RGB.G];
                    rgb.Blue    = dst[RGB.B];

                    // convert to YCbCr
                    AForge.Imaging.YCbCr.FromRGB( rgb, ycbcr );

                    switch ( this.channel )
                    {
                        case YCbCr.YIndex:
                            ycbcr.Y = (float) *ch / 255;
                            break;

                        case YCbCr.CbIndex:
                            ycbcr.Cb = (float) *ch / 255 - 0.5f;
                            break;

                        case YCbCr.CrIndex:
                            ycbcr.Cr = (float) *ch / 255 - 0.5f;
                            break;
                    }

                    // convert back to RGB
                    AForge.Imaging.YCbCr.ToRGB( ycbcr, rgb );

                    dst[RGB.R] = rgb.Red;
                    dst[RGB.G] = rgb.Green;
                    dst[RGB.B] = rgb.Blue;
                }
                dst += offset;
                ch  += offsetCh;
            }

            if ( chData != null )
            {
                // unlock
                this.channelImage.UnlockBits( chData );
            }
        }
    }
}

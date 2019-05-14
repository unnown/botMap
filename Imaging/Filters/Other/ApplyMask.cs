﻿// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2005-2011
// contacts@aforgenet.com
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Apply mask to the specified image.
    /// </summary>
    /// 
    /// <remarks><para>The filter applies mask to the specified image - keeps all pixels
    /// in the image if corresponding pixels/values of the mask are not equal to 0. For all
    /// 0 pixels/values in mask, corresponding pixels in the source image are set to 0.</para>
    /// 
    /// <para>Mask can be specified as <see cref="MaskImage">.NET's managed Bitmap</see>, as
    /// <see cref="UnmanagedMaskImage">UnmanagedImage</see> or as <see cref="Mask">byte array</see>.
    /// In the case if mask is specified as image, it must be 8 bpp grayscale image. In all case
    /// mask size must be the same as size of the image to process.</para>
    /// 
    /// <para>The filter accepts 8/16 bpp grayscale and 24/32/48/64 bpp color images for processing.</para>
    /// </remarks>
    /// 
    public class ApplyMask : BaseInPlacePartialFilter
    {
        private Bitmap maskImage;
        private UnmanagedImage unmanagedMaskImage;
        private byte[,] mask;

        /// <summary>
        /// Mask image to apply.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies mask image to use. The image must be grayscale
        /// (8 bpp format) and have the same size as the source image to process.</para>
        /// 
        /// <para>When the property is set, both <see cref="UnmanagedMaskImage"/> and
        /// <see cref="Mask"/> properties are set to <see langword="null"/>.</para>
        /// </remarks>
        /// 
        /// <exception cref="ArgumentException">The mask image must be 8 bpp grayscale image.</exception>
        /// 
        public Bitmap MaskImage
        {
            get { return this.maskImage; }
            set
            {
                if ( ( this.maskImage != null ) && ( this.maskImage.PixelFormat != PixelFormat.Format8bppIndexed ) )
                {
                    throw new ArgumentException( "The mask image must be 8 bpp grayscale image." );
                }

                this.maskImage = value;
                this.unmanagedMaskImage = null;
                this.mask = null;
            }
        }

        /// <summary>
        /// Unmanaged mask image to apply.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies unmanaged mask image to use. The image must be grayscale
        /// (8 bpp format) and have the same size as the source image to process.</para>
        /// 
        /// <para>When the property is set, both <see cref="MaskImage"/> and
        /// <see cref="Mask"/> properties are set to <see langword="null"/>.</para>
        /// </remarks>
        /// 
        /// <exception cref="ArgumentException">The mask image must be 8 bpp grayscale image.</exception>
        /// 
        public UnmanagedImage UnmanagedMaskImage
        {
            get { return this.unmanagedMaskImage; }
            set
            {
                if ( ( this.unmanagedMaskImage != null ) && ( this.unmanagedMaskImage.PixelFormat != PixelFormat.Format8bppIndexed ) )
                {
                    throw new ArgumentException( "The mask image must be 8 bpp grayscale image." );
                }

                this.unmanagedMaskImage = value;
                this.maskImage = null;
                this.mask = null;
            }
        }

        /// <summary>
        /// Mask to apply.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies mask array to use. Size of the array must
        /// be the same size as the size of the source image to process - its 0<sup>th</sup> dimension
        /// must be equal to image's height and its 1<sup>st</sup> dimension must be equal to width. For
        /// example, for 640x480 image, the mask array must be defined as:
        /// <code>
        /// byte[,] mask = new byte[480, 640];
        /// </code>
        /// </para></remarks>
        /// 
        public byte[,] Mask
        {
            get { return this.mask; }
            set
            {
                this.mask = value;
                this.maskImage = null;
                this.unmanagedMaskImage = null;
            }
        }

        // private format translation dictionary
        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>( );

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        /// 
        /// <remarks><para>See <see cref="IFilterInformation.FormatTranslations"/>
        /// documentation for additional information.</para></remarks>
        /// 
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations
        {
            get { return this.formatTranslations; }
        }

        private ApplyMask( )
        {
            this.formatTranslations[PixelFormat.Format8bppIndexed]    = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format24bppRgb]       = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppArgb]      = PixelFormat.Format32bppArgb;
            this.formatTranslations[PixelFormat.Format32bppRgb]       = PixelFormat.Format32bppRgb;
            this.formatTranslations[PixelFormat.Format32bppPArgb]     = PixelFormat.Format32bppPArgb;
            this.formatTranslations[PixelFormat.Format16bppGrayScale] = PixelFormat.Format16bppGrayScale;
            this.formatTranslations[PixelFormat.Format48bppRgb]       = PixelFormat.Format48bppRgb;
            this.formatTranslations[PixelFormat.Format64bppArgb]      = PixelFormat.Format64bppArgb;
            this.formatTranslations[PixelFormat.Format64bppPArgb]     = PixelFormat.Format64bppPArgb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyMask"/> class.
        /// </summary>
        /// 
        /// <param name="maskImage"><see cref="MaskImage">Mask image</see> to use.</param>
        /// 
        public ApplyMask( Bitmap maskImage ) : this( )
        {
            this.MaskImage = maskImage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyMask"/> class.
        /// </summary>
        /// 
        /// <param name="unmanagedMaskImage"><see cref="UnmanagedMaskImage">Unmanaged mask image</see> to use.</param>
        /// 
        public ApplyMask( UnmanagedImage unmanagedMaskImage ) : this( )
        {
            this.UnmanagedMaskImage = unmanagedMaskImage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyMask"/> class.
        /// </summary>
        /// 
        /// <param name="mask"><see cref="Mask"/> to use.</param>
        /// 
        public ApplyMask( byte[,] mask ) : this( )
        {
            this.Mask = mask;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// 
        /// <param name="image">Source image data.</param>
        /// <param name="rect">Image rectangle for processing by the filter.</param>
        /// 
        /// <exception cref="NullReferenceException">None of the possible mask properties were set. Need to provide mask before applying the filter.</exception>
        /// <exception cref="ArgumentException">Invalid size of provided mask. Its size must be the same as the size of the image to mask.</exception>
        ///
        protected override unsafe void ProcessFilter( UnmanagedImage image, Rectangle rect )
        {
            if ( this.mask != null )
            {
                if ( ( image.Width  != this.mask.GetLength( 1 ) ) ||
                     ( image.Height != this.mask.GetLength( 0 ) ) )
                {
                    throw new ArgumentException( "Invalid size of mask array. Its size must be the same as the size of the image to mask." );
                }

                fixed ( byte* maskPtr = this.mask )
                {
                    this.ProcessImage( image, rect, maskPtr, this.mask.GetLength( 1 ) );
                }
            }
            else if ( this.unmanagedMaskImage != null )
            {
                if ( ( image.Width  != this.unmanagedMaskImage.Width ) ||
                     ( image.Height != this.unmanagedMaskImage.Height ) )
                {
                    throw new ArgumentException( "Invalid size of unmanaged mask image. Its size must be the same as the size of the image to mask." );
                }

                this.ProcessImage( image, rect, (byte*)this.unmanagedMaskImage.ImageData.ToPointer( ),
                              this.unmanagedMaskImage.Stride );
            }
            else if ( this.maskImage != null )
            {
                if ( ( image.Width  != this.maskImage.Width ) ||
                     ( image.Height != this.maskImage.Height ) )
                {
                    throw new ArgumentException( "Invalid size of mask image. Its size must be the same as the size of the image to mask." );
                }

                var maskData = this.maskImage.LockBits( new Rectangle( 0, 0, image.Width, image.Height ),
                    ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed );

                try
                {
                    this.ProcessImage( image, rect, (byte*) maskData.Scan0.ToPointer( ),
                                  maskData.Stride );
                }
                finally
                {
                    this.maskImage.UnlockBits( maskData );
                }
            }
            else
            {
                throw new NullReferenceException( "None of the possible mask properties were set. Need to provide mask before applying the filter." );
            }
        }

        private unsafe void ProcessImage( UnmanagedImage image, Rectangle rect, byte* mask, int maskLineSize )
        {
            var pixelSize = Bitmap.GetPixelFormatSize( image.PixelFormat ) / 8;

            var startY  = rect.Top;
            var stopY   = startY + rect.Height;

            var startX  = rect.Left;
            var stopX   = startX + rect.Width;

            var stride = image.Stride;
            var maskOffset = maskLineSize - rect.Width;

            // allign mask to the first pixel
            mask += maskLineSize * startY + startX;

            if ( ( pixelSize <= 4 ) && ( pixelSize != 2 ) )
            {
                // 8 bits per channel
                var imagePtr = (byte*) image.ImageData.ToPointer( ) +
                                 stride * startY + pixelSize * startX;
                var offset = stride - rect.Width * pixelSize;

                #region 8 bit cases
                switch ( pixelSize )
                {
                    case 1:
                        // 8 bpp grayscale
                        for ( var y = startY; y < stopY; y++ )
                        {
                            for ( var x = startX; x < stopX; x++, imagePtr++, mask++ )
                            {
                                if ( *mask == 0 )
                                {
                                    *imagePtr = 0;
                                }
                            }
                            imagePtr += offset;
                            mask += maskOffset;
                        }
                        break;

                    case 3:
                        // 24 bpp color
                        for ( var y = startY; y < stopY; y++ )
                        {
                            for ( var x = startX; x < stopX; x++, imagePtr += 3, mask++ )
                            {
                                if ( *mask == 0 )
                                {
                                    imagePtr[RGB.R] = 0;
                                    imagePtr[RGB.G] = 0;
                                    imagePtr[RGB.B] = 0;
                                }
                            }
                            imagePtr += offset;
                            mask += maskOffset;
                        }
                        break;

                    case 4:
                        // 32 bpp color
                        for ( var y = startY; y < stopY; y++ )
                        {
                            for ( var x = startX; x < stopX; x++, imagePtr += 4, mask++ )
                            {
                                if ( *mask == 0 )
                                {
                                    imagePtr[RGB.R] = 0;
                                    imagePtr[RGB.G] = 0;
                                    imagePtr[RGB.B] = 0;
                                    imagePtr[RGB.A] = 0;
                                }
                            }
                            imagePtr += offset;
                            mask += maskOffset;
                        }
                        break;
                }
                #endregion
            }
            else
            {
                // 16 bits per channel
                var imagePtrBase = (byte*) image.ImageData.ToPointer( ) +
                                     stride * startY + pixelSize * startX;

                #region 16 bit cases
                switch ( pixelSize )
                {
                    case 2:
                        // 16 bpp grayscale
                        for ( var y = startY; y < stopY; y++ )
                        {
                            var imagePtr = (ushort*) imagePtrBase;

                            for ( var x = startX; x < stopX; x++, imagePtr++, mask++ )
                            {
                                if ( *mask == 0 )
                                {
                                    *imagePtr = 0;
                                }
                            }
                            imagePtrBase += stride;
                            mask += maskOffset;
                        }
                        break;

                    case 6:
                        // 16 bpp grayscale
                        for ( var y = startY; y < stopY; y++ )
                        {
                            var imagePtr = (ushort*) imagePtrBase;

                            for ( var x = startX; x < stopX; x++, imagePtr += 3, mask++ )
                            {
                                if ( *mask == 0 )
                                {
                                    imagePtr[RGB.R] = 0;
                                    imagePtr[RGB.G] = 0;
                                    imagePtr[RGB.B] = 0;
                                }
                            }
                            imagePtrBase += stride;
                            mask += maskOffset;
                        }
                        break;

                    case 8:
                        // 16 bpp grayscale
                        for ( var y = startY; y < stopY; y++ )
                        {
                            var imagePtr = (ushort*) imagePtrBase;

                            for ( var x = startX; x < stopX; x++, imagePtr += 4, mask++ )
                            {
                                if ( *mask == 0 )
                                {
                                    imagePtr[RGB.R] = 0;
                                    imagePtr[RGB.G] = 0;
                                    imagePtr[RGB.B] = 0;
                                    imagePtr[RGB.A] = 0;
                                }
                            }
                            imagePtrBase += stride;
                            mask += maskOffset;
                        }
                        break;
                }
                #endregion
            }
        }
    }
}

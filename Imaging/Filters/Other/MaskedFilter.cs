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
    /// Apply filter according to the specified mask.
    /// </summary>
    /// 
    /// <remarks><para>The image processing routine applies the specified <see cref="BaseFilter"/> to
    /// a source image according to the specified mask - if a pixel/value in the specified mask image/array
    /// is set to 0, then the original pixel's value is kept; otherwise the pixel is filtered using the
    /// specified base filter.</para>
    /// 
    /// <para>Mask can be specified as <see cref="MaskImage">.NET's managed Bitmap</see>, as
    /// <see cref="UnmanagedMaskImage">UnmanagedImage</see> or as <see cref="Mask">byte array</see>.
    /// In the case if mask is specified as image, it must be 8 bpp grayscale image. In all case
    /// mask size must be the same as size of the image to process.</para>
    /// 
    /// <para><note>Pixel formats accepted by this filter are specified by the <see cref="BaseFilter"/>.</note></para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create the filter
    /// MaskedFilter maskedFilter = new MaskedFilter( new Sepia( ), maskImage );
    /// // apply the filter
    /// maskedFilter.ApplyInPlace( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample1.jpg" width="480" height="361" />
    /// <para><b>Mask image:</b></para>
    /// <img src="img/imaging/mask.png" width="480" height="361" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/masked_image.jpg" width="480" height="361" />
    /// </remarks>
    /// 
    public class MaskedFilter : BaseInPlacePartialFilter
    {
        private IFilter baseFilter = null;

        // masks (one of them must be set)
        private Bitmap maskImage;
        private UnmanagedImage unmanagedMaskImage;
        private byte[,] mask;

        /// <summary>
        /// Base filter to apply to the source image.
        /// </summary>
        ///
        /// <remarks><para>The property specifies base filter which is applied to the specified source
        /// image (to all pixels which have corresponding none 0 value in mask image/array).</para>
        /// 
        /// <para><note>The base filter must implement <see cref="IFilterInformation"/> interface.</note></para>
        /// 
        /// <para><note>The base filter must never change image's pixel format. For example, if source
        /// image's pixel format is 24 bpp color image, then it must stay the same after the base
        /// filter is applied.</note></para>
        /// 
        /// <para><note>The base filter must never change size of the source image.</note></para>
        /// </remarks>
        /// 
        /// <exception cref="NullReferenceException">Base filter can not be set to null.</exception>
        /// <exception cref="ArgumentException">The specified base filter must implement IFilterInformation interface.</exception>
        /// <exception cref="ArgumentException">The specified filter must never change pixel format.</exception>
        ///
        public IFilter BaseFilter
        {
            get { return this.baseFilter; }
            private set
            {
                if ( value == null )
                {
                    throw new NullReferenceException( "Base filter can not be set to null." );
                }

                if ( !( value is IFilterInformation ) )
                {
                    throw new ArgumentException( "The specified base filter must implement IFilterInformation interface." );
                }

                // check that the base filter does not change pixel format of image
                var baseFormatTranslations =
                    ( (IFilterInformation) value ).FormatTranslations;

                foreach ( var translation in baseFormatTranslations )
                {
                    if ( translation.Key != translation.Value )
                    {
                        throw new ArgumentException( "The specified filter must never change pixel format." );
                    }
                }

                this.baseFilter = value;
            }
        }

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

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        /// 
        /// <remarks><para>See <see cref="IFilterInformation.FormatTranslations"/>
        /// documentation for additional information.</para>
        /// 
        /// <para><note>The property returns format translation table from the
        /// <see cref="BaseFilter"/>.</note></para>
        /// </remarks>
        /// 
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations
        {
            get { return ( (IFilterInformation)this.baseFilter ).FormatTranslations; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskedFilter"/> class.
        /// </summary>
        /// 
        /// <param name="baseFiler"><see cref="BaseFilter">Base filter</see> to apply to the specified source image.</param>
        /// <param name="maskImage"><see cref="MaskImage">Mask image</see> to use.</param>
        /// 
        public MaskedFilter( IFilter baseFiler, Bitmap maskImage )
        {
            this.BaseFilter = baseFiler;
            this.MaskImage = maskImage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskedFilter"/> class.
        /// </summary>
        /// 
        /// <param name="baseFiler"><see cref="BaseFilter">Base filter</see> to apply to the specified source image.</param>
        /// <param name="unmanagedMaskImage"><see cref="UnmanagedMaskImage">Unmanaged mask image</see> to use.</param>
        /// 
        public MaskedFilter( IFilter baseFiler, UnmanagedImage unmanagedMaskImage )
        {
            this.BaseFilter = baseFiler;
            this.UnmanagedMaskImage = unmanagedMaskImage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskedFilter"/> class.
        /// </summary>
        /// 
        /// <param name="baseFiler"><see cref="BaseFilter">Base filter</see> to apply to the specified source image.</param>
        /// <param name="mask"><see cref="Mask"/> to use.</param>
        /// 
        public MaskedFilter( IFilter baseFiler, byte[,] mask )
        {
            this.BaseFilter = baseFiler;
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
            // apply base filter to the specified image
            var filteredImage = this.baseFilter.Apply( image );

            if ( ( image.Width  != filteredImage.Width ) ||
                 ( image.Height != filteredImage.Height ) )
            {
                throw new ArgumentException( "Base filter must not change image size." );
            }

            var pixelSize = Bitmap.GetPixelFormatSize( image.PixelFormat ) / 8;

            var startY  = rect.Top;
            var stopY   = startY + rect.Height;

            var startX  = rect.Left;
            var stopX   = startX + rect.Width;

            var srcStride = image.Stride;
            var filteredStride = filteredImage.Stride;
            var maskOffset = maskLineSize - rect.Width;

            // allign mask to the first pixel
            mask += maskLineSize * startY + startX;

            if ( ( pixelSize <= 4 ) && ( pixelSize != 2 ) )
            {
                // 8 bits per channel
                var imagePtr = (byte*) image.ImageData.ToPointer( ) +
                                 srcStride * startY + pixelSize * startX;
                var srcOffset = srcStride - rect.Width * pixelSize;

                var filteredPtr = (byte*) filteredImage.ImageData.ToPointer( ) +
                                    filteredStride * startY + pixelSize * startX;
                var filteredOffset = filteredStride - rect.Width * pixelSize;

                #region 8 bit cases
                switch ( pixelSize )
                {
                    case 1:
                        // 8 bpp grayscale
                        for ( var y = startY; y < stopY; y++ )
                        {
                            for ( var x = startX; x < stopX; x++, imagePtr++, filteredPtr++, mask++ )
                            {
                                if ( *mask != 0 )
                                {
                                    *imagePtr = *filteredPtr;
                                }
                            }
                            imagePtr += srcOffset;
                            filteredPtr += filteredOffset;
                            mask += maskOffset;
                        }
                        break;

                    case 3:
                        // 24 bpp color
                        for ( var y = startY; y < stopY; y++ )
                        {
                            for ( var x = startX; x < stopX; x++, imagePtr += 3, filteredPtr += 3, mask++ )
                            {
                                if ( *mask != 0 )
                                {
                                    imagePtr[RGB.R] = filteredPtr[RGB.R];
                                    imagePtr[RGB.G] = filteredPtr[RGB.G];
                                    imagePtr[RGB.B] = filteredPtr[RGB.B];
                                }
                            }
                            imagePtr += srcOffset;
                            filteredPtr += filteredOffset;
                            mask += maskOffset;
                        }
                        break;

                    case 4:
                        // 32 bpp color
                        for ( var y = startY; y < stopY; y++ )
                        {
                            for ( var x = startX; x < stopX; x++, imagePtr += 4, filteredPtr += 4, mask++ )
                            {
                                if ( *mask != 0 )
                                {
                                    imagePtr[RGB.R] = filteredPtr[RGB.R];
                                    imagePtr[RGB.G] = filteredPtr[RGB.G];
                                    imagePtr[RGB.B] = filteredPtr[RGB.B];
                                    imagePtr[RGB.A] = filteredPtr[RGB.A];
                                }
                            }
                            imagePtr += srcOffset;
                            filteredPtr += filteredOffset;
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
                                     srcStride * startY + pixelSize * startX;
                var filteredPtrBase = (byte*) filteredImage.ImageData.ToPointer( ) +
                                        filteredStride * startY + pixelSize * startX;

                #region 16 bit cases
                switch ( pixelSize )
                {
                    case 2:
                        // 16 bpp grayscale
                        for ( var y = startY; y < stopY; y++ )
                        {
                            var imagePtr = (ushort*) imagePtrBase;
                            var filteredPtr = (ushort*) filteredPtrBase;

                            for ( var x = startX; x < stopX; x++, imagePtr++, filteredPtr++, mask++ )
                            {
                                if ( *mask != 0 )
                                {
                                    *imagePtr = *filteredPtr;
                                }
                            }
                            imagePtrBase += srcStride;
                            filteredPtrBase += filteredStride;
                            mask += maskOffset;
                        }
                        break;

                    case 6:
                        // 16 bpp grayscale
                        for ( var y = startY; y < stopY; y++ )
                        {
                            var imagePtr = (ushort*) imagePtrBase;
                            var filteredPtr = (ushort*) filteredPtrBase;

                            for ( var x = startX; x < stopX; x++, imagePtr += 3, filteredPtr += 3, mask++ )
                            {
                                if ( *mask != 0 )
                                {
                                    imagePtr[RGB.R] = filteredPtr[RGB.R];
                                    imagePtr[RGB.G] = filteredPtr[RGB.G];
                                    imagePtr[RGB.B] = filteredPtr[RGB.B];
                                }
                            }
                            imagePtrBase += srcStride;
                            filteredPtrBase += filteredStride;
                            mask += maskOffset;
                        }
                        break;

                    case 8:
                        // 16 bpp grayscale
                        for ( var y = startY; y < stopY; y++ )
                        {
                            var imagePtr = (ushort*) imagePtrBase;
                            var filteredPtr = (ushort*) filteredPtrBase;

                            for ( var x = startX; x < stopX; x++, imagePtr += 4, filteredPtr += 4, mask++ )
                            {
                                if ( *mask != 0 )
                                {
                                    imagePtr[RGB.R] = filteredPtr[RGB.R];
                                    imagePtr[RGB.G] = filteredPtr[RGB.G];
                                    imagePtr[RGB.B] = filteredPtr[RGB.B];
                                    imagePtr[RGB.A] = filteredPtr[RGB.A];
                                }
                            }
                            imagePtrBase += srcStride;
                            filteredPtrBase += filteredStride;
                            mask += maskOffset;
                        }
                        break;
                }
                #endregion
            }
        }
    }
}

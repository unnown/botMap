// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2005-2012
// contacts@aforgenet.com
//

namespace AForge.Imaging
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Blob counter based on recursion.
    /// </summary>
    /// 
    /// <remarks><para>The class counts and extracts stand alone objects in
    /// images using recursive version of connected components labeling
    /// algorithm.</para>
    /// 
    /// <para><note>The algorithm treats all pixels with values less or equal to <see cref="BackgroundThreshold"/>
    /// as background, but pixels with higher values are treated as objects' pixels.</note></para>
    /// 
    /// <para><note>Since this algorithm is based on recursion, it is
    /// required to be careful with its application to big images with big blobs,
    /// because in this case recursion will require big stack size and may lead
    /// to stack overflow. The recursive version may be applied (and may be even
    /// faster than <see cref="BlobCounter"/>) to an image with small blobs -
    /// "star sky" image (or small cells, for example, etc).</note></para>
    /// 
    /// <para>For blobs' searching the class supports 8 bpp indexed grayscale images and
    /// 24/32 bpp color images. 
    /// See documentation about <see cref="BlobCounterBase"/> for information about which
    /// pixel formats are supported for extraction of blobs.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create an instance of blob counter algorithm
    /// RecursiveBlobCounter bc = new RecursiveBlobCounter( );
    /// // process binary image
    /// bc.ProcessImage( image );
    /// Rectangle[] rects = bc.GetObjectsRectangles( );
    /// // process blobs
    /// foreach ( Rectangle rect in rects )
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </remarks>
    /// 
    public class RecursiveBlobCounter : BlobCounterBase
    {
        // temporary variable
        private int[] tempLabels;
        private int stride;
        private int pixelSize;

        private byte backgroundThresholdR = 0;
        private byte backgroundThresholdG = 0;
        private byte backgroundThresholdB = 0;

        /// <summary>
        /// Background threshold's value.
        /// </summary>
        /// 
        /// <remarks><para>The property sets threshold value for distinguishing between background
        /// pixel and objects' pixels. All pixel with values less or equal to this property are
        /// treated as background, but pixels with higher values are treated as objects' pixels.</para>
        /// 
        /// <para><note>In the case of colour images a pixel is treated as objects' pixel if <b>any</b> of its
        /// RGB values are higher than corresponding values of this threshold.</note></para>
        /// 
        /// <para><note>For processing grayscale image, set the property with all RGB components eqaul.</note></para>
        ///
        /// <para>Default value is set to <b>(0, 0, 0)</b> - black colour.</para></remarks>
        /// 
        public Color BackgroundThreshold
        {
            get { return Color.FromArgb(this.backgroundThresholdR , this.backgroundThresholdG , this.backgroundThresholdB ); }
            set
            {
                this.backgroundThresholdR = value.R;
                this.backgroundThresholdG = value.G;
                this.backgroundThresholdB = value.B;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecursiveBlobCounter"/> class.
        /// </summary>
        /// 
        /// <remarks>Creates new instance of the <see cref="RecursiveBlobCounter"/> class with
        /// an empty objects map. Before using methods, which provide information about blobs
        /// or extract them, the <see cref="BlobCounterBase.ProcessImage(Bitmap)"/>,
        /// <see cref="BlobCounterBase.ProcessImage(BitmapData)"/> or <see cref="BlobCounterBase.ProcessImage(UnmanagedImage)"/>
        /// method should be called to collect objects map.</remarks>
        /// 
        public RecursiveBlobCounter( ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecursiveBlobCounter"/> class.
        /// </summary>
        /// 
        /// <param name="image">Image to look for objects in.</param>
        /// 
        public RecursiveBlobCounter( Bitmap image ) : base( image ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecursiveBlobCounter"/> class.
        /// </summary>
        /// 
        /// <param name="imageData">Image data to look for objects in.</param>
        /// 
        public RecursiveBlobCounter( BitmapData imageData ) : base( imageData ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecursiveBlobCounter"/> class.
        /// </summary>
        /// 
        /// <param name="image">Unmanaged image to look for objects in.</param>
        /// 
        public RecursiveBlobCounter( UnmanagedImage image ) : base( image ) { }

        /// <summary>
        /// Actual objects map building.
        /// </summary>
        /// 
        /// <param name="image">Unmanaged image to process.</param>
        /// 
        /// <remarks>The method supports 8 bpp indexed grayscale images and 24/32 bpp color images.</remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the source image.</exception>
        /// 
        protected override void BuildObjectsMap( UnmanagedImage image )
        {
            this.stride = image.Stride;

            // check pixel format
            if ( ( image.PixelFormat != PixelFormat.Format8bppIndexed ) &&
                 ( image.PixelFormat != PixelFormat.Format24bppRgb ) &&
                 ( image.PixelFormat != PixelFormat.Format32bppRgb ) &&
                 ( image.PixelFormat != PixelFormat.Format32bppArgb ) &&
                 ( image.PixelFormat != PixelFormat.Format32bppPArgb ) )
            {
                throw new UnsupportedImageFormatException( "Unsupported pixel format of the source image." );
            }

            // allocate temporary labels array
            this.tempLabels = new int[( this.imageWidth + 2 ) * ( this.imageHeight + 2 )];
            // fill boundaries with reserved value
            for ( int x = 0, mx = this.imageWidth + 2; x < mx; x++ )
            {
                this.tempLabels[x] = -1;
                this.tempLabels[x + ( this.imageHeight + 1 ) * ( this.imageWidth + 2 )] = -1;
            }
            for ( int y = 0, my = this.imageHeight + 2; y < my; y++ )
            {
                this.tempLabels[y * ( this.imageWidth + 2 )] = -1;
                this.tempLabels[y * ( this.imageWidth + 2 ) + this.imageWidth + 1] = -1;
            }

            // initial objects count
            this.objectsCount = 0;

            // do the job
            unsafe
            {
                var src = (byte*) image.ImageData.ToPointer( );
                var p = this.imageWidth + 2 + 1;

                if ( image.PixelFormat == PixelFormat.Format8bppIndexed )
                {
                    var offset = this.stride - this.imageWidth;

                    // for each line
                    for ( var y = 0; y < this.imageHeight; y++ )
                    {
                        // for each pixel
                        for ( var x = 0; x < this.imageWidth; x++, src++, p++ )
                        {
                            // check for non-labeled pixel
                            if ( ( *src > this.backgroundThresholdG ) && ( this.tempLabels[p] == 0 ) )
                            {
                                this.objectsCount++;
                                this.LabelPixel( src, p );
                            }
                        }
                        src += offset;
                        p += 2;
                    }
                }
                else
                {
                    this.pixelSize = Bitmap.GetPixelFormatSize( image.PixelFormat ) / 8;
                    var offset = this.stride - this.imageWidth * this.pixelSize;

                    // for each line
                    for ( var y = 0; y < this.imageHeight; y++ )
                    {
                        // for each pixel
                        for ( var x = 0; x < this.imageWidth; x++, src += this.pixelSize, p++ )
                        {
                            // check for non-labeled pixel
                            if ( (
                                    ( src[RGB.R] > this.backgroundThresholdR ) ||
                                    ( src[RGB.G] > this.backgroundThresholdG ) ||
                                    ( src[RGB.B] > this.backgroundThresholdB )
                                  ) && 
                                ( this.tempLabels[p] == 0 ) )
                            {
                                this.objectsCount++;
                                this.LabelColorPixel( src, p );
                            }
                        }
                        src += offset;
                        p += 2;
                    }
                }
            }

            // allocate labels array
            this.objectLabels = new int[this.imageWidth * this.imageHeight];

            for ( var y = 0; y < this.imageHeight; y++ )
            {
                Array.Copy(this.tempLabels , ( y + 1 ) * ( this.imageWidth + 2 ) + 1, this.objectLabels , y * this.imageWidth , this.imageWidth );
            }
        }

        private unsafe void LabelPixel( byte* pixel, int labelPointer )
        {
            if ( ( this.tempLabels[labelPointer] == 0 ) && ( *pixel > this.backgroundThresholdG ) )
            {
                this.tempLabels[labelPointer] = this.objectsCount;

                this.LabelPixel( pixel + 1, labelPointer + 1 );                              // x + 1, y
                this.LabelPixel( pixel + 1 + this.stride , labelPointer + 1 + 2 + this.imageWidth );    // x + 1, y + 1
                this.LabelPixel( pixel + this.stride , labelPointer + 2 + this.imageWidth );            // x    , y + 1
                this.LabelPixel( pixel - 1 + this.stride , labelPointer - 1 + 2 + this.imageWidth );    // x - 1, y + 1
                this.LabelPixel( pixel - 1, labelPointer - 1 );                              // x - 1, y
                this.LabelPixel( pixel - 1 - this.stride , labelPointer - 1 - 2 - this.imageWidth );    // x - 1, y - 1
                this.LabelPixel( pixel - this.stride , labelPointer - 2 - this.imageWidth );            // x    , y - 1
                this.LabelPixel( pixel + 1 - this.stride , labelPointer + 1 - 2 - this.imageWidth );    // x + 1, y - 1
            }
        }

        private unsafe void LabelColorPixel( byte* pixel, int labelPointer )
        {
            if ( ( this.tempLabels[labelPointer] == 0 ) && (
                ( pixel[RGB.R] > this.backgroundThresholdR ) ||
                ( pixel[RGB.G] > this.backgroundThresholdG ) ||
                ( pixel[RGB.B] > this.backgroundThresholdB ) ) )
            {
                this.tempLabels[labelPointer] = this.objectsCount;

                this.LabelColorPixel( pixel + this.pixelSize , labelPointer + 1 );                              // x + 1, y
                this.LabelColorPixel( pixel + this.pixelSize + this.stride , labelPointer + 1 + 2 + this.imageWidth );    // x + 1, y + 1
                this.LabelColorPixel( pixel + this.stride , labelPointer + 2 + this.imageWidth );                    // x    , y + 1
                this.LabelColorPixel( pixel - this.pixelSize + this.stride , labelPointer - 1 + 2 + this.imageWidth );    // x - 1, y + 1
                this.LabelColorPixel( pixel - this.pixelSize , labelPointer - 1 );                              // x - 1, y
                this.LabelColorPixel( pixel - this.pixelSize - this.stride , labelPointer - 1 - 2 - this.imageWidth );    // x - 1, y - 1
                this.LabelColorPixel( pixel - this.stride , labelPointer - 2 - this.imageWidth );                    // x    , y - 1
                this.LabelColorPixel( pixel + this.pixelSize - this.stride , labelPointer + 1 - 2 - this.imageWidth );    // x + 1, y - 1
            }
        }
    }
}

// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright � AForge.NET, 2005-2012
// contacts@aforgenet.com
//

namespace AForge.Imaging
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using AForge;

    /// <summary>
    /// Possible object orders.
    /// </summary>
    /// 
    /// <remarks>The enumeration defines possible sorting orders of objects, found by blob
    /// counting classes.</remarks>
    /// 
    public enum ObjectsOrder
    {
        /// <summary>
        /// Unsorted order (as it is collected by algorithm).
        /// </summary>
        None,

        /// <summary>
        /// Objects are sorted by size in descending order (bigger objects go first).
        /// Size is calculated as <b>Width * Height</b>.
        /// </summary>
        Size,

        /// <summary>
        /// Objects are sorted by area in descending order (bigger objects go first).
        /// </summary>
        Area,

        /// <summary>
        /// Objects are sorted by Y coordinate, then by X coordinate in ascending order
        /// (smaller coordinates go first).
        /// </summary>
        YX,

        /// <summary>
        /// Objects are sorted by X coordinate, then by Y coordinate in ascending order
        /// (smaller coordinates go first).
        /// </summary>
        XY
    }

    /// <summary>
    /// Base class for different blob counting algorithms.
    /// </summary>
    /// 
    /// <remarks><para>The class is abstract and serves as a base for different blob counting algorithms.
    /// Classes, which inherit from this base class, require to implement <see cref="BuildObjectsMap"/>
    /// method, which does actual building of object's label's map.</para>
    /// 
    /// <para>For blobs' searcing usually all inherited classes accept binary images, which are actually
    /// grayscale thresholded images. But the exact supported format should be checked in particular class,
    /// inheriting from the base class. For blobs' extraction the class supports grayscale (8 bpp indexed)
    /// and color images (24 and 32 bpp).</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create an instance of blob counter algorithm
    /// BlobCounterBase bc = new ...
    /// // set filtering options
    /// bc.FilterBlobs = true;
    /// bc.MinWidth  = 5;
    /// bc.MinHeight = 5;
    /// // process binary image
    /// bc.ProcessImage( image );
    /// Blob[] blobs = bc.GetObjects( image, false );
    /// // process blobs
    /// foreach ( Blob blob in blobs )
    /// {
    ///     // ...
    ///     // blob.Rectangle - blob's rectangle
    ///     // blob.Image - blob's image
    /// }
    /// </code>
    /// </remarks>
    /// 
    public abstract class BlobCounterBase
    {
        // found blobs
        List<Blob> blobs = new List<Blob>( );

        // objects' sort order
        private ObjectsOrder objectsOrder = ObjectsOrder.None;

        // filtering by size is required or not
        private bool filterBlobs = false;
        private IBlobsFilter filter = null;

        // coupled size filtering or not
        private bool coupledSizeFiltering = false;

        // blobs' minimal and maximal size
        private int minWidth    = 1;
        private int minHeight   = 1;
        private int maxWidth    = int.MaxValue;
        private int maxHeight   = int.MaxValue;

        /// <summary>
        /// Objects count.
        /// </summary>
        protected int objectsCount;

        /// <summary>
        /// Objects' labels.
        /// </summary>
        protected int[] objectLabels;

        /// <summary>
        /// Width of processed image.
        /// </summary>
        protected int imageWidth;

        /// <summary>
        /// Height of processed image.
        /// </summary>
        protected int imageHeight;

        /// <summary>
        /// Objects count.
        /// </summary>
        /// 
        /// <remarks><para>Number of objects (blobs) found by <see cref="ProcessImage(Bitmap)"/> method.
        /// </para></remarks>
        /// 
        public int ObjectsCount
        {
            get { return this.objectsCount; }
        }

        /// <summary>
        /// Objects' labels.
        /// </summary>
        /// 
        /// <remarks>The array of <b>width</b> * <b>height</b> size, which holds
        /// labels for all objects. Background is represented with <b>0</b> value,
        /// but objects are represented with labels starting from <b>1</b>.</remarks>
        /// 
        public int[] ObjectLabels
        {
            get { return this.objectLabels; }
        }

        /// <summary>
        /// Objects sort order.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies objects' sort order, which are provided
        /// by <see cref="GetObjectsRectangles"/>, <see cref="GetObjectsInformation"/>, etc.
        /// </para></remarks>
        /// 
        public ObjectsOrder ObjectsOrder
        {
            get { return this.objectsOrder; }
            set { this.objectsOrder = value; }
        }

        /// <summary>
        /// Specifies if blobs should be filtered.
        /// </summary>
        /// 
        /// <remarks><para>If the property is equal to <b>false</b>, then there is no any additional
        /// post processing after image was processed. If the property is set to <b>true</b>, then
        /// blobs filtering is done right after image processing routine. If <see cref="BlobsFilter"/>
        /// is set, then custom blobs' filtering is done, which is implemented by user. Otherwise
        /// blobs are filtered according to dimensions specified in <see cref="MinWidth"/>,
        /// <see cref="MinHeight"/>, <see cref="MaxWidth"/> and <see cref="MaxHeight"/> properties.</para>
        /// 
        /// <para>Default value is set to <see langword="false"/>.</para></remarks>
        /// 
        public bool FilterBlobs
        {
            get { return this.filterBlobs; }
            set { this.filterBlobs = value; }
        }

        /// <summary>
        /// Specifies if size filetering should be coupled or not.
        /// </summary>
        /// 
        /// <remarks><para>In uncoupled filtering mode, objects are filtered out in the case if
        /// their width is smaller than <see cref="MinWidth"/> <b>or</b> height is smaller than 
        /// <see cref="MinHeight"/>. But in coupled filtering mode, objects are filtered out in
        /// the case if their width is smaller than <see cref="MinWidth"/> <b>and</b> height is
        /// smaller than <see cref="MinHeight"/>. In both modes the idea with filtering by objects'
        /// maximum size is the same as filtering by objects' minimum size.</para>
        /// 
        /// <para>Default value is set to <see langword="false"/>, what means uncoupled filtering by size.</para>
        /// </remarks>
        /// 
        public bool CoupledSizeFiltering
        {
            get { return this.coupledSizeFiltering; }
            set { this.coupledSizeFiltering = value; }
        }

        /// <summary>
        /// Minimum allowed width of blob.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies minimum object's width acceptable by blob counting
        /// routine and has power only when <see cref="FilterBlobs"/> property is set to
        /// <see langword="true"/> and <see cref="BlobsFilter">custom blobs' filter</see> is
        /// set to <see langword="null"/>.</para>
        /// 
        /// <para>See documentation to <see cref="CoupledSizeFiltering"/> for additional information.</para>
        /// </remarks>
        /// 
        public int MinWidth
        {
            get { return this.minWidth; }
            set { this.minWidth = value; }
        }

        /// <summary>
        /// Minimum allowed height of blob.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies minimum object's height acceptable by blob counting
        /// routine and has power only when <see cref="FilterBlobs"/> property is set to
        /// <see langword="true"/> and <see cref="BlobsFilter">custom blobs' filter</see> is
        /// set to <see langword="null"/>.</para>
        /// 
        /// <para>See documentation to <see cref="CoupledSizeFiltering"/> for additional information.</para>
        /// </remarks>
        /// 
        public int MinHeight
        {
            get { return this.minHeight; }
            set { this.minHeight = value; }
        }

        /// <summary>
        /// Maximum allowed width of blob.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies maximum object's width acceptable by blob counting
        /// routine and has power only when <see cref="FilterBlobs"/> property is set to
        /// <see langword="true"/> and <see cref="BlobsFilter">custom blobs' filter</see> is
        /// set to <see langword="null"/>.</para>
        /// 
        /// <para>See documentation to <see cref="CoupledSizeFiltering"/> for additional information.</para>
        /// </remarks>
        /// 
        public int MaxWidth
        {
            get { return this.maxWidth; }
            set { this.maxWidth = value; }
        }

        /// <summary>
        /// Maximum allowed height of blob.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies maximum object's height acceptable by blob counting
        /// routine and has power only when <see cref="FilterBlobs"/> property is set to
        /// <see langword="true"/> and <see cref="BlobsFilter">custom blobs' filter</see> is
        /// set to <see langword="null"/>.</para>
        /// 
        /// <para>See documentation to <see cref="CoupledSizeFiltering"/> for additional information.</para>
        /// </remarks>
        /// 
        public int MaxHeight
        {
            get { return this.maxHeight; }
            set { this.maxHeight = value; }
        }

        /// <summary>
        /// Custom blobs' filter to use.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies custom blobs' filtering routine to use. It has
        /// effect only in the case if <see cref="FilterBlobs"/> property is set to <see langword="true"/>.</para>
        /// 
        /// <para><note>When custom blobs' filtering routine is set, it has priority over default filtering done
        /// with <see cref="MinWidth"/>, <see cref="MinHeight"/>, <see cref="MaxWidth"/> and <see cref="MaxHeight"/>.</note></para>
        /// </remarks>
        /// 
        public IBlobsFilter BlobsFilter
        {
            get { return this.filter; }
            set { this.filter = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobCounterBase"/> class.
        /// </summary>
        /// 
        /// <remarks>Creates new instance of the <see cref="BlobCounterBase"/> class with
        /// an empty objects map. Before using methods, which provide information about blobs
        /// or extract them, the <see cref="ProcessImage(Bitmap)"/>,
        /// <see cref="ProcessImage(BitmapData)"/> or <see cref="ProcessImage(UnmanagedImage)"/>
        /// method should be called to collect objects map.</remarks>
        /// 
        public BlobCounterBase( ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobCounterBase"/> class.
        /// </summary>
        /// 
        /// <param name="image">Binary image to look for objects in.</param>
        /// 
        /// <remarks>Creates new instance of the <see cref="BlobCounterBase"/> class with
        /// initialized objects map built by calling <see cref="ProcessImage(Bitmap)"/> method.</remarks>
        /// 
        public BlobCounterBase( Bitmap image )
        {
            this.ProcessImage( image );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobCounterBase"/> class.
        /// </summary>
        /// 
        /// <param name="imageData">Binary image data to look for objects in.</param>
        /// 
        /// <remarks>Creates new instance of the <see cref="BlobCounterBase"/> class with
        /// initialized objects map built by calling <see cref="ProcessImage(BitmapData)"/> method.</remarks>
        /// 
        public BlobCounterBase( BitmapData imageData )
        {
            this.ProcessImage( imageData );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobCounterBase"/> class.
        /// </summary>
        /// 
        /// <param name="image">Unmanaged binary image to look for objects in.</param>
        /// 
        /// <remarks>Creates new instance of the <see cref="BlobCounterBase"/> class with
        /// initialized objects map built by calling <see cref="ProcessImage(UnmanagedImage)"/> method.</remarks>
        /// 
        public BlobCounterBase( UnmanagedImage image )
        {
            this.ProcessImage( image );
        }

        /// <summary>
        /// Build objects map.
        /// </summary>
        /// 
        /// <param name="image">Source binary image.</param>
        /// 
        /// <remarks><para>Processes the image and builds objects map, which is used later to extracts blobs.</para></remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the source image.</exception>
        /// 
        public void ProcessImage( Bitmap image )
        {
            // lock source bitmap data
            var imageData = image.LockBits(
                new Rectangle( 0, 0, image.Width, image.Height ),
                ImageLockMode.ReadOnly, image.PixelFormat );

            try
            {
                // process image
                this.ProcessImage( imageData );
            }
            finally
            {
                // unlock source image
                image.UnlockBits( imageData );
            }
        }

        /// <summary>
        /// Build objects map.
        /// </summary>
        /// 
        /// <param name="imageData">Source binary image data.</param>
        /// 
        /// <remarks><para>Processes the image and builds objects map, which is used later to extracts blobs.</para></remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the source image.</exception>
        /// 
        public void ProcessImage( BitmapData imageData )
        {
            // do actual objects map building
            this.ProcessImage( new UnmanagedImage( imageData ) );
        }

        /// <summary>
        /// Build object map from raw image data.
        /// </summary>
        /// 
        /// <param name="image">Source unmanaged binary image data.</param>
        /// 
        /// <remarks><para>Processes the image and builds objects map, which is used later to extracts blobs.</para></remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the source image.</exception>
        /// <exception cref="InvalidImagePropertiesException">Thrown by some inherited classes if some image property other
        /// than the pixel format is not supported. See that class's documentation or the exception message for details.</exception>
        /// 
        public void ProcessImage( UnmanagedImage image )
        {
            this.imageWidth  = image.Width;
            this.imageHeight = image.Height;

            // do actual objects map building
            this.BuildObjectsMap( image );

            // collect information about blobs
            this.CollectObjectsInfo( image );

            // filter blobs by size if required
            if ( this.filterBlobs )
            {
                // labels remapping array
                var labelsMap = new int[this.objectsCount + 1];
                for ( var i = 1; i <= this.objectsCount; i++ )
                {
                    labelsMap[i] = i;
                }

                // check dimension of all objects and filter them
                var objectsToRemove = 0;

                if ( this.filter == null )
                {
                    for ( var i = this.objectsCount - 1; i >= 0; i-- )
                    {
                        var blobWidth  = this.blobs[i].Rectangle.Width;
                        var blobHeight = this.blobs[i].Rectangle.Height;

                        if ( this.coupledSizeFiltering == false )
                        {
                            // uncoupled filtering
                            if (
                                ( blobWidth < this.minWidth ) || ( blobHeight < this.minHeight ) ||
                                ( blobWidth > this.maxWidth ) || ( blobHeight > this.maxHeight ) )
                            {
                                labelsMap[i + 1] = 0;
                                objectsToRemove++;
                                this.blobs.RemoveAt( i );
                            }
                        }
                        else
                        {
                            // coupled filtering
                            if (
                                ( ( blobWidth < this.minWidth ) && ( blobHeight < this.minHeight ) ) ||
                                ( ( blobWidth > this.maxWidth ) && ( blobHeight > this.maxHeight ) ) )
                            {
                                labelsMap[i + 1] = 0;
                                objectsToRemove++;
                                this.blobs.RemoveAt( i );
                            }
                        }
                    }
                }
                else
                {
                    for ( var i = this.objectsCount - 1; i >= 0; i-- )
                    {
                        if ( !this.filter.Check(this.blobs[i] ) )
                        {
                            labelsMap[i + 1] = 0;
                            objectsToRemove++;
                            this.blobs.RemoveAt( i );
                        }
                    }
                }

                // update labels remapping array
                var label = 0;
                for ( var i = 1; i <= this.objectsCount; i++ )
                {
                    if ( labelsMap[i] != 0 )
                    {
                        label++;
                        // update remapping array
                        labelsMap[i] = label;
                    }
                }

                // repair object labels
                for ( int i = 0, n = this.objectLabels.Length; i < n; i++ )
                {
                    this.objectLabels[i] = labelsMap[this.objectLabels[i]];
                }

                this.objectsCount -= objectsToRemove;

                // repair IDs
                for ( int i = 0, n = this.blobs.Count; i < n; i++ )
                {
                    this.blobs[i].ID = i + 1;
                }
            }

            // do we need to sort the list?
            if ( this.objectsOrder != ObjectsOrder.None )
            {
                this.blobs.Sort( new BlobsSorter(this.objectsOrder ) );
            }
        }

        /// <summary>
        /// Get objects' rectangles.
        /// </summary>
        /// 
        /// <returns>Returns array of objects' rectangles.</returns>
        /// 
        /// <remarks>The method returns array of objects rectangles. Before calling the
        /// method, the <see cref="ProcessImage(Bitmap)"/>, <see cref="ProcessImage(BitmapData)"/>
        /// or <see cref="ProcessImage(UnmanagedImage)"/> method should be called, which will
        /// build objects map.</remarks>
        /// 
        /// <exception cref="ApplicationException">No image was processed before, so objects' rectangles
        /// can not be collected.</exception>
        /// 
        public Rectangle[] GetObjectsRectangles( )
        {
            // check if objects map was collected
            if ( this.objectLabels == null )
                throw new ApplicationException( "Image should be processed before to collect objects map." );

            var rects = new Rectangle[this.objectsCount];

            for ( var i = 0; i < this.objectsCount; i++ )
            {
                rects[i] = this.blobs[i].Rectangle;
            }

            return rects;
        }

        /// <summary>
        /// Get objects' information.
        /// </summary>
        /// 
        /// <returns>Returns array of partially initialized blobs (without <see cref="Blob.Image"/> property initialized).</returns>
        /// 
        /// <remarks><para>By the amount of provided information, the method is between <see cref="GetObjectsRectangles"/> and
        /// <see cref="GetObjects( UnmanagedImage, bool )"/> methods. The method provides array of blobs without initialized their image.
        /// Blob's image may be extracted later using <see cref="ExtractBlobsImage( Bitmap, Blob, bool )"/>
        /// or <see cref="ExtractBlobsImage( UnmanagedImage, Blob, bool )"/> method.
        /// </para></remarks>
        /// 
        /// <example>
        /// <code>
        /// // create blob counter and process image
        /// BlobCounter bc = new BlobCounter( sourceImage );
        /// // specify sort order
        /// bc.ObjectsOrder = ObjectsOrder.Size;
        /// // get objects' information (blobs without image)
        /// Blob[] blobs = bc.GetObjectInformation( );
        /// // process blobs
        /// foreach ( Blob blob in blobs )
        /// {
        ///     // check blob's properties
        ///     if ( blob.Rectangle.Width > 50 )
        ///     {
        ///         // the blob looks interesting, let's extract it
        ///         bc.ExtractBlobsImage( sourceImage, blob );
        ///     }
        /// }
        /// </code>
        /// </example>
        /// 
        /// <exception cref="ApplicationException">No image was processed before, so objects' information
        /// can not be collected.</exception>
        /// 
        public Blob[] GetObjectsInformation( )
        {
            // check if objects map was collected
            if ( this.objectLabels == null )
                throw new ApplicationException( "Image should be processed before to collect objects map." );

            var blobsToReturn = new Blob[this.objectsCount];

            // create each blob
            for ( var k = 0; k < this.objectsCount; k++ )
            {
                blobsToReturn[k] = new Blob(this.blobs[k] );
            }

            return blobsToReturn;
        }

        /// <summary>
        /// Get blobs.
        /// </summary>
        /// 
        /// <param name="image">Source image to extract objects from.</param>
        /// 
        /// <returns>Returns array of blobs.</returns>
        /// <param name="extractInOriginalSize">Specifies size of blobs' image to extract.
        /// If set to <see langword="true"/> each blobs' image will have the same size as
        /// the specified image. If set to <see langword="false"/> each blobs' image will
        /// have the size of its blob.</param>
        ///
        /// <remarks><para>The method returns array of blobs. Before calling the
        /// method, the <see cref="ProcessImage(Bitmap)"/>, <see cref="ProcessImage(BitmapData)"/>
        /// or <see cref="ProcessImage(UnmanagedImage)"/> method should be called, which will build
        /// objects map.</para>
        /// 
        /// <para>The method supports 24/32 bpp color and 8 bpp indexed grayscale images.</para>
        /// </remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the provided image.</exception>
        /// <exception cref="ApplicationException">No image was processed before, so objects
        /// can not be collected.</exception>
        /// 
        public Blob[] GetObjects( Bitmap image, bool extractInOriginalSize )
        {
            Blob[] blobs = null;
            // lock source bitmap data
            var imageData = image.LockBits(
                new Rectangle( 0, 0, image.Width, image.Height ),
                ImageLockMode.ReadOnly, image.PixelFormat );

            try
            {
                // process image
                blobs = this.GetObjects( new UnmanagedImage( imageData ), extractInOriginalSize );
            }
            finally
            {
                // unlock source images
                image.UnlockBits( imageData );
            }
            return blobs;
        }

        /// <summary>
        /// Get blobs.
        /// </summary>
        /// 
        /// <param name="image">Source unmanaged image to extract objects from.</param>
        /// <param name="extractInOriginalSize">Specifies size of blobs' image to extract.
        /// If set to <see langword="true"/> each blobs' image will have the same size as
        /// the specified image. If set to <see langword="false"/> each blobs' image will
        /// have the size of its blob.</param>
        /// 
        /// <returns>Returns array of blobs.</returns>
        /// 
        /// <remarks><para>The method returns array of blobs. Before calling the
        /// method, the <see cref="ProcessImage(Bitmap)"/>, <see cref="ProcessImage(BitmapData)"/>
        /// or <see cref="ProcessImage(UnmanagedImage)"/> method should be called, which will build
        /// objects map.</para>
        /// 
        /// <para>The method supports 24/32 bpp color and 8 bpp indexed grayscale images.</para>
        /// </remarks>
        ///
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the provided image.</exception>
        /// <exception cref="ApplicationException">No image was processed before, so objects
        /// can not be collected.</exception>
        /// 
        public Blob[] GetObjects( UnmanagedImage image, bool extractInOriginalSize )
        {
            // check if objects map was collected
            if ( this.objectLabels == null )
                throw new ApplicationException( "Image should be processed before to collect objects map." );

            if (
                ( image.PixelFormat != PixelFormat.Format24bppRgb ) &&
                ( image.PixelFormat != PixelFormat.Format8bppIndexed ) &&
                ( image.PixelFormat != PixelFormat.Format32bppRgb ) &&
                ( image.PixelFormat != PixelFormat.Format32bppArgb ) &&
                ( image.PixelFormat != PixelFormat.Format32bppPArgb )
                )
                throw new UnsupportedImageFormatException( "Unsupported pixel format of the provided image." );

            // image size
            var width  = image.Width;
            var height = image.Height;
            var srcStride = image.Stride;
            var pixelSize = Bitmap.GetPixelFormatSize( image.PixelFormat ) / 8;

            var objects = new Blob[this.objectsCount];

            // create each image
            for ( var k = 0; k < this.objectsCount; k++ )
            {
                var objectWidth  = this.blobs[k].Rectangle.Width;
                var objectHeight = this.blobs[k].Rectangle.Height;

                var blobImageWidth  = ( extractInOriginalSize ) ? width : objectWidth;
                var blobImageHeight = ( extractInOriginalSize ) ? height : objectHeight;

                var xmin = this.blobs[k].Rectangle.X;
                var xmax = xmin + objectWidth - 1;
                var ymin = this.blobs[k].Rectangle.Y;
                var ymax = ymin + objectHeight - 1;

                var label = this.blobs[k].ID;

                // create new image
                var dstImage = UnmanagedImage.Create( blobImageWidth, blobImageHeight, image.PixelFormat );

                // copy image
                unsafe
                {
                    var src = (byte*) image.ImageData.ToPointer( ) + ymin * srcStride + xmin * pixelSize;
                    var dst = (byte*) dstImage.ImageData.ToPointer( );
                    var p = ymin * width + xmin;

                    if ( extractInOriginalSize )
                    {
                        // allign destination pointer also
                        dst += ymin * dstImage.Stride + xmin * pixelSize;
                    }

                    var srcOffset = srcStride - objectWidth * pixelSize;
                    var dstOffset = dstImage.Stride - objectWidth * pixelSize;
                    var labelsOffset = width - objectWidth;

                    // for each line
                    for ( var y = ymin; y <= ymax; y++ )
                    {
                        // copy each pixel
                        for ( var x = xmin; x <= xmax; x++, p++, dst += pixelSize, src += pixelSize )
                        {
                            if ( this.objectLabels[p] == label )
                            {
                                // copy pixel
                                *dst = *src;

                                if ( pixelSize > 1 )
                                {
                                    dst[1] = src[1];
                                    dst[2] = src[2];

                                    if ( pixelSize > 3 )
                                    {
                                        dst[3] = src[3];
                                    }
                                }
                            }
                        }
                        src += srcOffset;
                        dst += dstOffset;
                        p += labelsOffset;
                    }
                }

                objects[k] = new Blob(this.blobs[k] );
                objects[k].Image = dstImage;
                objects[k].OriginalSize = extractInOriginalSize;
            }

            return objects;
        }

        /// <summary>
        /// Extract blob's image.
        /// </summary>
        /// 
        /// <param name="image">Source image to extract blob's image from.</param>
        /// <param name="blob">Blob which is required to be extracted.</param>
        /// <param name="extractInOriginalSize">Specifies size of blobs' image to extract.
        /// If set to <see langword="true"/> each blobs' image will have the same size as
        /// the specified image. If set to <see langword="false"/> each blobs' image will
        /// have the size of its blob.</param>
        ///
        /// <remarks><para>The method is used to extract image of partially initialized blob, which
        /// was provided by <see cref="GetObjectsInformation"/> method. Before calling the
        /// method, the <see cref="ProcessImage(Bitmap)"/>, <see cref="ProcessImage(BitmapData)"/>
        /// or <see cref="ProcessImage(UnmanagedImage)"/> method should be called, which will build
        /// objects map.</para>
        /// 
        /// <para>The method supports 24/32 bpp color and 8 bpp indexed grayscale images.</para>
        /// </remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the provided image.</exception>
        /// <exception cref="ApplicationException">No image was processed before, so blob
        /// can not be extracted.</exception>
        /// 
        public void ExtractBlobsImage( Bitmap image, Blob blob, bool extractInOriginalSize )
        {
            // lock source bitmap data
            var imageData = image.LockBits(
                new Rectangle( 0, 0, image.Width, image.Height ),
                ImageLockMode.ReadOnly, image.PixelFormat );

            try
            {
                // process image
                this.ExtractBlobsImage( new UnmanagedImage( imageData ), blob, extractInOriginalSize );
            }
            finally
            {
                // unlock source images
                image.UnlockBits( imageData );
            }
        }

        /// <summary>
        /// Extract blob's image.
        /// </summary>
        /// 
        /// <param name="image">Source unmanaged image to extract blob's image from.</param>
        /// <param name="blob">Blob which is required to be extracted.</param>
        /// <param name="extractInOriginalSize">Specifies size of blobs' image to extract.
        /// If set to <see langword="true"/> each blobs' image will have the same size as
        /// the specified image. If set to <see langword="false"/> each blobs' image will
        /// have the size of its blob.</param>
        ///
        /// <remarks><para>The method is used to extract image of partially initialized blob, which
        /// was provided by <see cref="GetObjectsInformation"/> method. Before calling the
        /// method, the <see cref="ProcessImage(Bitmap)"/>, <see cref="ProcessImage(BitmapData)"/>
        /// or <see cref="ProcessImage(UnmanagedImage)"/> method should be called, which will build
        /// objects map.</para>
        /// 
        /// <para>The method supports 24/32 bpp color and 8 bpp indexed grayscale images.</para>
        /// </remarks>
        /// 
        /// <exception cref="UnsupportedImageFormatException">Unsupported pixel format of the provided image.</exception>
        /// <exception cref="ApplicationException">No image was processed before, so blob
        /// can not be extracted.</exception>
        /// 
        public void ExtractBlobsImage( UnmanagedImage image, Blob blob, bool extractInOriginalSize )
        {
            // check if objects map was collected
            if ( this.objectLabels == null )
                throw new ApplicationException( "Image should be processed before to collect objects map." );

            if (
                ( image.PixelFormat != PixelFormat.Format24bppRgb ) &&
                ( image.PixelFormat != PixelFormat.Format8bppIndexed ) &&
                ( image.PixelFormat != PixelFormat.Format32bppRgb ) &&
                ( image.PixelFormat != PixelFormat.Format32bppArgb ) &&
                ( image.PixelFormat != PixelFormat.Format32bppRgb ) &&
                ( image.PixelFormat != PixelFormat.Format32bppPArgb )
                )
                throw new UnsupportedImageFormatException( "Unsupported pixel format of the provided image." );

            // image size
            var width  = image.Width;
            var height = image.Height;
            var srcStride = image.Stride;
            var pixelSize = Bitmap.GetPixelFormatSize( image.PixelFormat ) / 8;

            var objectWidth  = blob.Rectangle.Width;
            var objectHeight = blob.Rectangle.Height;

            var blobImageWidth  = ( extractInOriginalSize ) ? width : objectWidth;
            var blobImageHeight = ( extractInOriginalSize ) ? height : objectHeight;

            var xmin = blob.Rectangle.Left;
            var xmax = xmin + objectWidth - 1;
            var ymin = blob.Rectangle.Top;
            var ymax = ymin + objectHeight - 1;

            var label = blob.ID;

            // create new image
            blob.Image = UnmanagedImage.Create( blobImageWidth, blobImageHeight, image.PixelFormat );
            blob.OriginalSize = extractInOriginalSize;

            // copy image
            unsafe
            {
                var src = (byte*) image.ImageData.ToPointer( ) + ymin * srcStride + xmin * pixelSize;
                var dst = (byte*) blob.Image.ImageData.ToPointer( );
                var p = ymin * width + xmin;

                if ( extractInOriginalSize )
                {
                    // allign destination pointer also
                    dst += ymin * blob.Image.Stride + xmin * pixelSize;
                }

                var srcOffset = srcStride - objectWidth * pixelSize;
                var dstOffset = blob.Image.Stride - objectWidth * pixelSize;
                var labelsOffset = width - objectWidth;

                // for each line
                for ( var y = ymin; y <= ymax; y++ )
                {
                    // copy each pixel
                    for ( var x = xmin; x <= xmax; x++, p++, dst += pixelSize, src += pixelSize )
                    {
                        if ( this.objectLabels[p] == label )
                        {
                            // copy pixel
                            *dst = *src;

                            if ( pixelSize > 1 )
                            {
                                dst[1] = src[1];
                                dst[2] = src[2];

                                if ( pixelSize > 3 )
                                {
                                    dst[3] = src[3];
                                }
                            }
                        }
                    }
                    src += srcOffset;
                    dst += dstOffset;
                    p += labelsOffset;
                }
            }
        }

        /// <summary>
        /// Get list of points on the left and right edges of the blob.
        /// </summary>
        /// 
        /// <param name="blob">Blob to collect edge points for.</param>
        /// <param name="leftEdge">List of points on the left edge of the blob.</param>
        /// <param name="rightEdge">List of points on the right edge of the blob.</param>
        /// 
        /// <remarks><para>The method scans each line of the blob and finds the most left and the
        /// most right points for it adding them to appropriate lists. The method may be very
        /// useful in conjunction with different routines from <see cref="AForge.Math.Geometry"/>,
        /// which allow finding convex hull or quadrilateral's corners.</para>
        /// 
        /// <para><note>Both lists of points are sorted by Y coordinate - points with smaller Y
        /// value go first.</note></para>
        /// </remarks>
        /// 
        /// <exception cref="ApplicationException">No image was processed before, so blob
        /// can not be extracted.</exception>
        /// 
        public void GetBlobsLeftAndRightEdges( Blob blob, out List<IntPoint> leftEdge, out List<IntPoint> rightEdge )
        {
            // check if objects map was collected
            if ( this.objectLabels == null )
                throw new ApplicationException( "Image should be processed before to collect objects map." );

            leftEdge  = new List<IntPoint>( );
            rightEdge = new List<IntPoint>( );

            var xmin = blob.Rectangle.Left;
            var xmax = xmin + blob.Rectangle.Width - 1;
            var ymin = blob.Rectangle.Top;
            var ymax = ymin + blob.Rectangle.Height - 1;

            var label = blob.ID;
            
            // for each line
            for ( var y = ymin; y <= ymax; y++ )
            {
                // scan from left to right
                var p = y * this.imageWidth + xmin;
                for ( var x = xmin; x <= xmax; x++, p++ )
                {
                    if ( this.objectLabels[p] == label )
                    {
                        leftEdge.Add( new IntPoint( x, y ) );
                        break;
                    }
                }

                // scan from right to left
                p = y * this.imageWidth + xmax;
                for ( var x = xmax; x >= xmin; x--, p-- )
                {
                    if ( this.objectLabels[p] == label )
                    {
                        rightEdge.Add( new IntPoint( x, y ) );
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Get list of points on the top and bottom edges of the blob.
        /// </summary>
        /// 
        /// <param name="blob">Blob to collect edge points for.</param>
        /// <param name="topEdge">List of points on the top edge of the blob.</param>
        /// <param name="bottomEdge">List of points on the bottom edge of the blob.</param>
        /// 
        /// <remarks><para>The method scans each column of the blob and finds the most top and the
        /// most bottom points for it adding them to appropriate lists. The method may be very
        /// useful in conjunction with different routines from <see cref="AForge.Math.Geometry"/>,
        /// which allow finding convex hull or quadrilateral's corners.</para>
        /// 
        /// <para><note>Both lists of points are sorted by X coordinate - points with smaller X
        /// value go first.</note></para>
        /// </remarks>
        /// 
        /// <exception cref="ApplicationException">No image was processed before, so blob
        /// can not be extracted.</exception>
        /// 
        public void GetBlobsTopAndBottomEdges( Blob blob, out List<IntPoint> topEdge, out List<IntPoint> bottomEdge )
        {
            // check if objects map was collected
            if ( this.objectLabels == null )
                throw new ApplicationException( "Image should be processed before to collect objects map." );

            topEdge    = new List<IntPoint>( );
            bottomEdge = new List<IntPoint>( );

            var xmin = blob.Rectangle.Left;
            var xmax = xmin + blob.Rectangle.Width - 1;
            var ymin = blob.Rectangle.Top;
            var ymax = ymin + blob.Rectangle.Height - 1;

            var label = blob.ID;

            // for each column
            for ( var x = xmin; x <= xmax; x++ )
            {
                // scan from top to bottom
                var p = ymin * this.imageWidth + x;
                for ( var y = ymin; y <= ymax; y++, p += this.imageWidth )
                {
                    if ( this.objectLabels[p] == label )
                    {
                        topEdge.Add( new IntPoint( x, y ) );
                        break;
                    }
                }

                // scan from bottom to top
                p = ymax * this.imageWidth + x;
                for ( var y = ymax; y >= ymin; y--, p -= this.imageWidth )
                {
                    if ( this.objectLabels[p] == label )
                    {
                        bottomEdge.Add( new IntPoint( x, y ) );
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Get list of object's edge points.
        /// </summary>
        /// 
        /// <param name="blob">Blob to collect edge points for.</param>
        /// 
        /// <returns>Returns unsorted list of blob's edge points.</returns>
        /// 
        /// <remarks><para>The method scans each row and column of the blob and finds the
        /// most top/bottom/left/right points. The method returns similar result as if results of
        /// both <see cref="GetBlobsLeftAndRightEdges"/> and <see cref="GetBlobsTopAndBottomEdges"/>
        /// methods were combined, but each edge point occurs only once in the list.</para>
        /// 
        /// <para><note>Edge points in the returned list are not ordered. This makes the list unusable
        /// for visualization with methods, which draw polygon or poly-line. But the returned list
        /// can be used with such algorithms, like convex hull search, shape analyzer, etc.</note></para>
        /// </remarks>
        /// 
        /// <exception cref="ApplicationException">No image was processed before, so blob
        /// can not be extracted.</exception>
        /// 
        public List<IntPoint> GetBlobsEdgePoints( Blob blob )
        {
            // check if objects map was collected
            if ( this.objectLabels == null )
                throw new ApplicationException( "Image should be processed before to collect objects map." );

            var edgePoints = new List<IntPoint>( );

            var xmin = blob.Rectangle.Left;
            var xmax = xmin + blob.Rectangle.Width - 1;
            var ymin = blob.Rectangle.Top;
            var ymax = ymin + blob.Rectangle.Height - 1;

            var label = blob.ID;

            // array of already processed points on left/right edges
            // (index in these arrays represent Y coordinate, but value - X coordinate)
            var leftProcessedPoints  = new int[blob.Rectangle.Height];
            var rightProcessedPoints = new int[blob.Rectangle.Height];

            // for each line
            for ( var y = ymin; y <= ymax; y++ )
            {
                // scan from left to right
                var p = y * this.imageWidth + xmin;
                for ( var x = xmin; x <= xmax; x++, p++ )
                {
                    if ( this.objectLabels[p] == label )
                    {
                        edgePoints.Add( new IntPoint( x, y ) );
                        leftProcessedPoints[y - ymin] = x;
                        break;
                    }
                }

                // scan from right to left
                p = y * this.imageWidth + xmax;
                for ( var x = xmax; x >= xmin; x--, p-- )
                {
                    if ( this.objectLabels[p] == label )
                    {
                        // avoid adding the point we already have
                        if ( leftProcessedPoints[y - ymin] != x )
                        {
                            edgePoints.Add( new IntPoint( x, y ) );
                        }
                        rightProcessedPoints[y - ymin] = x;
                        break;
                    }
                }
            }

            // for each column
            for ( var x = xmin; x <= xmax; x++ )
            {
                // scan from top to bottom
                var p = ymin * this.imageWidth + x;
                for ( int y = ymin, y0 = 0; y <= ymax; y++, y0++, p += this.imageWidth )
                {
                    if ( this.objectLabels[p] == label )
                    {
                        // avoid adding the point we already have
                        if ( ( leftProcessedPoints[y0] != x ) &&
                             ( rightProcessedPoints[y0] != x ) )
                        {
                            edgePoints.Add( new IntPoint( x, y ) );
                        }
                        break;
                    }
                }

                // scan from bottom to top
                p = ymax * this.imageWidth + x;
                for ( int y = ymax, y0 = ymax - ymin; y >= ymin; y--, y0--, p -= this.imageWidth )
                {
                    if ( this.objectLabels[p] == label )
                    {
                        // avoid adding the point we already have
                        if ( ( leftProcessedPoints[y0] != x ) &&
                             ( rightProcessedPoints[y0] != x ) )
                        {
                            edgePoints.Add( new IntPoint( x, y ) );
                        }
                        break;
                    }
                }
            }

            return edgePoints;
        }

        /// <summary>
        /// Actual objects map building.
        /// </summary>
        /// 
        /// <param name="image">Unmanaged image to process.</param>
        /// 
        /// <remarks><note>By the time this method is called bitmap's pixel format is not
        /// yet checked, so this should be done by the class inheriting from the base class.
        /// <see cref="imageWidth"/> and <see cref="imageHeight"/> members are initialized
        /// before the method is called, so these members may be used safely.</note></remarks>
        /// 
        protected abstract void BuildObjectsMap( UnmanagedImage image );


        #region Private Methods - Collecting objects' rectangles

        // Collect objects' rectangles
        private unsafe void CollectObjectsInfo( UnmanagedImage image )
        {
            int i = 0, label;

            // create object coordinates arrays
            var x1 = new int[this.objectsCount + 1];
            var y1 = new int[this.objectsCount + 1];
            var x2 = new int[this.objectsCount + 1];
            var y2 = new int[this.objectsCount + 1];

            var area = new int[this.objectsCount + 1];
            var xc = new long[this.objectsCount + 1];
            var yc = new long[this.objectsCount + 1];

            var meanR = new long[this.objectsCount + 1];
            var meanG = new long[this.objectsCount + 1];
            var meanB = new long[this.objectsCount + 1];

            var stdDevR = new long[this.objectsCount + 1];
            var stdDevG = new long[this.objectsCount + 1];
            var stdDevB = new long[this.objectsCount + 1];

            for ( var j = 1; j <= this.objectsCount; j++ )
            {
                x1[j] = this.imageWidth;
                y1[j] = this.imageHeight;
            }

            var src = (byte*) image.ImageData.ToPointer( );

            if ( image.PixelFormat == PixelFormat.Format8bppIndexed )
            {
                var offset = image.Stride - this.imageWidth;
                byte g; // pixel's grey value

                // walk through labels array
                for ( var y = 0; y < this.imageHeight; y++ )
                {
                    for ( var x = 0; x < this.imageWidth; x++, i++, src++ )
                    {
                        // get current label
                        label = this.objectLabels[i];

                        // skip unlabeled pixels
                        if ( label == 0 )
                            continue;

                        // check and update all coordinates

                        if ( x < x1[label] )
                        {
                            x1[label] = x;
                        }
                        if ( x > x2[label] )
                        {
                            x2[label] = x;
                        }
                        if ( y < y1[label] )
                        {
                            y1[label] = y;
                        }
                        if ( y > y2[label] )
                        {
                            y2[label] = y;
                        }

                        area[label]++;
                        xc[label] += x;
                        yc[label] += y;

                        g = *src;
                        meanG[label] += g;
                        stdDevG[label] += g * g;
                    }

                    src += offset;
                }

                for ( var j = 1; j <= this.objectsCount; j++ )
                {
                    meanR[j] = meanB[j] = meanG[j];
                    stdDevR[j] = stdDevB[j] = stdDevG[j];
                }
            }
            else
            {
                // color images
                var pixelSize = Bitmap.GetPixelFormatSize( image.PixelFormat ) / 8;
                var offset = image.Stride - this.imageWidth * pixelSize;
                byte r, g, b; // RGB value

                // walk through labels array
                for ( var y = 0; y < this.imageHeight; y++ )
                {
                    for ( var x = 0; x < this.imageWidth; x++, i++, src += pixelSize )
                    {
                        // get current label
                        label = this.objectLabels[i];

                        // skip unlabeled pixels
                        if ( label == 0 )
                            continue;

                        // check and update all coordinates

                        if ( x < x1[label] )
                        {
                            x1[label] = x;
                        }
                        if ( x > x2[label] )
                        {
                            x2[label] = x;
                        }
                        if ( y < y1[label] )
                        {
                            y1[label] = y;
                        }
                        if ( y > y2[label] )
                        {
                            y2[label] = y;
                        }

                        area[label]++;
                        xc[label] += x;
                        yc[label] += y;

                        r = src[RGB.R];
                        g = src[RGB.G];
                        b = src[RGB.B];

                        meanR[label] += r;
                        meanG[label] += g;
                        meanB[label] += b;

                        stdDevR[label] += r * r;
                        stdDevG[label] += g * g;
                        stdDevB[label] += b * b;
                    }

                    src += offset;
                }
            }

            // create blobs
            this.blobs.Clear( );

            for ( var j = 1; j <= this.objectsCount; j++ )
            {
                var blobArea = area[j];

                var blob = new Blob( j, new Rectangle( x1[j], y1[j], x2[j] - x1[j] + 1, y2[j] - y1[j] + 1 ) );
                blob.Area = blobArea;
                blob.Fullness = (double) blobArea / ( ( x2[j] - x1[j] + 1 ) * ( y2[j] - y1[j] + 1 ) );
                blob.CenterOfGravity = new AForge.Point( (float) xc[j] / blobArea, (float) yc[j] / blobArea );
                blob.ColorMean = Color.FromArgb( (byte) ( meanR[j] / blobArea ), (byte) ( meanG[j] / blobArea ), (byte) ( meanB[j] / blobArea ) );
                blob.ColorStdDev = Color.FromArgb(
                    (byte) ( Math.Sqrt( stdDevR[j] / blobArea - blob.ColorMean.R * blob.ColorMean.R ) ),
                    (byte) ( Math.Sqrt( stdDevG[j] / blobArea - blob.ColorMean.G * blob.ColorMean.G ) ),
                    (byte) ( Math.Sqrt( stdDevB[j] / blobArea - blob.ColorMean.B * blob.ColorMean.B ) ) );

                this.blobs.Add( blob );
            }
        }

        // Rectangles' and blobs' sorter
        private class BlobsSorter : System.Collections.Generic.IComparer<Blob>
        {
            private ObjectsOrder order;

            public BlobsSorter( ObjectsOrder order )
            {
                this.order = order;
            }

            public int Compare( Blob a, Blob b )
            {
                var aRect = a.Rectangle;
                var bRect = b.Rectangle;

                switch ( this.order )
                {
                    case ObjectsOrder.Size: // sort by size

                        // the order is changed to descending
                        return ( bRect.Width * bRect.Height - aRect.Width * aRect.Height );

                    case ObjectsOrder.Area: // sort by area
                        return b.Area - a.Area;

                    case ObjectsOrder.YX:   // YX order

                        return ( ( aRect.Y * 100000 + aRect.X ) - ( bRect.Y * 100000 + bRect.X ) );

                    case ObjectsOrder.XY:   // XY order

                        return ( ( aRect.X * 100000 + aRect.Y ) - ( bRect.X * 100000 + bRect.Y ) );
                }
                return 0;
            }
        }

        #endregion
    }
}

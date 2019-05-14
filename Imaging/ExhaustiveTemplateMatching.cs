// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © Andrew Kirillov, 2005-2009
// andrew.kirillov@aforgenet.com
//

namespace AForge.Imaging
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;

    /// <summary>
    /// Exhaustive template matching.
    /// </summary>
    /// 
    /// <remarks><para>The class implements exhaustive template matching algorithm,
    /// which performs complete scan of source image, comparing each pixel with corresponding
    /// pixel of template.</para>
    /// 
    /// <para>The class processes only grayscale 8 bpp and color 24 bpp images.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create template matching algorithm's instance
    /// ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching( 0.9f );
    /// // find all matchings with specified above similarity
    /// TemplateMatch[] matchings = tm.ProcessImage( sourceImage, templateImage );
    /// // highlight found matchings
    /// BitmapData data = sourceImage.LockBits(
    ///     new Rectangle( 0, 0, sourceImage.Width, sourceImage.Height ),
    ///     ImageLockMode.ReadWrite, sourceImage.PixelFormat );
    /// foreach ( TemplateMatch m in matchings )
    /// {
    ///     Drawing.Rectangle( data, m.Rectangle, Color.White );
    ///     // do something else with matching
    /// }
    /// sourceImage.UnlockBits( data );
    /// </code>
    /// 
    /// <para>The class also can be used to get similarity level between two image of the same
    /// size, which can be useful to get information about how different/similar are images:</para>
    /// <code>
    /// // create template matching algorithm's instance
    /// // use zero similarity to make sure algorithm will provide anything
    /// ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching( 0 );
    /// // compare two images
    /// TemplateMatch[] matchings = tm.ProcessImage( image1, image2 );
    /// // check similarity level
    /// if ( matchings[0].Similarity > 0.95f )
    /// {
    ///     // do something with quite similar images
    /// }
    /// </code>
    /// 
    /// </remarks>
    /// 
    public class ExhaustiveTemplateMatching : ITemplateMatching
    {
        private float similarityThreshold = 0.9f;

        /// <summary>
        /// Similarity threshold, [0..1].
        /// </summary>
        /// 
        /// <remarks><para>The property sets the minimal acceptable similarity between template
        /// and potential found candidate. If similarity is lower than this value,
        /// then object is not treated as matching with template.
        /// </para>
        /// 
        /// <para>Default value is set to <b>0.9</b>.</para>
        /// </remarks>
        /// 
        public float SimilarityThreshold
        {
            get { return this.similarityThreshold; }
            set { this.similarityThreshold = Math.Min( 1, Math.Max( 0, value ) ); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExhaustiveTemplateMatching"/> class.
        /// </summary>
        /// 
        public ExhaustiveTemplateMatching( ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExhaustiveTemplateMatching"/> class.
        /// </summary>
        /// 
        /// <param name="similarityThreshold">Similarity threshold.</param>
        /// 
        public ExhaustiveTemplateMatching( float similarityThreshold )
        {
            this.similarityThreshold = similarityThreshold;
        }

        /// <summary>
        /// Process image looking for matchings with specified template.
        /// </summary>
        /// 
        /// <param name="image">Source image to process.</param>
        /// <param name="template">Template image to search for.</param>
        /// 
        /// <returns>Returns array of found template matches. The array is sorted by similarity
        /// of found matches in descending order.</returns>
        /// 
        /// <exception cref="UnsupportedImageFormatException">The source image has incorrect pixel format.</exception>
        /// <exception cref="InvalidImagePropertiesException">Template image is bigger than source image.</exception>
        /// 
        public TemplateMatch[] ProcessImage( Bitmap image, Bitmap template )
        {
            return this.ProcessImage( image, template, new Rectangle( 0, 0, image.Width, image.Height ) );
        }

        /// <summary>
        /// Process image looking for matchings with specified template.
        /// </summary>
        /// 
        /// <param name="image">Source image to process.</param>
        /// <param name="template">Template image to search for.</param>
        /// <param name="searchZone">Rectangle in source image to search template for.</param>
        /// 
        /// <returns>Returns array of found template matches. The array is sorted by similarity
        /// of found matches in descending order.</returns>
        /// 
        /// <exception cref="UnsupportedImageFormatException">The source image has incorrect pixel format.</exception>
        /// <exception cref="InvalidImagePropertiesException">Template image is bigger than source image.</exception>
        /// 
        public TemplateMatch[] ProcessImage( Bitmap image, Bitmap template, Rectangle searchZone )
        {
            // check image format
            if (
                ( ( image.PixelFormat != PixelFormat.Format8bppIndexed ) &&
                  ( image.PixelFormat != PixelFormat.Format24bppRgb ) ) ||
                ( image.PixelFormat != template.PixelFormat ) )
            {
                throw new UnsupportedImageFormatException( "Unsupported pixel format of the source or template image." );
            }

            // check template's size
            if ( ( template.Width > image.Width ) || ( template.Height > image.Height ) )
            {
                throw new InvalidImagePropertiesException( "Template's size should be smaller or equal to source image's size." );
            }

            // lock source and template images
            var imageData = image.LockBits(
                new Rectangle( 0, 0, image.Width, image.Height ),
                ImageLockMode.ReadOnly, image.PixelFormat );

            var templateData = template.LockBits(
                new Rectangle( 0, 0, template.Width, template.Height ),
                ImageLockMode.ReadOnly, template.PixelFormat );

            TemplateMatch[] matchings;

            try
            {
                // process the image
                matchings = this.ProcessImage(
                    new UnmanagedImage( imageData ),
                    new UnmanagedImage( templateData ),
                    searchZone );
            }
            finally
            {
                // unlock images
                image.UnlockBits( imageData );
                template.UnlockBits( templateData );
            }

            return matchings;
        }

        /// <summary>
        /// Process image looking for matchings with specified template.
        /// </summary>
        /// 
        /// <param name="imageData">Source image data to process.</param>
        /// <param name="templateData">Template image to search for.</param>
        /// 
        /// <returns>Returns array of found template matches. The array is sorted by similarity
        /// of found matches in descending order.</returns>
        /// 
        /// <exception cref="UnsupportedImageFormatException">The source image has incorrect pixel format.</exception>
        /// <exception cref="InvalidImagePropertiesException">Template image is bigger than source image.</exception>
        /// 
        public TemplateMatch[] ProcessImage( BitmapData imageData, BitmapData templateData )
        {
            return this.ProcessImage( new UnmanagedImage( imageData ), new UnmanagedImage( templateData ),
                new Rectangle( 0, 0, imageData.Width, imageData.Height ) );
        }

        /// <summary>
        /// Process image looking for matchings with specified template.
        /// </summary>
        /// 
        /// <param name="imageData">Source image data to process.</param>
        /// <param name="templateData">Template image to search for.</param>
        /// <param name="searchZone">Rectangle in source image to search template for.</param>
        /// 
        /// <returns>Returns array of found template matches. The array is sorted by similarity
        /// of found matches in descending order.</returns>
        /// 
        /// <exception cref="UnsupportedImageFormatException">The source image has incorrect pixel format.</exception>
        /// <exception cref="InvalidImagePropertiesException">Template image is bigger than source image.</exception>
        /// 
        public TemplateMatch[] ProcessImage( BitmapData imageData, BitmapData templateData, Rectangle searchZone )
        {
            return this.ProcessImage( new UnmanagedImage( imageData ), new UnmanagedImage( templateData ), searchZone );
        }

        /// <summary>
        /// Process image looking for matchings with specified template.
        /// </summary>
        /// 
        /// <param name="image">Unmanaged source image to process.</param>
        /// <param name="template">Unmanaged template image to search for.</param>
        /// 
        /// <returns>Returns array of found template matches. The array is sorted by similarity
        /// of found matches in descending order.</returns>
        /// 
        /// <exception cref="UnsupportedImageFormatException">The source image has incorrect pixel format.</exception>
        /// <exception cref="InvalidImagePropertiesException">Template image is bigger than source image.</exception>
        ///
        public TemplateMatch[] ProcessImage( UnmanagedImage image, UnmanagedImage template )
        {
            return this.ProcessImage( image, template, new Rectangle( 0, 0, image.Width, image.Height ) );
        }

        public TemplateMatch[] ProcessImage(UnmanagedImage image , UnmanagedImage template , Rectangle searchZone)
        {
            return this.ProcessImage(image , template , searchZone , 1);
        }

        /// <summary>
        /// Process image looking for matchings with specified template.
        /// </summary>
        /// 
        /// <param name="image">Unmanaged source image to process.</param>
        /// <param name="template">Unmanaged template image to search for.</param>
        /// <param name="searchZone">Rectangle in source image to search template for.</param>
        /// <param name="parallelTasks">Number of used tasks</param>
        /// 
        /// <returns>Returns array of found template matches. The array is sorted by similarity
        /// of found matches in descending order.</returns>
        /// 
        /// <exception cref="UnsupportedImageFormatException">The source image has incorrect pixel format.</exception>
        /// <exception cref="InvalidImagePropertiesException">Template image is bigger than search zone.</exception>
        ///
        [HandleProcessCorruptedStateExceptions]
        public TemplateMatch[] ProcessImage( UnmanagedImage image, UnmanagedImage template, Rectangle searchZone, int parallelTasks=1 )
        {
            // check image format
            if (
                ( ( image.PixelFormat != PixelFormat.Format8bppIndexed ) &&
                  ( image.PixelFormat != PixelFormat.Format24bppRgb ) ) ||
                ( image.PixelFormat != template.PixelFormat ) )
            {
                throw new UnsupportedImageFormatException( "Unsupported pixel format of the source or template image." );
            }

            // clip search zone
            var zone = searchZone;
            zone.Intersect( new Rectangle( 0, 0, image.Width, image.Height ) );

            // search zone's starting point
            var startX = zone.X;
            var startY = zone.Y;

            // get source and template image size
            var sourceWidth    = zone.Width;
            var sourceHeight   = zone.Height;
            var templateWidth  = template.Width;
            var templateHeight = template.Height;

            // check template's size
            if ( ( templateWidth > sourceWidth ) || ( templateHeight > sourceHeight ) )
            {
                throw new InvalidImagePropertiesException( "Template's size should be smaller or equal to search zone." );
            }

            var pixelSize = ( image.PixelFormat == PixelFormat.Format8bppIndexed ) ? 1 : 3;
            var sourceStride = image.Stride;

            // similarity map. its size is increased by 4 from each side to increase
            // performance of non-maximum suppresion
            var mapWidth  = sourceWidth - templateWidth + 1;
            var mapHeight = sourceHeight - templateHeight + 1;
            var map = new int[mapHeight + 4, mapWidth + 4];

            // maximum possible difference with template
            var maxDiff = templateWidth * templateHeight * pixelSize * 255;

            // integer similarity threshold
            var threshold = (int) ( this.similarityThreshold * maxDiff );

            // width of template in bytes
            var templateWidthInBytes = templateWidth * pixelSize;

            // do the job
            unsafe
            {
                var baseSrc = (byte*) image.ImageData.ToPointer( );
                var baseTpl = (byte*) template.ImageData.ToPointer( );

                var sourceOffset = image.Stride - templateWidth * pixelSize;
                var templateOffset = template.Stride - templateWidth * pixelSize;

                // for each row of the source image
                //for ( int y = 0; y < mapHeight; y++ )
                //use multiple tasks.
                var opt=new System.Threading.Tasks.ParallelOptions
                { 
                    MaxDegreeOfParallelism=parallelTasks
                };

                for (int y = 0; y < mapHeight; y++)
                //System.Threading.Tasks.Parallel.For(0,mapHeight, opt, y=>
                {                    
                    try
                    {
                        // for each pixel of the source image
                        for ( var x = 0; x < mapWidth; x++ )
                        {
                            var src = baseSrc + sourceStride * ( y + startY ) + pixelSize * ( x + startX );
                            var tpl = baseTpl;

                            // compare template with source image starting from current X,Y
                            var dif = 0;

                            // for each row of the template
                            for ( var i = 0; i < templateHeight; i++ )
                            {
                                // for each pixel of the template
                                for ( var j = 0; j < templateWidthInBytes; j++, src++, tpl++ )
                                {
                                    var d = *src - *tpl;
                                    if ( d > 0 )
                                    {
                                        dif += d;
                                    }
                                    else
                                    {
                                        dif -= d;
                                    }
                                }
                                src += sourceOffset;
                                tpl += templateOffset;
                            }

                            // templates similarity
                            var sim = maxDiff - dif;

                            if ( sim >= threshold )
                                map[y + 2 , x + 2] = sim;
                        }
                    }
                    catch ( AccessViolationException ex ) { }
                }//);
            }

            // collect interesting points - only those points, which are local maximums
            var matchingsList = new List<TemplateMatch>( );

            // for each row
            for ( int y = 2, maxY = mapHeight + 2; y < maxY; y++ )
            {
                // for each pixel
                for ( int x = 2, maxX = mapWidth + 2; x < maxX; x++ )
                {
                    var currentValue = map[y, x];

                    // for each windows' row
                    for ( var i = -2; ( currentValue != 0 ) && ( i <= 2 ); i++ )
                    {
                        // for each windows' pixel
                        for ( var j = -2; j <= 2; j++ )
                        {
                            if ( map[y + i, x + j] > currentValue )
                            {
                                currentValue = 0;
                                break;
                            }
                        }
                    }

                    // check if this point is really interesting
                    if ( currentValue != 0 )
                    {
                        matchingsList.Add( new TemplateMatch(
                            new Rectangle( x - 2 + startX, y - 2 + startY, templateWidth, templateHeight ),
                            (float) currentValue / maxDiff ) );
                    }
                }
            }

            // convert list to array
            var matchings = new TemplateMatch[matchingsList.Count];
            matchingsList.CopyTo( matchings );
            // sort in descending order
            Array.Sort( matchings, new MatchingsSorter( ) );

            return matchings;
        }

        // Sorter of found matchings
        private class MatchingsSorter : System.Collections.IComparer
        {
            public int Compare( Object x, Object y )
            {
                var diff = ( (TemplateMatch) y ).Similarity - ( (TemplateMatch) x ).Similarity;

                return ( diff > 0 ) ? 1 : ( diff < 0 ) ? -1 : 0;
            }
        }
    }
}
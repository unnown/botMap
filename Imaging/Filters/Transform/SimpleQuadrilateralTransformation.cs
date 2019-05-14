﻿// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © Andrew Kirillov, 2007-2010
// andrew.kirillov@aforgenet.com
//

namespace AForge.Imaging.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;

    using AForge;

    /// <summary>
    /// Performs quadrilateral transformation of an area in the source image.
    /// </summary>
    /// 
    /// <remarks><para>The class implements simple algorithm described by
    /// <a href="http://www.codeguru.com/forum/showpost.php?p=1186454&amp;postcount=2">Olivier Thill</a>
    /// for transforming quadrilateral area from a source image into rectangular image.
    /// The idea of the algorithm is based on finding for each line of destination
    /// rectangular image a corresponding line connecting "left" and "right" sides of
    /// quadrilateral in a source image. Then the line is linearly transformed into the
    /// line in destination image.</para>
    /// 
    /// <para><note>Due to simplicity of the algorithm it does not do any correction for perspective.
    /// </note></para>
    /// 
    /// <para><note>To make sure the algorithm works correctly, it is preferred if the
    /// "left-top" corner of the quadrilateral (screen coordinates system) is
    /// specified first in the list of quadrilateral's corners. At least
    /// user need to make sure that the "left" side (side connecting first and the last
    /// corner) and the "right" side (side connecting second and third corners) are
    /// not horizontal.</note></para>
    /// 
    /// <para>Use <see cref="QuadrilateralTransformation"/> to avoid the above mentioned limitations,
    /// which is a more advanced quadrilateral transformation algorithms (although a bit more
    /// computationally expensive).</para>
    /// 
    /// <para>The image processing filter accepts 8 grayscale images and 24/32 bpp
    /// color images for processing.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // define quadrilateral's corners
    /// List&lt;IntPoint&gt; corners = new List&lt;IntPoint&gt;( );
    /// corners.Add( new IntPoint(  99,  99 ) );
    /// corners.Add( new IntPoint( 156,  79 ) );
    /// corners.Add( new IntPoint( 184, 126 ) );
    /// corners.Add( new IntPoint( 122, 150 ) );
    /// // create filter
    /// SimpleQuadrilateralTransformation filter =
    ///     new SimpleQuadrilateralTransformation( corners, 200, 200 );
    /// // apply the filter
    /// Bitmap newImage = filter.Apply( image );
    /// </code>
    /// 
    /// <para><b>Initial image:</b></para>
    /// <img src="img/imaging/sample18.jpg" width="320" height="240" />
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/quadrilateral_bilinear.png" width="200" height="200" />
    /// </remarks>
    /// 
    /// <seealso cref="QuadrilateralTransformation"/>
    ///
    public class SimpleQuadrilateralTransformation : BaseTransformationFilter
    {
        private bool automaticSizeCalculaton = true;
        private bool useInterpolation = true;

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

        /// <summary>
        /// New image width.
        /// </summary>
        protected int newWidth;

        /// <summary>
        /// New image height.
        /// </summary>
        protected int newHeight;

        /// <summary>
        /// Automatic calculation of destination image or not.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies how to calculate size of destination (transformed)
        /// image. If the property is set to <see langword="false"/>, then <see cref="NewWidth"/>
        /// and <see cref="NewHeight"/> properties have effect and destination image's size is
        /// specified by user. If the property is set to <see langword="true"/>, then setting the above
        /// mentioned properties does not have any effect, but destionation image's size is
        /// automatically calculated from <see cref="SourceQuadrilateral"/> property - width and height
        /// come from length of longest edges.
        /// </para>
        /// 
        /// <para>Default value is set to <see langword="true"/>.</para>
        /// </remarks>
        /// 
        public bool AutomaticSizeCalculaton
        {
            get { return this.automaticSizeCalculaton; }
            set
            {
                this.automaticSizeCalculaton = value;
                if ( value )
                {
                    this.CalculateDestinationSize( );
                }
            }
        }

        // Quadrilateral's corners in source image.
        private List<IntPoint> sourceQuadrilateral;

        /// <summary>
        /// Quadrilateral's corners in source image.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies four corners of the quadrilateral area
        /// in the source image to be transformed.</para>
        /// 
        /// <para>See documentation to the <see cref="SimpleQuadrilateralTransformation"/>
        /// class itself for additional information.</para>
        /// </remarks>
        /// 
        public List<IntPoint> SourceQuadrilateral
        {
            get { return this.sourceQuadrilateral; }
            set
            {
                this.sourceQuadrilateral = value;
                if ( this.automaticSizeCalculaton )
                {
                    this.CalculateDestinationSize( );
                }
            }
        }

        /// <summary>
        /// Width of the new transformed image.
        /// </summary>
        /// 
        /// <remarks><para>The property defines width of the destination image, which gets
        /// transformed quadrilateral image.</para>
        /// 
        /// <para><note>Setting the property does not have any effect, if <see cref="AutomaticSizeCalculaton"/>
        /// property is set to <see langword="true"/>. In this case destination image's width
        /// is calculated automatically based on <see cref="SourceQuadrilateral"/> property.</note></para>
        /// </remarks>
        /// 
        public int NewWidth
        {
            get { return this.newWidth; }
            set
            {
                if ( !this.automaticSizeCalculaton )
                {
                    this.newWidth = Math.Max( 1, value );
                }
            }
        }

        /// <summary>
        /// Height of the new transformed image.
        /// </summary>
        /// 
        /// <remarks><para>The property defines height of the destination image, which gets
        /// transformed quadrilateral image.</para>
        /// 
        /// <para><note>Setting the property does not have any effect, if <see cref="AutomaticSizeCalculaton"/>
        /// property is set to <see langword="true"/>. In this case destination image's height
        /// is calculated automatically based on <see cref="SourceQuadrilateral"/> property.</note></para>
        /// </remarks>
        /// 
        public int NewHeight
        {
            get { return this.newHeight; }
            set
            {
                if ( !this.automaticSizeCalculaton )
                {
                    this.newHeight = Math.Max( 1, value );
                }
            }
        }

        /// <summary>
        /// Specifies if bilinear interpolation should be used or not.
        /// </summary>
        /// 
        /// <remarks><para>Default value is set to <see langword="true"/> - interpolation
        /// is used.</para>
        /// </remarks>
        /// 
        public bool UseInterpolation
        {
            get { return this.useInterpolation; }
            set { this.useInterpolation = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleQuadrilateralTransformation"/> class.
        /// </summary>
        /// 
        public SimpleQuadrilateralTransformation( )
        {
            this.formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
            this.formatTranslations[PixelFormat.Format24bppRgb]    = PixelFormat.Format24bppRgb;
            this.formatTranslations[PixelFormat.Format32bppRgb]    = PixelFormat.Format32bppRgb;
            this.formatTranslations[PixelFormat.Format32bppArgb]   = PixelFormat.Format32bppArgb;
            this.formatTranslations[PixelFormat.Format32bppPArgb]  = PixelFormat.Format32bppPArgb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleQuadrilateralTransformation"/> class.
        /// </summary>
        /// 
        /// <param name="sourceQuadrilateral">Corners of the source quadrilateral area.</param>
        /// <param name="newWidth">Width of the new transformed image.</param>
        /// <param name="newHeight">Height of the new transformed image.</param>
        /// 
        /// <remarks><para>This constructor sets <see cref="AutomaticSizeCalculaton"/> to
        /// <see langword="false"/>, which means that destination image will have width and
        /// height as specified by user.</para></remarks>
        /// 
        public SimpleQuadrilateralTransformation( List<IntPoint> sourceQuadrilateral, int newWidth, int newHeight )
            : this( )
        {
            this.automaticSizeCalculaton = false;
            this.sourceQuadrilateral = sourceQuadrilateral;
            this.newWidth  = newWidth;
            this.newHeight = newHeight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleQuadrilateralTransformation"/> class.
        /// </summary>
        /// 
        /// <param name="sourceQuadrilateral">Corners of the source quadrilateral area.</param>
        /// 
        /// <remarks><para>This constructor sets <see cref="AutomaticSizeCalculaton"/> to
        /// <see langword="true"/>, which means that destination image will have width and
        /// height automatically calculated based on <see cref="SourceQuadrilateral"/> property.</para></remarks>
        ///
        public SimpleQuadrilateralTransformation( List<IntPoint> sourceQuadrilateral )
            : this( )
        {
            this.automaticSizeCalculaton = true;
            this.sourceQuadrilateral = sourceQuadrilateral;
            this.CalculateDestinationSize( );
        }

        /// <summary>
        /// Calculates new image size.
        /// </summary>
        /// 
        /// <param name="sourceData">Source image data.</param>
        /// 
        /// <returns>New image size - size of the destination image.</returns>
        /// 
        /// <exception cref="NullReferenceException">Source quadrilateral was not set.</exception>
        /// 
        protected override System.Drawing.Size CalculateNewImageSize( UnmanagedImage sourceData )
        {
            if ( this.sourceQuadrilateral == null )
                throw new NullReferenceException( "Source quadrilateral was not set." );

            return new Size(this.newWidth , this.newHeight );
        }

        // Calculates size of destination image
        private void CalculateDestinationSize( )
        {
            if ( this.sourceQuadrilateral == null )
                throw new NullReferenceException( "Source quadrilateral was not set." );

            this.newWidth  = (int) Math.Max(this.sourceQuadrilateral[0].DistanceTo(this.sourceQuadrilateral[1] ),
                                        this.sourceQuadrilateral[2].DistanceTo(this.sourceQuadrilateral[3] ) );
            this.newHeight = (int) Math.Max(this.sourceQuadrilateral[1].DistanceTo(this.sourceQuadrilateral[2] ),
                                        this.sourceQuadrilateral[3].DistanceTo(this.sourceQuadrilateral[0] ) );
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// 
        /// <param name="sourceData">Source image data.</param>
        /// <param name="destinationData">Destination image data.</param>
        /// 
        protected override unsafe void ProcessFilter( UnmanagedImage sourceData, UnmanagedImage destinationData )
        {
            // get source and destination images size
            var srcWidth  = sourceData.Width;
            var srcHeight = sourceData.Height;
            var dstWidth  = destinationData.Width;
            var dstHeight = destinationData.Height;

            var pixelSize = Image.GetPixelFormatSize( sourceData.PixelFormat ) / 8;
            var srcStride = sourceData.Stride;
            var dstStride = destinationData.Stride;

            // find equations of four quadrilateral's edges ( f(x) = k*x + b )
            double kTop,    bTop;
            double kBottom, bBottom;
            double kLeft,   bLeft;
            double kRight,  bRight;

            // top edge
            if ( this.sourceQuadrilateral[1].X == this.sourceQuadrilateral[0].X )
            {
                kTop = 0;
                bTop = this.sourceQuadrilateral[1].X;
            }
            else
            {
                kTop = (double) ( this.sourceQuadrilateral[1].Y - this.sourceQuadrilateral[0].Y ) /
                                ( this.sourceQuadrilateral[1].X - this.sourceQuadrilateral[0].X );
                bTop = (double)this.sourceQuadrilateral[0].Y - kTop * this.sourceQuadrilateral[0].X;
            }

            // bottom edge
            if ( this.sourceQuadrilateral[2].X == this.sourceQuadrilateral[3].X )
            {
                kBottom = 0;
                bBottom = this.sourceQuadrilateral[2].X;
            }
            else
            {
                kBottom = (double) ( this.sourceQuadrilateral[2].Y - this.sourceQuadrilateral[3].Y ) /
                                   ( this.sourceQuadrilateral[2].X - this.sourceQuadrilateral[3].X );
                bBottom = (double)this.sourceQuadrilateral[3].Y - kBottom * this.sourceQuadrilateral[3].X;
            }

            // left edge
            if ( this.sourceQuadrilateral[3].X == this.sourceQuadrilateral[0].X )
            {
                kLeft = 0;
                bLeft = this.sourceQuadrilateral[3].X;
            }
            else
            {
                kLeft = (double) ( this.sourceQuadrilateral[3].Y - this.sourceQuadrilateral[0].Y ) /
                                 ( this.sourceQuadrilateral[3].X - this.sourceQuadrilateral[0].X );
                bLeft = (double)this.sourceQuadrilateral[0].Y - kLeft * this.sourceQuadrilateral[0].X;
            }

            // right edge
            if ( this.sourceQuadrilateral[2].X == this.sourceQuadrilateral[1].X )
            {
                kRight = 0;
                bRight = this.sourceQuadrilateral[2].X;
            }
            else
            {
                kRight = (double) ( this.sourceQuadrilateral[2].Y - this.sourceQuadrilateral[1].Y ) /
                                  ( this.sourceQuadrilateral[2].X - this.sourceQuadrilateral[1].X );
                bRight = (double)this.sourceQuadrilateral[1].Y - kRight * this.sourceQuadrilateral[1].X;
            }

            // some precalculated values
            var leftFactor  = (double) ( this.sourceQuadrilateral[3].Y - this.sourceQuadrilateral[0].Y ) / dstHeight;
            var rightFactor = (double) ( this.sourceQuadrilateral[2].Y - this.sourceQuadrilateral[1].Y ) / dstHeight;

            var srcY0 = this.sourceQuadrilateral[0].Y;
            var srcY1 = this.sourceQuadrilateral[1].Y;

            // do the job
            var baseSrc = (byte*) sourceData.ImageData.ToPointer( );
            var baseDst = (byte*) destinationData.ImageData.ToPointer( );

            // source width and height decreased by 1
            var ymax = srcHeight - 1;
            var xmax = srcWidth - 1;

            // coordinates of source points
            double  dx1, dy1, dx2, dy2;
            int     sx1, sy1, sx2, sy2;

            // temporary pointers
            byte* p1, p2, p3, p4, p;

            // for each line
            for ( var y = 0; y < dstHeight; y++ )
            {
                var dst = baseDst + dstStride * y;

                // find corresponding Y on the left edge of the quadrilateral
                var yHorizLeft = leftFactor * y + srcY0;
                // find corresponding X on the left edge of the quadrilateral
                var xHorizLeft = ( kLeft == 0 ) ? bLeft : ( yHorizLeft - bLeft ) / kLeft;

                // find corresponding Y on the right edge of the quadrilateral
                var yHorizRight = rightFactor * y + srcY1;
                // find corresponding X on the left edge of the quadrilateral
                var xHorizRight = ( kRight == 0 ) ? bRight : ( yHorizRight - bRight ) / kRight;

                // find equation of the line joining points on the left and right edges
                double kHoriz, bHoriz;

                if ( xHorizLeft == xHorizRight )
                {
                    kHoriz = 0;
                    bHoriz = xHorizRight;
                }
                else
                {
                    kHoriz = ( yHorizRight - yHorizLeft ) / ( xHorizRight - xHorizLeft );
                    bHoriz = yHorizLeft - kHoriz * xHorizLeft;
                }

                var horizFactor = ( xHorizRight - xHorizLeft ) / dstWidth;

                if ( !this.useInterpolation )
                {
                    for ( var x = 0; x < dstWidth; x++ )
                    {
                        var xs = horizFactor * x + xHorizLeft;
                        var ys = kHoriz * xs + bHoriz;

                        if ( ( xs >= 0 ) && ( ys >= 0 ) && ( xs < srcWidth ) && ( ys < srcHeight ) )
                        {
                            // get pointer to the pixel in the source image
                            p = baseSrc + ( (int) ys * srcStride + (int) xs * pixelSize );
                            // copy pixel's values
                            for ( var i = 0; i < pixelSize; i++, dst++, p++ )
                            {
                                *dst = *p;
                            }
                        }
                        else
                        {
                            dst += pixelSize;
                        }
                    }
                }
                else
                {
                    for ( var x = 0; x < dstWidth; x++ )
                    {
                        var xs = horizFactor * x + xHorizLeft;
                        var ys = kHoriz * xs + bHoriz;

                        if ( ( xs >= 0 ) && ( ys >= 0 ) && ( xs < srcWidth ) && ( ys < srcHeight ) )
                        {
                            sx1 = (int) xs;
                            sx2 = ( sx1 == xmax ) ? sx1 : sx1 + 1;
                            dx1 = xs - sx1;
                            dx2 = 1.0 - dx1;

                            sy1 = (int) ys;
                            sy2 = ( sy1 == ymax ) ? sy1 : sy1 + 1;
                            dy1 = ys - sy1;
                            dy2 = 1.0 - dy1;

                            // get four points
                            p1 = p2 = baseSrc + sy1 * srcStride;
                            p1 += sx1 * pixelSize;
                            p2 += sx2 * pixelSize;

                            p3 = p4 = baseSrc + sy2 * srcStride;
                            p3 += sx1 * pixelSize;
                            p4 += sx2 * pixelSize;

                            // interpolate using 4 points
                            for ( var i = 0; i < pixelSize; i++, dst++, p1++, p2++, p3++, p4++ )
                            {
                                *dst = (byte) (
                                    dy2 * ( dx2 * ( *p1 ) + dx1 * ( *p2 ) ) +
                                    dy1 * ( dx2 * ( *p3 ) + dx1 * ( *p4 ) ) );
                            }
                        }
                        else
                        {
                            dst += pixelSize;
                        }
                    }
                }
            }
        }
    }
}

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
    /// Performs quadrilateral transformation using nearest neighbor algorithm for interpolation.
    /// </summary>
    /// 
    /// <remarks><para>The class is deprecated and <see cref="SimpleQuadrilateralTransformation"/> should be used instead.</para>
    /// </remarks>
    /// 
    /// <seealso cref="SimpleQuadrilateralTransformation"/>
    ///
    [Obsolete( "The class is deprecated and SimpleQuadrilateralTransformation should be used instead" )]
    public class QuadrilateralTransformationNearestNeighbor : BaseTransformationFilter
    {
        private SimpleQuadrilateralTransformation baseFilter = null;

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations
        {
            get { return this.baseFilter.FormatTranslations; }
        }

        /// <summary>
        /// Automatic calculation of destination image or not.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies how to calculate size of destination (transformed)
        /// image. If the property is set to <see langword="false"/>, then <see cref="NewWidth"/>
        /// and <see cref="NewHeight"/> properties have effect and destination image's size is
        /// specified by user. If the property is set to <see langword="true"/>, then setting the above
        /// mentioned properties does not have any effect, but destionation image's size is
        /// automatically calculated from <see cref="SourceCorners"/> property - width and height
        /// come from length of longest edges.
        /// </para></remarks>
        /// 
        public bool AutomaticSizeCalculaton
        {
            get { return this.baseFilter.AutomaticSizeCalculaton; }
            set { this.baseFilter.AutomaticSizeCalculaton = value;  }
        }

        /// <summary>
        /// Quadrilateral's corners in source image.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies four corners of the quadrilateral area
        /// in the source image to be transformed.</para>
        /// </remarks>
        /// 
        public List<IntPoint> SourceCorners
        {
            get { return this.baseFilter.SourceQuadrilateral; }
            set { this.baseFilter.SourceQuadrilateral = value; }
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
        /// is calculated automatically based on <see cref="SourceCorners"/> property.</note></para>
        /// </remarks>
        /// 
        public int NewWidth
        {
            get { return this.baseFilter.NewWidth; }
            set { this.baseFilter.NewWidth = value; }
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
        /// is calculated automatically based on <see cref="SourceCorners"/> property.</note></para>
        /// </remarks>
        /// 
        public int NewHeight
        {
            get { return this.baseFilter.NewHeight; }
            set { this.baseFilter.NewHeight = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuadrilateralTransformationNearestNeighbor"/> class.
        /// </summary>
        /// 
        /// <param name="sourceCorners">Corners of the source quadrilateral area.</param>
        /// <param name="newWidth">Width of the new transformed image.</param>
        /// <param name="newHeight">Height of the new transformed image.</param>
        /// 
        /// <remarks><para>This constructor sets <see cref="AutomaticSizeCalculaton"/> to
        /// <see langword="false"/>, which means that destination image will have width and
        /// height as specified by user.</para></remarks>
        /// 
        public QuadrilateralTransformationNearestNeighbor( List<IntPoint> sourceCorners, int newWidth, int newHeight )
		{
            this.baseFilter = new SimpleQuadrilateralTransformation( sourceCorners, newWidth, newHeight );
            this.baseFilter.UseInterpolation = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuadrilateralTransformationNearestNeighbor"/> class.
        /// </summary>
        /// 
        /// <param name="sourceCorners">Corners of the source quadrilateral area.</param>
        /// 
        /// <remarks><para>This constructor sets <see cref="AutomaticSizeCalculaton"/> to
        /// <see langword="true"/>, which means that destination image will have width and
        /// height automatically calculated based on <see cref="SourceCorners"/> property.</para></remarks>
        ///
        public QuadrilateralTransformationNearestNeighbor( List<IntPoint> sourceCorners ) 
        {
            this.baseFilter = new SimpleQuadrilateralTransformation( sourceCorners );
            this.baseFilter.UseInterpolation = false;
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
            this.baseFilter.Apply( sourceData, destinationData );
        }

        /// <summary>
        /// Calculates new image size.
        /// </summary>
        /// 
        /// <param name="sourceData">Source image data.</param>
        /// 
        /// <returns>New image size - size of the destination image.</returns>
        /// 
        /// <exception cref="ArgumentException">The specified quadrilateral's corners are outside of the given image.</exception>
        /// 
        protected override System.Drawing.Size CalculateNewImageSize( UnmanagedImage sourceData )
        {
            // perform checking of source corners - they must feet into the image
            foreach ( var point in this.baseFilter.SourceQuadrilateral )
            {
                if ( ( point.X < 0 ) ||
                     ( point.Y < 0 ) ||
                     ( point.X >= sourceData.Width ) ||
                     ( point.Y >= sourceData.Height ) )
                {
                    throw new ArgumentException( "The specified quadrilateral's corners are outside of the given image." );
                }
            }

            return new Size(this.baseFilter.NewWidth, this.baseFilter.NewHeight );
        }
    }
}

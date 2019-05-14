// AForge Image Processing Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright � Andrew Kirillov, 2005-2010
// andrew.kirillov@aforgenet.com
//

namespace AForge.Imaging.Textures
{
    using System;

    /// <summary>
    /// Textile texture.
    /// </summary>
    /// 
    /// <remarks><para>The texture generator creates textures with effect of textile.</para>
    /// 
    /// <para>The generator is based on the <see cref="AForge.Math.PerlinNoise">Perlin noise function</see>.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create texture generator
    /// TextileTexture textureGenerator = new TextileTexture( );
    /// // generate new texture
    /// float[,] texture = textureGenerator.Generate( 320, 240 );
    /// // convert it to image to visualize
    /// Bitmap textureImage = TextureTools.ToBitmap( texture );
    /// </code>
    ///
    /// <para><b>Result image:</b></para>
    /// <img src="img/imaging/textile_texture.jpg" width="320" height="240" />
    /// </remarks>
    /// 
    public class TextileTexture : ITextureGenerator
    {
        // Perlin noise function used for texture generation
        private AForge.Math.PerlinNoise noise = new AForge.Math.PerlinNoise( 3, 0.65, 1.0 / 8, 1.0 );

        // randmom number generator
        private Random rand = new Random( );
        private int r;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextileTexture"/> class.
        /// </summary>
        /// 
        public TextileTexture( )
        {
            this.Reset( );
        }

        /// <summary>
        /// Generate texture.
        /// </summary>
        /// 
        /// <param name="width">Texture's width.</param>
        /// <param name="height">Texture's height.</param>
        /// 
        /// <returns>Two dimensional array of intensities.</returns>
        /// 
        /// <remarks>Generates new texture of the specified size.</remarks>
        /// 
        public float[,] Generate( int width, int height )
        {
            var texture = new float[height, width];

            for ( var y = 0; y < height; y++ )
            {
                for ( var x = 0; x < width; x++ )
                {
                    texture[y, x] =
                        Math.Max( 0.0f, Math.Min( 1.0f,
                            (
                                (float) Math.Sin( x + this.noise.Function2D( x + this.r , y + this.r ) ) +
                                (float) Math.Sin( y + this.noise.Function2D( x + this.r , y + this.r ) )
                            ) * 0.25f + 0.5f
                        ) );

                }
            }
            return texture;
        }

        /// <summary>
        /// Reset generator.
        /// </summary>
        /// 
        /// <remarks>Regenerates internal random numbers.</remarks>
        /// 
        public void Reset( )
        {
            this.r = this.rand.Next( 5000 );
        }
    }
}

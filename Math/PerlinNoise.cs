// AForge Math Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright � Andrew Kirillov, 2005-2009
// andrew.kirillov@aforgenet.com
//

namespace AForge.Math
{
    using System;

    /// <summary>
    /// Perlin noise function.
    /// </summary>
    /// 
    /// <remarks><para>The class implements 1-D and 2-D Perlin noise functions, which represent
    /// sum of several smooth noise functions with different frequency and amplitude. The description
    /// of Perlin noise function and its calculation may be found on
    /// <a href="http://freespace.virgin.net/hugo.elias/models/m_perlin.htm" target="_blank">Hugo Elias's page</a>.
    /// </para>
    /// 
    /// <para>The number of noise functions, which comprise the resulting Perlin noise function, is
    /// set by <see cref="Octaves"/> property. Amplitude and frequency values for each octave
    /// start from values, which are set by <see cref="InitFrequency"/> and <see cref="InitAmplitude"/>
    /// properties.</para>
    /// 
    /// <para>Sample usage (clouds effect):</para>
    /// <code>
    /// // create Perlin noise function
    /// PerlinNoise noise = new PerlinNoise( 8, 0.5, 1.0 / 32 );
    /// // generate clouds effect
    /// float[,] texture = new float[height, width];
    /// 
    /// for ( int y = 0; y &lt; height; y++ )
    /// {
    /// 	for ( int x = 0; x &lt; width; x++ )
    /// 	{
    /// 		texture[y, x] = 
    /// 			Math.Max( 0.0f, Math.Min( 1.0f,
    /// 				(float) noise.Function2D( x, y ) * 0.5f + 0.5f
    /// 			) );
    /// 	}
    /// }
    /// </code>
    /// </remarks>
    /// 
    public class PerlinNoise
    {
        private double	initFrequency = 1.0;
        private double	initAmplitude = 1.0;
        private double	persistence = 0.65;
        private int		octaves = 4;

        /// <summary>
        /// Initial frequency.
        /// </summary>
        /// 
        /// <remarks><para>The property sets initial frequency of the first octave. Frequencies for
        /// next octaves are calculated using the next equation:<br />
        /// frequency<sub>i</sub> = <see cref="InitFrequency"/> * 2<sup>i</sup>,
        /// where i = [0, <see cref="Octaves"/>).
        /// </para>
        /// 
        /// <para>Default value is set to <b>1</b>.</para>
        /// </remarks>
        /// 
        public double InitFrequency
        {
            get { return this.initFrequency; }
            set { this.initFrequency = value; }
        }

        /// <summary>
        /// Initial amplitude.
        /// </summary>
        /// 
        /// <remarks><para>The property sets initial amplitude of the first octave. Amplitudes for
        /// next octaves are calculated using the next equation:<br />
        /// amplitude<sub>i</sub> = <see cref="InitAmplitude"/> * <see cref="Persistence"/><sup>i</sup>,
        /// where i = [0, <see cref="Octaves"/>).
        /// </para>
        /// 
        /// <para>Default value is set to <b>1</b>.</para>
        /// </remarks>
        ///
        public double InitAmplitude
        {
            get { return this.initAmplitude; }
            set { this.initAmplitude = value; }
        }

        /// <summary>
        /// Persistence value.
        /// </summary>
        ///
        /// <remarks><para>The property sets so called persistence value, which controls the way
        /// how <see cref="InitAmplitude">amplitude</see> is calculated for each octave comprising
        /// the Perlin noise function.</para>
        /// 
        /// <para>Default value is set to <b>0.65</b>.</para>
        /// </remarks>
        ///
        public double Persistence
        {
            get { return this.persistence; }
            set { this.persistence = value; }
        }

        /// <summary>
        /// Number of octaves, [1, 32].
        /// </summary>
        /// 
        /// <remarks><para>The property sets the number of noise functions, which sum up the resulting
        /// Perlin noise function.</para>
        /// 
        /// <para>Default value is set to <b>4</b>.</para>
        /// </remarks>
        /// 
        public int Octaves
        {
            get { return this.octaves; }
            set { this.octaves = System.Math.Max( 1, System.Math.Min( 32, value ) ); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerlinNoise"/> class.
        /// </summary>
        /// 
        public PerlinNoise( ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerlinNoise"/> class.
        /// </summary>
        /// 
        /// <param name="octaves">Number of octaves (see <see cref="Octaves"/> property).</param>
        /// <param name="persistence">Persistence value (see <see cref="Persistence"/> property).</param>
        /// 
        public PerlinNoise( int octaves, double persistence )
        {
            this.octaves = octaves;
            this.persistence = persistence;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="PerlinNoise"/> class.
        /// </summary>
        /// 
        /// <param name="octaves">Number of octaves (see <see cref="Octaves"/> property).</param>
        /// <param name="persistence">Persistence value (see <see cref="Persistence"/> property).</param>
        /// <param name="initFrequency">Initial frequency (see <see cref="InitFrequency"/> property).</param>
        /// <param name="initAmplitude">Initial amplitude (see <see cref="InitAmplitude"/> property).</param>
        /// 
        public PerlinNoise( int octaves, double persistence, double initFrequency, double initAmplitude )
        {
            this.octaves       = octaves;
            this.persistence   = persistence;
            this.initFrequency = initFrequency;
            this.initAmplitude = initAmplitude;
        }

        /// <summary>
        /// 1-D Perlin noise function.
        /// </summary>
        /// 
        /// <param name="x">x value.</param>
        /// 
        /// <returns>Returns function's value at point <paramref name="x"/>.</returns>
        /// 
        public double Function( double x )
        {
            var	frequency = this.initFrequency;
            var	amplitude = this.initAmplitude;
            double	sum = 0;

            // octaves
            for ( var i = 0; i < this.octaves; i++ )
            {
                sum += this.SmoothedNoise( x * frequency ) * amplitude;

                frequency *= 2;
                amplitude *= this.persistence;
            }
            return sum;
        }

        /// <summary>
        /// 2-D Perlin noise function.
        /// </summary>
        /// 
        /// <param name="x">x value.</param>
        /// <param name="y">y value.</param>
        /// 
        /// <returns>Returns function's value at point (<paramref name="x"/>, <paramref name="y"/>).</returns>
        /// 
        public double Function2D( double x, double y )
        {
            var	frequency = this.initFrequency;
            var	amplitude = this.initAmplitude;
            double	sum = 0;

            // octaves
            for ( var i = 0; i < this.octaves; i++ )
            {
                sum += this.SmoothedNoise( x * frequency, y * frequency ) * amplitude;

                frequency *= 2;
                amplitude *= this.persistence;
            }
            return sum;
        }


        /// <summary>
        /// Ordinary noise function
        /// </summary>
        private double Noise( int x )
        {
            var n = ( x << 13 ) ^ x;

            return ( 1.0 - ( ( n * ( n * n * 15731 + 789221 ) + 1376312589 ) & 0x7fffffff ) / 1073741824.0 );
        }
        private double Noise( int x, int y )
        {
            var n = x + y * 57;
            n = ( n << 13 ) ^ n;

            return ( 1.0 - ( ( n * ( n * n * 15731 + 789221 ) + 1376312589 ) & 0x7fffffff ) / 1073741824.0 );
        }


        /// <summary>
        /// Smoothed noise.
        /// </summary>
        private double SmoothedNoise( double x )
        {
            var		xInt = (int) x;
            var	xFrac = x - xInt;

            return this.CosineInterpolate(this.Noise( xInt ), this.Noise( xInt + 1 ), xFrac );
        }
        private double SmoothedNoise( double x, double y )
        {
            var		xInt = (int) x;
            var		yInt = (int) y;
            var	xFrac = x - xInt;
            var	yFrac = y - yInt;

            // get four noise values
            var	x0y0 = this.Noise( xInt, yInt );
            var	x1y0 = this.Noise( xInt + 1, yInt );
            var	x0y1 = this.Noise( xInt, yInt + 1 );
            var	x1y1 = this.Noise( xInt + 1, yInt + 1 );

            // x interpolation
            var	v1 = this.CosineInterpolate( x0y0, x1y0, xFrac );
            var	v2 = this.CosineInterpolate( x0y1, x1y1, xFrac );
            // y interpolation
            return this.CosineInterpolate( v1, v2, yFrac );
        }

        /// <summary>
        /// Cosine interpolation.
        /// </summary>
        private double CosineInterpolate( double x1, double x2, double a )
        {
            var f = ( 1 - Math.Cos( a * Math.PI ) ) * 0.5;

            return x1 * ( 1 - f ) + x2 * f;
        }
    }
}

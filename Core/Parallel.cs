﻿// AForge Core Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © Andrew Kirillov, 2007-2009
// andrew.kirillov@aforgenet.com
//
// Copyright © Israel Lot, 2008
// israel.lot@gmail.com
//

namespace AForge
{
    using System;
    using System.Threading;

    /// <summary>
    /// The class provides support for parallel computations, paralleling loop's iterations.
    /// </summary>
    /// 
    /// <remarks><para>The class allows to parallel loop's iteration computing them in separate threads,
    /// what allows their simultaneous execution on multiple CPUs/cores.
    /// </para></remarks>
    ///
    public sealed class Parallel
    {
        /// <summary>
        /// Delegate defining for-loop's body.
        /// </summary>
        /// 
        /// <param name="index">Loop's index.</param>
        /// 
        public delegate void ForLoopBody( int index );

        // number of threads for parallel computations
        private static int threadsCount = System.Environment.ProcessorCount;
        // object used for synchronization
        private static object sync = new Object( );

        // single instance of the class to implement singleton pattern
        private static volatile Parallel instance = null;
        // background threads for parallel computation
        private Thread[] threads = null;

        // events to signal about job availability and thread availability
        private AutoResetEvent[] jobAvailable = null;
        private ManualResetEvent[] threadIdle = null;

        // loop's body and its current and stop index
        private int currentIndex;
        private int stopIndex;
        private ForLoopBody loopBody;

        /// <summary>
        /// Number of threads used for parallel computations.
        /// </summary>
        /// 
        /// <remarks><para>The property sets how many worker threads are created for paralleling
        /// loops' computations.</para>
        /// 
        /// <para>By default the property is set to number of CPU's in the system
        /// (see <see cref="System.Environment.ProcessorCount"/>).</para>
        /// </remarks>
        /// 
        public static int ThreadsCount
        {
            get { return threadsCount; }
            set
            {
                lock ( sync )
                {
                    threadsCount = Math.Max( 1, value );
                }
            }
        }

        /// <summary>
        /// Executes a for-loop in which iterations may run in parallel. 
        /// </summary>
        /// 
        /// <param name="start">Loop's start index.</param>
        /// <param name="stop">Loop's stop index.</param>
        /// <param name="loopBody">Loop's body.</param>
        /// 
        /// <remarks><para>The method is used to parallel for-loop running its iterations in
        /// different threads. The <b>start</b> and <b>stop</b> parameters define loop's
        /// starting and ending loop's indexes. The number of iterations is equal to <b>stop - start</b>.
        /// </para>
        /// 
        /// <para>Sample usage:</para>
        /// <code>
        /// Parallel.For( 0, 20, delegate( int i )
        /// // which is equivalent to
        /// // for ( int i = 0; i &lt; 20; i++ )
        /// {
        ///     System.Diagnostics.Debug.WriteLine( "Iteration: " + i );
        ///     // ...
        /// } );
        /// </code>
        /// </remarks>
        /// 
        public static void For( int start, int stop, ForLoopBody loopBody  )
        {
            lock ( sync )
            {
                // get instance of parallel computation manager
                var instance = Instance;

                instance.currentIndex   = start - 1;
                instance.stopIndex      = stop;
                instance.loopBody       = loopBody;

                // signal about available job for all threads and mark them busy
                for ( var i = 0; i < threadsCount; i++ )
                {
                    instance.threadIdle[i].Reset( );
                    instance.jobAvailable[i].Set( );
                }

                // wait until all threads become idle
                for ( var i = 0; i < threadsCount; i++ )
                {
                    instance.threadIdle[i].WaitOne( );
                }

                instance.loopBody = null;
            }
        }

        // Private constructor to avoid class instantiation
        private Parallel( ) { }

        // Get instace of the Parallel class
        private static Parallel Instance
        {
            get
            {
                if ( instance == null )
                {
                    instance = new Parallel( );
                    instance.Initialize( );
                }
                else
                {
                    if ( instance.threads.Length != threadsCount )
                    {
                        // terminate old threads
                        instance.Terminate( );
                        // reinitialize
                        instance.Initialize( );

                        // TODO: change reinitialization to reuse already created objects
                    }
                }
                return instance;
            }
        }

        // Initialize Parallel class's instance creating required number of threads
        // and synchronization objects
        private void Initialize( )
        {
            // array of events, which signal about available job
            this.jobAvailable = new AutoResetEvent[threadsCount];
            // array of events, which signal about available thread
            this.threadIdle = new ManualResetEvent[threadsCount];
            // array of threads
            this.threads = new Thread[threadsCount];

            for ( var i = 0; i < threadsCount; i++ )
            {
                this.jobAvailable[i] = new AutoResetEvent( false );
                this.threadIdle[i]   = new ManualResetEvent( true );

                this.threads[i] = new Thread( new ParameterizedThreadStart(this.WorkerThread ) );
                this.threads[i].Name = "AForge.Parallel";
                this.threads[i].IsBackground = true;
                this.threads[i].Start( i );
            }
        }

        // Terminate all worker threads used for parallel computations and close all
        // synchronization objects
        private void Terminate( )
        {
            // finish thread by setting null loop body and signaling about available work
            this.loopBody = null;
            for ( int i = 0, threadsCount = this.threads.Length ; i < threadsCount; i++ )
            {
                this.jobAvailable[i].Set( );
                // wait for thread termination
                this.threads[i].Join( );

                // close events
                this.jobAvailable[i].Close( );
                this.threadIdle[i].Close( );
            }

            // clean all array references
            this.jobAvailable    = null;
            this.threadIdle      = null;
            this.threads         = null;
        }

        // Worker thread performing parallel computations in loop
        private void WorkerThread( object index )
        {
            var threadIndex = (int) index;
            var localIndex = 0;

            while ( true )
            {
                // wait until there is job to do
                this.jobAvailable[threadIndex].WaitOne( );

                // exit on null body
                if ( this.loopBody == null )
                    break;

                while ( true )
                {
                    // get local index incrementing global loop's current index
                    localIndex = Interlocked.Increment( ref this.currentIndex );

                    if ( localIndex >= this.stopIndex )
                        break;

                    // run loop's body
                    this.loopBody( localIndex );
                }

                // signal about thread availability
                this.threadIdle[threadIndex].Set( );
            }
        }
    }
}

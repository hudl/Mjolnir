// ThreadPool.cs
//
// Known issues/limitations/comments:
//
// * The PropogateCASMarkers property exists for future support for propogating
//   the calling thread's installed CAS markers in the same way that the built-in thread
//   pool does.  Currently, there is no support for user-defined code to perform that
//   operation.
//
// * PropogateCallContext and PropogateHttpContext both use reflection against private
//   members to due their job.  As such, these two properties are set to false by default,
//   but do work on the first release of the framework (including .NET Server) and its
//   service packs.  These features have not been tested on Everett at this time.
//
// Mike Woodring
// http://staff.develop.com/woodring
//
using System;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Security.Principal;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using log4net;

namespace Hudl.Mjolnir.ThreadPool
{
    public delegate void WorkRequestDelegate(object state, DateTime requestEnqueueTime);
    public delegate void ThreadPoolDelegate();

    #region IWorkRequest interface
    public interface IWorkRequest
    {
        bool Cancel();
        bool IsComplete
        {
            get;
        }
    }
    #endregion

    #region ThreadPool class

    public sealed class WoodringThreadPool : WaitHandle
    {
        #region ThreadPool constructors

        public WoodringThreadPool(int initialThreadCount, int maxThreadCount, string poolName)
            : this(initialThreadCount, maxThreadCount, poolName,
                    DEFAULT_NEW_THREAD_TRIGGER_TIME,
                    DEFAULT_DYNAMIC_THREAD_DECAY_TIME,
                    DEFAULT_THREAD_PRIORITY,
                    DEFAULT_REQUEST_QUEUE_LIMIT)
        {
        }

        public WoodringThreadPool(int initialThreadCount, int maxThreadCount, string poolName,
                           int newThreadTrigger, int dynamicThreadDecayTime,
                           ThreadPriority threadPriority, int requestQueueLimit)
        {
            Debug.WriteLine(string.Format("New thread pool {0} created:", poolName));
            Debug.WriteLine(string.Format("  initial thread count:      {0}", initialThreadCount));
            Debug.WriteLine(string.Format("  max thread count:          {0}", maxThreadCount));
            Debug.WriteLine(string.Format("  new thread trigger:        {0} ms", newThreadTrigger));
            Debug.WriteLine(string.Format("  dynamic thread decay time: {0} ms", dynamicThreadDecayTime));
            Debug.WriteLine(string.Format("  request queue limit:       {0} entries", requestQueueLimit));

            SafeWaitHandle = stopCompleteEvent.SafeWaitHandle;
            //Handle = stopCompleteEvent.Handle;

            if (maxThreadCount < initialThreadCount)
            {
                throw new ArgumentException("Maximum thread count must be >= initial thread count.", "maxThreadCount");
            }

            if (dynamicThreadDecayTime <= 0)
            {
                throw new ArgumentException("Dynamic thread decay time cannot be <= 0.", "dynamicThreadDecayTime");
            }

            if (newThreadTrigger <= 0)
            {
                throw new ArgumentException("New thread trigger time cannot be <= 0.", "newThreadTrigger");
            }

            this.initialThreadCount = initialThreadCount;
            this.maxThreadCount = maxThreadCount;
            this.requestQueueLimit = (requestQueueLimit < 0 ? DEFAULT_REQUEST_QUEUE_LIMIT : requestQueueLimit);
            this.decayTime = dynamicThreadDecayTime;
            this.newThreadTrigger = new TimeSpan(TimeSpan.TicksPerMillisecond * newThreadTrigger);
            this.threadPriority = threadPriority;
            this.requestQueue = new Queue(requestQueueLimit < 0 ? 4096 : requestQueueLimit);

            if (poolName == null)
            {
                throw new ArgumentNullException("poolName", "Thread pool name cannot be null");
            }
            else
            {
                this.threadPoolName = poolName;
            }
        }

        #endregion

        #region ThreadPool properties
        // The Priority & DynamicThreadDecay properties are not thread safe
        // and can only be set before Start is called.
        //
        public ThreadPriority Priority
        {
            get { return (threadPriority); }

            set
            {
                if (hasBeenStarted)
                {
                    throw new InvalidOperationException("Cannot adjust thread priority after pool has been started.");
                }

                threadPriority = value;
            }
        }

        public int DynamicThreadDecay
        {
            get { return (decayTime); }

            set
            {
                if (hasBeenStarted)
                {
                    throw new InvalidOperationException("Cannot adjust dynamic thread decay time after pool has been started.");
                }

                if (value <= 0)
                {
                    throw new ArgumentException("Dynamic thread decay time cannot be <= 0.", "value");
                }

                decayTime = value;
            }
        }

        public int NewThreadTrigger
        {
            get { return ((int)newThreadTrigger.TotalMilliseconds); }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("New thread trigger time cannot be <= 0.", "value");
                }

                lock (this)
                {
                    newThreadTrigger = new TimeSpan(TimeSpan.TicksPerMillisecond * value);
                }
            }
        }

        public int RequestQueueLimit
        {
            get { return (requestQueueLimit); }
            set { requestQueueLimit = (value < 0 ? DEFAULT_REQUEST_QUEUE_LIMIT : value); }
        }

        public int AvailableThreads
        {
            get { return (maxThreadCount - currentThreadCount); }
        }

        public int MaxThreads
        {
            get { return (maxThreadCount); }

            set
            {
                if (value < initialThreadCount)
                {
                    throw new ArgumentException("Maximum thread count must be >= initial thread count.", "MaxThreads");
                }

                maxThreadCount = value;
            }
        }

        public bool IsStarted
        {
            get { return (hasBeenStarted); }
        }

        public bool PropogateThreadPrincipal
        {
            get { return (propogateThreadPrincipal); }
            set { propogateThreadPrincipal = value; }
        }

        public bool PropogateCallContext
        {
            get { return (propogateCallContext); }
            set { propogateCallContext = value; }
        }

        public bool PropogateCASMarkers
        {
            get { return (propogateCASMarkers); }

            // When CompressedStack get/set is opened up,
            // add the following setter back in.
            //
            // set { propogateCASMarkers = value; }
        }

        public bool IsBackground
        {
            get { return (useBackgroundThreads); }

            set
            {
                if (hasBeenStarted)
                {
                    throw new InvalidOperationException("Cannot adjust background status after pool has been started.");
                }

                useBackgroundThreads = value;
            }
        }
        #endregion

        #region ThreadPool events
        public event ThreadPoolDelegate Started;
        public event ThreadPoolDelegate Stopped;
        #endregion

        public void Start()
        {
            lock (this)
            {
                if (hasBeenStarted)
                {
                    throw new InvalidOperationException("Pool has already been started.");
                }

                hasBeenStarted = true;

                // Check to see if there were already items posted to the queue
                // before Start was called.  If so, reset their timestamps to
                // the current time.
                //
                if (requestQueue.Count > 0)
                {
                    ResetWorkRequestTimes();
                }

                for (int n = 0; n < initialThreadCount; n++)
                {
                    ThreadWrapper thread =
                        new ThreadWrapper(this, true, threadPriority,
                                        string.Format("{0} (static)", threadPoolName));
                    thread.Start();
                }

                if (Started != null)
                {
                    Started(); // TODO: reconsider firing this event while holding the lock...
                }
            }
        }


        #region ThreadPool.Stop and InternalStop

        public void Stop()
        {
            InternalStop(false, Timeout.Infinite);
        }

        public void StopAndWait()
        {
            InternalStop(true, Timeout.Infinite);
        }

        public bool StopAndWait(int timeout)
        {
            return InternalStop(true, timeout);
        }

        private bool InternalStop(bool wait, int timeout)
        {
            if (!hasBeenStarted)
            {
                throw new InvalidOperationException("Cannot stop a thread pool that has not been started yet.");
            }

            lock (this)
            {
                Debug.WriteLine(string.Format("[{0}, {1}] Stopping pool (# threads = {2})",
                                               Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Name,
                                               currentThreadCount));
                stopInProgress = true;
                Monitor.PulseAll(this);
            }

            if (wait)
            {
                bool stopComplete = WaitOne(timeout, true);

                if (stopComplete)
                {
                    // If the stop was successful, we can support being
                    // to be restarted.  If the stop was requested, but not
                    // waited on, then we don't support restarting.
                    //
                    hasBeenStarted = false;
                    stopInProgress = false;
                    requestQueue.Clear();
                    stopCompleteEvent.Reset();
                }

                return (stopComplete);
            }

            return (true);
        }

        #endregion

        #region ThreadPool.PostRequest(early bound)

        // Overloads for the early bound WorkRequestDelegate-based targets.
        //
        public bool PostRequest(WorkRequestDelegate cb)
        {
            return PostRequest(cb, (object)null);
        }

        public bool PostRequest(WorkRequestDelegate cb, object state)
        {
            IWorkRequest notUsed;
            return PostRequest(cb, state, out notUsed);
        }

        public bool PostRequest(WorkRequestDelegate cb, object state, out IWorkRequest reqStatus)
        {
            WorkRequest request =
                new WorkRequest(cb, state, propogateThreadPrincipal, propogateCallContext, propogateCASMarkers);
            reqStatus = request;
            return PostRequest(request);
        }

        #endregion

        #region ThreadPool.PostRequest(late bound)

        // Overloads for the late bound Delegate.DynamicInvoke-based targets.
        //
        public bool PostRequest(Delegate cb, object[] args)
        {
            IWorkRequest notUsed;
            return PostRequest(cb, args, out notUsed);
        }

        public bool PostRequest(Delegate cb, object[] args, out IWorkRequest reqStatus)
        {
            WorkRequest request =
                new WorkRequest(cb, args, propogateThreadPrincipal, propogateCallContext, propogateCASMarkers);
            reqStatus = request;
            return PostRequest(request);
        }

        #endregion

        // The actual implementation of PostRequest.
        //
        bool PostRequest(WorkRequest request)
        {
            lock (this)
            {
                // A requestQueueLimit of -1 means the queue is "unbounded"
                // (subject to available resources).  IOW, no artificial limit
                // has been placed on the maximum # of requests that can be
                // placed into the queue.
                //
                if ((requestQueueLimit == -1) || (requestQueue.Count < requestQueueLimit))
                {
                    try
                    {
                        requestQueue.Enqueue(request);
                        Monitor.Pulse(this);
                        return (true);
                    }
                    catch
                    {
                    }

                }
            }

            return (false);
        }

        void ResetWorkRequestTimes()
        {
            lock (this)
            {
                DateTime newTime = DateTime.Now; // DateTime.Now.Add(pool.newThreadTrigger);

                foreach (WorkRequest wr in requestQueue)
                {
                    wr.workingTime = newTime;
                }
            }
        }

        #region Private ThreadPool constants

        // Default parameters.
        //
        const int DEFAULT_DYNAMIC_THREAD_DECAY_TIME = 5 /* minutes */ * 60 /* sec/min */ * 1000 /* ms/sec */;
        const int DEFAULT_NEW_THREAD_TRIGGER_TIME = 500; // milliseconds
        const ThreadPriority DEFAULT_THREAD_PRIORITY = ThreadPriority.Normal;
        const int DEFAULT_REQUEST_QUEUE_LIMIT = -1; // unbounded

        #endregion

        #region Private ThreadPool member variables

        private bool hasBeenStarted = false;
        private bool stopInProgress = false;
        private readonly string threadPoolName;
        private readonly int initialThreadCount;     // Initial # of threads to create (called "static threads" in this class).
        private int maxThreadCount;         // Cap for thread count.  Threads added above initialThreadCount are called "dynamic" threads.
        private int currentThreadCount = 0; // Current # of threads in the pool (static + dynamic).
        private int decayTime;              // If a dynamic thread is idle for this period of time w/o processing work requests, it will exit.
        private TimeSpan newThreadTrigger;       // If a work request sits in the queue this long before being processed, a new thread will be added to queue up to the max.
        private ThreadPriority threadPriority;
        private ManualResetEvent stopCompleteEvent = new ManualResetEvent(false); // Signaled after Stop called and last thread exits.
        private Queue requestQueue;
        private int requestQueueLimit;      // Throttle for maximum # of work requests that can be added.
        private bool useBackgroundThreads = true;
        private bool propogateThreadPrincipal = false;
        private bool propogateCallContext = false;
        private bool propogateCASMarkers = false;

        #endregion

        #region ThreadPool.ThreadInfo

        class ThreadInfo
        {
            public static ThreadInfo Capture(bool propogateThreadPrincipal, bool propogateCallContext, bool propogateCASMarkers)
            {
                return new ThreadInfo(propogateThreadPrincipal, propogateCallContext, propogateCASMarkers);
            }

            public static ThreadInfo Impersonate(ThreadInfo ti)
            {
                if (ti == null) throw new ArgumentNullException("ti");

                ThreadInfo prevInfo = Capture(true, true, true);
                Restore(ti);
                return (prevInfo);
            }

            public static void Restore(ThreadInfo ti)
            {
                if (ti == null) throw new ArgumentNullException("ti");

                // Restore call context.
                //
                if (miSetLogicalCallContext != null)
                {
                    miSetLogicalCallContext.Invoke(Thread.CurrentThread, new object[] { ti.callContext });
                }

                // Restore thread identity.  It's important that this be done after
                // restoring call context above, since restoring call context also
                // overwrites the current thread principal setting.  If propogateCallContext
                // and propogateThreadPrincipal are both true, then the following is redundant.
                // However, since propogating call context requires the use of reflection
                // to capture/restore call context, I want that behavior to be independantly
                // switchable so that it can be disabled; while still allowing thread principal
                // to be propogated.  This also covers us in the event that call context
                // propogation changes so that it no longer propogates thread principal.
                //
                Thread.CurrentPrincipal = ti.principal;

                if (ti.compressedStack != null)
                {
                    // TODO: Uncomment the following when Thread.SetCompressedStack is no longer guarded
                    //       by a StrongNameIdentityPermission.
                    //
                    // Thread.CurrentThread.SetCompressedStack(ti.compressedStack);
                }
            }

            private ThreadInfo(bool propogateThreadPrincipal, bool propogateCallContext, bool propogateCASMarkers)
            {
                if (propogateThreadPrincipal)
                {
                    principal = Thread.CurrentPrincipal;
                }

                if (propogateCallContext && (miGetLogicalCallContext != null))
                {
                    callContext = (LogicalCallContext)miGetLogicalCallContext.Invoke(Thread.CurrentThread, null);
                    callContext = (LogicalCallContext)callContext.Clone();

                    // TODO: consider serialize/deserialize call context to get a MBV snapshot
                    //       instead of leaving it up to the Clone method.
                }

                if (propogateCASMarkers)
                {
                    // TODO: Uncomment the following when Thread.GetCompressedStack is no longer guarded
                    //       by a StrongNameIdentityPermission.
                    //
                    // compressedStack = Thread.CurrentThread.GetCompressedStack();
                }
            }

            IPrincipal principal;
            LogicalCallContext callContext;
            CompressedStack compressedStack = null; // Always null until Get/SetCompressedStack are opened up.

            // Cached type information.
            //
            const BindingFlags bfNonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            const BindingFlags bfNonPublicStatic = BindingFlags.Static | BindingFlags.NonPublic;

            static MethodInfo miGetLogicalCallContext =
                    typeof(Thread).GetMethod("GetLogicalCallContext", bfNonPublicInstance);

            static MethodInfo miSetLogicalCallContext =
                    typeof(Thread).GetMethod("SetLogicalCallContext", bfNonPublicInstance);
        }

        #endregion

        #region ThreadPool.WorkRequest

        class WorkRequest : IWorkRequest
        {
            internal const int PENDING = 0;
            internal const int PROCESSED = 1;
            internal const int CANCELLED = 2;

            public WorkRequest(WorkRequestDelegate cb, object arg,
                                bool propogateThreadPrincipal, bool propogateCallContext, bool propogateCASMarkers)
            {
                targetProc = cb;
                procArg = arg;
                procArgs = null;

                Initialize(propogateThreadPrincipal, propogateCallContext, propogateCASMarkers);
            }

            public WorkRequest(Delegate cb, object[] args,
                                bool propogateThreadPrincipal, bool propogateCallContext, bool propogateCASMarkers)
            {
                targetProc = cb;
                procArg = null;
                procArgs = args;

                Initialize(propogateThreadPrincipal, propogateCallContext, propogateCASMarkers);
            }

            void Initialize(bool propogateThreadPrincipal, bool propogateCallContext, bool propogateCASMarkers)
            {
                workingTime = timeStampStarted = DateTime.Now;
                threadInfo = ThreadInfo.Capture(propogateThreadPrincipal, propogateCallContext, propogateCASMarkers);
            }

            public bool Cancel()
            {
                // If the work request was pending, mark it cancelled.  Otherwise,
                // this method was called too late.  Note that this call can
                // cancel an operation without any race conditions.  But if the
                // result of this test-and-set indicates the request is in the
                // "processed" state, it might actually be about to be processed.
                //
                return (Interlocked.CompareExchange(ref state, CANCELLED, PENDING) == PENDING);
            }

            public bool IsComplete
            {
                get
                {
                    return state == PROCESSED;
                }
            }

            internal Delegate targetProc;         // Function to call.
            internal object procArg;            // State to pass to function.
            internal object[] procArgs;           // Used with Delegate.DynamicInvoke.
            internal DateTime timeStampStarted;   // Time work request was originally enqueued (held constant).
            internal DateTime workingTime;        // Current timestamp used for triggering new threads (moving target).
            internal ThreadInfo threadInfo;         // Everything we know about a thread.
            internal int state = PENDING;    // The state of this particular request.
        }

        #endregion

        #region ThreadPool.ThreadWrapper

        class ThreadWrapper
        {
            WoodringThreadPool pool;
            bool isPermanent;
            ThreadPriority priority;
            string name;

            public ThreadWrapper(WoodringThreadPool pool, bool isPermanent,
                                  ThreadPriority priority, string name)
            {
                this.pool = pool;
                this.isPermanent = isPermanent;
                this.priority = priority;
                this.name = name;

                lock (pool)
                {
                    // Update the total # of threads in the pool.
                    //
                    pool.currentThreadCount++;
                }
            }

            public void Start()
            {
                Thread t = new Thread(new ThreadStart(ThreadProc));
                t.SetApartmentState(ApartmentState.MTA);
                t.Name = name;
                t.Priority = priority;
                t.IsBackground = pool.useBackgroundThreads;
                t.Start();
            }

            void ThreadProc()
            {
                Debug.WriteLine(string.Format("[{0}, {1}] Worker thread started",
                                               Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Name));

                bool done = false;

                while (!done)
                {
                    WorkRequest wr = null;
                    ThreadWrapper newThread = null;

                    lock (pool)
                    {
                        // As long as the request queue is empty and a shutdown hasn't
                        // been initiated, wait for a new work request to arrive.
                        //
                        bool timedOut = false;

                        while (!pool.stopInProgress && !timedOut && (pool.requestQueue.Count == 0))
                        {
                            if (!Monitor.Wait(pool, (isPermanent ? Timeout.Infinite : pool.decayTime)))
                            {
                                // Timed out waiting for something to do.  Only dynamically created
                                // threads will get here, so bail out.
                                //
                                timedOut = true;
                            }
                        }

                        // We exited the loop above because one of the following conditions
                        // was met:
                        //   - ThreadPool.Stop was called to initiate a shutdown.
                        //   - A dynamic thread timed out waiting for a work request to arrive.
                        //   - There are items in the work queue to process.

                        // If we exited the loop because there's work to be done,
                        // a shutdown hasn't been initiated, and we aren't a dynamic thread
                        // that timed out, pull the request off the queue and prepare to
                        // process it.
                        //
                        if (!pool.stopInProgress && !timedOut && (pool.requestQueue.Count > 0))
                        {
                            wr = (WorkRequest)pool.requestQueue.Dequeue();
                            Debug.Assert(wr != null);

                            // Check to see if this work request languished in the queue
                            // very long.  If it was in the queue >= the new thread trigger
                            // time, and if we haven't reached the max thread count cap,
                            // add a new thread to the pool.
                            //
                            // If the decision is made, create the new thread object (updating
                            // the current # of threads in the pool), but defer starting the new
                            // thread until the lock is released.
                            //
                            TimeSpan requestTimeInQ = DateTime.Now.Subtract(wr.workingTime);

                            if ((requestTimeInQ >= pool.newThreadTrigger) && (pool.currentThreadCount < pool.maxThreadCount))
                            {
                                // Note - the constructor for ThreadWrapper will update
                                // pool.currentThreadCount.
                                //
                                newThread =
                                    new ThreadWrapper(pool, false, priority,
                                                       string.Format("{0} (dynamic)", pool.threadPoolName));

                                // Since the current request we just dequeued is stale,
                                // everything else behind it in the queue is also stale.
                                // So reset the timestamps of the remaining pending work
                                // requests so that we don't start creating threads
                                // for every subsequent request.
                                //
                                pool.ResetWorkRequestTimes();
                            }
                        }
                        else
                        {
                            // Should only get here if this is a dynamic thread that
                            // timed out waiting for a work request, or if the pool
                            // is shutting down.
                            //
                            Debug.Assert((timedOut && !isPermanent) || pool.stopInProgress);
                            pool.currentThreadCount--;

                            if (pool.currentThreadCount == 0)
                            {
                                // Last one out turns off the lights.
                                //
                                Debug.Assert(pool.stopInProgress);

                                if (pool.Stopped != null)
                                {
                                    pool.Stopped();
                                }

                                pool.stopCompleteEvent.Set();
                            }

                            done = true;
                        }
                    } // lock

                    // No longer holding pool lock here...

                    if (!done && (wr != null))
                    {
                        // Check to see if this request has been cancelled while
                        // stuck in the work queue.
                        //
                        // If the work request was pending, mark it processed and proceed
                        // to handle.  Otherwise, the request must have been cancelled
                        // before we plucked it off the request queue.
                        if (Interlocked.CompareExchange(ref wr.state, WorkRequest.PROCESSED, WorkRequest.PENDING) != WorkRequest.PENDING)
                        {
                            // Request was cancelled before we could get here.
                            // Bail out.
                            continue;
                        }

                        if (newThread != null)
                        {
                            Debug.WriteLine(string.Format("[{0}, {1}] Adding dynamic thread to pool",
                                                        Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Name));
                            newThread.Start();
                        }

                        // Dispatch the work request.
                        ThreadInfo originalThreadInfo = null;

                        try
                        {
                            // Impersonate (as much as possible) what we know about
                            // the thread that issued the work request.
                            originalThreadInfo = ThreadInfo.Impersonate(wr.threadInfo);

                            WorkRequestDelegate targetProc = wr.targetProc as WorkRequestDelegate;

                            if (targetProc != null)
                            {
                                targetProc(wr.procArg, wr.timeStampStarted);
                            }
                            else
                            {
                                wr.targetProc.DynamicInvoke(wr.procArgs);
                            }
                        }
                        catch (Exception e)
                        {
                            LogManager.GetLogger("ThreadProc-" + name).Error("Exception in ThreadPool operation", e);
                        }
                        finally
                        {
                            // Restore our worker thread's identity.
                            ThreadInfo.Restore(originalThreadInfo);
                        }
                    }
                }

                Debug.WriteLine(string.Format("[{0}, {1}] Worker thread exiting pool", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Name));
            }
        }

        #endregion
    }
    #endregion
}

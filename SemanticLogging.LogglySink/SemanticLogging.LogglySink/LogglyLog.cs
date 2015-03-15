using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace SemanticLogging.LogglySink
{
    /// <summary>
    /// Factories and helpers for using the <see cref="LogglySink"/>.
    /// </summary>
    public static class LogglyLog
    {
        /// <summary>
        /// Subscribes to an <see cref="IObservable{EventEntry}" /> using a <see cref="LogglySink" />.
        /// </summary>
        /// <param name="eventStream">The event stream. Typically this is an instance of <see cref="ObservableEventListener" />.</param>
        /// <param name="instanceName">The name of the instance originating the entries.</param>
        /// <param name="connectionString">The endpoint address for the Loggly Service.</param>
        /// <param name="customerToken">The loggly customerToken that must be part of the Url.</param>
        /// <param name="tag">The tag used in loggly updates. Default is to use the instanceName.</param>
        /// <param name="flattenPayload">Flatten the payload if you want the parameters serialized.</param>
        /// <param name="bufferingInterval">The buffering interval between each batch publishing. Default value is <see cref="Buffering.DefaultBufferingInterval" />.</param>
        /// <param name="onCompletedTimeout">Defines a timeout interval for when flushing the entries after an <see cref="LogglySink.OnCompleted" /> call is received and before disposing the sink.</param>
        /// <param name="bufferingCount">Buffering count to send entries sot Loggly. Default value is <see cref="Buffering.DefaultBufferingCount" /></param>
        /// <param name="maxBufferSize">The maximum number of entries that can be buffered while it's sending to Loggly before the sink starts dropping entries.
        /// This means that if the timeout period elapses, some event entries will be dropped and not sent to the store. Normally, calling <see cref="IDisposable.Dispose" /> on
        /// the <see cref="System.Diagnostics.Tracing.EventListener" /> will block until all the entries are flushed or the interval elapses.
        /// If <see langword="null" /> is specified, then the call will block indefinitely until the flush operation finishes.</param>
        /// <returns>
        /// A subscription to the sink that can be disposed to unsubscribe the sink and dispose it, or to get access to the sink instance.
        /// </returns>
        public static SinkSubscription<LogglySink> LogToLoggly(this IObservable<EventEntry> eventStream,
            string instanceName, string connectionString, string customerToken, string tag, bool flattenPayload = true, TimeSpan? bufferingInterval = null,
            TimeSpan? onCompletedTimeout = null,
            int bufferingCount = Buffering.DefaultBufferingCount,
            int maxBufferSize = Buffering.DefaultMaxBufferSize)
        {
            var sink = new LogglySink(instanceName, connectionString, customerToken, tag, flattenPayload,
                bufferingInterval ?? Buffering.DefaultBufferingInterval,
                bufferingCount,
                maxBufferSize,
                onCompletedTimeout ?? Timeout.InfiniteTimeSpan);

            var subscription = eventStream.Subscribe(sink);
            return new SinkSubscription<LogglySink>(subscription, sink);
        }

        /// <summary>
        /// Creates an event listener that logs using a <see cref="LogglySink" />.
        /// </summary>
        /// <param name="instanceName">The name of the instance originating the entries.</param>
        /// <param name="connectionString">The endpoint address for the Loggly Service.</param>
        /// <param name="customerToken">The loggly customerToken that must be part of the Url.</param>
        /// <param name="tag">The tag used in loggly updates. Default is to use the instanceName.</param>
        /// <param name="flattenPayload">Flatten the payload if you want the parameters serialized.</param>
        /// <param name="bufferingInterval">The buffering interval between each batch publishing.</param>
        /// <param name="listenerDisposeTimeout">Defines a timeout interval for the flush operation when the listener is disposed.</param>
        /// <param name="maxBufferSize">The maximum number of entries that can be buffered while it's sending to Loggly before the sink starts dropping entries.
        /// This means that if the timeout period elapses, some event entries will be dropped and not sent to the store. Calling <see cref="IDisposable.Dispose" /> on
        /// the <see cref="EventListener" /> will block until all the entries are flushed or the interval elapses.
        /// If <see langword="null" /> is specified, then the call will block indefinitely until the flush operation finishes.</param>
        /// <returns>
        /// An event listener that uses <see cref="LogglySink" /> to log events.
        /// </returns>
        public static EventListener CreateListener(string instanceName, string connectionString, string customerToken, string tag, bool flattenPayload,
            TimeSpan? bufferingInterval = null, TimeSpan? listenerDisposeTimeout = null, int maxBufferSize = Buffering.DefaultMaxBufferSize)
        {
            var listener = new ObservableEventListener();
            listener.LogToLoggly(instanceName, connectionString, customerToken, tag, flattenPayload, 
                bufferingInterval, listenerDisposeTimeout, maxBufferSize);
            return listener;
        }
    }
}
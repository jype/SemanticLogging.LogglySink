using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Newtonsoft.Json.Linq;
using SemanticLogging.LogglySink.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticLogging.LogglySink
{
    /// <summary>
    /// Sink that asynchronously writes entries to a Loggly server/bulk endpoint.
    /// </summary>
    public class LogglySink : IObserver<EventEntry>, IDisposable
    {
        private const string BulkServiceOperationPath = "/bulk/{0}/tag/{1}/";

        private readonly BufferedEventPublisher<EventEntry> bufferedPublisher;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private readonly string customerToken;
        private readonly string instanceName;
        private readonly string tag;
        private readonly bool flattenPayload;

        private readonly Uri logglyUrl;
        private readonly TimeSpan onCompletedTimeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogglySink"/> class with the specified connection string and table address.
        /// </summary>
        /// <param name="instanceName">The name of the instance originating the entries.</param>
        /// <param name="connectionString">The connection string for the storage account.</param>
        /// <param name="customerToken">The loggly customerToken that must be part of the Url.</param>
        /// <param name="tag">The tag used in loggly updates. Default is to use the instanceName.</param>
        /// <param name="flattenPayload">Flatten the payload if you want the parameters serialized.</param>
        /// <param name="bufferInterval">The buffering interval to wait for events to accumulate before sending them to Loggly.</param>
        /// <param name="bufferingCount">The buffering event entry count to wait before sending events to Loggly </param>
        /// <param name="maxBufferSize">The maximum number of entries that can be buffered while it's sending to Windows Azure Storage before the sink starts dropping entries.</param>
        /// <param name="onCompletedTimeout">Defines a timeout interval for when flushing the entries after an <see cref="OnCompleted"/> call is received and before disposing the sink.
        /// This means that if the timeout period elapses, some event entries will be dropped and not sent to the store. Normally, calling <see cref="IDisposable.Dispose"/> on 
        /// the <see cref="System.Diagnostics.Tracing.EventListener"/> will block until all the entries are flushed or the interval elapses.
        /// If <see langword="null"/> is specified, then the call will block indefinitely until the flush operation finishes.</param>
        public LogglySink(string instanceName, string connectionString, string customerToken, string tag, bool flattenPayload, TimeSpan bufferInterval,
            int bufferingCount, int maxBufferSize, TimeSpan onCompletedTimeout)
        {
            Guard.ArgumentNotNullOrEmpty(instanceName, "instanceName");
            Guard.ArgumentNotNullOrEmpty(connectionString, "connectionString");
            Guard.ArgumentNotNullOrEmpty(customerToken, "index");
            Guard.ArgumentIsValidTimeout(onCompletedTimeout, "onCompletedTimeout");
            Guard.ArgumentGreaterOrEqualThan(0, bufferingCount, "bufferingCount");

            this.onCompletedTimeout = onCompletedTimeout;

            this.instanceName = instanceName;
            this.logglyUrl = new Uri(new Uri(connectionString), string.Format(BulkServiceOperationPath, customerToken, tag));
            this.customerToken = customerToken;
            this.tag = string.IsNullOrWhiteSpace(tag) ? instanceName : tag;
            this.flattenPayload = flattenPayload;
            var sinkId = string.Format(CultureInfo.InvariantCulture, "LogglySink ({0})", instanceName);
            bufferedPublisher = BufferedEventPublisher<EventEntry>.CreateAndStart(sinkId, PublishEventsAsync, bufferInterval,
                bufferingCount, maxBufferSize, cancellationTokenSource.Token);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="LogglySink"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted()
        {
            FlushSafe();
            Dispose();
        }

        /// <summary>
        /// Provides the sink with new data to write.
        /// </summary>
        /// <param name="value">The current entry to write to Windows Azure.</param>
        public void OnNext(EventEntry value)
        {
            if (value == null)
            {
                return;
            }

            bufferedPublisher.TryPost(value);
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
            FlushSafe();
            Dispose();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="LogglySink"/> class.
        /// </summary>
        ~LogglySink()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">A value indicating whether or not the class is disposing.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed",
            MessageId = "cancellationTokenSource", Justification = "Token is canceled")]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancellationTokenSource.Cancel();
                bufferedPublisher.Dispose();
            }
        }

        /// <summary>
        /// Causes the buffer to be written immediately.
        /// </summary>
        /// <returns>The Task that flushes the buffer.</returns>
        public Task FlushAsync()
        {
            return bufferedPublisher.FlushAsync();
        }

        internal async Task<int> PublishEventsAsync(IList<EventEntry> collection)
        {
            HttpClient client = null;

            try
            {
                client = new HttpClient();

                string logMessages;
                using (var serializer = new LogglyEventEntrySerializer(this.instanceName, this.flattenPayload))
                {
                    logMessages = serializer.Serialize(collection);
                }
                var content = new StringContent(logMessages);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.PostAsync(this.logglyUrl, content, cancellationTokenSource.Token).ConfigureAwait(false);

                // If there is an exception
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Check the response for 400 bad request
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var messagesDiscarded = collection.Count();

                        var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        string serverErrorMessage;

                        // Try to parse the exception message
                        try
                        {
                            var errorObject = JObject.Parse(errorContent);
                            serverErrorMessage = errorObject["response"].Value<string>();
                        }
                        catch (Exception)
                        {
                            // If for some reason we cannot extract the server error message log the entire response
                            serverErrorMessage = errorContent;
                        }

                        // We are unable to write the batch of event entries - Possible poison message
                        // I don't like discarding events but we cannot let a single malformed event prevent others from being written
                        // We might want to consider falling back to writing entries individually here
                        SemanticLoggingEventSource.Log.CustomSinkUnhandledFault(
                            string.Format("Discarded message:{0} Server error:{1}", messagesDiscarded, serverErrorMessage));

                        return messagesDiscarded;
                    }

                    // This will leave the messages in the buffer
                    return 0;
                }

                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseObject = JObject.Parse(responseString);

                var responseValue = responseObject["response"].Value<string>();

                // If the response is successful.
                if (responseValue.Equals("ok", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Loggly doesnt return number of entries, so return the same count we send in.
                    return collection.Count();
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                // Although this is generally considered an anti-pattern this is not logged upstream and we have context
                SemanticLoggingEventSource.Log.CustomSinkUnhandledFault(ex.ToString());
                throw;
            }
            finally
            {
                if (client != null)
                {
                    client.Dispose();
                }
            }
        }

        private void FlushSafe()
        {
            try
            {
                FlushAsync().Wait(onCompletedTimeout);
            }
            catch (AggregateException ex)
            {
                // Flush operation will already log errors. Never expose this exception to the observable.
                ex.Handle(e => e is FlushFailedException);
            }
        }
    }
}
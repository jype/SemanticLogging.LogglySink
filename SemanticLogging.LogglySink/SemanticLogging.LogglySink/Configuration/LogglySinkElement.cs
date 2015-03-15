using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using SemanticLogging.LogglySink.Utility;
using System;
using System.Threading;
using System.Xml.Linq;

namespace SemanticLogging.LogglySink
{
    public class LogglySinkElement : ISinkElement
    {
        private readonly XName sinkName = XName.Get("logglySink", "urn:SemanticLogging.LogglySink");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated with Guard class")]
        public bool CanCreateSink(XElement element)
        {
            Guard.ArgumentNotNull(element, "element");

            return element.Name == sinkName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated with Guard class")]
        public IObserver<EventEntry> CreateSink(XElement element)
        {
            Guard.ArgumentNotNull(element, "element");

            return new LogglySink(
                (string)element.Attribute("instanceName"),
                (string)element.Attribute("connectionString"),
                (string)element.Attribute("customerToken"),
                (string)element.Attribute("tag"),
                (bool?)element.Attribute("flattenPayload") ?? false,
                element.Attribute("bufferingIntervalInSeconds").ToTimeSpan() ?? Buffering.DefaultBufferingInterval,
                (int?)element.Attribute("bufferingCount") ?? Buffering.DefaultBufferingCount,
                (int?)element.Attribute("maxBufferSize") ?? Buffering.DefaultMaxBufferSize,
                element.Attribute("bufferingFlushAllTimeoutInSeconds").ToTimeSpan() ?? Timeout.InfiniteTimeSpan);
        }
    }
}
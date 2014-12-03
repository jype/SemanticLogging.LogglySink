﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace SemanticLogging.LogglySink.Utility
{
    /// <summary>
    /// Converts LogglyLogEntry to JSON formatted Loggly bulk service index operation
    /// </summary>
    internal class LogglyEventEntrySerializer : IDisposable
    {
        public readonly string instanceName;

        private JsonWriter writer;

        internal LogglyEventEntrySerializer(string instanceName)
        {
            this.instanceName = instanceName;
        }

        internal string Serialize(IEnumerable<EventEntry> entries)
        {
            if (entries == null)
            {
                return null;
            }

            var sb = new StringBuilder();
            this.writer = new JsonTextWriter(new StringWriter(sb, CultureInfo.InvariantCulture)) { CloseOutput = false, StringEscapeHandling = StringEscapeHandling.EscapeHtml };

            foreach (var entry in entries)
            {
                this.WriteJsonEntry(entry);
            }

            // Close the writer
            this.writer.Close();
            this.writer = null;

            return sb.ToString();
        }

        private void WriteJsonEntry(EventEntry entry)
        {
            this.writer.WriteStartObject();
            WriteValue("EventId", entry.EventId);
            WriteValue("Timestamp", ToJsonIso8601(entry.Timestamp.UtcDateTime));
            WriteValue("Keywords", (long)entry.Schema.Keywords);
            WriteValue("ProviderId", entry.Schema.ProviderId);
            WriteValue("ProviderName", entry.Schema.ProviderName);
            WriteValue("InstanceName", this.instanceName);
            WriteValue("Level", (int)entry.Schema.Level);
            WriteValue("Message", entry.FormattedMessage);
            WriteValue("Opcode", (int)entry.Schema.Opcode);
            WriteValue("Task", (int)entry.Schema.Task);
            WriteValue("Version", entry.Schema.Version);
            WriteValue("ProcessId", entry.ProcessId);
            WriteValue("ThreadId", entry.ThreadId);

            this.writer.WriteEndObject();
            this.writer.WriteWhitespace("\n");
        }

        private void WriteValue(string key, object valueObj)
        {
            this.writer.WritePropertyName(key);
            this.writer.WriteValue(valueObj);
        }

        public string ToJsonIso8601(DateTimeOffset timestamp)
        {
            return timestamp.ToString(@"yyyy-MM-ddTHH\:mm\:ss.ffffffZ");
        }

        public void Dispose()
        {
            if (writer != null)
            {
                this.writer.Close();
                this.writer = null;
            }
        }
    }
}

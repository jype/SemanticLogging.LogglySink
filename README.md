SemanticLogging Loggly Sink
===========================
Sink extension to the Enterprise Library Semantic Logging Application Block. Adding support for the log data management tool Loggly, www.loggly.com.

Documentation
=============
See examples:
* [Example of using the loggly sink in-process.](https://github.com/jype/SemanticLogging.LogglySink/blob/master/SemanticLoggingExample.cs)
* [Out-of-process configuration.](https://github.com/jype/SemanticLogging.LogglySink/blob/master/SemanticLogging.LogglySink/SemanticLogging.LogglySink/SemanticLogging-svc.Out-of-process-Example.xml)
* [XML schema for the above configuration.](https://github.com/jype/SemanticLogging.LogglySink/blob/master/SemanticLogging.LogglySink/SemanticLogging.LogglySink/SemanticLogging.LogglySink.xsd)

References
==========
Read Microsofts documentation on Semantic Logging block here: http://msdn.microsoft.com/en-us/library/dn774980.aspx

Much of the code is based on the documentation and examples found here: http://msdn.microsoft.com/en-us/library/dn775003(v=pandp.20).aspx
Especially I reused the code for making the sink asynchronously and buffered.

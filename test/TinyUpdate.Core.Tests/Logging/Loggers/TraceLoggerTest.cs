﻿using System;
using NUnit.Framework;
using System.Diagnostics;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Core.Tests.Logging.Loggers
{
    public class TraceLoggerTest : ILoggingTestByEvent<TraceLogger, TraceLoggerBuilder>
    {
        private readonly EventTraceListener _traceListener = new EventTraceListener();

        [OneTimeSetUp]
        public void Start()
        {
            Trace.Listeners.Add(_traceListener);
            _traceListener.NewWriteLine += (_, s) => NewOutput?.Invoke(this, s);
        }
        
        [OneTimeTearDown]
        public void Finish()
        {
            Trace.Listeners.Remove(_traceListener);
        }
        
        public TraceLoggerTest() : base(new TraceLoggerBuilder())
        { }
        
        protected override string GetExceptionMessage(Exception e, object?[] props)
        {
            return e.Message + string.Format(ILoggingHelper.GetPropertyDetails(props) ?? string.Empty, props);
        }

        protected override string GetWholeOutput() => _traceListener.ToString();
    }

    public class EventTraceListener : DefaultTraceListener
    {
        public EventHandler<string>? NewWriteLine;
        public EventHandler<string>? NewWrite;
        private string _currentMessages = string.Empty;

        private readonly object _writeLock = new object();
        public override void Write(string? message)
        {
            base.Write(message);
            lock (_writeLock)
            {
                _currentMessages += message;
            }
        }

        public override void WriteLine(string? message)
        {
            Write(message);
            Write(Environment.NewLine);
            
            if (!string.IsNullOrWhiteSpace(message))
            {
                NewWriteLine?.Invoke(this, message);
            }
        }

        public override string ToString()
        {
            lock (_writeLock)
            {
                return _currentMessages;
            }
        }
    }
}
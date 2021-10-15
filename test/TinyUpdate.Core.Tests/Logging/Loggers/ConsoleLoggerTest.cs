using System;
using System.IO;
using NUnit.Framework;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Core.Tests.Logging.Loggers
{
    public class ConsoleLoggerTest : ILoggingTestByEvent<ConsoleLogger, ConsoleLoggerBuilder>
    {
        private EventTextWriter _stream = null!;
        private TextWriter _oldStream = null!;
        
        [OneTimeSetUp]
        public void Start()
        {
            _oldStream = Console.Out;
            _stream = new EventTextWriter(_oldStream, false);
            Console.SetOut(_stream);
            _stream.NewWriteLine += (_, s) => NewOutput?.Invoke(this, s);
        }
        
        [OneTimeTearDown]
        public void Finish()
        {
            Console.SetOut(_oldStream);
            _stream.Dispose();
        }
        
        public ConsoleLoggerTest() : base(new ConsoleLoggerBuilder())
        { }

        protected override string GetExceptionMessage(Exception e, object?[] props)
        {
            return e.Message + string.Format(ILoggingHelper.GetPropertyDetails(props) ?? string.Empty, props);
        }

        protected override string GetWholeOutput() => _stream.ToString();
    }
}
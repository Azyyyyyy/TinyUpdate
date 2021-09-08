using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Core.Tests.Logging.Loggers
{
    public class FileLoggerTest : ILoggingTestByEvent<FileLogger, FileLoggerBuilder>
    {
        private readonly EventTextWriter _stream;
        public FileLoggerTest() : base(new FileLoggerBuilder("logs"))
        {
            _stream = new EventTextWriter(Logger._fileWriter.Value);
            Logger._fileWriter = new Lazy<TextWriter>(() => _stream);
            _stream.NewWriteLine += (_, s) => NewOutput?.Invoke(this, s);
        }

        [OneTimeTearDown]
        public void Finish()
        {
            _stream.Dispose();
        }
        
        protected override string GetExceptionMessage(Exception e, object?[] props)
        {
            return e.Message + string.Format(Logger.GetPropertyDetails(props) ?? string.Empty, props);
        }
        
        protected override string GetWholeOutput() => _stream.ToString();
    }
}
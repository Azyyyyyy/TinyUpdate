using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TinyUpdate.Core.Tests.Logging
{
    public class EventTextWriter : TextWriter
    {
        public override Encoding Encoding { get; } = Encoding.Default;

        public EventHandler<string>? NewWrite;
        public EventHandler<string>? NewWriteLine;
        private string _line = string.Empty;

        private readonly TextWriter? _textWriter;
        public EventTextWriter(TextWriter? textWriter = null)
        {
            _textWriter = textWriter;
        }
        
        public override void Write(string? output)
        {
            if (_textWriter != null)
            {
                _textWriter.Write(output);
            }
            else
            {
                base.Write(output);
            }

            lock (_lock)
            {
                _line += output;
                if (_line.Contains(Environment.NewLine))
                {
                    NewWriteLine?.Invoke(this, _line);
                    _line = string.Empty;
                }
                _lastWrite = DateTime.Now;
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                NewWrite?.Invoke(this, output);
            }
            _ = GiveNewLine();
        }

        private readonly object _lock = new object();
        private DateTime _lastWrite = DateTime.Now;
        //This will give the write as a new line after some time
        //of not getting anything new
        private async Task GiveNewLine()
        {
            DateTime write;
            lock (_lock)
            {
                write = _lastWrite;
            }
            await Task.Delay(250);

            lock (_lock)
            {
                if (write == _lastWrite
                    || !string.IsNullOrWhiteSpace(_line))
                {
                    NewWriteLine?.Invoke(this, _line);
                    _line = string.Empty;
                    _lastWrite = DateTime.Now;
                }
            }
        }

        public override void WriteLine(string? output)
        {
            if (_textWriter != null)
            {
                _textWriter.WriteLine(output);
            }
            else
            {
                base.WriteLine(output);
            }

            lock (_lock)
            {
                if (!string.IsNullOrWhiteSpace(_line))
                {
                    NewWriteLine?.Invoke(this, _line);
                    _line = string.Empty;
                }
                _lastWrite = DateTime.Now;
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                NewWriteLine?.Invoke(this, output);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _textWriter?.Dispose();
        }
    }
}
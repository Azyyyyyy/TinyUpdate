using System;
using System.IO;
using System.Text;

namespace TinyUpdate.Core.Tests.Logging
{
    public class EventTextWriter : TextWriter
    {
        public override Encoding Encoding { get; } = Encoding.Default;

        public EventHandler<string>? NewWriteLine;
        private string _line = string.Empty;
        private string _wholeLine = string.Empty;

        private readonly TextWriter? _textWriter;
        private readonly bool _closeTextWriter;
        public EventTextWriter(TextWriter? textWriter = null, bool closeTextWriter = true)
        {
            _textWriter = textWriter;
            _closeTextWriter = closeTextWriter;
        }
        
        public override void Write(string? output)
        {
            _line += output;
            _wholeLine += output;
                
            if (_textWriter != null)
            {
                _textWriter.Write(output);
            }
            else
            {
                base.Write(output);
            }

            if (output?.EndsWith(string.Concat(CoreNewLine)) ?? false)
            {
                NewWriteLine?.Invoke(this, _line);
                _line = string.Empty;
            }
        }

        public override void WriteLine()
        {
            foreach (var newLineChar in CoreNewLine)
            {
                _line += newLineChar;
                _wholeLine += newLineChar;
            }
                
            NewWriteLine?.Invoke(this, _line);
            _line = string.Empty;
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

            Write(output);
            WriteLine();
        }

        public override string ToString() => _wholeLine;
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_closeTextWriter)
            {
                _textWriter?.Dispose();
            }
        }
    }
}
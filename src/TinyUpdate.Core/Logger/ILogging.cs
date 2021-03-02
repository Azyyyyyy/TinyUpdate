using System;

namespace TinyUpdate.Core.Logger
{
    public interface ILogging
    {
        string Name { get; }

        void Debug(string message, params object[] propertyValues);

        void Information(string message, params object[] propertyValues);

        void Warning(string message, params object[] propertyValues);

        void Error(string message, params object[] propertyValues);

        void Error(Exception e, params object[] propertyValues);
    }
}

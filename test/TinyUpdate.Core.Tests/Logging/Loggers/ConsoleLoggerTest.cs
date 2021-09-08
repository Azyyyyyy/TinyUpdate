﻿using System;using System.IO;using System.Threading.Tasks;using NUnit.Framework;using TinyUpdate.Core.Logging.Loggers;namespace TinyUpdate.Core.Tests.Logging.Loggers{    public class ConsoleLoggerTest : ILoggingTestByEvent<ConsoleLogger, ConsoleLoggerBuilder>    {        private readonly EventTextWriter _stream = new EventTextWriter();        private TextWriter _oldStream = null!;                [OneTimeSetUp]        public void Start()        {            _oldStream = Console.Out;            Console.SetOut(_stream);            _stream.NewWriteLine += (_, s) => NewOutput?.Invoke(this, s);        }                [OneTimeTearDown]        public void Finish()        {            Console.SetOut(_oldStream);            _stream.Dispose();        }                public ConsoleLoggerTest() : base(new ConsoleLoggerBuilder())        { }        protected override Task TestExceptionOverload(object props)        {            return Task.CompletedTask;            //throw new NotImplementedException();        }    }}
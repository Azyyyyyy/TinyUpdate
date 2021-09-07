﻿using System;using System.Threading;using System.Threading.Tasks;using TinyUpdate.Core.Logging;namespace TinyUpdate.Core.Tests.Logging{    public abstract class ILoggingTestByEvent<T> : ILoggingTest<T> where T : ILogging    {        public ILoggingTestByEvent(T logger) : base(logger)        { }        protected override bool DoesLogOutput(Action action)        {            var hasChanged = false;            var tokenS = GetTokenSource();            tokenS.Token.Register(() => hasChanged = true);            action.Invoke();            try            {                if (!tokenS.IsCancellationRequested)                {                    Task.Delay(500, tokenS.Token).Wait(tokenS.Token);                }            }            catch (TaskCanceledException) { }            return hasChanged;        }        private CancellationTokenSource GetTokenSource()        {            var token = new CancellationTokenSource();            HookEvent(token);            return token;        }        public abstract void HookEvent(CancellationTokenSource token);    }}
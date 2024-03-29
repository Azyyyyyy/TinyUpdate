﻿#if Windows || Linux || macOS
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core.Helper;

namespace TinyUpdate.Core.Tests.Helper
{
    public class TaskHelperTest
    {
        [Test]
        public void RunsCorrectFunc()
        {
            var winTask = new Func<int>(() => 1);
            var linuxTask = new Func<int>(() => 2);
            var macOSTask = new Func<int>(() => 3);

            var osResult =
#if Windows
                1;
#elif Linux
                2;
#elif macOS
                3;
#endif
            var result = TaskHelper.RunTaskBasedOnOS(winTask, linuxTask, macOSTask);
            Assert.IsTrue(result == osResult, 
                "We should be reporting '{0}' as the result which is based on OS ('{1}') but reported '{2}'", osResult, OSHelper.ActiveOS, result);
        }
        
        [Test]
        public async Task RunsCorrectTask()
        {
            var winTask = Task.Run(() => 1);
            var linuxTask = Task.Run(() => 2);
            var macOSTask = Task.Run(() => 3);

            var osResult =
#if Windows
                1;
#elif Linux
                2;
#elif macOS
                3;
#endif
            var result = await TaskHelper.RunTaskBasedOnOSAsync(winTask, linuxTask, macOSTask);
            Assert.IsTrue(result == osResult, 
                "We should be reporting '{0}' as the result which is based on OS ('{1}') but reported '{2}'", osResult, OSHelper.ActiveOS, result);
        }
    }
}
#endif
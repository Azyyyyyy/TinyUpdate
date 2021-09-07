using System;
using System.Linq;
using NUnit.Framework;
using TinyUpdate.Core.Extensions;

namespace TinyUpdate.Core.Tests.Extensions
{
    //TODO: Check this to see if it doesn't check a certain way
    public class EnumerableExtTest
    {
        private static EnumerableExtTestData[] _testData =
        {
            new(new []
            {
                "1", "2", "3"
            }, 1),
            new(new []
            {
                "One", "Two", "Three"
            }, 2),
            new(new []
            {
                "hdsfkjhdsfkjh", "ruirbgmdnbf", "wuhernmcs;foif", "djdjfd993jz,chkz"
            },2),
            new(Array.Empty<object?>(), -1),
        };
        
        [Test]
        [TestCaseSource(nameof(_testData))]
        public void CorrectIndexOf(EnumerableExtTestData testData)
        {
            var index = testData.Data.IndexOf(s => s == testData.DataToFind);
            Assert.True(index == testData.ExpectedInt, "Index wasn't {0}, should of been {1}", testData.ExpectedInt, index);
        }
    }

    public class EnumerableExtTestData
    {
        public EnumerableExtTestData(object?[] data, int expectedInt, object? dataToFind = null)
        {
            Data = data;
            ExpectedInt = expectedInt;
            DataToFind = dataToFind ?? data.ElementAtOrDefault(expectedInt);
        }
        
        public object?[] Data { get; }

        public object? DataToFind { get; }

        public int ExpectedInt { get; }
    }
}
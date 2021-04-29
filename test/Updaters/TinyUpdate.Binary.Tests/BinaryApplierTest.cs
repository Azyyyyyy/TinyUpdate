using NUnit.Framework;
using TinyUpdate.Test.Update;

namespace TinyUpdate.Binary.Tests
{
    /// <summary>
    /// Tests <see cref="BinaryApplier"/>
    /// </summary>
    [Ignore("Need to remake in a way that works everywhere")]
    public class BinaryApplierTest : IUpdateApplierTest
    {
        public BinaryApplierTest() : base(new BinaryApplier())
        { }
    }
}
using NUnit.Framework;
using TinyUpdate.Test.Update;

namespace TinyUpdate.Binary.Tests
{
    [Ignore("Need to remake in a way that works everywhere")]
    public class BinaryCreatorTest : IUpdateCreatorTest
    {
        public BinaryCreatorTest() : base(new BinaryCreator())
        { }
    }
}
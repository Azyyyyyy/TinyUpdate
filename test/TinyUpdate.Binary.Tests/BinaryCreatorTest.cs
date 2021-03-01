using TinyUpdate.Test;

namespace TinyUpdate.Binary.Tests
{
    public class BinaryCreatorTest : IUpdateCreatorTest
    {
        public BinaryCreatorTest() : base(new BinaryCreator())
        { }
    }
}
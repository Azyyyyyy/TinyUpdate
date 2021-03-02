using TinyUpdate.Test.Update;

namespace TinyUpdate.Binary.Tests
{
    /// <summary>
    /// Tests <see cref="BinaryApplier"/>
    /// </summary>
    public class BinaryApplierTest : IUpdateApplierTest
    {
        public BinaryApplierTest() : base(new BinaryApplier())
        { }
    }
}
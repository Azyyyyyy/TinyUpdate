
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Tests.Attributes;

namespace TinyUpdate.Core.Tests.Abstract;

public abstract class HasherCan(IHasher hasher)
{
    private IHasher Hasher { get; } = hasher;

    [ExternalTest]
    public void CompareCorrectly_Stream(bool expectedStatus, string expectedHash, Stream streamToHash)
    {
        var hash = Hasher.CompareHash(streamToHash, expectedHash);
        Assert.That(hash, Is.EqualTo(expectedStatus));
    }
    
    [ExternalTest]
    public void CompareCorrectly_Array(bool expectedStatus, string expectedHash, byte[] arrayToHash)
    {
        var hash = Hasher.CompareHash(arrayToHash, expectedHash);
        Assert.That(hash, Is.EqualTo(expectedStatus));
    }
    
    [ExternalTest]
    public void ReturnCorrectHash_Stream(string expectedHash, Stream streamToHash)
    {
        var hash = Hasher.HashData(streamToHash);
        Assert.That(hash, Is.EqualTo(expectedHash));
    }
    
    [ExternalTest]
    public void ReturnCorrectHash_Array(string expectedHash, byte[] arrayToHash)
    {
        var hash = Hasher.HashData(arrayToHash);
        Assert.That(hash, Is.EqualTo(expectedHash));
    }

    [ExternalTest]
    public void ValidateHashCorrectly(string hash, bool expectedValidation)
    {
        Assert.That(Hasher.IsValidHash(hash), Is.EqualTo(expectedValidation));
    }
}
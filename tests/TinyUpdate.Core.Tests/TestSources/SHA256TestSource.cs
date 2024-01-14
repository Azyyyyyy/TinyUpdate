namespace TinyUpdate.Core.Tests.TestSources;

public static class SHA256TestSource
{
    private const string BadHash = "E3B0298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";
    
    public static IEnumerable<TestCaseData> CompareStreamCorrectly()
    {
        var testCases = 
            CreateTestData(true, true)
                .Append(GetHashTestData(BadHash, true, true, false))
                .Select(x => new TestCaseData(x));

        return CreateCompareTestData(testCases);
    }

    public static IEnumerable<TestCaseData> CompareArrayCorrectly()
    {
        var testCases = CreateTestData(false, true)
            .Append(GetHashTestData(BadHash, false, true, false))
            .Select(x => new TestCaseData(x));

        return CreateCompareTestData(testCases);
    }

    public static IEnumerable<TestCaseData> ReturnCorrectHash_Stream() 
        => CreateTestData(true, false).Select(x => new TestCaseData(x));
    
    public static IEnumerable<TestCaseData> ReturnCorrectHash_Array() 
        => CreateTestData(false, false).Select(x => new TestCaseData(x));

    public static IEnumerable<TestCaseData> ValidateHashCorrectly()
    {
        yield return new TestCaseData("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", true);
        yield return new TestCaseData("1d1976D40aEbF07749A6e10450DB2Cc7103073d6FE5304bA5F4A1533ED0eF786", true);
        yield return new TestCaseData("d27e597613de5f4948f0785afe0fc1a959694f6cc2e0bdeb68e845785d80d17c", true);
        yield return new TestCaseData(BadHash, false);
    }
    
    private static IEnumerable<TestCaseData> CreateCompareTestData(IEnumerable<TestCaseData> testCases)
    {
        var count = 0;
        foreach (var testCase in testCases)
        {
            if (count == 2)
            {
                testCase.Arguments[0] = testCase.OriginalArguments[0] = false;
                testCase.Arguments[1] = testCase.OriginalArguments[1] = "D27E597613DE5F4948F0785AFE0FC1A959694F6CC2E0BDEB68E845785D80D17C";
            }
            
            yield return testCase;
            count++;
        }
    }
    
    private static IEnumerable<object[]> CreateTestData(bool createStream, bool addExpectedHash)
    {
        yield return GetHashTestData("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", createStream, addExpectedHash); //Nothing
        yield return GetHashTestData("0056767DA22B0BA1658B391ED9E778BDB6FE60F476F93DBBF3B562552912DAB9", createStream, addExpectedHash);
        yield return GetHashTestData("1D1976D40AEBF07749A6E10450DB2CC7103073D6FE5304BA5F4A1533ED0EF786", createStream, addExpectedHash);
        yield return GetHashTestData("D27E597613DE5F4948F0785AFE0FC1A959694F6CC2E0BDEB68E845785D80D17C", createStream, addExpectedHash);
    }

    private static object[] GetHashTestData(string expectedHash, bool createStream, bool addExpectedHash, bool expectedResult = true)
    {
        var fileSystem = Functions.SetupMockFileSystem();
        var filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Assets", "SHA256", expectedHash);

        if (addExpectedHash)
        {
            return createStream
                ? [expectedResult, expectedHash, fileSystem.File.OpenRead(filePath)]
                : [expectedResult, expectedHash, fileSystem.File.ReadAllBytes(filePath)];
        }

        return createStream 
            ? [expectedHash, fileSystem.File.OpenRead(filePath)]
            : [expectedHash, fileSystem.File.ReadAllBytes(filePath)];
    }
}
using TinyUpdate.Core.Services;
using TinyUpdate.Core.Tests.Abstract;
using TinyUpdate.Core.Tests.Attributes;
using TinyUpdate.Core.Tests.TestSources;

namespace TinyUpdate.Core.Tests;

[DynamicTestCaseSource(nameof(ValidateHashCorrectly), typeof(SHA256TestSource), nameof(SHA256TestSource.ValidateHashCorrectly))]
[DynamicTestCaseSource(nameof(CompareCorrectly_Array), typeof(SHA256TestSource), nameof(SHA256TestSource.CompareArrayCorrectly))]
[DynamicTestCaseSource(nameof(CompareCorrectly_Stream), typeof(SHA256TestSource), nameof(SHA256TestSource.CompareStreamCorrectly))]
[DynamicTestCaseSource(nameof(ReturnCorrectHash_Array), typeof(SHA256TestSource), nameof(SHA256TestSource.ReturnCorrectHash_Array))]
[DynamicTestCaseSource(nameof(ReturnCorrectHash_Stream), typeof(SHA256TestSource), nameof(SHA256TestSource.ReturnCorrectHash_Stream))]
public class SHA256Tests() : HasherCan(SHA256.Instance);
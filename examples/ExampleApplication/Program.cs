// See https://aka.ms/new-console-template for more information

//TODO: When local client is made, add it to this for testing

using TinyUpdate.Core;

var versionDetails = VersionHelper.GetVersionDetails();

Console.WriteLine("Hello, Updates!");
Console.WriteLine("In Test Runner?: " + Testing.InTestRunner);
Console.WriteLine("Version: " + versionDetails.Version.ToString());
Console.WriteLine("SourceRevisionId: " + versionDetails.SourceRevisionId.ToString());
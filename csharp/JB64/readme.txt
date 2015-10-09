*************************** C# JSON-Base64 Library ***************************

This directory contains a public domain implementation of JSON-Base64 in C#
and the relevant test suite.  The .NET Solution was built in Visual Studio
2012 and targets version 3.5 of the .NET Framework.  However, the library
probably works on older versions of both Visual Studio and .NET just fine.

The library is self-contained in the 'JB64\JB64.cs' file and should be able to
be dropped into your project as-is.  There is a dependency on the Json.NET
NuGet package.


To build the test suite:

Open the 'JB64.sln' solution file in Visual Studio.
Install the NuGet package Json.NET:
  Project -> Manage NuGet Packages -> Locate and Install "Json.NET".
  Close the window.
'Build' the solution.
'Debug' the solution.

All tests should [PASS].

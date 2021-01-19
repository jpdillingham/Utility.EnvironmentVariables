# Utility.EnvironmentVariables

[![NuGet version](https://img.shields.io/nuget/v/Utility.CommandLine.Arguments.svg)](https://www.nuget.org/packages/Utility.CommandLine.Arguments/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/jpdillingham/Utility.CommandLine.Arguments/blob/master/LICENSE)

A C# .NET Class Library containing tools for populating properties from environment variables.

## Why?

It fits well with [Utility.CommandLine.Arguments](https://github.com/jpdillingham/Utility.CommandLine.Arguments) and saves some tedious boilerplate around parsing values.


## Installation

Install from the NuGet gallery GUI or with the Package Manager Console using the following command:

```Install-Package Utility.EnvironmentVariables```

The code is also designed to be incorporated into your project as a single source file (EnvironmentVariables.cs).

## Quick Start

Create private static properties in the class containing your ```Main()``` and mark them with the ```EnvironmentVariable``` attribute, assigning the name of the environment variable from which to source the value.  Invoke
the ```EnvironmentVariables.Populate()``` method within ```Main()```, then implement the rest of your logic.  

The library will populate your properties with the values specified in the referenced environment variables.

```
export BOOL=true
export FLOAT=1.23
export COMMA_SEPARATED_LIST="some,values,here,  spaces are trimmed"
```

```c#
internal class Program
{
    [EnvironmentVariable("BOOL")]
    private static bool Bool { get; set; }

    [EnvironmentVariable("FLOAT")]
    private static double Double { get; set; }

    [EnvironmentVariable("COMMA_SEPARATED_LIST")]
    private static string[] String { get; set; }
        
    private static void Main(string[] args)
    {
        Arguments.Populate();

        Console.WriteLine("Bool: " + Bool);
        Console.WriteLine("Double: " + Double);

        foreach (string s in String)
        {
            Console.WriteLine("String: " + s);
        }
    }
}
```
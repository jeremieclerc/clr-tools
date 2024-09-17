
# Generate Binary CLI Tool Documentation

## Overview

`Generate Binary` is a CLI tool that allows users to generate a SQL script for installing CLR (Common Language Runtime) assemblies on SQL Server. This script automates the process of creating SQL CLR functions and assemblies from a .NET DLL. The generated SQL script includes the binary content of the DLL and creates the necessary SQL function(s) to interact with it.

---

## Table of Contents

- [Installation](#installation)
- [Usage](#usage)
- [Command Line Arguments](#command-line-arguments)
- [Features](#features)
- [Example Usage](#example-usage)
- [How It Works](#how-it-works)
- [Troubleshooting](#troubleshooting)

---

## Installation

Before using the CLI tool, ensure you have the [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine.

To install the necessary dependencies, navigate to your project directory and run:

```bash
dotnet restore
```

Build the project using:

```bash
dotnet build
```

---

## Usage

To generate the SQL script for installing the assembly, run the following command in your terminal:

```bash
dotnet run -- <inputFolder> <outputFolder> <dllName> [--functionName]
```

### Arguments:

- `<inputFolder>`: The folder that contains the `.dll` file.
- `<outputFolder>`: The folder where the generated SQL script will be saved.
- `<dllName>`: The name of the DLL file (e.g., `AssemblyHttp`).
- `--functionName`: (Optional) The name of the SQL CLR function to be created. Defaults to the DLL name prefixed by `Function`.

---

## Command Line Arguments

- **inputFolder**: *(Required)* The path to the folder containing the DLL file to convert into a binary format for SQL Server.
- **outputFolder**: *(Required)* The path where the generated SQL file will be saved.
- **dllName**: *(Required)* The name of the DLL file (without the `.dll` extension).
- **--functionName**: *(Optional)* The name of the SQL CLR function to be created in SQL Server. Defaults to the DLL name prefixed with `Function`.

---

## Features

- Converts a .NET DLL into a binary format suitable for SQL Server.
- Generates a SQL script for installing the assembly as a SQL CLR function.
- Automatically handles `NVARCHAR(MAX)`, `INT`, and other SQL types based on the DLL's method signatures.
- Supports both generic and non-generic collections for SQL Table-Valued Functions (TVFs).
- Allows for optional custom naming of SQL functions.

---

## Example Usage

Here’s an example of how to use the `Generate Binary` tool:

```bash
dotnet run -- "C:\MyAssemblies" "C:\SQLScripts" "AssemblyHttp" --functionName "CustomHttpFunction"
```

This will generate a SQL file at `C:\SQLScripts\AssemblyHttp_YYYY-MM-DD.sql`, containing the necessary script to install the assembly and create the function `dbo.CustomHttpFunction` in SQL Server.

If you omit the `--functionName` option, the SQL function will be named `dbo.FunctionAssemblyHttp` by default.

---

## How It Works

1. **Reading the DLL**: The tool reads the binary content of the specified `.dll` file from the `inputFolder`.
2. **Generating Binary String**: The binary content is converted into a hexadecimal string that can be used in SQL Server.
3. **SQL Script Creation**: The tool generates a SQL script to:
   - Install the DLL as a trusted assembly in SQL Server.
   - Create SQL CLR functions based on the methods defined in the assembly.
   - Drop any existing functions or assemblies with the same name.
4. **Custom SQL Function Name**: If a `--functionName` is provided, the tool creates the SQL function with the given name.

---

## Troubleshooting

### Common Errors

- **Error: DLL file not found**: 
  - Make sure the path to the DLL is correct and the file exists in the specified `inputFolder`.
  
- **Cannot determine the element type of the collection**:
  - Ensure the method returns an enumerable or specify the table structure in your `SqlFunctionAttribute`.

- **Assembly already exists in SQL Server**:
  - The generated SQL script automatically drops existing assemblies and functions before recreating them.

If you encounter any issues, please check that the `.NET SDK` is installed and ensure all command line arguments are correct.

---

## License

This tool is distributed under the MIT License.


# JSON Parser code snippet documentation

## Overview

`JSON Parser` was created to give an easy to install solution to parse simple JSON objects inside SQL Server CLR, which don't have any JSON parsing librairies compatible.

## Table of Contents

- [Installation](#installation)
- [Example Usage](#example-usage)
- [Features](#features)
- [Troubleshooting](#troubleshooting)

## Installation

Just copy and paste the JsonParser.cs file inside your CLR to have a static JsonParser class at your disposal.

## Example Usage

```cs
Dictionary<string, string> parsedJson = JsonParser.ParseJson(jsonString);
```

## Features

- Parses JSON strings into key/value dictionnary
- Handles basic data types such as booleans, numbers and strings

## Limitations

- Cannot handle the parsing of nested JSON
- Cannot parse dates and arrays

## Troubleshooting

- `ArgumentNullException`: Thrown when you are trying to parse `null` value
- `FormatException`: Thrown when you are trying to parse an invalid JSON file (or at least one that can't be parsed by this simple parser)

## License

This tool is distributed under the MIT License.

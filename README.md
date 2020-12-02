# gmic-sharp-pdn

Extends [gmic-sharp](https://github.com/0xC0000054/gmic-sharp) for use with [Paint.NET](https://www.getpaint.net) Effect plugins.

## Dependencies

This repository depends on libraries from the following repositories:

[gmic-sharp](https://github.com/0xC0000054/gmic-sharp), provides the .NET G'MIC wrapper that this library uses.   
[gmic-sharp-native](https://github.com/0xC0000054/gmic-sharp-native), provides the native interface between gmic-sharp and [libgmic](https://github.com/dtschump/gmic).

## Example Effect plugin

[gmic-sharp-pdn-example](https://github.com/0xC0000054/gmic-sharp-pdn-example), a Paint.NET Effect plugin that wraps the G'MIC water command.

## License

This project is licensed under the terms of the MIT License.   
See [License.txt](License.txt) for more information.

### Native libraries

The gmic-sharp native libraries (GmicSharpNative*) are dual-licensed under the terms of the either the [CeCILL v2.1](https://cecill.info/licences/Licence_CeCILL_V2.1-en.html) (GPL-compatible) or [CeCILL-C v1](https://cecill.info/licences/Licence_CeCILL-C_V1-en.html) (similar to the LGPL).  
Pick the one you want to use.

This was done to match the licenses used by [libgmic](https://github.com/dtschump/gmic).

# Source code

## Prerequisites

* Visual Studio 2019
* Paint.NET 4.2.14 or later
* gmic-sharp
* [Sandcastle Help File Builder](https://github.com/EWSoftware/SHFB) is required to build the documentation	

## Building the plugin

* Open the solution
* Change the PaintDotNet references in the GmicSharpPdn project to match your Paint.NET install location
* Change the GmicSharp reference in the GmicSharpPdn project to match your GmicSharp install location
* Update the post build events to copy the build output to the Paint.NET FileTypes folder
* Build the solution


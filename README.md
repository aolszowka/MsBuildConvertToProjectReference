# MsBuildConvertToProjectReference
Utility Program to Convert MsBuild Reference (Assembly Reference) Tags to ProjectReference Tags

## Background
ProjectReferences are very expensive/taxing on Visual Studio, in deep/complex Dependency Trees in the past Visual Studio (Prior to 2015+) would simply be unable to load such Solutions. A lot of times you would either run out of memory or it would just simply be too slow to be usable.

Therefore your only options were:

1. Reduce the complexity of your dependency tree <- This is the correct solution
2. Convert some of these ProjectReferences to "Assembly References" (The `<Reference>` Element)

To learn more about `<ProjectReference>` and `<Reference>` ("Assembly Reference") See [Microsoft Docs: Common MSBuild project items](https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items?view=vs-2017)

If you chose to go down Path 2 ("Assembly References") it introduced all types of wild and complex issues (Intellisense stops working as expected, Version/"DLL Hell"/Difficulty Stepping into the Debugger) that resulted in making Development Hard for your Developers, whereas if you could just use Project References life would be much simpler.

Microsoft has made incredible strides in recent years to improve the performance of loads of large solutions, which means that Project References are again viable for extremely large solutions (This has been tested as performant up to around 600 projects).

## When To Use This Tool
Assume in the story above you foolishly chose Path 2. In the brave new world of Visual Studio you are now attempting to correct this mistake. Unfortunately you have several hundred (if not thousands) of Projects and you have no idea where they all are.

The Microsoft Solution is to open each one of these projects individually and correct them. This is time consuming, tedious, and error prone and is guaranteed to result in failure.

Furthermore the "Microsoft Solution" does not address "code rot" from unsuspecting Developers.

Therefore this tool can be used to quickly address the above scenario as well as be put on a Tattler Process/Commit Hook to prevent "code rot".

## Usage
```text
Usage: MsBuildConvertToProjectReference C:\DirectoryWithProjects -ld=C:\lookupDir [-ld=C:\lookupDir2] [-validate]

Scans given directory for MSBuild Style Projects and Converts their References
to ProjectReferences if the Project was found in the Lookup Directories.

When ran with --validate it performs the above operation as
described but instead the return code represents the number of projects that
would be modified.

Arguments:

               <>            A Directory to scan for Project Files
      --validate             Indicates if this tool should only be run in
                               validation mode
      --lookupdirectory, --ld=VALUE
                             One or more directories to use to find projects
  -?, -h, --help             Show this message and exit
```

### Example
```text
MsBuildConvertToProjectReference R:\Trunk\Dotnet --lookupDirectory=R:\Trunk\Dotnet -ld=R:\Trunk\ExternalLibs --lookupdirectory=R:\Trunk\LegacyLibs
```

Will attempt to convert all `<Reference>` Elements in any MSBuild Project File Found in `R:\Trunk\Dotnet` to `<ProjectReference>` Elements if the Reference is found (via `AssemblyName`) in `R:\Trunk\Dotnet`, `R:\Trunk\ExternalLibs`, or `R:\Trunk\LegacyLibs`

## Assumptions/Gotchas
This tool assumes that the `AssemblyName` is unique throughout all given `--lookupdirectory` arguments and that any assembly with the same name is the one you want to add reference to.

You are allowed to have multiple `--lookupdirectory` arguments (you can also mix and match the shorthand `-ld=`), and the tool will happily set relative paths for `<ProjectReference>` to any location you want. However you need to be careful that this is what you really want (consider if you had a Feature Branch or accidently had the code on another drive).

This tool does not account for differences in `AssemblyVersion` Currently (See Hacking#AssemblyVersion).

## Hacking
### Supported Files
The most likely change you will want to make is changing the supported project files. You need to be careful here, as the code is currently written it assumes that `<AssemblyName>` Exists as a Property (See [Microsoft Docs: Common MSBuild project properties](https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties?view=vs-2017)) This is used as a Key in a lookup dictionary for replacement of `<Reference>` Tags and is case insenstive.

See ConvertToProjectReference.GetProjectsInDirectory(string) for the place to modify this.

### AssemblyVersion Support
As written this tool does not account for difference in assembly version; consider the following:

ProjectA.csproj
```xml
    <Reference Include="MyAssembly, Version=4.0.0.0">
```

ProjectB.csproj
```xml
    <Reference Include="MyAssembly, Version=5.0.0.0">
```

Now consider this folder structure:

```
MyAssembly
    ├───4.0.0.0
    │       MyAssembly.csproj
    │
    └───5.0.0.0
            MyAssembly.csproj
```

As the tool is written today this will error (because it violates the Single `AssemblyName` rule) however a more advanced version of this tool would take these versions into account.

## Contributing
Pull requests and bug reports are welcomed so long as they are MIT Licensed.

## License
This tool is MIT Licensed.

## Third Party Licenses
This project uses other open source contributions see [LICENSES.md](LICENSES.md) for a comprehensive listing.

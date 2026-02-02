
# Releasing a New Version

## Preconditions

- Be aware of the changes since the last release and define the new version number accordingly.


## Build & Release

To release a new library version to nuget.org, run the following commands in the project root:

```bash
VERSION= # set version here
dotnet cake build.cake --target Lib:Build --lib-version $VERSION
```

Now run [push.sh](push.sh) and specify the desired packages to upload.

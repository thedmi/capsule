#!/bin/bash

# Pushes built nuget packages to nuget.org. 
#
# Usage:
#   NUGET_API_KEY=the_key ./push.sh package.nupkg
#

dotnet nuget push -s nuget.org -k $NUGET_API_KEY $@

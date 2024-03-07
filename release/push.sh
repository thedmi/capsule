#!/bin/bash

# Pushes all built nuget packages to nuget.org. 
#
# Usage:
#   NUGET_API_KEY=the_key ./push.sh
#

SCRIPT_DIR=$(dirname "$0")
dotnet nuget push -s nuget.org -k $NUGET_API_KEY $SCRIPT_DIR/lib/*.nupkg

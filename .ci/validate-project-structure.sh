#!/bin/bash

find . -name "*.csproj" | xargs grep -H "<Version>"
if [ $? -eq 0 ]; then
    echo '<Version> tag found in csproj file. Please remove <Version> and replace with <VersionPrefix>.'
    exit 1
fi

exit 0
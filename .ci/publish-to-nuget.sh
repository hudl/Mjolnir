#!/bin/bash
# Usage ./publish-to-nuget.sh <nuget url> <nuget api key>

NUGET_URL=$1
NUGET_API_KEY=$2

cd /out

NUGET_PACKAGES=`find "$(pwd -P)" -name "*.nupkg"`

while IFS= read -r NUGET_PACKAGE; do
    echo ""
    echo "Uploading package $NUGET_PACKAGE to nuget."
    echo ""
    
    COMMAND_OUTPUT=`dotnet nuget push $NUGET_PACKAGE --skip-duplicate --source $NUGET_URL --api-key $NUGET_API_KEY`
    echo "$COMMAND_OUTPUT"
done <<< "$NUGET_PACKAGES"
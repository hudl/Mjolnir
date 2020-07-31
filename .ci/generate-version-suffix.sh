#!/bin/bash

# NuGet has a limit of 20 chars for its prerelease strings, so the combined length of these
# shouldn't exceed that.
LOCAL_BRANCH=`git rev-parse --abbrev-ref HEAD | sed 's/[^a-zA-Z0-9]//g' | cut -c -14`
GIT_COMMIT_SHA1=`git rev-parse --short=6 HEAD | sed 's/[^a-zA-Z0-9]//g'`


if [[ -z "${LOCAL_BRANCH}" ]] || [[ -z "${GIT_COMMIT_SHA1}" ]]; then
    echo "Empty branch or git sha1, branch=$LOCAL_BRANCH commit=$GIT_COMMIT_SHA1" >&2
    echo "BROKEN_PUBLISH"
    exit 1
fi

# If our "publish official" flag is true and we're on master, don't output a prerelease string.
if [ $# -gt 0 ] && [ "$1" = "true" ] && [ "$LOCAL_BRANCH" = "master" ]; then
    exit 0
fi
if [ -z "${BUILD_NUMBER}" ]
    echo "--version-suffix $LOCAL_BRANCH$GIT_COMMIT_SHA1"
else
    echo "--version-suffix $LOCAL_BRANCH$BUILD_NUMBER"
fi
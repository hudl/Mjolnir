#!/bin/bash
# This script is meant to be run from a continuous integration build step.
set -e

function usage {
    echo "Usage: $0 [-op] -i image_prefix -t image_tag [-k nuget_api_key]"
    echo "  -o                This is an official build - if master, no prerelease version will be used; should only be passed by the CI build step itself"
    echo "  -p                Publish the NuGet package that gets built; usually skipped when no source code changed"
    echo "  -i image_prefix   A string specific to this library, e.g. 'dotnet-mylibrary'"
    echo "  -t image_tag      A value unique per build, like the CI build number"
    echo "  -k nuget_api_key  NuGet API key used for publishing; required if -p is used, otherwise optional"
    echo ""
    echo "Example (no publish):   $0 -i dotnet-mylibrary -t 1234"
    echo "Example (with publish): $0 -p -i dotnet-mylibrary -t 1234 -k ABCDE12345"
}

function main {
    parse_options "$@"
    REPOSITORY_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )"/.. >/dev/null && pwd )"

    trap exithandler EXIT

    set -x
    docker build -f $REPOSITORY_DIR/.ci/Dockerfile.ci --target build --build-arg PUBLISH_OFFICIAL_VERSION_IF_MASTER=$IS_OFFICIAL -t $IMAGE_PREFIX-build:$IMAGE_TAG $REPOSITORY_DIR
    mkdir -p $(pwd)/out
    docker run -v $(pwd)/out:/mount --rm $IMAGE_PREFIX-build:$IMAGE_TAG

    if [[ "$DO_PUBLISH" = true ]]; then
        docker build -f $REPOSITORY_DIR/.ci/Dockerfile.ci --target publish --cache-from $IMAGE_PREFIX-build:$IMAGE_TAG --build-arg PUBLISH_OFFICIAL_VERSION_IF_MASTER=$IS_OFFICIAL -t $IMAGE_PREFIX-publish:$IMAGE_TAG $REPOSITORY_DIR
        docker run -e NUGET_API_KEY=$NUGET_API_KEY --rm $IMAGE_PREFIX-publish:$IMAGE_TAG
    else
        echo "Skipping NuGet publish, -p option was not used"
    fi
}

function exithandler {
    docker rmi $(docker images --filter=reference="$IMAGE_PREFIX-build:$IMAGE_TAG" -q) 2>/dev/null || true
    docker rmi $(docker images --filter=reference="$IMAGE_PREFIX-publish:$IMAGE_TAG" -q) 2>/dev/null || true
    set +x
}

function parse_options {
    IS_OFFICIAL="false"
    DO_PUBLISH="false"
    IMAGE_PREFIX=""
    IMAGE_TAG=""
    NUGET_API_KEY=""

    if [[ $# = 0 ]]; then usage_error; fi
    while getopts 'opd:i:t:k:' flag; do
        LOWER_OPT_ARG=$(echo $OPTARG | tr '[:upper:]' '[:lower:]')
        case "${flag}" in
            o) IS_OFFICIAL="true" ;;
            p) DO_PUBLISH="true" ;;
            i) IMAGE_PREFIX="${LOWER_OPT_ARG}"; if [[ $IMAGE_PREFIX = -* ]]; then usage_error "Missing image_prefix argument for -i"; fi ;;
            t) IMAGE_TAG="${LOWER_OPT_ARG}"; if [[ $IMAGE_TAG = -* ]]; then usage_error "Missing image_tag argument for -t"; fi ;;
            k) NUGET_API_KEY="${OPTARG}"; if [[ $NUGET_API_KEY = -* ]]; then usage_error "Missing nuget_api_key argument for -k"; fi ;;
            *) usage; exit 1 ;;
        esac
    done
echo "Blah" | awk '{print tolower($0)}'
    echo "REPOSITORY_DIR=$REPOSITORY_DIR"
    echo "IS_OFFICIAL=$IS_OFFICIAL"
    echo "DO_PUBLISH=$DO_PUBLISH"
    echo "IMAGE_PREFIX=$IMAGE_PREFIX"
    echo "IMAGE_TAG=$IMAGE_TAG"
    if [[ ! -z "$NUGET_API_KEY" ]]; then
        echo "NUGET_API_KEY has a non-empty value"
    fi

    if [[ -z "$IMAGE_PREFIX" ]]; then usage_error "Missing required option -i image_prefix"; fi
    if [[ -z "$IMAGE_TAG" ]]; then usage_error "Missing required option -t image_tag"; fi
    if [[ "$DO_PUBLISH" = "true" && -z "$NUGET_API_KEY" ]]; then
        usage_error "Publish option (-p) was passed without Nuget API Key option (-k)"
    fi
}

function usage_error {
    if [[ $# -gt 0 ]]; then
        echo "Error: $1" >&2
        echo ""
    fi
    usage
    exit 1
}

main "$@"
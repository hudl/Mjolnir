#!/bin/bash
# This script is meant to be run from a continuous integration build step.
set -e

function usage {
    echo "Usage: $0 [-op] -i image_prefix -t image_tag [-k nuget_api_key]"
    echo "  -o                This is an official build - if master, no prerelease version will be used; should only be passed by the CI build step itself"
    echo "  -i image_prefix   A string specific to this library, e.g. 'dotnet-mylibrary'"
    echo "  -t image_tag      A value unique per build, like the CI build number"
    echo ""
    echo "Example:   $0 -i dotnet-mylibrary -t 1234"
}

function main {
    parse_options "$@"
    REPOSITORY_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )"/.. >/dev/null && pwd )"

    trap exithandler EXIT

    set -x
    docker build -f $REPOSITORY_DIR/.ci/Dockerfile.ci --target build --build-arg PUBLISH_OFFICIAL_VERSION_IF_MASTER=$IS_OFFICIAL -t $IMAGE_PREFIX-build:$IMAGE_TAG $REPOSITORY_DIR
    mkdir -p $(pwd)/out
    docker run -v $(pwd)/out:/mount --rm $IMAGE_PREFIX-build:$IMAGE_TAG
}

function exithandler {
    docker rmi $(docker images --filter=reference="$IMAGE_PREFIX-build:$IMAGE_TAG" -q) 2>/dev/null || true
    docker rmi $(docker images --filter=reference="$IMAGE_PREFIX-publish:$IMAGE_TAG" -q) 2>/dev/null || true
    set +x
}

function parse_options {
    IS_OFFICIAL="false"
    IMAGE_PREFIX=""
    IMAGE_TAG=""

    if [[ $# = 0 ]]; then usage_error; fi
    while getopts 'opd:i:t:k:' flag; do
        case "${flag}" in
            o) IS_OFFICIAL="true" ;;
            i) IMAGE_PREFIX="${OPTARG}"; if [[ $IMAGE_PREFIX = -* ]]; then usage_error "Missing image_prefix argument for -i"; fi ;;
            t) IMAGE_TAG="${OPTARG}"; if [[ $IMAGE_TAG = -* ]]; then usage_error "Missing image_tag argument for -t"; fi ;;
            *) usage; exit 1 ;;
        esac
    done

    echo "REPOSITORY_DIR=$REPOSITORY_DIR"
    echo "IS_OFFICIAL=$IS_OFFICIAL"
    echo "IMAGE_PREFIX=$IMAGE_PREFIX"
    echo "IMAGE_TAG=$IMAGE_TAG"

    if [[ -z "$IMAGE_PREFIX" ]]; then usage_error "Missing required option -i image_prefix"; fi
    if [[ -z "$IMAGE_TAG" ]]; then usage_error "Missing required option -t image_tag"; fi
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

#!/usr/bin/env bash
set -euo pipefail

IMAGE_REPO_PREFIX=$1
IMAGE_TAG_VERSION=$2
IMAGE_TAG="$IMAGE_REPO_PREFIX-test:$IMAGE_TAG_VERSION"

REPOSITORY_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )"/.. >/dev/null && pwd )"
OUT_DIR="$REPOSITORY_DIR/out"
echo "Test output files will be written to $OUT_DIR"

# Integration tests
docker build -f $REPOSITORY_DIR/.ci/Dockerfile.ci --target test -t $IMAGE_TAG $REPOSITORY_DIR

# Always clean up image
trap "docker rmi $(docker images --filter=reference="$IMAGE_TAG" -q) 2>/dev/null || true" EXIT

# Check to see if we are running in Team City
mkdir -p $OUT_DIR
docker run -v $OUT_DIR:/mount --rm $IMAGE_TAG

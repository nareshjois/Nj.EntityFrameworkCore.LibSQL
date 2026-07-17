#!/usr/bin/env bash
# WP-12: build and run LocalSample in a linux-x64 container.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

IMAGE="${LOCAL_SAMPLE_IMAGE:-nj-libsql-local-sample:smoke}"

echo "==> docker build ${IMAGE} (linux/amd64)"
docker build --platform linux/amd64 -f eng/docker/Dockerfile.local-sample -t "$IMAGE" .

echo "==> docker run ${IMAGE}"
docker run --platform linux/amd64 --rm "$IMAGE"

echo "Container smoke succeeded."

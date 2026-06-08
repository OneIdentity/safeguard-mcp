#!/usr/bin/env bash
# Render the safeguard-mcp Helm chart with the canonical golden value
# set. Used both to (re)generate `manifests.yaml` locally and by CI
# to diff against it.
#
# See `README.md` in this directory for parameter rationale.

set -euo pipefail

CHART_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

helm template safeguard-mcp "${CHART_DIR}" \
    --namespace safeguard-mcp \
    --kube-version 1.27.0 \
    --set safeguardHost=safeguard.example.com \
    --set ingress.host=mcp.example.com \
    --set relay.autoscaling.enabled=true

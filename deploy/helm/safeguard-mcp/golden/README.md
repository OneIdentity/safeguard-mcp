# Helm-render golden output

`manifests.yaml` is the canonical render of this chart with the
fixed parameter set described below. CI re-runs the render and
diffs it against this file; any unintended drift in the chart
templates blocks the PR.

## How to regenerate

When you intentionally change a template's output (and have updated
the chart accordingly), regenerate the golden:

```sh
deploy/helm/safeguard-mcp/golden/render.sh > \
  deploy/helm/safeguard-mcp/golden/manifests.yaml
```

Commit the regenerated `manifests.yaml` together with the template
change. Reviewers should look at the golden diff to confirm the
template change has the intended effect.

## What gets rendered

The render uses `--kube-version 1.27.0` (the chart's minimum) so
API-version selection is stable across CI runners, and the following
fixed values:

* `safeguardHost=safeguard.example.com`
* `mcpPublicUrl=https://mcp.example.com`
* `rstsClientId=https://mcp.example.com`
* `ingress.host=mcp.example.com`
* `relay.autoscaling.enabled=true` (so the HPA appears in golden)

All other values come from the chart's defaults.

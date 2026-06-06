{{/*
Common helpers for the safeguard-mcp chart.
*/}}

{{- define "safeguard-mcp.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "safeguard-mcp.fullname" -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}

{{- define "safeguard-mcp.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "safeguard-mcp.relayName" -}}
{{- printf "%s-relay" (include "safeguard-mcp.fullname" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "safeguard-mcp.bridgeName" -}}
{{- printf "%s-bridge" (include "safeguard-mcp.fullname" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "safeguard-mcp.configMapName" -}}
{{- printf "%s-config" (include "safeguard-mcp.fullname" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Common labels applied to every resource.
*/}}
{{- define "safeguard-mcp.labels" -}}
helm.sh/chart: {{ include "safeguard-mcp.chart" . }}
{{ include "safeguard-mcp.selectorLabels" . }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{- define "safeguard-mcp.selectorLabels" -}}
app.kubernetes.io/name: {{ include "safeguard-mcp.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}

{{- define "safeguard-mcp.relayLabels" -}}
{{ include "safeguard-mcp.labels" . }}
app.kubernetes.io/component: relay
{{- end -}}

{{- define "safeguard-mcp.relaySelectorLabels" -}}
{{ include "safeguard-mcp.selectorLabels" . }}
app.kubernetes.io/component: relay
{{- end -}}

{{- define "safeguard-mcp.bridgeLabels" -}}
{{ include "safeguard-mcp.labels" . }}
app.kubernetes.io/component: bridge
{{- end -}}

{{- define "safeguard-mcp.bridgeSelectorLabels" -}}
{{ include "safeguard-mcp.selectorLabels" . }}
app.kubernetes.io/component: bridge
{{- end -}}

{{/*
Resolved container image reference. Falls back to .Chart.AppVersion
when image.tag is empty.
*/}}
{{- define "safeguard-mcp.image" -}}
{{- $tag := default .Chart.AppVersion .Values.image.tag -}}
{{- printf "%s:%s" .Values.image.repository $tag -}}
{{- end -}}

{{/*
Hardened container securityContext shared by both Deployments.
*/}}
{{- define "safeguard-mcp.containerSecurityContext" -}}
allowPrivilegeEscalation: false
readOnlyRootFilesystem: true
capabilities:
  drop:
    - ALL
{{- end -}}

{{- define "safeguard-mcp.podSecurityContext" -}}
runAsNonRoot: true
seccompProfile:
  type: RuntimeDefault
{{- end -}}

#!/usr/bin/env node

// Launcher for @oneidentity/safeguard-mcp
// Resolves the platform-specific binary from optional dependencies and spawns it.

import { execFileSync } from "node:child_process";
import { existsSync } from "node:fs";
import { createRequire } from "node:module";
import { join } from "node:path";

const PLATFORM_PACKAGES = {
  "linux-x64": { pkg: "@oneidentity/safeguard-mcp-linux-x64", bin: "safeguard-mcp" },
  "linux-arm64": { pkg: "@oneidentity/safeguard-mcp-linux-arm64", bin: "safeguard-mcp" },
  "win32-x64": { pkg: "@oneidentity/safeguard-mcp-win-x64", bin: "safeguard-mcp.exe" },
  "darwin-arm64": { pkg: "@oneidentity/safeguard-mcp-darwin-arm64", bin: "safeguard-mcp" },
};

function getPlatformKey() {
  return `${process.platform}-${process.arch}`;
}

function findBinary() {
  const key = getPlatformKey();
  const entry = PLATFORM_PACKAGES[key];

  if (!entry) {
    console.error(
      `Unsupported platform: ${key}\n` +
      `Supported: ${Object.keys(PLATFORM_PACKAGES).join(", ")}\n` +
      `Download a binary from https://github.com/OneIdentity/safeguard-mcp/releases`
    );
    process.exit(1);
  }

  // Try to resolve from the optional dependency
  const require = createRequire(import.meta.url);
  try {
    const pkgDir = join(require.resolve(`${entry.pkg}/package.json`), "..");
    const binPath = join(pkgDir, entry.bin);
    if (existsSync(binPath)) {
      return binPath;
    }
  } catch {
    // Package not installed — fall through
  }

  // Fallback: check if binary is adjacent (development scenario)
  const localBin = join(import.meta.dirname, "..", entry.bin);
  if (existsSync(localBin)) {
    return localBin;
  }

  console.error(
    `Could not find safeguard-mcp binary for ${key}.\n` +
    `Expected package: ${entry.pkg}\n\n` +
    `Try reinstalling: npm install -g @oneidentity/safeguard-mcp\n` +
    `Or download directly from https://github.com/OneIdentity/safeguard-mcp/releases`
  );
  process.exit(1);
}

const binary = findBinary();

try {
  execFileSync(binary, process.argv.slice(2), {
    stdio: "inherit",
    env: process.env,
  });
} catch (err) {
  if (err.status !== null) {
    process.exit(err.status);
  }
  throw err;
}

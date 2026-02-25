#!/bin/bash

set -e

# Scriptin yerləşdiyi qovluqdan project root-a keç
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

KEY_DIR="$PROJECT_ROOT/keys"

mkdir -p "$KEY_DIR"

PRIVATE_KEY="$KEY_DIR/private.pem"
PUBLIC_KEY="$KEY_DIR/public.pem"

if [ ! -f "$PRIVATE_KEY" ]; then
  echo "Generating RSA keys..."

  openssl genrsa -out "$PRIVATE_KEY" 2048
  openssl rsa -in "$PRIVATE_KEY" -pubout -out "$PUBLIC_KEY"

  chmod 600 "$PRIVATE_KEY" || true
fi

echo "Keys ready at: $KEY_DIR"
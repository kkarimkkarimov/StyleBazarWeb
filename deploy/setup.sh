#!/usr/bin/env bash
# StyleBazar — one-shot bootstrap for a fresh Ubuntu 22.04/24.04 server.
# Installs Docker + the compose plugin, then prints the remaining steps.
#
#   ssh root@YOUR-SERVER-IP
#   bash setup.sh
set -euo pipefail

echo "==> Installing Docker (official convenience script)..."
if ! command -v docker >/dev/null 2>&1; then
  curl -fsSL https://get.docker.com | sh
fi
systemctl enable --now docker

echo
echo "==> Docker is ready:"
docker --version
docker compose version

cat <<'NEXT'

============================================================
 Növbəti addımlar (bu qovluqda davam et):
   1) git clone https://github.com/kkarimkkarimov/StyleBazarWeb.git
      cd StyleBazarWeb
   2) cp .env.example .env
      nano .env          # 5 dəyəri yapışdır, sonra Ctrl+O, Enter, Ctrl+X
   3) docker compose -f docker-compose.prod.yml up -d --build
   4) Brauzerdə https://SƏNİN-DOMENİN aç (ilk build bir neçə dəqiqə çəkir)
============================================================
NEXT

#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  "Push per update te ri"
#  Perdorim:  ./scripts/release.sh 1.0.1 "Çfare ka te re ne kete version"
#
#  Cfare ndodh:
#   1. ruhet versioni i ri ne skedarin VERSION
#   2. shtohet ne CHANGELOG.md
#   3. commit + tag v<version> + push
#   4. installer-i ndertohet vete dhe versioni publikohet
#   5. te gjithe perdoruesit me LIKAsys aktiv marrin njoftimin brenda 6 oreve
#      (ose menjehere kur e hapin programin) dhe perditesohen me nje klik
# ---------------------------------------------------------------------------
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

VERSION="${1:-}"
NOTES="${2:-Permiresime dhe rregullime te vogla.}"

if [ -z "$VERSION" ]; then
  echo "Perdorim: ./scripts/release.sh <version> [shenime]"
  echo "Shembull: ./scripts/release.sh 1.0.1 \"Shtuar grafiket e vegjel\""
  exit 1
fi

if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Versioni duhet te jete ne formatin X.Y.Z (p.sh. 1.0.1)"
  exit 1
fi

echo "$VERSION" > VERSION

DATE="$(date +%Y-%m-%d)"
TMP="$(mktemp)"
{
  echo "## v$VERSION - $DATE"
  echo
  echo "$NOTES"
  echo
  if [ -f CHANGELOG.md ]; then cat CHANGELOG.md; fi
} > "$TMP"
mv "$TMP" CHANGELOG.md

git add -A
git commit -m "LIKAsys v$VERSION"
git tag -a "v$VERSION" -m "LIKAsys v$VERSION"
git push origin HEAD
git push origin "v$VERSION"

echo
echo "=============================================================="
echo " Tag v$VERSION u shty."
echo " Installer-i po ndertohet tani."
echo "=============================================================="

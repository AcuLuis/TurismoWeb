#!/usr/bin/env sh
# Verificación completa del proyecto en local (sin servicios externos).
# Uso:  sh scripts/verificar.sh
set -e
RAIZ="$(cd "$(dirname "$0")/.." && pwd)"

paso() { printf '\n==> %s\n' "$1"; }

cd "$RAIZ/backend"
paso "Backend: compilación";  dotnet build TurismoGo.sln -nologo -v q
paso "Backend: pruebas";      dotnet test TurismoGo.sln --no-build -nologo
paso "Backend: paquetes vulnerables"
if dotnet list TurismoGo.sln package --vulnerable --include-transitive | tee /dev/stderr | grep -qE "vulnerables siguientes|following vulnerable packages"; then
  echo "FALLÓ: hay paquetes vulnerables"; exit 1
fi

cd "$RAIZ/frontend"
paso "Frontend: lint";        npm run lint
paso "Frontend: pruebas";     npm test
paso "Frontend: auditoría";   npm audit --audit-level=moderate
paso "Frontend: build";       npm run build

printf '\nTodo correcto.\n'

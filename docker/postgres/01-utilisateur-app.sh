#!/bin/bash
# Exécuté une seule fois, à la création du volume : crée l'utilisateur de l'application,
# propriétaire de la base « lg » mais sans aucun droit d'administration sur le serveur.
set -euo pipefail

psql -v ON_ERROR_STOP=1 -U postgres -v mdp="$LG_APP_PASSWORD" <<'SQL'
CREATE ROLE lg_app LOGIN PASSWORD :'mdp' NOSUPERUSER NOCREATEDB NOCREATEROLE;
CREATE DATABASE lg OWNER lg_app;
REVOKE ALL ON DATABASE lg FROM PUBLIC;
SQL

psql -v ON_ERROR_STOP=1 -U postgres -d lg <<'SQL'
ALTER SCHEMA public OWNER TO lg_app;
REVOKE ALL ON SCHEMA public FROM PUBLIC;
SQL

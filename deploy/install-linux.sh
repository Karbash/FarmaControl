#!/usr/bin/env bash
set -Eeuo pipefail

APP_NAME="farmacontrol"
SERVICE_NAME="${APP_NAME}.service"
APP_USER="${APP_USER:-farmacontrol}"
APP_GROUP=""
INSTALL_ROOT="${INSTALL_ROOT:-/opt/farmacontrol}"
DATA_ROOT="${DATA_ROOT:-/var/lib/farmacontrol}"
CONFIG_DIR="${CONFIG_DIR:-/etc/farmacontrol}"
WEB_ROOT="${WEB_ROOT:-/var/www/farmacontrol}"
API_PORT="${API_PORT:-5076}"
SERVER_NAME="${SERVER_NAME:-_}"
SKIP_DEPS=0
CREATED_ADMIN_PASSWORD=""

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
STAGING_DIR="$(mktemp -d -t farmacontrol-publish.XXXXXX)"
trap 'rm -rf "${STAGING_DIR}"' EXIT

usage() {
  cat <<EOF
Usage: sudo bash deploy/install-linux.sh [options]

Options:
  --server-name NAME  Domain or IP used by Nginx. Default: _
  --api-port PORT     Local Kestrel port. Default: 5076
  --app-user USER     Linux service user. Default: farmacontrol
  --install-root DIR  API install directory. Default: /opt/farmacontrol
  --data-root DIR     SQLite data directory. Default: /var/lib/farmacontrol
  --web-root DIR      Frontend directory served by Nginx. Default: /var/www/farmacontrol
  --skip-deps         Do not install apt dependencies.
  -h, --help          Show this help.

Environment overrides:
  FARMACONTROL_INITIAL_ADMIN_PASSWORD  Initial admin password for a new database.
  FARMACONTROL_JWT_KEY                 JWT signing key.
EOF
}

log() {
  printf '\n==> %s\n' "$*"
}

fail() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

command_exists() {
  command -v "$1" >/dev/null 2>&1
}

node_major_version() {
  if ! command_exists node; then
    printf '0'
    return
  fi

  node -v | sed -E 's/^v([0-9]+).*/\1/'
}

has_dotnet_sdk_10() {
  command_exists dotnet && dotnet --list-sdks 2>/dev/null | grep -q '^10\.'
}

parse_args() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --server-name|--domain)
        SERVER_NAME="${2:?Missing value for $1}"
        shift 2
        ;;
      --api-port)
        API_PORT="${2:?Missing value for $1}"
        shift 2
        ;;
      --app-user)
        APP_USER="${2:?Missing value for $1}"
        shift 2
        ;;
      --install-root)
        INSTALL_ROOT="${2:?Missing value for $1}"
        shift 2
        ;;
      --data-root)
        DATA_ROOT="${2:?Missing value for $1}"
        shift 2
        ;;
      --web-root)
        WEB_ROOT="${2:?Missing value for $1}"
        shift 2
        ;;
      --skip-deps)
        SKIP_DEPS=1
        shift
        ;;
      -h|--help)
        usage
        exit 0
        ;;
      *)
        fail "Unknown option: $1"
        ;;
    esac
  done
}

ensure_root() {
  if [[ "${EUID}" -ne 0 ]]; then
    fail "Run with sudo: sudo bash deploy/install-linux.sh"
  fi
}

validate_path() {
  local name="$1"
  local path="$2"

  [[ "${path}" == /* ]] || fail "${name} must be an absolute path."
  [[ "${path}" != *" "* ]] || fail "${name} cannot contain spaces."

  case "${path}" in
    /|/bin|/boot|/dev|/etc|/home|/lib|/lib64|/opt|/proc|/root|/run|/sbin|/sys|/tmp|/usr|/var|/var/www)
      fail "${name} is too broad: ${path}"
      ;;
  esac
}

validate_options() {
  [[ "${API_PORT}" =~ ^[0-9]+$ ]] || fail "--api-port must be numeric."
  [[ "${SERVER_NAME}" =~ ^[A-Za-z0-9_.:-]+$|^_$ ]] || fail "--server-name contains unsupported characters."

  validate_path "--install-root" "${INSTALL_ROOT}"
  validate_path "--data-root" "${DATA_ROOT}"
  validate_path "--web-root" "${WEB_ROOT}"
  validate_path "CONFIG_DIR" "${CONFIG_DIR}"
}

install_microsoft_package_feed() {
  [[ -r /etc/os-release ]] || fail "Cannot detect Linux distribution."
  # shellcheck disable=SC1091
  . /etc/os-release

  case "${ID:-}" in
    ubuntu|debian)
      ;;
    *)
      fail "Automatic .NET install supports Debian/Ubuntu only. Install .NET SDK 10 and rerun with --skip-deps."
      ;;
  esac

  local repo_deb="/tmp/packages-microsoft-prod-${ID}-${VERSION_ID}.deb"
  local repo_url="https://packages.microsoft.com/config/${ID}/${VERSION_ID}/packages-microsoft-prod.deb"

  log "Installing Microsoft package feed"
  curl -fsSL "${repo_url}" -o "${repo_deb}" ||
    fail "Could not download ${repo_url}. Install .NET SDK 10 manually and rerun with --skip-deps."
  dpkg -i "${repo_deb}"
  rm -f "${repo_deb}"
}

install_dependencies() {
  if [[ "${SKIP_DEPS}" -eq 1 ]]; then
    log "Skipping dependency installation"
    return
  fi

  command_exists apt-get ||
    fail "Automatic dependency installation requires apt-get. Install dotnet-sdk-10.0, Node.js 20+ or 22+, npm and nginx, then rerun with --skip-deps."

  export DEBIAN_FRONTEND=noninteractive

  log "Installing base packages"
  apt-get update
  apt-get install -y ca-certificates curl gnupg openssl nginx

  if [[ "$(node_major_version)" -lt 20 ]]; then
    log "Installing Node.js 22"
    curl -fsSL https://deb.nodesource.com/setup_22.x -o /tmp/nodesource_setup.sh
    bash /tmp/nodesource_setup.sh
    apt-get install -y nodejs
  fi

  if ! has_dotnet_sdk_10; then
    install_microsoft_package_feed
    apt-get update
    log "Installing .NET SDK 10"
    apt-get install -y dotnet-sdk-10.0
  fi
}

check_required_tools() {
  command_exists systemctl || fail "systemctl is required."
  command_exists nginx || fail "nginx is required."
  command_exists npm || fail "npm is required."
  [[ "$(node_major_version)" -ge 20 ]] || fail "Node.js 20 or newer is required."
  has_dotnet_sdk_10 || fail ".NET SDK 10 is required."
}

build_frontend() {
  log "Building Angular frontend"
  pushd "${REPO_ROOT}/src/frontend" >/dev/null
  npm ci
  npm run build -- --configuration production
  popd >/dev/null

  [[ -d "${REPO_ROOT}/src/frontend/dist/frontend/browser" ]] ||
    fail "Angular build output not found."
}

publish_backend() {
  log "Publishing .NET API"
  dotnet publish "${REPO_ROOT}/src/backend/FarmaControl.Api/FarmaControl.Api.csproj" \
    -c Release \
    --self-contained false \
    -o "${STAGING_DIR}/api"
}

ensure_linux_user() {
  if ! id "${APP_USER}" >/dev/null 2>&1; then
    log "Creating service user ${APP_USER}"
    useradd --system --home-dir "${DATA_ROOT}" --create-home --shell /usr/sbin/nologin "${APP_USER}"
  fi

  APP_GROUP="$(id -gn "${APP_USER}")"
}

install_directories() {
  log "Creating install directories"
  install -d -m 0755 "${INSTALL_ROOT}"
  install -d -o "${APP_USER}" -g "${APP_GROUP}" -m 0750 "${DATA_ROOT}"
  install -d -m 0750 "${CONFIG_DIR}"
  install -d -m 0755 "${WEB_ROOT}"
}

install_files() {
  log "Installing API files"
  rm -rf "${INSTALL_ROOT}/api"
  install -d -m 0755 "${INSTALL_ROOT}/api"
  cp -a "${STAGING_DIR}/api/." "${INSTALL_ROOT}/api/"
  chown -R root:root "${INSTALL_ROOT}"
  chmod -R a=rX,u+w "${INSTALL_ROOT}"

  log "Installing frontend files"
  rm -rf "${WEB_ROOT:?}/"*
  cp -a "${REPO_ROOT}/src/frontend/dist/frontend/browser/." "${WEB_ROOT}/"
  if id www-data >/dev/null 2>&1; then
    chown -R www-data:www-data "${WEB_ROOT}"
  fi
}

generate_secret() {
  openssl rand -base64 "$1" | tr -d '\n'
}

upsert_env() {
  local env_file="$1"
  local key="$2"
  local value="$3"
  local tmp_file
  tmp_file="$(mktemp)"

  if grep -q "^${key}=" "${env_file}" 2>/dev/null; then
    awk -v key="${key}" -v line="${key}=${value}" \
      'index($0, key "=") == 1 { print line; next } { print }' \
      "${env_file}" > "${tmp_file}"
  else
    cat "${env_file}" > "${tmp_file}" 2>/dev/null || true
    printf '%s=%s\n' "${key}" "${value}" >> "${tmp_file}"
  fi

  cat "${tmp_file}" > "${env_file}"
  rm -f "${tmp_file}"
}

ensure_secret_env() {
  local env_file="$1"
  local key="$2"
  local value="$3"

  if ! grep -q "^${key}=" "${env_file}" 2>/dev/null; then
    printf '%s=%s\n' "${key}" "${value}" >> "${env_file}"
  fi
}

write_environment_file() {
  local env_file="${CONFIG_DIR}/${APP_NAME}.env"
  local jwt_key="${FARMACONTROL_JWT_KEY:-$(generate_secret 48)}"
  local admin_password="${FARMACONTROL_INITIAL_ADMIN_PASSWORD:-$(generate_secret 18)}"

  touch "${env_file}"

  upsert_env "${env_file}" "ASPNETCORE_ENVIRONMENT" "Production"
  upsert_env "${env_file}" "ASPNETCORE_URLS" "http://127.0.0.1:${API_PORT}"
  upsert_env "${env_file}" "ConnectionStrings__FarmaControlDb" "\"Data Source=${DATA_ROOT}/farmacontrol.db\""
  upsert_env "${env_file}" "Https__Redirect" "false"
  ensure_secret_env "${env_file}" "Jwt__Key" "${jwt_key}"

  if ! grep -q "^FARMACONTROL_INITIAL_ADMIN_PASSWORD=" "${env_file}" 2>/dev/null; then
    ensure_secret_env "${env_file}" "FARMACONTROL_INITIAL_ADMIN_PASSWORD" "${admin_password}"
    CREATED_ADMIN_PASSWORD="${admin_password}"
  fi

  chown root:"${APP_GROUP}" "${env_file}"
  chmod 0640 "${env_file}"
}

write_systemd_service() {
  log "Writing systemd service"
  cat > "/etc/systemd/system/${SERVICE_NAME}" <<EOF
[Unit]
Description=FarmaControl API
After=network.target

[Service]
WorkingDirectory=${INSTALL_ROOT}/api
ExecStart=/usr/bin/dotnet ${INSTALL_ROOT}/api/FarmaControl.Api.dll
Restart=always
RestartSec=5
SyslogIdentifier=${APP_NAME}
User=${APP_USER}
EnvironmentFile=${CONFIG_DIR}/${APP_NAME}.env
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=full
ReadWritePaths=${DATA_ROOT}

[Install]
WantedBy=multi-user.target
EOF
}

write_nginx_site() {
  log "Writing Nginx site"
  [[ -d /etc/nginx/sites-available ]] || install -d -m 0755 /etc/nginx/sites-available
  [[ -d /etc/nginx/sites-enabled ]] || install -d -m 0755 /etc/nginx/sites-enabled

  cat > "/etc/nginx/sites-available/${APP_NAME}.conf" <<EOF
server {
    listen 80;
    server_name ${SERVER_NAME};

    root ${WEB_ROOT};
    index index.html;

    location /api/ {
        proxy_pass http://127.0.0.1:${API_PORT};
        proxy_http_version 1.1;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    location = /api {
        proxy_pass http://127.0.0.1:${API_PORT};
        proxy_http_version 1.1;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    location / {
        try_files \$uri \$uri/ /index.html;
    }
}
EOF

  ln -sfn "/etc/nginx/sites-available/${APP_NAME}.conf" "/etc/nginx/sites-enabled/${APP_NAME}.conf"
  rm -f /etc/nginx/sites-enabled/default
  nginx -t
}

restart_services() {
  log "Starting services"
  systemctl daemon-reload
  systemctl enable "${SERVICE_NAME}"
  systemctl restart "${SERVICE_NAME}"
  systemctl restart nginx
}

check_health() {
  log "Checking API health"
  sleep 2

  if curl -fsS "http://127.0.0.1:${API_PORT}/api/health" >/dev/null; then
    printf 'API health: ok\n'
  else
    printf 'API health check failed. Inspect with: sudo journalctl -u %s -n 100 --no-pager\n' "${SERVICE_NAME}" >&2
  fi
}

print_summary() {
  local url="http://${SERVER_NAME}"
  if [[ "${SERVER_NAME}" == "_" ]]; then
    url="http://<server-ip>"
  fi

  cat <<EOF

FarmaControl installed.
URL: ${url}
Service: ${SERVICE_NAME}
API data: ${DATA_ROOT}/farmacontrol.db
Environment file: ${CONFIG_DIR}/${APP_NAME}.env

Useful commands:
  sudo systemctl status ${SERVICE_NAME}
  sudo journalctl -u ${SERVICE_NAME} -f
  sudo systemctl reload nginx
EOF

  if [[ -n "${CREATED_ADMIN_PASSWORD}" ]]; then
    cat <<EOF

Initial login:
  user: admin
  password: ${CREATED_ADMIN_PASSWORD}

Change this password after the first login.
EOF
  fi
}

main() {
  parse_args "$@"
  ensure_root
  validate_options
  install_dependencies
  check_required_tools
  build_frontend
  publish_backend
  ensure_linux_user
  install_directories
  install_files
  write_environment_file
  write_systemd_service
  write_nginx_site
  restart_services
  check_health
  print_summary
}

main "$@"

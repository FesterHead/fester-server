#!/bin/bash

TEXT_RESET='\e[0m'
TEXT_YELLOW='\e[0;33m'
TEXT_GREEN='\e[0;32m'
TEXT_RED_B='\e[1;31m'

function print_array_item() {
  local index="$1"
  local value="$2"

  printf "%2d = %s\n" $index $value
}

function do_update() {
  cd ~/docker

  if [[ "$1" == "modernuo" ]]; then
    echo -e $TEXT_YELLOW
    echo "-----------------------------------------------"
    echo "Starting docker compose build for $1 ..."
    echo -e $TEXT_RESET
    docker compose build --pull $1
  else
    echo -e $TEXT_YELLOW
    echo "-----------------------------------------------"
    echo "Starting docker compose pull for $1 ..."
    echo -e $TEXT_RESET
    docker compose pull $1
  fi

  echo -e $TEXT_YELLOW
  echo "-----------------------------------------------"
  echo "Starting docker compose up -d for $1 ..."
  echo -e $TEXT_RESET
  docker compose up -d $1
}

services=("bazarr" \
          "container-mon" \
          "crosswatch" \
          "diun" \
          "dockerproxy" \
          "dockhand" \
          "duckdns" \
          "emby" \
          "flaresolverr" \
          "homepage" \
          "modernuo" \
          "prowlarr" \
          "radarr" \
          "rpdb-folders" \
          "sabnzbd" \
          "seerr" \
          "sonarr" \
          "swag" \
          "transmission-openvpn" \
          "transmission-rush")

# Check command line argument
if [[ "$1" == "all" ]]; then
  echo -e $TEXT_YELLOW
  echo "Updating all services via command line argument..."
  echo -e $TEXT_RESET
  for service in "${services[@]}"; do
    do_update "$service"
  done
else
  # Interactive mode: display services list
  for index in "${!services[@]}"; do
    print_array_item "$index" "${services[$index]}"
  done

  echo "Enter system # to pull and update (or 'all'):"
  read index

  if [[ "$index" == "all" ]]; then
    for service in "${services[@]}"; do
      do_update "$service"
    done
  elif [[ "$index" -ge 0 && "$index" -lt "${#services[@]}" ]]; then
    do_update "${services[$index]}"
  else
    echo "Bad input - try again"
    exit 1
  fi
fi

echo -e $TEXT_YELLOW
echo "-----------------------------------------------"
echo "Starting docker image prune --all --force ..."
echo -e $TEXT_RESET
docker image prune --all --force

echo -e $TEXT_GREEN
echo "Pau."
echo -e $TEXT_RESET
echo ""

echo "Pau. Have a great day! 🏄 🌈 🌴 🌺 🦄"

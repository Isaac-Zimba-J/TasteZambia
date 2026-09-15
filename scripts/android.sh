#!/usr/bin/env bash
# Build and deploy Taste Zambia to an Android phone.
#
#   scripts/android.sh usb        deploy over the USB cable
#   scripts/android.sh wireless   find the phone on the network, connect, deploy
#   scripts/android.sh connect    just (re)connect wireless debugging
#   scripts/android.sh build      build only, no deploy
#
# Builds android-arm64 only - the phone's architecture - which keeps a redeploy
# at about a minute instead of three.
set -euo pipefail

export PATH="/usr/local/share/dotnet:$PATH"
export ANDROID_HOME="${ANDROID_HOME:-$HOME/Library/Android/sdk}"
export PATH="$ANDROID_HOME/platform-tools:$PATH"

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/TasteZambia.Mobile/TasteZambia.Mobile.csproj"
USB_SERIAL="R5CR80TVGDZ"

build_args=(
  "$PROJECT"
  -f net10.0-android
  -c Debug
  -p:RuntimeIdentifier=android-arm64
)

connect_wireless() {
  # The wireless port changes every time the phone reconnects, so discover it.
  local addr
  addr="$(adb mdns services 2>/dev/null | awk '/_adb-tls-connect/ {print $NF}' | head -1)"

  if [[ -z "$addr" ]]; then
    echo "No phone advertising wireless debugging on this network."
    echo "On the phone: Developer options -> Wireless debugging must be ON,"
    echo "and the phone must be awake and on the same Wi-Fi."
    exit 1
  fi

  echo "Connecting to $addr ..."
  adb connect "$addr"
}

require_single_device() {
  # Cable and wireless at once makes MAUI's tooling see the phone twice and
  # fail with "device offline". Refuse rather than let that happen silently.
  local count
  count="$(adb devices | awk 'NR>1 && $2=="device"' | wc -l | tr -d ' ')"
  if [[ "$count" -eq 0 ]]; then
    echo "No device connected."; exit 1
  elif [[ "$count" -gt 1 ]]; then
    echo "More than one transport is connected to the phone:"
    adb devices
    echo "Unplug the cable or disconnect wireless, then retry."
    exit 1
  fi
}

case "${1:-}" in
  usb)
    # An explicit serial disambiguates, so cable + wireless together is fine here.
    if ! adb devices | grep -q "^$USB_SERIAL[[:space:]]*device"; then
      echo "Phone not connected over USB (looking for $USB_SERIAL)."; adb devices; exit 1
    fi
    dotnet build "${build_args[@]}" -t:Run -p:AndroidAttachDebugger=false \
      -p:AdbTarget="-s $USB_SERIAL"
    ;;
  wireless)
    connect_wireless
    require_single_device
    dotnet build "${build_args[@]}" -t:Run -p:AndroidAttachDebugger=false
    ;;
  connect)
    connect_wireless
    adb devices
    ;;
  build)
    dotnet build "${build_args[@]}"
    ;;
  *)
    sed -n '2,8p' "$0"
    exit 1
    ;;
esac

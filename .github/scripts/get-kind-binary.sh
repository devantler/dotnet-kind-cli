#!/bin/bash
set -e

get() {
  local url=$1
  local binary=$2
  local target_dir=$3
  local target_name=$4
  local isTar=$5

  if [ "$isTar" = true ]; then
    curl -LJ "$url" | tar xvz -C "$target_dir" "$binary"
    mv "$target_dir/$binary" "${target_dir}/$target_name"
  elif [ "$isTar" = false ]; then
    curl -LJ "$url" -o "$target_dir/$target_name"
  fi
  chmod +x "$target_dir/$target_name"
}

get "https://getbin.io/kubernetes-sigs/kind?os=darwin&arch=arm64" "kind-darwin-arm64" "Devantler.KindCLI/runtimes/osx-arm64/native" "kind-osx-arm64" false
get "https://getbin.io/kubernetes-sigs/kind?os=darwin&arch=amd64" "kind-darwin-amd64" "Devantler.KindCLI/runtimes/osx-x64/native" "kind-osx-x64" false
get "https://getbin.io/kubernetes-sigs/kind?os=linux&arch=arm64" "kind-linux-arm64" "Devantler.KindCLI/runtimes/linux-arm64/native" "kind-linux-arm64" false
get "https://getbin.io/kubernetes-sigs/kind?os=linux&arch=amd64" "kind-linux-amd64" "Devantler.KindCLI/runtimes/linux-x64/native" "kind-linux-x64" false
get "https://getbin.io/kubernetes-sigs/kind?os=windows&arch=amd64" "kind-windows-amd64" "Devantler.KindCLI/runtimes/win-x64/native" "kind-win-x64.exe" false

#!/usr/bin/env bash

#CRDIR="$(cd "`dirname "$0"`"; pwd)"
FWDIR="$(cd "`dirname "$0"`"/..; pwd)"

cd $FWDIR/ar-drivers-rs && \
cargo build --release

TARGET="$FWDIR/Assets/Plugins/libar_drivers.so"
cp $FWDIR/ar-drivers-rs/target/release/libar_drivers.so $TARGET && \
echo "Copied to $TARGET"

echo "Please give sudo permission to open access to hidraw device(s) ..."
sudo chmod 777 /dev/hidraw*
echo "... permission granted"


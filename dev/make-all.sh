#!/usr/bin/env bash

#CRDIR="$(cd "`dirname "$0"`"; pwd)"
FWDIR="$(cd "`dirname "$0"`"/..; pwd)"

cd $FWDIR/ar-drivers-rs && \
cargo build --release

cp $FWDIR/ar-drivers-rs/target/release/libar_drivers.so $FWDIR/Assets/Plugins/libar_drivers.so
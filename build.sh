#!/bin/bash
set -e

pushd ts >/dev/null
npm install
npm run test
popd >/dev/null

pushd example/js >/dev/null
npm install
npm run test
popd >/dev/null

pushd example/ts >/dev/null
npm install
npm run test
popd >/dev/null

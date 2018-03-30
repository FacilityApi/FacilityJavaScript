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

echo TRAVIS_TAG=$TRAVIS_TAG
if [[ $TRAVIS_TAG =~ ^npm-v[0-9]+\.[0-9]+\.[0-9]+(-.*)?$ ]] ; then
    echo $TRAVIS_TAG is deployable
fi

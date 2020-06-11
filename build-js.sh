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

pushd ts >/dev/null
if [[ "${TRAVIS_BRANCH}" == "master" ]] && [[ "${TRAVIS_PULL_REQUEST}" == "false" ]]
then
echo "//registry.npmjs.org/:_authToken=$NPM_TOKEN" >> .npmrc
npm publish || true
fi
popd >/dev/null

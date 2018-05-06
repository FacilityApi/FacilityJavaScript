#!/bin/bash
set -e

cd ts
echo "//registry.npmjs.org/:_authToken=$NPM_TOKEN" >> .npmrc
npm publish

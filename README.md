# Facility JavaScript and TypeScript Support

[JavaScript and TypeScript support](https://facilityapi.github.io/generate/javascript) for the [Facility API Framework](https://facilityapi.github.io/).
[![Build](https://github.com/FacilityApi/FacilityJavaScript/workflows/Build/badge.svg)](https://github.com/FacilityApi/FacilityJavaScript/actions?query=workflow%3ABuild)

Name | Description | npm/NuGet
--- | --- | ---
facility-core | Common code for the Facility API Framework. | [![npm](https://img.shields.io/npm/v/facility-core.svg)](https://www.npmjs.com/package/facility-core)
fsdgenjs | A tool that generates JavaScript or TypeScript for a Facility Service Definition. | [![NuGet](https://img.shields.io/nuget/v/fsdgenjs.svg)](https://www.nuget.org/packages/fsdgenjs)
Facility.CodeGen.JavaScript | A library that generates JavaScript or TypeScript for a Facility Service Definition. | [![NuGet](https://img.shields.io/nuget/v/Facility.CodeGen.JavaScript.svg)](https://www.nuget.org/packages/Facility.CodeGen.JavaScript)

[Documentation](https://facilityapi.github.io/) | [Release Notes](https://github.com/FacilityApi/FacilityJavaScript/blob/master/ReleaseNotes.md) | [Contributing](https://github.com/FacilityApi/FacilityJavaScript/blob/master/CONTRIBUTING.md)

## Conformance

To run conformance tests, first start one of the conformance servers from within the `/conformance` folder:

```
npm run fastify:ts
npm run fastify:js
```

Then run the conformance tool against the running service.

```
npm run test
```

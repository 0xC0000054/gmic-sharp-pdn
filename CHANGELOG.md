# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

A `FromSelection` method to the `PdnGmicBitmap` class.

## v0.2.0 - 2020-07-14

### Added

* Support for G'MIC filters that produce multiple output images.

### Changed

* The host application name is now set to `paintdotnet`.
* Improved the exception documentation for `PdnGmicSharp`.
* Renamed the `Output` property to `OutputImages` and changed the return type to `IReadOnlyList<PdnGmicBitmap>`.

## v0.1.0

### Added

First version


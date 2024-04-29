# Changelog

## [Unreleased]
### Added

### Changed
- Changed to assume IgnoreTransforms

### Deprecated

### Removed

### Fixed

### Security

## [0.4.0-beta.1] - 2024-04-28
### Added
- Add window to Window/Module Creator
- Add option to disable PhysBone/PhysBoneColider output
- Add option to rename PhysBone RootTransform
- Add option to specify Root Object

### Changed
- All unnecessary components will be removed

### Fixed
- Unnecessary PhysBone is not removed
- Some objects are not active

## [0.3.2] - 2024-04-27
### Changed
- Relaxed error to warning for armature search 

## [0.3.1] - 2024-04-23
### Changed
- Save as prefab variant if possible 
- Exclude avatar name from prefabs.
- Specify unique names for prefabs.

## [0.3.0] - 2024-04-12
### Added
- Add CHANGELOG

### Removed
- AvatarDynamics movement
- rootTransform renaming

### Fixed
- Fixed issue with scripts running during build
- Fixed unnecessary PB Transforms output with multiple meshes under one PB component

## [0.2.0] - 2024-04-08
### Changed
- Disabled AvatarDynamics movement functionality by default 
- Disabled rootTransform renaming functionality by default

## [0.1.2] - 2024-04-07
### Fixed
- Fixed to properly search for armature

## [0.1.1] - 2024-04-07
### Changed
- Changed packaging

## [0.1.0] - 2024-04-07
### Added
- initial release

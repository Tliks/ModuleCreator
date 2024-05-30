# Changelog

## [Unreleased]
### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.4.1] - 2024-05-18
### Changed
- Change log message to appropriate format

### Fixed
- rootbone of skinned mesh is not held
- anchor of skinned mesh is not held
- Prefab Asset is not updated

## [0.4.0] - 2024-05-09
### Added
- window to Window/Module Creator
- option to disable PhysBone/PhysBoneColider output
- option to rename PhysBone RootTransform
- option to specify Root Object
- option to output additional PhysBones Affected Transforms for exact movement
- option to include IgnoreTransforms
- tooltip to advanced options

### Changed
- Changed to assume IgnoreTransforms
- Use Hips search instead of armature search
- Log messages are now unified in English
- All unnecessary components will be removed

### Fixed
- Error occurred when a null collider was associated with physBone.
- Error occurred when a null ignoreTransform was associated with physBone.
- Error when a bone associated with a mesh cannot be found was not appropriate.
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

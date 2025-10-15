# Changelog

## [Unreleased]
### Added

### Changed

### Deprecated

### Removed

### Fixed
 
### Security

## [0.6.2] - 2025-10-16
### Fixed
- Fixed an issue where PhysBones or Constraints with weighted root Transforms were mistakenly deleted.

## [0.6.1] - 2025-04-25
### Added
- Add Japanese changelog.

### Changed
- Improved performance.

### Fixed
- Fixed the name of the Prefab when a single renderer was selected.
- Fixed processing related to PhysBone's IgnoreTransforms.

## [0.6.0] - 2025-04-13
### Changed
- The behavior of renaming the root transform of phybone, which was enabled by default in v0.5.0, has been disabled.
    - Because this could cause unintended behavior, prioritize compatibility with previous versions.

## [0.5.0] - 2025-04-03
### Added
- Add support for MeshRenderer, UnityConstraints and VRCConstraints
- Add contextmenu at `Tools/ModuleCreator`
    - Specify whether each component should be included in the prefab.
    - Specify whether to unpack the prefab to the origin.
- Add .meta file

### Changed
- The parent Prefab is now set to the original Prefab (typically the FBX) instead of the target Prefab.
    - This is intended to be used as a static asset that will not be affected by changes of other Prefabs while still maintaining a connection to the FBX.
- When multiple renderers are selected, a prefab containing all the selected renderers is now output.
- Complete rewrite.

### Removed
- Removed the "Window/Module Creator" window.
- Removed the check for the existence of hips.

### Fixed
- Fixed an issue where PhysBones on costumes might not work when applying the module to the original avatar with Modular Avatar.
    - The option to automatically rename the root bone of PhysBones is now enabled by default.
    - Note: This may lead to duplicate PhysBones.
- Fixed an issue where prefabs could not be saved with missing components.
- Fixed the "cyclic prefab nesting not supported" error.
 
## [0.4.3] - 2024-07-31
### Deprecated
- The ability for prefab instances to be selected during generation has been deprecated due to an error

### Fixed
- ArgumentOutOfRangeException

## [0.4.2] - 2024-07-31
### Changed
- add changelogurl to package.json
- prefab instance is now selected when generated
- The operation is now performed only on object with skinnedmeshrender

### Fixed
- ArgumentException: Can't save part of a Prefab instance as a Prefab
- operations could be performed on prefab assets.
- prefab instance was placed in different scene
- skinnedMeshRenderer.bones was being called too much
- namespace is not used

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

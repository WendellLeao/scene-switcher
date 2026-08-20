# Changelog

All notable changes to this package are documented in this file.

## [1.1.0] - 2026-08-19

### Added
- Drag-and-drop reordering of scenes within the search popup.
- A hide button and Hidden section, letting scenes be excluded from the default list and toggled back into view.

### Changed
- Section headers are now hidden while searching, so filtered results read as a flat list.
- The visibility toggle now uses eye icons instead of text.

### Fixed
- Empty sections and toggle placement in the search popup no longer leave stray headers or misplaced controls.
- Reduced excessive bottom padding that left a dead blank strip under the scene list.

## [1.0.0] - 2026-08-19

### Added
- Initial release: an arrow next to each scene's name in the Hierarchy window header, positioned dynamically based on the name width, opens a searchable `SceneSearchPopup`.
- The popup lists every scene in the project (`SceneCatalog`), split into a Starred section (`SceneStarred`) and All Scenes, with a search field and hover highlighting.
- Left-click opens a scene (Shift-click opens it additively); right-click pings it in the Project window; the star toggles Starred.
- The currently active scene is highlighted in the list, and the popup height adapts to the number of listed scenes.

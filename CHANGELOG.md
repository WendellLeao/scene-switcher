# Changelog

All notable changes to this package are documented in this file.

## [1.0.0] - 2026-08-19

### Added
- Initial release: an arrow next to each scene's name in the Hierarchy window header, positioned dynamically based on the name width, opens a searchable `SceneSearchPopup`.
- The popup lists every scene in the project (`SceneCatalog`), split into a Starred section (`SceneStarred`) and All Scenes, with a search field and hover highlighting.
- Left-click opens a scene (Shift-click opens it additively); right-click pings it in the Project window; the star toggles Starred.
- The currently active scene is highlighted in the list, and the popup height adapts to the number of listed scenes.

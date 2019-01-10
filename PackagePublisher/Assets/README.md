# Package Publisher

## Readme

Unity plugin that adds an editor window which allows pushing NPM packages to custom registry server.

## Changelog

### [0.1.1] 2019-01-10

### Bug Fixes

- Fixed get_datapath error that sometimes occured when opening the Package Publisher window.

### [0.1.0] 2019-01-10

### Features

- Added more extensive automatic dependency injection. Package Manager UI is hardcoded as being ignored when publishing. All other packages are currently included in the publishing process as dependencies.

- Added toolbar button in the Landfall category which clears all dependencies in the manifest file except the Package Manager UI. Button is called "Clear Manifest Dependencies".

### Bug Fixes

- Fixed Package Publisher UI not updating correctly when manifest dependencies changed. Still need to close the Package Publisher and open it again to refresh dependency list though.

### [0.0.11] 2019-01-10

### Features

- Added automatic dependency injection into the package.json. Currently just retrieves all the dependencies from the manifest.json and injects into package.json.

### [0.0.9] 2019-01-10

### Bug fixes

- Fixed readme.md to use correct bullet markdown.

### [0.0.8] 2019-01-10

### Features

- Added dropdown for choosing which registry to publish package to. 

- Added support for author, contributors, repository and dependencies in package.json

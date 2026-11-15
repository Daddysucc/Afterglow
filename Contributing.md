# Contributing

## Local Development

This project relies on [DevContainers](https://containers.dev) for setting up the local development environment in a consistent and reproducible manner.
Make sure you use VS Code or any other [compatible IDE or tooling](https://containers.dev/supporting) for it to work.

To interact with the desktop inside the container, you can use the web-based tunnel accessible at [http://localhost:6080](http://localhost:6080).

## Releasing

Pushing a version tag builds the installer and the portable executable for each architecture, and publishes them as a GitHub release:

```
git tag 3.0.0
git push origin 3.0.0
```

Tags may also start with a `v` (e.g. `v3.0.0`), which is left out of the version number.
The version is otherwise left as a development version, which the app points out when it starts.

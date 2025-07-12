# RemoteMaster Legacy

RemoteMaster is a cross-platform remote management suite consisting of a web server and host agents for Windows and Linux. The project targets **.NET 9** and implements features such as host registration, remote control via SignalR hubs, script execution, device and registry management and much more. Most source files contain the header:

```
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.
```

This repository contains the legacy implementation used for experimentation with source generators and a variety of host services.

## Repository layout

- `RemoteMaster.Server` – ASP.NET Core server project providing API endpoints, identity management and the web UI.
- `RemoteMaster.Host.Core` – shared host logic used by both Windows and Linux agents (SignalR hubs, services, authorization handlers, etc.).
- `RemoteMaster.Host.Windows` – Windows host implementation with system services, screen capture and input handling.
- `RemoteMaster.Host.Linux` – Linux host implementation using DBus and X11 for screen capture, power management and other operations.
- `RemoteMaster.Shared` – DTOs, enums and common services shared between all projects.
- `RemoteMaster.SourceGenerators` – Roslyn source generators used to create repositories and unit of work classes.
- `*.Tests` – xUnit test projects for the corresponding components.

The solution file `RemoteMaster.sln` includes all of these projects.

## Build prerequisites

The code currently targets **.NET 9** as specified in `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    ...
  </PropertyGroup>
</Project>
```

To build the projects you need a .NET SDK that supports .NET 9. At the time of writing such SDKs are available only as previews. The server also relies on NodeJS tooling to compile the web assets (`package.json`, `tailwind.config.js`).

## Running tests

All tests can be executed via the .NET CLI:

```bash
$ dotnet test
```

However, the included projects require the .NET 9 SDK. Running the command with an older SDK results in an error like:

```
error NETSDK1045: The current .NET SDK does not support targeting .NET 9.0.
```

Make sure you have a recent preview of the .NET SDK installed before running the tests.

## Configuration

Sample server configuration can be found in `RemoteMaster.Server/appsettings.template.json`. It contains connection strings, logging settings and options for certificate authorities and update parameters:

```json
"update": {
  "executablesRoot": "\\\n10.14.206.253\Install\RemoteMaster",
  "userName": "",
  "password": "",
  "forceUpdate": false,
  "allowDowngrade": false
}
```

Additional options exist for JWT signing keys, certificate authorities and a Telegram bot integration.

## License

All source files are distributed under the **GNU Affero General Public License v3.0** as noted in the file headers.

# This is the dev setup for NetDaemon

Use this repository as template when developing apps for NetDaemon. This template is a good starting point for your development environment. It has pre-built structure both for building apps and unit test them. Unit tests fake objects are alpha and the fake API can be changed.

## Getting started

1. Make new repository with this repo as template
2. RENAME `src/_appsettings.json` to `src/appsettings.json`. Edit the appsettings.json file to provide details about how to connect to Home Assistant. Token, Host is mandatory. Rest is optional. 
3. Run dotnet restore in the terminal
4. Add and edit your apps in the `/src/apps` folder. There are a few code-snippets you can use.
5. Copy the edited `/src/apps` to the folder `netdaemon/apps` under your Hass.io config folder. See advanced deployment for alternatives.
6. Install NetDaemon runtime using add-on or run a docker container. Please see [https://netdaemon.xyz/docs/started/installation](https://netdaemon.xyz/docs/started/installation) for details how to run the daemon.

For detailed information about using netdaemon please see [https://netdaemon.xyz](https://netdaemon.xyz).

## Setup the environment vars
You can also setup environment varables for your Home Assistant instance. In that case you just need to rename `src/_appsettings.json` to `src/appsettings.json` and set corresponding environment vars in your host and rebuild container. The mandantory settings is `HOMEASSISTANT__TOKEN` and `HOMEASSISTANT__HOST`. All other is optional.

| Environment variable | Description |
| ------ | ------ |
| HOMEASSISTANT__TOKEN   |  Token secret to access the HA instance
  |
| HOMEASSISTANT__HOST | The ip or hostname of HA
| HOMEASSISTANT__PORT | The port of home assistant (defaults to 8123 if not specified) |
| NETDAEMON__GENERATEENTITIES | Generate entities, recommed set false unless debugging|
| NETDAEMON__APPSOURCE | The folder where it will find the NetDaemon runtime and source. If you set a `folder`, the deafult dynamically compiled sourc in this folder, if `daemonapp.csproj` it will run that project and `daemonapp.dll` for published project. These settings only apply in addon/container. For developmen just do not not set or empty `""`. 

## What about my existing apps?
The recommended way to use this template is to clone new fresh version when you upgrade and copy all .cs and .yaml files from your apps into the ./src/apps folder. 

## Advanced deployment
The default behavour of NetDaemon is do dynamically compile all apps in apps folder. If you need to deploy dependencies like custom nuget packages you have two choices, project or publish.
### Docker users
The docs assume you map volyme like:
`-v ~/netdaemon:/data`
- Project deployment: Copy everything under `./src/*` -> `~/netdaemon`. The csproj file should be in root of `~/netdaemon` . Set the `NETDAEMON__APPSOURCE="daemonapp.csproj"`
- Published deployment: publish the project by `dotnet publish -c Release ./src.` Copy everything under `src/bin/Release/net5.0/publish/*` to `~/netdaemon` and set `NETDAEMON__APPSOURCE="daemonapp.dll"` 

### Addon users
- Project deployment: Copy everything under `./src/*` -> `/config/netdaemon`. The csproj file should be in root of `~/netdaemon` . Set the `AppSource=daemonapp.csproj` in addon settings.
- Published deployment: publish the project by `dotnet publish -c Release ./src.` Copy everything under `src/bin/Release/net5.0/publish/*` to `/config/netdaemon` and set `AppSource=daemonapp.dll` in addon settings.

**Make sure you take security meassures using custom deployments!** 
## Read this if you are going to deploy apps through HACS

Each app should have it´s own subfolder under the `apps` folder. So rename the `HelloWorld` folder and `HelloWorld.cs` and `HelloWorld.yaml` according to your app. The class name should also be renamed to the same unique app name. We also recommend using namespaces and fully qualified names like the sample included in the template.

[![buymeacoffee](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/ij1qXRM6E)

## Issues

- If you have issues or suggestions of improvements to this template, please [add an issue](https://github.com/net-daemon/netdaemon-app-template)
- If you have issues or suggestions of improvements to NetDaemon, please [add an issue](https://github.com/net-daemon/netdaemon/issues)

## Discuss the NetDaemon

Please [join the Discord server](https://discord.gg/K3xwfcX) to get support or if you want to contribute and help others.

## Attribution

ICON: Attribution: [chris](https://commons.wikimedia.org/wiki/User:Chrkl) 論

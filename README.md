# This is the dev setup for NetDaemon

Use this repository as template when developing apps for NetDaemon. This template is a good starting point for your development environment. It has pre-built structure both for building apps and unit test them. Unit tests fake objects are alpha and the fake API can be changed.

## Getting started

1. Make new repository with this repo as template
2. RENAME `src/_appsettings.json` to `src/appsettings.json`. Edit the appsettings.json file to provide details about how to connect to Home Assistant. You will need a long lived token, ip, port.
3. Run dotnet restore in the terminal
4. Add and edit your apps in the `/src/apps` folder. There are a few code-snippets you can use.
5. Copy the edited `/src/apps` to the folder `netdaemon/apps` under your Hass.io config folder. 
6. Install NetDaemon runtime using add-on or run a docker container. Please see [https://netdaemon.xyz/docs/started/installation](https://netdaemon.xyz/docs/started/installation) for details how to run the daemon.

For detailed information about using netdaemon please see [https://netdaemon.xyz](https://netdaemon.xyz).

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

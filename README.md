# Collection of C# Example Projects

<img src="resources/Kifa.png?raw=true" width="80" height="80">

This repo contains various useful libraries and tools written in C#. It aims to help both as tool and as reference.

## Cloud service access libraries

We have several libraries with unified interface to access cloud storage services easily.
You can easily upload and download files with [Baidu YunPan](https://github.com/Kimi-Arthur/KifaNet/tree/master/src/Kifa.Cloud.BaiduCloud), [Google Drive](https://github.com/Kimi-Arthur/KifaNet/tree/master/src/Kifa.Cloud.Google), [Swisscom](https://github.com/Kimi-Arthur/KifaNet/tree/master/src/Kifa.Cloud.Swisscom), [Telegram](https://github.com/Kimi-Arthur/KifaNet/tree/master/src/Kifa.Cloud.Telegram) etc.

We also easily access video and manga services provided by [bilibili](https://github.com/Kimi-Arthur/KifaNet/tree/master/src/Kifa.Bilibili) and [language services](https://github.com/Kimi-Arthur/KifaNet/tree/master/src/Kifa.Languages) provided by Wiktionary, Cambridge, Dwds.

## Convenient Tools

We also provide many pre-built tools to help simplify workflows in digital life, like verified uploading and downloading files with end to end encryption with [FileUtil](https://github.com/Kimi-Arthur/KifaNet/tree/master/src/Kifa.Tools.FileUtil), creating custom Memrise courses with [MemriseUtil](https://github.com/Kimi-Arthur/KifaNet/tree/master/src/Kifa.Tools.MemriseUtil), viewing, tailoring local videos with [MediaUtil](https://github.com/Kimi-Arthur/KifaNet/tree/master/src/Kifa.Tools.MediaUtil).

## How to install

Most tools are available from [Nuget](https://www.nuget.org/packages?q=kifa.tools&packagetype=&prerel=true&sortby=relevance), while libraries are mostly provided as source code here, which you will need to clone the whole project and build with `dotnet`.

## Deployment

There are two types of binaries that can be deployed. To publish the server, you should run `tools/publish_server.sh`; to publish the binaries to Nuget, run `tools/publish.sh <path_to_csproj_file>`.

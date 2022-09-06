# MoonAsset

![](https://img.shields.io/badge/version-v0.2-green.svg)
![](https://img.shields.io/badge/license-MIT-blue.svg)

作为[MGF](https://github.com/Sarofc/com.saro.mgf)子模块

## 环境

- Unity 2021.3

## 特性

- 资源分组，冗余分析
- 一键打包，可扩展的打包流程
- 资源加载异步化，提供async/await接口
- 引用计数的资源释放模式
- 边玩边下，可直接从远端资源服务器下载资源
- 支持非unity资源(rawfile)也纳入资源管理，例如，将wwise bank之类的资源
- 虚拟文件(vfs)打包接口
- 使用`Scriptable Build Pipline`
- 提供AssetBundle浏览器，查看AssetBundle打包状况
- 提供资源引用窗口查看引用计数

## 如何使用

demo项目 [tetris-ecs-unity](https://github.com/Sarofc/tetris-ecs-unity)

## 声明

此项目基于`XAsset 4.x`版本开发，此版本为`MIT协议`

本项目不使用`XAsset 7.x`版本<https://github.com/xasset/xasset>

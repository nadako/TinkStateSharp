## Unity Specifics

While the base library itself is very generic and does not have any dependencies, one of the main motivations for its creation was to assist Unity developers with handling their models and view models. So this library also integrates with Unity out-of-the-box.

## Installation

You can easily install it via Unity's Package Manager by specifying Git URL: `https://github.com/nadako/TinkStateSharp.git?path=src`.

FYI this is the reason why the project source code is structured very differently from an usual C# solution. The `src` folder is supposed to only contain source files as well as some Unity-specific definition JSONs and meta files.

## Binding Batching

As described in the [Binding Batching](binding-batching.md) article, bindings are designed to be triggered in a batch once per frame.

To support that in the Unity engine, the library contains an implementation of a batching `Scheduler` that hooks into Unity's player loop.

## Examples

The library repository contains the [`playground-unity`](https://github.com/nadako/TinkStateSharp/tree/master/playground-unity) folder with the Unity project that contains some usage examples.

This project is also used for running automated tests in the Unity environment to make sure everything works correctly.

## TODO

> At some point the library will also include standard extensions to simplify bindings to/from uGUI and UI Toolkit as well as some tools to make sure bindings are disposed together with the game object.

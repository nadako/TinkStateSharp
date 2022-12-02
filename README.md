[![Build](https://github.com/nadako/TinkStateSharp/actions/workflows/build.yml/badge.svg)](https://github.com/nadako/TinkStateSharp/actions/workflows/build.yml)
[![codecov](https://codecov.io/gh/nadako/TinkStateSharp/branch/master/graph/badge.svg?token=92NEEMYYBL)](https://codecov.io/gh/nadako/TinkStateSharp)

# Tinkerbell Reactive State Handling for C#

Here's an uncomplicated library for dealing with mutable state in a nice reactive way.

> It is a C# port of an excellent [Haxe](https://haxe.org/) library [tink_state](https://github.com/haxetink/tink_state) by Juraj Kirchheim ([@back2dos](https://github.com/back2dos)).

[Generated API documentation](https://nadako.github.io/TinkStateSharp/api/TinkState.html) is here. Unity users might want to check out the [respective section](#unity).

## Status

**BETA**. Everything seems to be working and test coverage makes sure of it, but the code could use some polishing, naming review, performance audit. See also TODOs in the code.

## Description

This library provides three main things:

 - Observable data structures (observable meaning you can read values and subscribe to changes)
 - Simple way to define derived live-computed data from those observables (changes to the source data will automatically update the derived data)
 - Efficient binding mechanism for your code to react to changes in data (e.g. update UI)

If you're into MVVM, this library can cover both M and VM parts for you :-)

## Usage

### Observable

The core interface representing observable data is called `Observable` and is pretty straightforward (some minor details omitted for brevity):

```cs
interface Observable<T>
{
	T Value { get; }

	IDisposable Bind(Action<T> callback);
}
```

So there's a getter for the current value and a way to bind a callback for its changes. `Bind` also returns an `IDisposable` so you can easily unbind when you're not interested anymore.

There are multiple implementations of `Observable`, the most simple and lightweight one being a const observable for values that never change. It is created by a static function in the `Observable` helper class:

```cs
static class Observable
{
	static Observable<T> Const<T>(T value);
}
```

### State

One important variation of `Observable` is `State`, which is a mutable observable, meaning you can also set the value:

```cs
interface State<T> : Observable<T>
{
	T Value { get; set; }
}
```

It is also created by a static function in the `Observable` helper class:

```cs
static class Observable
{
	static State<T> State<T>(T initialValue);
}
```

### Auto-Observable

This is where the magic begins... Auto-Observable is a way to create `Observable` data derived from another source `Observable`(s) by providing a simple computation function. The resulting observable will automatically subscribe (and unsubscribe) to changes of its source data, update its value and trigger its bindings.

Like the others, it is created by a static function in the `Observable` interface:

```cs
static class Observable
{
	static Observable<T> Auto<T>(Func<T> compute);
}
```

## Examples

Here's a very basic example. We define one mutable `State` that we can change manually and derive an auto-Observable that will automatically update itself.

```cs
using TinkState;

var userName = Observable.State("Dan");
var greeting = Observable.Auto(() => $"Hello, {userName.Value}!");

Console.WriteLine(userName.Value); // Dan
Console.WriteLine(greeting.Value); // Hello, Dan!
userName.Value = "World";
Console.WriteLine(greeting.Value); // Hello, World!
```

Note how `greeting` automatically tracks changes to `userName` simply because `userName.Value` is used in the computation function we pass to its constructor.

Auto-Observable can track any kind of Observable, including other auto-Observables, which allows for creating a nice hierarchical structure for all your data and letting Tinkerbell manage update propagation for you. Let's make our example more interesting to illustrate that:

```cs
using TinkState;

var userName = Observable.State("Dan");
var hourOfDay = Observable.State(13);

var partOfDay = Observable.Auto(() =>
{
    var hour = hourOfDay.Value;
    if (hour < 7) return "night";
    if (hour < 12) return "morning";
    if (hour < 18) return "day";
    if (hour < 23) return "evening";
    return "night";
});

var greeting = Observable.Auto(() => $"Good {partOfDay.Value}, {userName.Value}!");

Console.WriteLine(greeting.Value); // Good day, Dan!

userName.Value = "World";
hourOfDay.Value = 19;
Console.WriteLine(greeting.Value); // Good evening, World!
```

Here we create an auto-Observable to calculate the part of day from the hour value, and then create another auto-Observable to construct a greeting that uses both user name and part of day. Note how changes to the source `State`s are automatically propagated through auto-Observables.

## Binding

We briefly mentioned the `Bind` method in the `Observable` interface, which allows for subscribing to `Observable` value changes to bind your code to it. There are some interesting things to talk about here as well. Let's look at the simple example:

```cs
using TinkState;

var userName = Observable.State("Dan");

var binding = userName.Bind(name => Console.WriteLine($"The name is: {name}"));
// "The name is: Dan" is immediately printed on binding

userName.Value = "John";
// "The name is: John" is printed because the value has changed

binding.Dispose();

userName.Value = "World";
// Nothing is printed, because the binding was disposed
```

Firstly, the binding callback is automatically invoked upon binding. This is done for practical reasons, as the most frequent use-case for bindings are updating UIs and we (almost) always want initial values to be set together with binding.

Next, while value is changed, the binding is obviously triggered, so we get a second print after that.

And finally, if we dispose the binding, subsequent changes won't invoke our callback, since the subscription was undone.

### Binding batching

One interesting thing about bindings is that normally their callbacks are not executed directly when an observable value is changed. Instead they are scheduled for execution in a batch once per frame. This makes a lot of sense for the main use case for this library: updating UIs. So when your code updates states and trigger value change notifications, all bindings will be batched for execution before the next UI redraw happens.

This has a nice effect that a binding is only invoked once per changed observable, even if you actually set values multiple times during processing. Even more so, if the final value is unchanged after all assignments and/or recomputations - the binding will not be invoked at all!

This is a good optimization and is what we want in most cases, however sometimes we want to make our bindings executed directly. Luckily we can control this by providing the `scheduler` argument to the `Bind` method and using `Scheduler.Direct` instance:

```cs
using TinkState;

var playerName = Observable.State("Dan");

// this binding is not batched and is executed directly
playerName.Bind(name => Console.WriteLine(name), scheduler: Scheduler.Direct);
```

NOTE: The scheduling mechanism obviously depends on the run-time environment the library is used in. And per-frame-batching requires some kind of application main loop. You should implement the `Scheduler` interface and assign an instance of the implementation to `Observable.Scheduler` at start (before doing bindings), otherwise bindings will fall back to the direct scheduler and the callbacks will be invoked synchronously on value changes.

The library currently provides a single batch scheduler implementation for the Unity engine. See [Unity](#unity) section for more info.

## Auto-runs

Sometimes we just want to run some code that depends on observable values and have it automatically re-running in case any of the used values are changed. We could use `Observable.Auto` with some dummy value for that, however if there's no need for an actual value it can get a bit awkward. In cases like this there's a handy `Observable.AutoRun` helper that provides a way to run some code while tracking observable access and re-run it when any of their values change, just like with auto-Observable computation functions. Check out this example:

```cs
using TinkState;

var amount = Observable.State(1);
var fruit = Observable.State("apple");

var binding = Observable.AutoRun(() =>
{
    Console.WriteLine($"I want {amount.Value} of {fruit.Value}");
});
// immediately prints: I want 1 of apple

fruit.Value = "banana";
// prints: I want 1 of banana

amount.Value = 100;
// prints: I want 100 of banana

binding.Dispose();

amount.Value = 5;
fruit.Value = "orange";
// nothing is printed as the binding was disposed
```

## Asynchronous Auto-Observables

It is also possible to have auto-observables from `async` computation functions. Auto-observable will take care of starting and cancelling asynchronous tasks while tracking observable access at their every execution step.

The value of such executables will be the special `AsyncComputeResult<T>` structure that represents current loading state and holds the resulting value (or exception).

Here's an example. Let's say we have some user search service that returns a list of names based on given search string:

```cs
class SearchService
{
    public static async ValueTask<string[]> Request(string searchString)
    {
        var database = new string[]
        {
            "John Doe",
            "Jane Doe",
            "Alexei",
            "Alexander",
        };
        await Task.Delay(500); // pretend we're doing some async loading :)
        return database.Where(name => name.ToLower().Contains(searchString.ToLower())).ToArray();
    }
}
```

Now we can have a state object for the search string and an auto-Observable that tracks it and automatically requests the service for results:

```cs
using TinkState;

var searchString = Observable.State("");
var searchResults = Observable.Auto(async () => await SearchService.Request(searchString.Value));

searchResults.Bind(result =>
{
    var text = result.Status switch
    {
        AsyncComputeStatus.Loading => "Loading...",
        AsyncComputeStatus.Done => string.Join("\n", result.Result),
        AsyncComputeStatus.Failed => "Error: " + result.Exception.Message,
        _ => throw new Exception()
    };
    Console.WriteLine(text);
});
// prints "Loading..." on bind
// prints the full list after 500ms

Thread.Sleep(600);

searchString.Value = "doe";
// prints "Loading..." again as auto-observable is asynchronously recomputing
// prints "John Doe" and "Jane Doe" as per search results after 500ms
```

> TODO: cancellable compute tasks

## Custom Comparers

`Observable`s are smart enough not to trigger bindings and recalculations when the value hasn't changed. This is happening on two levels:
 - Observable objects only dispatch updates when the new value is actually different.
 - Binding only invoke callbacks when the new value is different from the previous one.

You can control how the old vs new comparison is made by providing an `IEqualityComparer<T>` implementation to either an observable or a binding or both. It's done by passing an optional `comparer` argument to `Observable.State`, `Observable.Auto` or `Bind`. If not provided, the standard `EqualityComparer<T>.Default` comparer will be used.

Let's look at the example. Say we want to work with this structure and implement a comparer for it:

```cs
class Entry
{
    public string Name;
    public int Age;
}

class EntryComparer : IEqualityComparer<Entry>
{
    public static readonly EntryComparer Instance = new EntryComparer();

    public bool Equals(Entry x, Entry y)
    {
        return x.Name == y.Name && x.Age == y.Age;
    }

    public int GetHashCode(Entry obj)
    {
        return HashCode.Combine(obj.Name, obj.Age);
    }
}
```

Now we can simply pass the comparer to the `State` constructor and see how the binding is not being triggered if the field values stay the same even if the actual value object is new.

```cs
using TinkState;

var entry = Observable.State(new Entry { Name = "John", Age = 30 }, EntryComparer.Instance);

entry.Bind(entry => Console.WriteLine(entry.Name + " " + entry.Age));
// triggered on bind

entry.Value = new Entry { Name = "John", Age = 30 };
// not triggered as the value is the same according to EntryComparer

entry.Value = new Entry { Name = "John", Age = 31 };
// triggered as the value is different according to EntryComparer
```

You can also specify a comparer for a single binding, in which case it will be used together with observable's comparer as an additional check for skipping binding callback invocation. Here's an example:

```cs
using TinkState;

var name = Observable.State("Dan");

name.Bind(name => Console.WriteLine($"Name: {name}"), CaseInsensitiveComparer.Instance);

name.Value = "DAN";
// the binding is not invoked as it considers values the same because of our custom comparer
// the actual State has the new value though, because we only specified comparer for a binding
Console.WriteLine(name.Value); // DAN

name.Value = "John"; // binding is invoked (Name: John)

class CaseInsensitiveComparer : IEqualityComparer<string>
{
    public static readonly CaseInsensitiveComparer Instance = new CaseInsensitiveComparer();

    public bool Equals(string x, string y)
    {
        return x.ToLower() == y.ToLower();
    }

    public int GetHashCode(string obj)
    {
        return obj.ToLower().GetHashCode();
    }
}
```

## Observable Collections

In addition to single value observables, there are also a number of observable collections, such as `ObservableList` and `ObservableDictionary`. They are wrappers over standard collections and provide pretty much the same API. The difference is that any changes to the collection will be tracked by auto-Observables if they are accessed in their compute function. Let's look at this example:

```cs
using TinkState;

var list = Observable.List(new[] { 1, 2, 3 });

var sum = Observable.Auto(() =>
{
    var sum = 0;
    foreach (var i in list) sum += i;
    return sum;
});

sum.Bind(sum => Console.WriteLine("Sum is: " + sum));
// initial print: Sum is: 6

list.Add(10);
// changed print: Sum is: 16
```

As you can see, the sum auto-Observable gets automatically updated when we add a new element to our observable list.

## Best Practices & Things to Watch Out For

> TODO: state updates from bindings and computations, binding disposal

## Thread-safety

This library is NOT thread-safe. There's no synchronization code and it uses some static fields, so beware.

## Unity

While the base library itself is generic and does not have any dependencies, one of the main motivations for its creation was to assist Unity developers with handling their models (and most importantly view models). So this library also integrates with Unity out-of-the-box, meaning that:

 - You can easily install it via Unity's Package Manager by specifying Git URL: `https://github.com/nadako/TinkStateSharp.git?path=src`
 - It implements [batching `Scheduler`](#binding-batching) for bindings that hooks into Unity's player loop.

> TODO: at some point it will also provide extensions to simplify bindings to Unity UI/UIElements as well as binding lifetime management bound to GameObject.

FYI, Unity is also the reason the project sources structure is so... different from a normal C# solution. The `src` folder is supposed to only contain source files as well as some Unity-specific definition JSONs and meta files.

This repo also contains the `unity-playground` folder with the Unity project that contains some usage examples as well as run all tests in the Unity environment.
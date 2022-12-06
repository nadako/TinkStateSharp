## Binding batching

The binding mechanism is designed to support *batching*. This means that the binding callbacks are not executed directly when an observable value is changed, instead they are scheduled for execution all together in a batch once per *frame*.

This makes a lot of sense for updating views: after the business-logic is done with updating observable data, the bindings for changed values are executed in a batch before the next UI redraw happens.

This approach has a nice effect that a binding is only invoked once per changed observable, even if you actually set values multiple times during frame. Even more so, if the final value is unchanged after all assignments and/or recomputations - the binding will not be invoked at all!

This is a good optimization and is what we want in most cases, however sometimes we want to make our bindings executed directly. Luckily we can control this by providing the `scheduler` argument to the `Bind` method and using `Scheduler.Direct` instance:

```cs
var playerName = Observable.State("Dan");

// this binding is never batched and is executed directly
playerName.Bind(name => Console.WriteLine(name), scheduler: Scheduler.Direct);
```

## Run-time environment specifics

The scheduling mechanism obviously depends on the run-time environment the library is used in. Per-frame batching requires some kind of application loop where there's a notion of *frame* to begin with, which depends on the program.

TinkState# provides a batching scheduler implementation for the [Unity](https://unity.com/) game engine out of the box. See the [respective article](unity.md) for more details.

In any other environment, the direct scheduler will be used by default, meaning that all bindings will be executed directly. Thankfully it's pretty easy to plug in a custom scheduler. To do that, one should implement the [`Scheduler`](xref:TinkState.Scheduler) interface and assign an instance of the implementation to `Observable.Scheduler` at start (before doing any bindings).
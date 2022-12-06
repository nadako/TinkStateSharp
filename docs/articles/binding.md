## Bindings

We briefly mentioned the `Bind` method in the [`Observable`](observable.md) article, which allows for subscribing to `Observable<T>` value changes to bind your code to it.

The most common use-case for binding is updating view components automatically and in a timely manner. However it can be used for anything else as the `Bind` method simply takes a callback to invoke with an updated value.

There are a couple interesting details to describe about bindings. Let's take a look at the following example:

```cs
// create an observable state
var userName = Observable.State("Dan");

// create a binding
var binding = userName.Bind(name => Console.WriteLine($"The name is: {name}"));
// "The name is: Dan" is immediately printed on binding

userName.Value = "John";
// "The name is: John" is printed because the value has changed

// dispose the binging
binding.Dispose();

userName.Value = "World";
// nothing is printed, because the binding was disposed
```

Firstly, the binding callback is immediately invoked upon binding. This is done for practical reasons as we almost always want to set initial values together with subscribing to changes.

Next, when the observable value is changed the binding is obviously triggered, so the callback gets invoked.

Finally, bindings are `IDisposable` objects and disposing a binding means that subsequent value changes won't invoke its callback, since the subscription was undone.

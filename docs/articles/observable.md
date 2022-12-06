## Observable

The core type of this library is `Observable<T>`. It represents observable data and its definition is pretty straightforward:

```cs
interface Observable<T>
{
	T Value { get; }

	IDisposable Bind(Action<T> callback);

	// some minor details omitted for brevity
}
```

So there's a getter for the current value and a way to bind a callback for its changes. `Bind` also returns an `IDisposable` so you can easily unbind when you're not interested anymore.

## Creating observables

There are multiple implementations of `Observable<T>` and most of them can be created using static methods in the [`Observable`](xref:TinkState.Observable) helper class.

See further articles about specific kinds of observables.
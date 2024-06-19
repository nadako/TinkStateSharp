# Constant Observable

The most simple and lightweight kind of observable is the  *const* observable for values that never change.

Like most of observables, it is created by the `Observable.Const<T>` method in the [`Observable`](xref:TinkState.Observable) helper class.

```cs
static class Observable
{
	static Observable<T> Const<T>(T value);
}
```

By itself a constant observable is not very useful as it's simply a wrapper around a read-only value, however since it's an implementation of the general `Observable<T>` interface, it can be passed to any code that works with observables, including [auto-observables](auto.md).

## Example

```cs
var o = Observable.Const(42);
Console.WriteLine(o.Value); // 42
```

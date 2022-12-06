## State

One important variation of `Observable<T>` is `State<T>`, which is a mutable observable, meaning you can also set the value:

```cs
interface State<T> : Observable<T>
{
	T Value { get; set; }
}
```

Like most of observables, `State<T>` can be created by a static function in the `Observable` helper class:

```cs
static class Observable
{
	static State<T> State<T>(T initialValue);
}
```

## Example

```cs
var o = Observable.State(42);
Console.WriteLine(o.Value); // 42
o.Value = 24;
Console.WriteLine(o.Value); // 24
```

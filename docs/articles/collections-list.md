# Observable List

`ObservableList<T>` is an observable wrapper around the standard `List<T>`, providing the same API for managing the collection.

In addition to the standard list method, it provides the [`Observe`](xref:TinkState.ObservableList`1.Observe) method to expose itself
as a read-only observable so one can bind to it and react to its changes.

```csharp
interface ObservableList<T> : IList<T>
{
	Observable<IReadOnlyList<T>> Observe();
}
```

Like most of observables, `ObservableList<T>` can be created by a static function in the `Observable` helper class:

```csharp
static class Observable
{
	static ObservableList<T> List<T>();
}
```

Adding and removing elements will naturally trigger its bindings and propagate through auto-observables.

## Example

```csharp
var list = Observable.List<int>();
list.Add(1);
list.Add(2);

var auto = Observable.Auto(() => list.Sum());
Console.WriteLine(auto.Value); // 3

// initial binding prints 1, 2
list.Observe().Bind(readOnlyList =>
{
	foreach (var item in readOnlyList) Console.WriteLine(item);
});

list.Add(3); // binding prints 1, 2, 3

Console.WriteLine(auto.Value); // 6
```

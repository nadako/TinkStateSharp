# Observable Dictionary

`ObservableDictionary<T>` is an observable wrapper around the standard `Dictionary<T>`, providing the same API for managing the collection.

In addition to the standard dictionary methods, it provides the [`Observe`](xref:TinkState.ObservableDictionary`2.Observe) method to expose itself as a read-only observable so one can bind to it and react to its changes.

```csharp
interface ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
{
	Observable<IReadOnlyDictionary<TKey, TValue>> Observe();
}
```

Like most of observables, `ObservableDictionary<T>` can be created by a static function in the `Observable` helper class:

```csharp
static class Observable
{
	static ObservableDictionary<TKey, TValue> Dictionary<TKey, TValue>();
}
```

Adding and removing items will naturally trigger its bindings and propagate through auto-observables.

## Example

```csharp
var dict = Observable.Dictionary<string, int>();
dict["a"] = 1;
dict["b"] = 2;

var auto = Observable.Auto(() => string.Join(",", dict.Select(kvp => $"{kvp.Key}:{kvp.Value}")));
Console.WriteLine(auto.Value); // a:1,b:2

// initial binding prints a:1 and b:2
dict.Observe().Bind(readOnlyDict =>
{
	foreach (var (key, value) in readOnlyDict) Console.WriteLine($"{key}:{value}");
});

dict["c"] = 3; // binding prints a:1, b:2 and c:3

Console.WriteLine(auto.Value); // a:1,b:2,c:3
```

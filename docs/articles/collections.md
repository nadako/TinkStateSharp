# Observable Collections

In addition to single-value observables, there are also a number of observable collections, such as `ObservableList` and `ObservableDictionary`.

They are wrappers over standard collections and provide pretty much the same API. The difference is that any changes to the collection will be tracked by auto-observables if the collection is read in the auto-observable computation function.

## Example

```cs
var list = Observable.List(new[] { 1, 2, 3 });

var sum = Observable.Auto(() => list.Sum()); // LINQ sum method

Console.WriteLine(sum.Value); // 6

list.Add(10);

Console.WriteLine(sum.Value); // 16
```

As you can see, the sum auto-observable gets automatically updated when we add a new element to our observable list.

For more details, see articles about specific collection types.

# Custom Comparers

Observables are smart enough not to trigger bindings and recalculations when the value hasn't actually changed. This is handled on two levels:

 - Observable objects only dispatch updates when the new value is actually different.
 - Binding only invoke callbacks when the new value is different from the previous one.

You can control how the old vs new comparison is made by providing an `IEqualityComparer<T>` implementation to an observable or a binding or both.

It's done by passing an optional `comparer` argument to observable creation methods or the `Bind` method. If not provided, the the standard `EqualityComparer<T>.Default` comparer will be used.

## Example: Custom Comparer for State

Say we want to work with the following `Entry` class and implement a comparer that considers objects equal if their names are equal:

```cs
class Entry
{
	public string Name;
	public int Age;
}

class EntryComparer : IEqualityComparer<Entry>
{
	public static readonly EntryComparer Instance = new EntryComparer();

	EntryComparer() {}

	public bool Equals(Entry x, Entry y)
	{
		if (x == null && y == null) return true;
		if (x == null || y == null) return false;
		return x.Name == y.Name && x.Age == y.Age;
	}

	public int GetHashCode(Entry obj)
	{
		return HashCode.Combine(obj.Name, obj.Age); // not used by TinkState#
	}
}
```

Now we can simply pass the comparer to the `State` constructor and see how the binding is not being triggered if the field values stay the same even if the actual value object is new:

```cs
var state = Observable.State(new Entry { Name = "John", Age = 30 }, EntryComparer.Instance);

state.Bind(entry => Console.WriteLine(entry.Name + " " + entry.Age));
// triggered normally on bind

state.Value = new Entry { Name = "John", Age = 30 };
// not triggered as the value is the same according to EntryComparer

state.Value = new Entry { Name = "John", Age = 31 };
// triggered as the value is different according to EntryComparer
```

## Example: Custom Comparer for Binding

We can also specify a comparer for a single binding, in which case it will work together with the observable's comparer as an additional check for skipping binding callback invocation.

Let's say we don't want our binding callback to a string observable to be invoked when just the case is changed. For that we implement a custom comparer that compares strings while ignoring their case:

```cs
class CaseInsensitiveComparer : IEqualityComparer<string>
{
	public static readonly CaseInsensitiveComparer Instance = new CaseInsensitiveComparer();

	CaseInsensitiveComparer() {}

	public bool Equals(string x, string y)
	{
		if (x == null && y == null) return true;
		if (x == null || y == null) return false;
		return string.Equals(x, y, StringComparison.CurrentCultureIgnoreCase);
	}

	public int GetHashCode(string obj)
	{
		return obj.ToLower().GetHashCode(); // not used by TinkState#
	}
}
```

Now if we pass our custom comparer to the `Bind` method, the binding callback won't be invoked for the updates that only change letter case:

```cs
var name = Observable.State("Dan");

name.Bind(value => Console.WriteLine($"Name: {value}"), CaseInsensitiveComparer.Instance);
// the callback is invoked normally on bind: `Name: Dan`

name.Value = "DAN";
// the callback is NOT invoked, since the custom comparer considers old and new values the same

Console.WriteLine(name.Value); // DAN
// the actual State has the new value though, because we only specified comparer for a binding

name.Value = "John";
// the callback is invoked: `Name: John`
```

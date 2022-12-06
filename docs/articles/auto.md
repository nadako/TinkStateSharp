## Auto-Observable

Auto-observables are the main way to derive observable data from a number of *source* observables by providing a *computation* function.

The resulting observable will automatically subscribe (and unsubscribe) to changes of its source data, update its value and trigger its bindings.

Like the others, it is created by a static function in the [`Observable`](xref:TinkState.Observable) helper class:

```cs
static class Observable
{
	static Observable<T> Auto<T>(Func<T> compute);
}
```

## Examples

Here's a very basic example. We define one mutable [`State`](state.md) that we can change manually and derive an auto-observable that will automatically update itself.

```cs
var userName = Observable.State("Dan");
var greeting = Observable.Auto(() => $"Hello, {userName.Value}!");

Console.WriteLine(userName.Value); // Dan
Console.WriteLine(greeting.Value); // Hello, Dan!
userName.Value = "World";
Console.WriteLine(greeting.Value); // Hello, World!
```

Note how `greeting` automatically tracks changes to `userName` simply because `userName.Value` is used in the computation function we pass to its constructor.

---

Auto-observable can track any kind of observable, including other auto-observables, which allows for creating nice hierarchical structure for the data and letting TinkState# manage update propagation for you. Let's make our example more interesting to illustrate that:

```cs
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

Here we create an auto-observable to calculate the part of day from the hour value, and then create another auto-observable to construct a greeting that uses both user name and part of day. Note how changes to the source `State`s are automatically propagated through auto-observables.

## Computations, caching and performance.

Auto-observables make an effort to minimize redundant work by doing some tracking and caching internally.

They are *lazy*, meaning they only call computations when someone is interested in their values (i.e. `Value` property access or a `Bind`).

They also cache the computation result and only call computation again if any of the tracked *source* observables are changed.

And they also make sure to only track those observables that are accessed by the current computation, subscribing to and unsubscribing from source observables accordingly. This can make a difference in case of branching within computation function, like in the following example:

```cs
var stateA = Observable.State("a");
var stateB = Observable.State("b");

var condition = Observable.State(true);

var auto = Observable.Auto(() =>
{
	if (condition.Value)
	{
		return stateA.Value; // tracked only when condition is true
	}
	else
	{
		return stateB.Value; // tracked only when condition is false
	}
});

Console.WriteLine(auto.Value); // a
condition.Value = false;
Console.WriteLine(auto.Value); // b
```

As you can see, while auto-observables are the most complex observables internally, they do their best to be as efficient as possible, so you shouldn't hesitate to use them for any kind of derived computed data.

With that in mind, of course one has to be careful with unnecessary invalidations and heavy recomputations.

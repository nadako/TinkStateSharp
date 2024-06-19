# Auto-Runs

Sometimes we simply want to run some code that depends on observable values and have it automatically re-invoked in case any of the used values is changed. We could use an [auto-observable](auto.md) with some dummy value for that, however if there's no need for an actual value it can get a bit awkward.

For cases like this there's [`Observable.AutoRun`](xref:TinkState.Observable.AutoRun(System.Action,TinkState.Scheduler)), a handy helper that provides a way to run some code while tracking observable access and re-run it when any of their values change, just like with auto-observable computation functions.

## Example

```cs
var amount = Observable.State(1);
var fruit = Observable.State("apple");

var binding = Observable.AutoRun(() =>
{
    Console.WriteLine($"I want {amount.Value} of {fruit.Value}");
});
// immediately prints: I want 1 of apple

fruit.Value = "banana";
// prints: I want 1 of banana

amount.Value = 100;
// prints: I want 100 of banana

binding.Dispose();

amount.Value = 5;
fruit.Value = "orange";
// nothing is printed as the auto-run binding was disposed
```

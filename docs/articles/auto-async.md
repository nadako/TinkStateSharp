# Asynchronous Auto-Observable

In addition to normal [`auto-observables`](auto.md), it is also possible to create auto-observables using `async` computation functions. Auto-observable will take care of starting and awaiting asynchronous computation while tracking observable access at their every execution step.

The value of such executables will be the special [`AsyncComputeResult<T>`](xref:TinkState.AsyncComputeResult`1) structure that represents current loading state and holds the resulting value (or exception).

## Example

Let's say we have some user asynchronous search service that returns a list of names based on given search string:

```cs
class SearchService
{
    public static async ValueTask<string[]> Request(string searchString)
    {
        var database = new string[]
        {
            "John Doe",
            "Jane Doe",
            "Alexei",
            "Alexander",
        };
        await Task.Delay(50); // pretend we're doing some async loading
        return database.Where(name => name.Contains(searchString)).ToArray();
    }
}
```

Now let's create a mutable [`State`](state.md) to hold our search string value and an auto-observable that tracks it and automatically requests the service for results:

```cs
var searchString = Observable.State("");
var searchResults = Observable.Auto(async () => await SearchService.Request(searchString.Value));

Console.WriteLine(searchResults.Value.Status); // Loading

// let some time pass
await Task.Delay(100);

Console.WriteLine(searchResults.Value.Status); // Done
Console.WriteLine(string.Join(", ", searchResults.Value.Result)); // full list of names

// now change the search string to only include names with "Doe"
searchString.Value = "Doe";

Console.WriteLine(searchResults.Value.Status); // Loading

// let some time pass again
await Task.Delay(100);

Console.WriteLine(searchResults.Value.Status); // Done
Console.WriteLine(string.Join(", ", searchResults.Value.Result)); // only John and Jane Doe
```

As you can see, async auto-observables work pretty much like normal ones when it comes to source tracking and recomputing their values.

## Implementation detail

Internally, async auto-observables are a bit interesting. In order to track observable access at each step of the `async` function execution (i.e. between `await`s), they rely on C# 7.0 [custom async task builder](https://github.com/dotnet/roslyn/blob/main/docs/features/task-types.md) feature. This is why the task type for the async computation functions is required to be the special [`AsyncComputeTask<T>`](xref:TinkState.AsyncComputeTask`1) one.

## Cancellable computations

> TODO: document when the feature is finalized

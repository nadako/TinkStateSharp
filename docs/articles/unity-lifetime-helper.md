## Binding Lifetime

It is very important to dispose bindings to observables when they are no longer needed for two main reasons:

 * bindings hold reference to their handlers, and thus to everything that handler is accessing, and not disposing it in a timely manner can lead to memory leaks
 * invoking a binding callback that tries to access a destroyed Unity object will lead to errors

This means that we need to store references to binding `IDisposable` objects and dispose them on `OnDestroy` (or `OnDisable` callbacks). Simple enough, however it can get a bit tedious and boilerplate-y to do this manually, which leads to mistakes and ultimately to issues described above.

So, Tinkerbell provides a set of helper extension methods to make this easier.

## DisposeOnDestroy

The simplest way to make your binding (or any `IDisposable`) getting disposed when a game object is destroyed is the `DisposeOnDestroy` helper method. It does just that: when a game object receives the `OnDestroy` message, dispose given disposable.

```csharp
public void Init(Observable<int> health)
{
    var binding = health.Bind(OnHealthChanged);
    gameObject.DisposeOnDestroy(binding);
}
```

**NOTE:** This works well in many cases, however you should be aware of the caveats of using `OnDestroy` in Unity: this method will NOT be invoked if the object is inactive. So you have to be very careful with managing the life cycle of your actual game objects when using this method.

There is a sister extension method `ClearOnDestroyDisposes` that you can manually call to dispose all currently registered disposables.

## RunOnActive

A different approach is to only create bindings when the game object actually becomes active, and then dispose those bindings when it becomes inactive again (which also happens when the object is destroyed). The `RunOnActive` method provides some help to implement this pattern. Let's rewrite the previous example using it:

```csharp
public void Init(Observable<int> health)
{
    gameObject.RunOnActive(() =>
    {
        var binding = health.Bind(OnHealthChanged);
        return binding;
    });
}
```

The given callback will be automatically invoked once the game object becomes active (or immediately, if the game object is already active). The callback must return an `IDisposable` that will be automatically disposed once the game object becomes inactive or destroyed.

Using this pattern, if the game object switches between being active and inactive multiple times, the bindings will be automatically created and disposed correspondingly, which can be useful for avoiding unnecessary processing for invisible objects.

There is a sister extension method `ClearOnActiveRuns` that you can manually call to dispose all currently registered on-active run callbacks.

## Game Object Pooling

The methods described above play well with object pooling. Depending on your pool design you can use either `RunOnActive` or `DisposeOnDestroy` in the initialization method of your pooled objects, just don't forget to call the corresponding `ClearOnActiveRuns` or `ClearOnDestroyDisposes` methods when you return the object into the pool, since you don't want to have lingering bindings from the previous usage of the pooled object.

Here's an example:

```csharp
public class PooledItem : MonoBehaviour
{
    public void Init(Observable<string> data)
    {
        gameObject.DisposeOnDestroy(data.Bind(...));
    }

    public void OnPoolGet()
    {
        gameObject.SetActive(true);
    }

    public void OnPoolRelease()
    {
        gameObject.ClearOnDestroyDisposes();
        gameObject.SetActive(false);
    }
}
```
```csharp
var itemPool = new ObjectPool<PooledItem>(
    () => Instantiate(itemPrefab, itemContainer),
    item => item.OnPoolGet(),
    item => item.OnPoolRelease(),
    item => Destroy(item.gameObject)
);

itemPool.Get().Init(myData);
```
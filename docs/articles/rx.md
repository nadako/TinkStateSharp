# Rx/R3 and TinkState#

Probably the most popuplar general reactivity framework is [Rx](https://reactivex.io/). For C# (and Unity) a great implementation of it is [R3](https://github.com/Cysharp/R3/) (former UniRx).

The use cases and some functionality of R3 and TinkState# interesects, which might rise a question of which library to choose for your project. Let's try comparing TinkState# and R3 regarding their scope, philosophy and technical implementation.

---

**R3** defines observables and observers in a very generic way, more or less in the spirit of standard .NET IObservable/IObserver:
 - Observable is anything that can be subscribed to.
 - Observer is a set of callbacks (next/error/complete) invoked by the subscribed observable.

So basically, an R3 observable is a potentially finite stream of values with error handling. The power comes from a large library of LINQ-like observable implementations (operators and factories) that you can chain together to filter, combine and transform these streams and ultimately subscribe to the final values.

One important (in our context) kind of R3 observable is ReactiveProperty, which is a wrapper around a value
that notifies its observers when you change it.

---

**TinkState#** however focuses specifically on observable *values* rather than generic notifications, thus its definition of Observable is different:

 - Observable represents a value that can change.
 - Bindings can be added to observables to react to their values.

From these definitions, TinkState# Observable is closer to R3 ReactiveProperty and instead of thinking in terms of value streams and processing pipelines, we operate with observable values and bindings to them.

When it comes to processing values and creating derived observables, instead of the aforementioned collection of operator/factory observables found in R3, TinkState# relies on the idea of [*auto-observables*](auto.md), which are created from a computation function that automatically tracks source observables without the need to explicitly link them together.

---

As you can see, TinkState# focus is much narrower, it doesn't seek to unify *anything that can be reacted to* under the standard API and instead aims to streamline mutable state management and provide minimalistic and concise APIs for that. Whether that's a good thing or not - that's up to the reader to decide for their projects. :)

---

> TODO: add performance benchmark comparisons for the intersecting functionality

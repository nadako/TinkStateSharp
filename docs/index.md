# Introduction

Managing and displaying large amounts of mutable data is probably the main problem of UI-centric programs, including a lot of modern games.

We want to structure our data nicely, as well as combine, transform and cache it, and we want to make sure that changes are properly propagated and views are updated accordingly.

TinkState# aims to simplify and streamline this. In essence, it provides:

 - Observable data structures (meaning you can read values and subscribe to changes).
 - Means to define derived live-computed data from those observables (meaning changes to source values automatically updates derived values).
 - Efficient binding mechanism for your code to react to changes (e.g. update the UI).

In other worlds, TinkState# implements reactive state handling. In terms of [MVVM](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93viewmodel) architecture, it covers both model and viewmodel components.

---

If you're considering using TinkState# in a [Unity](https://unity.com/) project, please also check out the [Unity Specifics](articles/unity.md) article.

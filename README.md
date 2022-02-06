# Gio's Gameplay Framework for Unity

A solid, battle-tested framework to architect games in Unity. This repository is meant to be used as a [UPM package](https://docs.unity3d.com/Manual/upm-ui.html).

# Summary

## Installation:
Install via UPM using the link: `https://github.com/GiovanniZambiasi/gameplay-framework-unity.git`

## Callbacks

This framework defines *custom callbacks*. This is done to avoid the inconsistencies and performance concerns of Unity's built-in *life-cycle callbacks\**. Unity callbacks should be avoided as often as possible.

<a name="life-cycle-callbacks">\* *Unity's life-cycle callbacks are:* `Awake`, `Start`, `Update`, and `OnDestroy`</a>

### Custom callback cheat sheet

| Keyword | Corresponding Unity Callback |
| :---: | :---: |
| Setup | Start/Awake |
| Dispose | OnDestroy |
| Tick | Update |

### Why custom callbacks?

Having manual control over the order of the *setup/update/dispose* of your classes can be **very beneficial**. This avoids race-conditions between `MonoBehaviour`s (ever had a bug where some `MonoBehaviour`'s `Start` method got called before another's, and the former depended on the latter to initialize itself?). It also enables any developer in the project to understand **exactly in what order** things are happenning, without having to worry about the [Script Execution Order settings](https://docs.unity3d.com/Manual/class-MonoManager.html) hidden away in a menu. Defining custom `Tick(float deltaTime)` methods can also be extremely useful when writing [unit tests](https://docs.unity3d.com/2017.4/Documentation/Manual/testing-editortestsrunner.html).

# Systems
A `System` is an **entry point with the engine**. It's meant to be an autonomous class that initializes/disposes/updates itself through [*Callbacks*](#callbacks). `System`s can be big or small, and you can define as many as you like. While designing `System`s, try to find *isolated chunks of behaviour* in your project. This will help keep related things together, and will naturally avoid making a mess with your scripts and `namespace`s. It's encouraged to put each `System` in a separate [Assembly](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html) (and therefore a separate `namespace`). Try to keep `System`s **as indepentent from one another as possible**. For a *Counter-Strike* style game, you could define the gameplay portion as a `GameplaySystem`, and the Menus and matchmaking as a `MenuSystem`, for example. **Communication between `Systems` must always be abstracted** (a `System` shouldn't have *any* knowledge about another `System`).

A `System` can encapsulate it's behaviour using `Manager`s. It can have any number of them, and their lifetimes will be managed by their owning `System`. They have generic callbacks for `Setup`, `Dispose` and `Tick(float deltaTime)`, but can implement any overloads your project requires. `Manager`s can be added to a system using the *hierarchy* of a *Unity scene*. Any `GameObject` with a `Manager` component **that's a child of a `System`** will be registered:

![image](https://user-images.githubusercontent.com/46461122/152656464-d37024dc-b370-4d74-8fb4-e41ed753a112.png)

During `Setup`, the `System` will initialize each `Manager`, in the exact order of the hierarchy. In the example above, `EarlyManager` will be setup *first*, and `LateManager` will be setup *last*. This is also the order in which the `Tick` funcitions will get called.
`Dispose` is a bit different. When the `System` is disposing it's `Managers`, this will happen in the **reverse order**. In the example above, `LateManager` will be the *first disposed*, and `EarlyManager` will be *last*. In most cases, this is the desired behaviour.

# Managers
A `Manager` is the basic *building-block* of a `System`. The main difference between `System`s and `Manager`s is that **`Manager`s are not autonomous**. This means that their life-cycles must be managed entirely by a `System`. In other words, they must not implement any [*life-cycle related*](#life-cycle-callbacks) Unity callbacks. `Manager`s already implement default life-cycle callbacks, but more callbacks can be defined to match your game's needs. In a turn-based game, you could define `TurnEnd` and `TurnStart` callbacks, passed along to your `Manager`s by some `GameplaySystem`, for example.

Just like with `System`s, **communication between `Manager`s must always be abstracted**.  `Manager`s should also have their own `namespaces`:

![Anatomy of a System](https://user-images.githubusercontent.com/46461122/152659092-e5dedab5-48a6-431c-8f3b-8281e3120fa9.png)  
*In the above example, `Market` is the root namespace of the `System`. Each `Manager` defines it's own child-namespace, and must never reference one another*

## Managing dependencies
`Manager`s are likely to have varying dependencies that must be fulfilled during their `Setup`. You could have a `StoreManager` which needs a reference to a `StoreData` `ScriptableObject`, for example. In this case, the default `Setup` callback may not be so useful, and *custom overloads* should be defined. It's encouraged to use the same naming for your default callback overloads. For example, the `StoreManager` could have a `Setup(StoreData storeData)` overload for the default `Setup` callback. The `System` could then *override* the `SetupManagers` method, and call the overloaded version of `Setup`:
```cs
public class MenuSystem : System
{
    [SerializeField] private StoreData _storeData;

    protected override void SetupManagers()
    {
        base.SetupManagers();

        StoreManager storeManager = GetManager<StoreManager>();
        storeManager.Setup(_storeData);
    }
}
```

# Abstraction
As mentioned before: *abstraction is important* and, in most cases, *should be encouraged*. It enables developers to write clean, encapsulated code that is easily **understood, managed and tested.** However, as important as it may be, abstraction can also be quite complicated to apply consistently.

## When do I use abstraction?
Provided you've followed all the rules from the chapters above, a good guideline you can follow when it comes to abstraction is this:

- **Types should only know about other types in their own `namespace`s**

This doesn't mean that you shouldn't use abstraction within a particular `namespace`. It can, and should, be considered. However, the above quote should be followed *as a rule*. Here's an example:

![Abstraction map](https://user-images.githubusercontent.com/46461122/152663172-970889fc-f20d-4f28-bf84-33a00ca6ffa9.png)

In the image above, the dotted lines represent a map of *who knows whom*. Notice how all the lines coming from the inner namespace `TheAncientScrolls.Dragons` have arrows, symbolizing that they only go one way. In the root ``namespace``, there are 3 types: `AttributeSet`, `IInteractable`, `ICatchFire`. They can all know about each other, as they're in the same `namespace`. However, they **can't know about** any types inside `TheAncientScrolls.Dragons`. On the other hand, `TheAncientScrolls.Dragons` is still a part of `TheAncientScrolls` `namespace`. Therefore, all types inside `TheAncientScrolls.Dragons` **can know about** any types in `TheAncientScrolls`.

This guideline, in combination with the namespace and assembliy rules from the previous chapters, should make for a very organized and well abstracted codebase.

## So who can go in the root `namespace`?
The root `namespace` of an assembly should be reserved mosttly to interfaces, shared data objects, extension methods and some components. **No `Entity` or `Manager`** should be in the root `namespace` of an assembly.

## How do I abstract?
There are many ways of achieving abstraction, but here are some examples:
- Using [C# events](https://docs.microsoft.com/en-us/dotnet/standard/events/):
  - A `Manager` could declare a specific `event` for when something happens. The `System` could subscribe to this event, and pass the callback along to another `Manager`  
![Abstraction with CSharp Events](https://user-images.githubusercontent.com/46461122/152659476-24bb47ae-e87f-48e6-ba9b-e5bf1312619b.png)

- Using [interfaces](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/interface):
  - A `Manager` can implement some `IDoesSomething` *interface*, and another `Manager` could define a `Setup(IDoesSomething dependency)`, which gets fulfilled by the `System`  
![Abstraction with Interface](https://user-images.githubusercontent.com/46461122/152659540-2b6e00ea-af6c-46d0-9168-ba0227b7b084.png)  
*For a more in-depth look, open the Abstraction with Interface example in this repository*

- Using some sort of event bus, to achieve complete abstraction between an event's *sender* and *listener*. [Here's](https://www.youtube.com/watch?v=WLDgtRNK2VE) a good example of how to implement one
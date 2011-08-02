What?
====

`RespectOrderDirectivedHandlersFilter` is a simple, source-code-only thing, that can help Castle Windsor order handlers during calls to `ResolveAll`.

Why?
====

Well, `ResolveAll` may be called by you directly or indirectly via e.g. `CollectionResolver`, each time returning all services of a given type. These instances will most likely be ordered in the order they were registered in the container, but the container doesn't even guarantee this.

So, in order to introduce some order, we add `RespectOrderDirectivedHandlersFilter` to the container, and then we decorate our implementations with some attributes that specify each component's ordering relative to another component.

How?
====

1. Put RespectOrderDirectivesHandlersFilter.cs in a lib folder in your project.
1. In your project, go to the "Add existing item..." menu, and MARK RespectOrderDirectivesHandlersFilter.cs (don't SELECT it just yet...)
1. Click that funny little arrow next to the "Add" button
1. Select "Add As Link"

and BAM! - your project should now _reference_ RespectOrderDirectivesHandlersFilter.cs, thus allowing it to reside in your `lib` folder and be updated like you update all of your other dependencies.

For more information and examples on usage, check out [the Windsor posts on my blog][3] in general, and [this post][4] in particular.

Nifty, huh?

License
====

RespectOrderDirectivesHandlersFilter is [Beer-ware][1].

[1]: http://en.wikipedia.org/wiki/Beerware
[2]: http://twitter.com/#!/mookid8000
[3]: http://mookid.dk/oncode/archives/category/castlewindsor
[4]: http://mookid.dk/oncode/archives/2295
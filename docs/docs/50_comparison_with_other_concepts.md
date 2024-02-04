

# Comparison with Other Concepts

## Actor Model

Capsule can be considered an opinionated actor model implementation. It fulfils the concurrency guarantees made by the actor model and supports all operations that actors must support.

However, because different people have different understandings of how an actor implementation in .NET should look like, Capsule avoids the term "actor" and calls them "capsules" instead.

### Capsule vs. Orleans / Dapr Actors

Orleans and Dapr Actor use a pattern they call "virtual actors". With this approach, the actor client does not care about actor activation and life-cycle. Also, actor location is transparent, so an actor may be running locally or remotely. The virtual actor pattern is well suited for distributed applications where scalability is a major concern.

Capsules, on the other hand, are explicitly created and destroyed, and they always live in the same process as the caller. This makes it a good choice for local thread-safety problems where life-cycle is relevant, e.g. device controllers or file / process management.


### Capsule vs. Akka.NET / Proto.Actor

In contrast to "pure" actor implementations such as [Akka.NET](https://getakka.net/) or [Proto.Actor](https://proto.actor/), Capsule directly supports object-oriented interfaces where the caller receives return values directly. This avoids the request/response pattern of pure actors that often lead to accidental complexity in the form of synchronization state machines.

Capsule is meant to solve individual multi-threading problems where necessary, leaving the rest of the application unaffected. This is different from the "everything is an actor" approach that Akka.NET and Proto.Actor take.

So if you're looking for an all-in actor framework with lots of features geared towards that approach, use Akka.NET or Proto.Actor. If, on the other hand, you want to use actors only in a part of the application and have actor interfaces that feel object-oriented, Capsule is for you.


## State Machines

Capsule is not a state machine library, but it provides the runtime environment that works well with state machines. In fact, when using [UML state machine](https://en.wikipedia.org/wiki/UML_state_machine) formalisms or similar approaches, a [run-to-completion execution model](https://en.wikipedia.org/wiki/UML_state_machine#Run-to-completion_execution_model) is assumed. Capsule is exactly that.

Consequently, Capsule can be combined with any form of state machine implementation (e.g. switch/case or state pattern) to get thread-safe, reactive state machines.

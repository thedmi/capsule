
# Introduction

Capsule is a .NET Standard 2.0 library and C# source generator that provides thread-safe object encapsulation.

It turns ordinary objects into thread-safe *capsules*. A capsule can be used *concurrently* without the risk of race conditions. The original interface is retained, so this is *transparent* for callers.

![Encapsulating an object](capsule.drawio.svg)

To create a capsule, you provide an implementation (green) and specify how the methods and properties (yellow) are to be encapsulated. Capsule then adds synchronization infrastructure (blue) and the hull (red), a generated interface adapter.

Apart from a few attributes and the requirement to make methods `async`, implementations remain free of synchronization code. This keeps code readable and focused. It also improves testability because synchronization concerns are separated.

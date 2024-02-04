
# Motivation

Writing thread-safe code is hard. .NET offers a lot of powerful synchronization primitives and helper classes, but using them in complex problem domains often leads to a lot of boilerplate, lower readability, reduced testability and consequently a higher risk for bugs.

Capsule aims to improve the situation through the following approaches:

- Provide generic thread-safe synchronization that allows for different synchronization modes such as "await result" or "fire and forget"
- Ensure usage of thread-safe objects follows an object-oriented approach that integrates seamlessly with ordinary code
- Generate boilerplate instead of making library users maintain it themselves

To achieve this, Capsule employs well-known synchronization primitives such as `Channel<T>` and `TaskCompletionSource` and a purpose-built source generator.

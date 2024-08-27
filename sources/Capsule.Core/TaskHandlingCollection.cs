using System.Collections;

namespace Capsule;

/// <summary>
/// A collection of <typeparamref name="T"/> that provides completed handling of tasks contained in
/// <typeparamref name="T"/>. 
/// </summary>
/// <param name="taskSelector">A function that extracts the task from an item</param>
public class TaskHandlingCollection<T>(Func<T, Task> taskSelector) : IEnumerable<T>
{
    private List<T> _items = [];

    public void Add(T item)
    {
        _items.Add(item);
    }

    /// <summary>
    /// Removes and returns the items with completed tasks, leaving the ones with not-yet-complete tasks in the
    /// collection.
    /// </summary>
    public IReadOnlyList<T> RemoveCompleted()
    {
        // Evaluate isCompleted *once* to avoid race conditions
        var taskCompletions = _items.Select(t => (item: t, completed: taskSelector(t).IsCompleted)).ToList();
        
        _items = taskCompletions.Where(c => !c.completed).Select(c => c.item).ToList();
        return taskCompletions.Where(c => c.completed).Select(c => c.item).ToList();
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

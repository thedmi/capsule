using System.Collections;

namespace Capsule;

public class TaskCollection : IEnumerable<Task>
{
    private List<Task> _tasks = new();

    public void Add(Task task)
    {
        _tasks.Add(task);
    }

    /// <summary>
    /// Removes and returns the completed tasks, leaving the not completed tasks in the collection.
    /// </summary>
    public IReadOnlyList<Task> RemoveCompleted()
    {
        // Evaluate isCompleted *once* to avoid race conditions
        var taskCompletions = _tasks.Select(t => (task: t, completed: t.IsCompleted)).ToList();
        
        _tasks = taskCompletions.Where(c => !c.completed).Select(c => c.task).ToList();
        return taskCompletions.Where(c => c.completed).Select(c => c.task).ToList();
    }

    public IEnumerator<Task> GetEnumerator() => _tasks.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

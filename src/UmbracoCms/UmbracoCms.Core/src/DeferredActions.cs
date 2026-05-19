using Umbraco.Cms.Core.Scoping;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

/// <summary>
/// Manages actions that are deferred until the current scope completes successfully.
/// </summary>
public sealed class DeferredActions
{
    private readonly List<Func<ValueTask>> _actions = [];

    /// <summary>
    /// Gets or creates a <see cref="DeferredActions"/> instance enlisted in the current scope context.
    /// </summary>
    /// <param name="scopeProvider">The scope provider to enlist in.</param>
    /// <returns>The deferred actions instance, or <c>null</c> if no scope context is available.</returns>
    public static DeferredActions? Get(ICoreScopeProvider scopeProvider)
    {
        IScopeContext? scopeContext = scopeProvider.Context;

        return scopeContext?.Enlist(
            "composeDeferredActions",
            () => new DeferredActions(),
            async (completed, deferredActions) =>
            {
                if (!completed || deferredActions is null)
                {
                    return;
                }
                await deferredActions.ExecuteAsync().ConfigureAwait(false);
            });
    }

    /// <summary>
    /// Executes the specified action immediately if no scope context is available, or defers it until scope completion.
    /// </summary>
    /// <param name="coreScopeProvider">The scope provider to enlist in.</param>
    /// <param name="action">The action to execute or defer.</param>
    /// <returns>A <see cref="ValueTask"/> representing the operation.</returns>
    public static ValueTask ExecuteDeferredAsync(ICoreScopeProvider coreScopeProvider, Func<ValueTask> action)
    {
        DeferredActions? actions = Get(coreScopeProvider);
        if (actions is null)
        {
            return action();
        }

        actions.Add(action);
        return default;
    }

    /// <summary>
    /// Adds an asynchronous action to the deferred list.
    /// </summary>
    /// <param name="action">The asynchronous action to defer.</param>
    public void Add(Func<ValueTask> action) =>
        _actions.Add(action);

    /// <summary>
    /// Adds a synchronous action to the deferred list.
    /// </summary>
    /// <param name="action">The synchronous action to defer.</param>
    public void Add(Action action) =>
        Add(() => { action(); return default; });

    private async ValueTask ExecuteAsync()
    {
        foreach (Func<ValueTask> action in _actions)
        {
            await action().ConfigureAwait(false);
        }
    }
}

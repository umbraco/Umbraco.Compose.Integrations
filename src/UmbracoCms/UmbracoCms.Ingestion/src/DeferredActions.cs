using Umbraco.Cms.Core.Scoping;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class DeferredActions
{
    private readonly List<Func<ValueTask>> _actions = [];

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

    public void Add(Func<ValueTask> action) =>
        _actions.Add(action);

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

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

internal static class StringExtensions
{
    extension(string value)
    {
        internal string ToGraphQLFieldNameCase() =>
        string.Create(
            value.Length,
            value,
            (buffer, state) =>
            {
                int length = state.Length;
                for (int i = 0; i < length; ++i)
                {
                    char c = state[i];
                    if (i == 0)
                    {
                        buffer[i] = char.IsAsciiLetter(c) ? char.ToLowerInvariant(c) : '_';
                    }
                    else
                    {
                        buffer[i] = char.IsAsciiLetterOrDigit(c) ? c : '_';
                    }
                }
            }
        );
    }
}

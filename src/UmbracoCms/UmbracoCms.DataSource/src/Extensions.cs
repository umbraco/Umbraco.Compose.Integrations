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

        internal string ToGraphQLTypeNameCase()
        {
            int length = char.IsAsciiDigit(value[0]) ? value.Length + 1 : value.Length;
            return string.Create(
                length,
                value,
                (buffer, state) =>
                {
                    int i = 0;
                    int j = 0;
                    var wasNonAsciiLetter = false;

                    if (char.IsAsciiDigit(state[0]))
                    {
                        buffer[j++] = '_';
                        buffer[j++] = state[0];
                        wasNonAsciiLetter = true;
                        // skip first character in loop below
                        i = 1;
                    }

                    for (; i < state.Length; ++i)
                    {
                        char c = state[i];

                        if (!char.IsAsciiLetterOrDigit(c))
                        {
                            buffer[j++] = '_';
                            wasNonAsciiLetter = true;
                        }
                        else if (i == 0 || wasNonAsciiLetter)
                        {
                            buffer[j++] = char.ToUpperInvariant(c);
                            wasNonAsciiLetter = false;
                        }
                        else
                        {
                            buffer[j++] = c;
                        }
                    }
                }
            );
        }
    }
}

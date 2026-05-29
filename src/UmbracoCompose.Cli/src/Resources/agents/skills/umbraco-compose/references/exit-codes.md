# Exit Codes

| Code | Constant | Description |
|------|----------|-------------|
| `0` | `Success` | Command completed successfully |
| `1` | `InvalidCommand` | Invalid command or help requested |
| `2` | `ValidationError` | Input validation failed (bad JSON, missing fields, etc.) |
| `3` | `RuntimeError` | Runtime error during command execution |
| `4` | `AuthenticationFailure` | Authentication failed (invalid credentials) |
| `5` | `NetworkError` | Network error (connection refused, timeout, etc.) |

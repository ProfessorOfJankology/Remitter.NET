# Migration from the Python desktop client

Remitter.NET is a replacement for the Windows Tkinter client, not a replacement for the Remitter backend.

Both clients may coexist during migration and should use the same backend API where practical.

## Initial milestones

1. Establish application configuration, Hub URL, bearer token and connection-status handling.
2. Load and display the shared queue.
3. Reproduce existing status classification and currency formatting.
4. Add selection, refresh, recheck and removal workflows.
5. Add expandable invoice rows with service-level financial detail.
6. Add manual entry and HCN search.
7. Port CSV/Excel import UX.
8. Implement Remit submission while preserving backend idempotency and uncertain-outcome behaviour.
9. Add settings UI.
10. Add remittance/email viewing and ESCompose integration.
11. Retire the Python desktop client only after behavioural parity is verified.

## Migration rule

Do not port backend financial logic into Remitter.NET. Where the old Python client contains logic that has become authoritative in the Hub, consume the Hub result instead of reimplementing it.

# Migration from the Python desktop client

Remitter.NET is a replacement for the Windows Tkinter client, not a replacement for the Remitter backend.

Both clients may coexist during migration and should use the same backend API where practical.

## Current parity status

Completed in the WPF client:

- application configuration, Hub URL, bearer token and connection-status handling
- shared queue loading and background refresh
- current status classification, currency formatting and queue styling
- selection-preserving refresh, recheck, removal and paid-row cleanup
- expandable invoice rows with service-level financial detail
- durable manual quick entry
- HCN operator mapping, verification and remembered/session-only user selection
- bounded HCN invoice search with paging and paid/cancelled filters
- leased payment add/edit/resolve workflow
- manual and automatic service-allocation editing with backend leases
- preservation of orphaned manual allocation instructions for server validation

Still to port before replacement:

1. CSV/Excel import UX and WorkSafe import entry point.
2. Full Settings dialog with validation, atomic save, connection testing and Change User entry.
3. Remit submission with duplicate acknowledgement, persistent request identity and uncertain-outcome recovery.
4. Remaining desktop interaction parity such as copy behaviour, exact selection semantics and auto-grow.
5. Remittance/email viewing and ESCompose executable integration.
6. Behavioural parity verification before retiring the Python desktop.

## Migration rule

Do not port backend financial logic into Remitter.NET. Where the old Python client contains logic that has become authoritative in the Hub, consume the Hub result instead of reimplementing it.

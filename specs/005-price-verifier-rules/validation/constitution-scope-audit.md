# Constitution and scope audit

## Functional requirements

| Range | Evidence | Status |
|---|---|---|
| FR-001..FR-006 | `ProductService` plus 13 Products tests; empty inputs make zero calls and safe failures/cancellation propagate | PASS |
| FR-007..FR-009 | `PriceFormatterService` plus 27 Pricing cases across cultures and `ToEven` midpoints | PASS |
| FR-010..FR-012 | `LabelData` rejects null product price/formatted price and preserves exact information | PASS |
| FR-013..FR-019 | `LabelQueueService` and immutable result entities validate 1..3, ordering, completion, snapshots, cancellation and capacity changes | PASS |
| FR-020 | BusinessRules suite executes without WPF, Firebird, barcode, license or reporting | PASS |
| FR-021 | Composition test resolves all three Business interfaces as singletons; ViewModel remains untouched | PASS |

## Success criteria

| Criterion | Evidence | Status |
|---|---|---|
| SC-001 | Empty lookup/search theory cases assert zero repository calls | PASS |
| SC-002 | 27 pricing tests include more than 20 values and three current cultures | PASS |
| SC-003 | Reliability tests complete 100 rows at each capacity with exact identity/order | PASS |
| SC-004 | Cancellation tests leave zero pending and restart at position 1 | PASS |
| SC-005 | Label tests preserve `Árbol  ñ  50%` and intentional spaces | PASS |
| SC-006 | Feature suite uses only doubles and pure values | PASS |
| SC-007 | Architecture tests and project-reference listing enforce dependency boundaries | PASS |

## Constitution gates

- **3 layers + Entities**: PASS. Business references Entities only; repository remains an abstraction; no Data changes.
- **WPF + MVVM**: PASS. No View or ViewModel changed. Presentation change is composition only.
- **Incremental Spec Kit**: PASS. Feature 005 has spec, plan, research, data model, contract, tasks and validations.
- **Legacy compatibility with improvement**: PASS. Preserves MX/CO and 1/2/3 columns while replacing ad-hoc variables with immutable snapshots and controlled null handling.
- **Security and controlled errors**: PASS. Source audit found no logging, credentials, connection strings, product payload logging or provider exceptions in new Business/Entities code. `ToString` tests prevent label payload exposure.
- **Minimum Business validation**: PASS. 72 feature tests and full regression passed.

## Scope audit

No implementation was added for UI behavior, licensing, barcode generation, server date on labels, report templates, preview, printer selection or printing. Those remain assigned to later features.

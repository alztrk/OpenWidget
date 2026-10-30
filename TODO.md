# OpenWidget Modularization and Internationalization TODO

## Completed in this pass

- [x] Document the global hardware price-source strategy.
- [x] Replace fake Amazon price refreshes with an API-ready provider abstraction.
- [x] Add explicit price states: live, stale, estimated, manual, and unavailable.
- [x] Record price-provider settings in `config.json` and `config.schema.json`.
- [x] Remove the remaining user-facing Amazon refresh claim from the price model.
- [x] Move compact telemetry badge colors into shared theme resources.
- [x] Route context-menu and settings validation messages through the localization service.
- [x] Add Russian (`ru-RU`) language selection, core translations, culture formatting, schema support, and tests.
- [x] Fall back to English for unsupported language codes.
- [x] Remove hardware prices and Amazon URL entry from the user-facing hardware settings.
- [x] Add an optional clipboard height limit that can be disabled.
- [x] Add an optional modular card layout while preserving the default single-card layout.

## Modularization

- [ ] Split `MainWindow.xaml.cs` into window lifecycle, widget layout, context-menu, and refresh coordinators.
- [x] Introduce a shared `IConfigStore` for loading, migration, validation, and persistence.
- [ ] Introduce a shared `IProductCatalog` model keyed by GTIN, MPN, ASIN, or provider-specific product ID.
- [ ] Move hardware scanning rules into category-specific scanner modules.
- [x] Replace provider-specific method names such as `UpdateAmazonPricesAsync` with provider-neutral names.
- [ ] Add provider adapters for PCPartPicker, Keepa, Geizhals, and regional providers behind `IHardwarePriceProvider`.
- [ ] Keep provider credentials and endpoints outside source control and document deployment configuration.

## Localization audit

- [ ] Replace remaining hardcoded widget labels in XAML with localized view-model properties or localized resource bindings.
- [ ] Replace remaining direct `CurrentLanguage == "en"` conditionals with localized keys.
- [ ] Localize validation errors, update status messages, tooltips, and empty states in every widget.
- [ ] Localize dynamic date and number formatting according to the selected language and currency.
- [ ] Add a test that detects user-facing Turkish/English literals outside localization resources.

## Theme audit

- [ ] Replace remaining inline ARGB values in widget XAML with semantic theme resources.
- [ ] Add light/dark or accent-theme resource dictionaries without changing component names.
- [ ] Remove duplicate legacy styles from `SettingsWindow.xaml` after all controls use shared components.
- [ ] Add a visual smoke-test checklist for hover, pressed, disabled, popup, and validation states.

## Validation

- [ ] Add migration tests for old price dictionaries and old hardware links.
- [ ] Add provider timeout, malformed-response, stale-cache, and cancellation tests.
- [ ] Add settings tests for localized labels and theme resource availability.
- [ ] Run build, unit tests, schema validation, and publish validation before each release.

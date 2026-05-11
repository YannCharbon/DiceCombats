# Release notes

### New

- **Event automation system**
  You can now create automations triggered by combat events. Automations can call external applications through HTTP requests, chain several actions, use delays, headers, body templates and event placeholders.

- **Custom field automation triggers**
  Custom fields now publish automation events. You can react to numeric, hit point, checkbox, condition, grid, text, colour, stats and detailed popup changes, including thresholds, direction changes and numeric maximum values.

### Improvements

- **Automation editor assistance**
  The automation page now includes integrated help, examples, troubleshooting notes and a complete placeholder catalogue for HTTP request templates.

- **Automation configuration and feedback**
  Automation settings are now saved through the integrated application storage, delivery results can be reviewed in a log viewer, and a notification popup confirms successful saves.

- **French localization for automations**
  Automation pages, editors, home shortcuts and related labels now use localization resources, including French translations.

- **Simplified automation architecture**
  The event and automation system has been moved into the core DiceCombats project and decoupled from the component manager to simplify maintenance and extension.

### Bug fixes

-
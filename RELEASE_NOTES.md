# Release notes

The application has been redrawn. Every MudBlazor control is gone, replaced by one the application draws
itself, which means the creature manager, the explorers, the combat board and the automations page
finally agree with each other instead of each looking slightly like a different program. DiceAudio has
been given the same treatment, in its own colours, so the two halves of the suite now read as one.

### New

- **Pinned combats**
  What used to be called favourites is now pinning, with a pin where the star was. Existing favourites
  are carried over on first start.

- **Custom fields reorder by dragging**
  A field is moved by picking it up, so the move backward and move forward buttons that each field
  carried are gone.

- **Three ways to show a detailed popup**
  A detailed popup field can present its entries as a list, as a grid or as tabs. Existing fields keep
  the layout they had.

- **A checkbox option can light up when it is ticked**
  Optional, per field. The whole cell takes the accent, so a ticked option is visible across a strip
  rather than only at the box itself.

- **Checking for updates on demand**
  The settings page carries a "Check now" button and says when it last looked.

### Improvements

- **One visual language**
  Buttons, inputs, tick boxes, steppers, selects, menus, modals and confirmations are the application's
  own and answer to one set of tokens: one card colour, one border, one radius, one set of accents.

- **The automations page**
  Five levels of nesting used to land on the same colour, because MudBlazor paints every panel from a
  single surface. Each level now takes its own step, and an automation's fields sit several to a row
  instead of one short control per full-width line. An open automation stays in its column.

- **Readable controls**
  The edge of anything you can operate now clears the contrast ratio the standard asks for, and the ink
  on a damage kind chip is computed against the colour you chose for it rather than assumed.

- **The creature whose turn it is**
  Its frame carries a travelling gradient again, in the colours the application actually uses.

- **The initiative window**
  Rebuilt as a table, with the "next creature" button given the weight it deserves.

- **The taskbar shows the application's name**
  The window had no title, so Windows had only the icon to go on.

### Fixed

- **An update that disappeared**
  The settings page asked GitHub on every render and ended by asking for another render, so it looped
  and spent the hourly limit for your address within seconds. The refusals that followed were reported
  as "up to date", which is how an update found a moment earlier vanished. The check now runs once,
  is cached for six hours, and a failure says so while keeping what it last knew.

- **The version was always 0.0.0**
  Shown in the settings, and compared against the latest release to decide whether an update exists.

- **Text boxes that had lost their lines**
  The text grid cells, the per-option note on a checkbox field and the default value of a text field
  could only show one line however much was written in them.

- **Imported sheets turning the GitHub button red**
  A creature sheet saved from aidedd.org carries the site's own stylesheet, which was recolouring parts
  of the application around it.

- **The settings icon**
  It was a circle with eight radiating spokes, which reads as a sun rather than a cog.

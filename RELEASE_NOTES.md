# Release notes

A follow-up to 0.6.0, which redrew the application but was only ever checked on a desktop window. On a
phone several of the new controls ran past the right edge of the screen and were quietly cut off, because
the pages are clipped rather than scrolled sideways. This release makes the redrawn layout hold together
at phone width. Nothing about the desktop layout changes, with one exception noted below.

### Fixed

- **The hit-point row on a creature card**
  Six controls — the name, the bar, the stepper, damage, heal and apply — do not fit one line on a phone,
  so the layout squeezed the one item with room to give: the stepper collapsed to a hairline a pixel and
  a half wide, leaving no way to type or step a value, and the apply button was still cut in half. The
  row now wraps and drops apply onto its own line.

- **The custom fields of a creature, cut off down the right-hand side**
  The field wall could not shrink below the width its cards wanted, so roughly fifty pixels of every card
  were off-screen: the delete and library buttons at the end of each header, and the whole right column
  of every option grid — half the conditions of a D&D conditions field could not be reached.

- **Panels and grids wider than the screen they are on**
  A grid whose columns declare a minimum width does not shrink below it, it overflows. Every such grid
  now gives way when there is not enough room: the sections of the hit-point configuration dialog, the
  explorers, the detailed popup, the option grids, the assistance panels and the automations page.

- **A placeholder name wider than the screen**
  The copyable variables under an automation trigger pushed their row past the edge of the panel.

- **A horizontal scrollbar in the combat edition dialog**
  Its two panels asked for the full width of their column and then added a margin on top, which put each
  of them past the column on both sides. This one was never specific to phones — the same dialog carried
  the same scrollbar on a desktop window, ten pixels wide instead of five. It is the one change in this
  release that also alters the desktop layout.

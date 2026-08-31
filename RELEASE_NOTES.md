# Release notes

### New

- **Damage kinds on the hit points field**
  A hit points field now carries its own list of damage kinds, each with a colour and an icon, and the reduction rules that apply to the creature. Resistance, immunity and vulnerability are covered, attack attributes such as magical decide whether a rule applies, and temporary hit points, maximum hit points reduction and negative hit points are supported. Creatures that only have current and maximum hit points keep working as before.

- **Damage calculator**
  A popup lets you stage several damage lines of different kinds before applying them, shows the whole calculation with the rule used for each line, and applies everything in one step that a single undo takes back. The quick buttons in the card still apply a single hit directly, and open the popup only when the creature has rules to resolve.

- **Combat log**
  Starting a combat opens a recording session that follows the rounds and stores every hit, heal and turn. The log window shows the damage dealt by each creature with the best hit, the average and the split by damage kind, the damage taken by each target, and the round by round timeline. Sessions are kept per combat and can be exported as JSON, statistics included.

- **Combat events for damage and healing**
  Damage and healing now raise their own automation events. An attack raises one event describing the whole hit and one event per damage kind, all carrying the attacker, the target, the amounts before and after reduction, the damage kind, the attack attributes and an intensity that says how hard the hit landed. Starting and ending a combat raise events as well.

- **Sheet links in the app**
  Links of a creature sheet imported from aidedd.org, such as the spells a creature can cast, open in a window of the app instead of the system browser. The creature editor carries the option, on by default, and it can be turned off per creature.

### Improvements

- **Damage source attribution**
  The damage popup lets you pick who is dealing the hit from the creatures of the combat, players first and marked as such. It follows the creature holding the initiative, so the usual case takes no click, and the combat log reports separately what was attributed automatically.

- **Automation triggers for damage**
  The new triggers can be filtered by attacker and by damage kind, so an automation can be written as "when this player deals slashing damage" without hand written conditions.

- **Variables listed in the automation editor**
  The trigger block now lists the variables the selected trigger delivers. Clicking one copies it, ready to be pasted into a body, a URL, a header or a condition.

- **Starting a combat**
  Sorting the initiative now asks whether the combat starts, which begins the timer and the log recording in one step. Stopping the combat or closing the app closes the session and saves it.

- **Amount fields validate while typing**
  The value fields of the damage popup no longer need to lose the focus to be taken into account.

### Bug fixes

- **Creatures joined a combat at zero hit points**
  A creature added to a combat or synchronised now starts at its maximum hit points.

- **The damage popup kept its previous content**
  Reopening the popup after closing it, whichever way it was closed, no longer shows the damage lines of the previous attack.

- **Placeholders in automation requests**
  Decimal values are now written with a dot and booleans as true and false whatever the language of the application is, so a request body stays valid JSON in French.

- **Build and warnings**
  The project builds again after the package update, and all build warnings have been cleared.

- **PDF pages rebuilt every second**
  A PDF inside a detailed popup was torn down and rendered again on every render of the
  page holding it - about once a second while a combat timer runs - which made it flash,
  jump and lose the zoom level. Each page now keeps a stable identity so it is rendered
  once, and the zoom-to-fit no longer re-triggers itself.

- **Detailed popup jumping off the screen**
  Opening a detailed popup no longer makes the creature cards slide and the modal shoot off
  the right edge of the screen before snapping back. Relayouts of the masonry grids are now
  instantaneous instead of animated: the animation put a temporary transform on the creature
  card, which made the card - rather than the window - the reference for the popup placed
  inside it.

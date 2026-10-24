# Release notes

This release is about your data: you can now share combats and creatures with other game masters, keep
backups and restore them from Settings, and on Windows choose the folder where your data is kept and
use it from several computers. The website has a new page, "Your data", which explains all of this step
by step.

### New

- **Exporting**
  Export to file on a combat saves it together with the creatures it uses, so whoever imports it gets
  the whole encounter. Export creatures and Export combats save a whole list in one file, and Settings →
  Data offers Export the whole library…, which also holds your player groups, user custom fields and
  automations. The access tokens of your automations are never exported.

- **Importing**
  Import from a file… is available in the creature list, in the combat list and in Settings, and it also
  opens files exported by older versions. The import window lists everything the file holds, with a tick
  box for each part. When an item is already in your library, you choose what to do with it: import it as
  a copy, replace yours or keep yours. One button gives the same answer to many items at once, and you
  can set a text to put before and after the names of the copies.

- **Backups**
  DiceCombats backs up your creatures, combats, player groups, user custom fields, automations, combat
  logs and settings the first time you open it each day, and before every import or restore. Settings →
  Data shows the most recent backup with a Restore button, and All backups… lists the others. A backup
  can also be saved to a file and restored from one, which is also the way to take your data to another
  Android device.

- **A data folder of your choice (Windows)**
  Choose another folder… in Settings moves your data there, checks the copy and restarts. Your previous
  data stays where it was until you delete it.

- **The same data on several computers (Windows)**
  A data folder kept in sync by OneDrive, Dropbox or a similar service can be used from several
  computers. DiceCombats tells you when the folder is already open on another computer, offers to reload
  when data was changed elsewhere, and asks which version to keep when both sides changed the same item.
  It never reloads by itself, even in the middle of a combat.

### Improvements

- **Safer saving**
  Creatures and combats are written in a way that survives a crash or a power cut in the middle of a
  save, and a file that failed to load is no longer overwritten at the next save.

- **Creatures with the same name**
  A combat now recognises its creatures by an identifier rather than by their name. Two creatures that
  share a name are no longer mixed up, and updating the creatures of a combat from your library tells you
  which ones it could not find.

- **Automations**
  The first "next turn" event of a combat now has an empty previous creature, instead of "Default".

### Fixed

- **The title bar**
  It could move out of place when the pages were rebuilt.

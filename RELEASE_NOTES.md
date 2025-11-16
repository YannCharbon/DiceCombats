# Release notes

## Detailed popups and richer creature notes

### Fixes

Fix of the masonry layout when rendering Detailed popup sub-contents with kind "text" or "markdown".

### New

- **“Detailed popup” custom field for creatures**  
  You can now add a new type of custom field called **DetailedPopup** on your creatures.  
  This field lets you attach rich “detail sheets” to a creature, instead of squeezing everything into a single text box.

- **Rich content inside popups (text, Markdown, HTML, PDF)**  
  Inside a Detailed popup you can now store:
  - Simple text notes  
  - Markdown notes (for headings, lists, emphasis, etc.)  
  - HTML “cards” based on templates (for more advanced layouts)  
  - PDFs (for example: spell cards, stat blocks, handouts, or rules extracts)  

- **Quick search inside a popup**  
  When you open a detailed popup during a game, you get:
  - A **search box** to quickly find the right section by name or content  
  - A **toggle between list mode and grid mode**, so you can either read everything top-to-bottom or browse “cards” at a glance  

- **Grid view for detailed content**  
  The new grid layout lets you see several pieces of information side by side as “cards”.  
  This is especially handy for long sheets (e.g. complex monsters, bosses, or NPCs with a lot of lore, abilities, and notes).

### Improvements

- **Better help when creating custom fields**  
  The creature custom fields editor now shows clearer explanations and examples for each field type, including the new Detailed popup field.  
  This should make it easier to choose the right field type without needing technical knowledge.

- **More comfortable popup windows**  
  The in-app popups (modals) have been reworked:
  - They are easier to read on both large screens and smaller laptops  
  - Scroll behaviour is smoother when you edit long content  
  - Dialogs from DiceCombats (confirmation boxes, etc.) now correctly appear **above** other popups instead of hiding behind them

- **Visual polish in the custom fields editor**  
  Several small visual tweaks make the custom fields area clearer:
  - Better spacing and alignment  
  - Clearer grouping of controls  
  - A slightly wider layout for the field selection and help area, so examples are easier to see

### Bug fixes

- **HTML details from file now load correctly**  
  A problem where HTML-based content loaded from a file would sometimes fail to display has been fixed.  
  You can now safely keep your HTML snippets or templates in separate files and use them in your Detailed popups.

- **General minor visual fixes**  
  A few small UI glitches and inconsistencies were cleaned up to make the app feel smoother and more coherent.

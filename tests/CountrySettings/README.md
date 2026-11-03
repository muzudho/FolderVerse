# Country settings checks

The domestic system menu now opens a 60-country controller/appearance editor. Human subordinates retain their own turns; CPU subordinates inherit a human superior's controls. Removing the last human starts the existing automatic-play mode, which can be interrupted to reopen settings.

Primary color, secondary color and one canonical pattern (solid, horizontal stripes, dots, grid, checkerboard) are persisted. Choose the pattern before the palettes; solid hides and disables the secondary palette while preserving its previous choice. New palettes distinguish 60 countries using 20 primary colors and three pattern groups. Rotated variants are not separate choices. Robot affiliation IDs and reservations are unaffected. Map territory fills and round ownership badges carry the pattern. Country-color terrain fills use the existing country-color map layer.

Primary buttons and square folder/directory secondary buttons keep their flat backgrounds and show small transparent secondary-color pattern marks in the top-right/bottom-left corners. Solid countries omit these redundant marks. The portrait medallion retains its plain rim and adds pattern marks just outside its 3/9 o'clock positions, away from names. Country/pattern selection buttons use their flat selected background. The shared hangar-style robot glyph retains patterned part edges. Battlefield soldier/captain clothing now uses the army's pattern in every facing; faces and weapons are untouched, cheap-part white stripes take priority and damage tint is applied last. Tiny world-map robot sprites and harbor icons keep their primary colors. UI captures include a 16-soldier battlefield with four patterns/directions and normal/cheap/damaged combinations.

World-generation portrait cards and player-selection cards show a pattern swatch. Generation also displays the requested small hint that human/computer assignments and country colors can be changed later. CircleSpaceCoordinator's procedural pattern approach was reviewed; the game uses its own surface-local masks at its existing 10×10 terrain resolution.

Validation (from repository root, PowerShell):

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers
dotnet artifacts/corner-lesson/CountrySettings.dll
$env:ALSOFT_DRIVERS='null'
dotnet artifacts/corner-lesson/CountrySettings.dll --ui
```

Model checks cover separate human subordinate turns, switching during a partially submitted round, all-CPU/manual transitions without changing country counts, stable robot ownership, save roundtrip, old-save defaults and absence of rotated duplicate patterns. The UI host uses real input coordinates at 1920×1080 and 960×540, captures settings/net/globe/generation frames to the reused output directory, and does not overwrite the player's autosave. Existing `tests/PlayerCount` also verifies all participant configurations and full-turn progression.

# All-in-1 Converter

A Habbo asset workstation that bundles every step of a hotel pipeline behind
a single binary: download Habbo's official CDN assets, import custom packs,
merge furnidata / clothesdata / productdata, convert SWF → Nitro, generate
the SQL inserts for `items_base` + `catalog_items`, and fix the database in
place.

It ships with three interchangeable UI shells (same workflows behind each)
plus a self-contained binary for Windows and Linux.

## At a glance

| | |
|---|---|
| Runtime | .NET 10 LTS, cross-platform (Windows x64, Linux x64) |
| Spritesheet engine | SixLabors.ImageSharp 3.x (replaces System.Drawing) |
| Terminal UI | Terminal.Gui 1.19 (mouse + keyboard) |
| Desktop UI | Avalonia 12 (Mainframe + Matrix themes) |
| Database | MySql.Data 9.7 (MariaDB / MySQL) |
| SWF decompiler | JPEXS Free Flash Decompiler (`Tools/ffdec/`) |
| Data formats | Flat JSON **or** JSON5 split-mode (`manifest.json5` + `core/custom/seasonal/` tiers, matches Nitro V3 `split-gamedata.mjs`) |

## Three ways to drive it

The same menus and tools sit behind each shell, so anything you learn in
one carries over.

- **TUI** — mouse-driven mainframe terminal (Terminal.Gui).
  IBM 3270 / CICS look, mouse + keyboard, F-keys, captured output in a
  scrollable log pane. **Recommended.**

- **CLI** — same mainframe theme rendered with plain `Console.WriteLine`.
  Keyboard only. Useful over SSH, piped, or in restricted terminals.

- **GUI** — native desktop window (Avalonia). Two themes:
  - **Mainframe** — cyan header bar, phosphor green body, red BACK/EXIT,
    classic bank-terminal feel.
  - **Matrix** — pure phosphor green on black, with a digital-rain
    background of digits cascading behind the menus.

When the binary is **double-clicked from a file manager** it opens straight
into the GUI. When it's launched from a **terminal** it asks whether you
want TUI or CLI (GUI is not offered from a terminal session because the
operator usually wants to stay in the terminal). Inside any shell, the
`s` menu entry hot-swaps between modes without restarting the process.

```text
Habbo Downloader            # mode selector if launched from a terminal,
                            # GUI directly if double-clicked
Habbo Downloader --tui      # mouse mainframe TUI
Habbo Downloader --cli      # keyboard-only mainframe CLI
Habbo Downloader --gui      # desktop window (prompts theme)
Habbo Downloader --help     # flags + commands
Habbo Downloader --version  # fetched current Habbo client version
```

## Top-level menus

Every menu has an auto-injected `?` entry that documents what each item
does and what input it expects — **read it from inside the tool, no need
to keep notes**.

1. **Habbo Original Downloads** — pull every official asset from the Habbo
   CDN: badges, clothes (figuredata + figuremap), effects, furnidata,
   furniture SWF, icons, MP3, productdata, quest images, reception art,
   texts, variables. `all` runs the full bootstrap.
2. **Nitro Custom Downloads** — multi-source import of custom furniture /
   clothes packs.
3. **Hotel Tools** — Merge Furnidata / Productdata / Clothesdata (with
   dual flat ↔ JSON5 split-mode IO), Generate SQL for `items_base` +
   `catalog_items`, Decompile / Compile `.nitro` bundles, SWF → Nitro for
   Furniture / Clothes / Pets / Effects.
4. **Database Tools** — show DB version, optimize tables, fix offer_id,
   fix sit/lay/walk in `items_base`, fix sprite_id / item_id from JSON.

## JSON + JSON5 split-mode IO

Every Merge tool and the SQL Generator auto-detect the input format:

- a `*.json` file → read as **flat** (the legacy single-blob layout)
- a directory with `manifest.json5` + `core/` / `custom/` / `seasonal/`
  tiers → merged in load order, later tier wins by id / classname / code

On output you're prompted **F** (flat single file) or **S** (split: `manifest.json5` + `core/floor-NNN.json5` + `core/wall-NNN.json5`, chunks of 300; same layout produced by Nitro V3's `scripts/split-gamedata.mjs`).

If you only want a one-shot conversion (flat → split or split → flat), run
the Merge entry with an empty `Import_*/` directory and pick the desired
output format — no merge, just a re-encode.

## SQL generator workflow

1. Drop `.nitro` (or `.swf`) files into `Generate/Furniture/` (recursive).
2. Drop `FurnitureData.json` (or a split directory) into `Generate/Furnidata/`.
3. Run Hotel Tools → Generate SQL.
4. The tool reads width / length / height / interaction count straight
   from each `.nitro` and asks for:
   - Starting ID for `items_base` and `catalog_items` (e.g. `select * from
     items_base order by id desc limit 1`).
   - `Catalog_Page` ID where you want the items.
5. Output goes to `Generate/Output_SQL/` with a timestamp, one INSERT per
   item.

## Runtime layout

These directories are created next to the binary on first run (do **not**
commit them):

```
Habbo_Default/          downloaded official Habbo assets
Merge/                  Original_/Import_/Merged_ for Furnidata, Productdata, Clothes
Generate/               Furnidata/, Furniture/, Output_SQL/
NitroCompiler/          compile/, compiled/, extract/, extracted/
SWFCompiler/            import/, plus output directories per asset class
Database/Variables/     drop FurnitureData.json here for the DB fix tools
custom_downloads/       multi-source custom downloader output
```

## Build

```bash
cd SourceCode
dotnet build "Habbo Downloader.csproj"          # debug build
dotnet run -- --tui                             # run with TUI mode
```

Prerequisites: **.NET SDK 10.x** + **Java** (for FFDec, used by every SWF → Nitro
path) + **Node.js** (for some helpers).

## Self-contained single-file releases

```bash
./publish-win.bat        # → publish/win-x64/Habbo Downloader.exe (~70 MB)
./publish-linux.sh       # → publish/linux-x64/Habbo Downloader  (~70 MB) + .desktop entry
```

Both scripts produce a self-contained single-file binary (no .NET runtime
required on the target machine) with the FFDec tooling next to it. The
Linux script also drops an `all-in-1-converter.desktop` so file managers
can double-click straight into the GUI.

## Continuous integration

- `.github/workflows/build.yml` — on push / PR / manual dispatch, builds
  both Windows and Linux targets and uploads them as 30-day artifacts.
- `.github/workflows/release.yml` — on tag `v*` (or manual dispatch with a
  tag input), builds both targets, zips / tar.gzs them, and publishes a
  GitHub Release with the binaries attached.

```bash
git tag v2.0.0 && git push origin v2.0.0   # cuts a Release with both binaries
```

## Custom effects

Drop-in custom effects live in `Addons/Custom Effects/` — read the README
there for the file naming convention. The Among Us pack and the Leet pack
are included as references.

## Credits

- **medievalshell** — .NET 10 modernization, cross-platform refactor,
  ImageSharp migration, JSON5 split-mode IO, three UI shells (CLI / TUI /
  GUI), Mainframe + Matrix themes, GitHub Actions pipeline.
- **duckietm** — original all-in-1 downloader, SWF → Nitro pipeline, SQL
  generator, database tools, upstream maintainer.
- **Nitro Team** — pets converter base (`discord.gg/yCXcMqrT`).
- **AtlasOmega** — "Among Us" effects (enable 880-903).
- **Leet** — enables 500-688.
- The whole Habbo retro community.

## Want to help?

Discord: https://discord.gg/txfNucJv

Everything here is free — don't get scammed.

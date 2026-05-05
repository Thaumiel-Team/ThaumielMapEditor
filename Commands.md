# Thaumiel Map Editor - Commands

This document outlines all available commands, subcommands, and required permissions for the Thaumiel Map Editor.

## Remote Admin Commands

*No Remote Admin commands found.*

---

### Admin Subcommands

> [!IMPORTANT]
> These commands are executed via the main Remote Admin command `tme <subcommand>`.

| Subcommand | Aliases | Arguments | Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `thaumielmapeditor` | `tme` | None | None | Manage the features of Thaumiel Map Editor |
| `convert` | `cv` | <code>&lt;Schematic Name&gt;</code> | `tme.convert` | Converts the PMER schematic with the specified name |
| `coroutines` | `coro, cor` | None | `tme.coroutines` | Lists all the coroutines running or ran |
| `destroy` | `de, delete, remove, del` | <code>&lt;Schematic Id&gt;</code> | `tme.destroy` | Destroys the specified schematic |
| `grab` | `gr` | <code>&lt;Schematic ID&gt;</code> | `tme.grab` | Grabs the specified schematic |
| `list` | `li` | None | `tme.list` | Lists all schematics |
| `modify` | `mod` | None | `tme.modify` | Modifies the specified values in the specified schematic |
| `reload` | `re` | None | `tme.reload` | Reloads all schematics |
| `save` | None | <code>&lt;Map Name&gt;</code> | `tme.save` | Saves the current spawned schematics into a map file |
| `spawn` | `sp, create, cr` | <code>&lt;Schematic name&gt;, &lt;X&gt;, &lt;Y&gt;, &lt;Z&gt;</code> | `tme.spawn` | Spawns the named Schematic |
| `spawned` | `spd` | None | `tme.spawned` | Gets all spawned Schematics |

---

## Console Commands

| Command | Aliases | Description |
| :--- | :--- | :--- |
| `tmelogs` | `tmelogsupload, tmeupload` | Uploads your logs to the TME API. |
| `tmeupdate` | None | Updates the Thaumiel Map Editor plugin to the latest version. |
| `tmeupdatecheck` | None | Checks for updates to the Thaumiel Map Editor plugin. |

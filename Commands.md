# Thaumiel Map Editor - Commands

This document outlines all available commands, subcommands, and required permissions for the Thaumiel Map Editor.

## Remote Admin Commands

| Command | Aliases | Description |
| :--- | :--- | :--- |
| `thaumielmapeditor` | `tme` | Manage the features of Thaumiel Map Editor |

---

### Admin Subcommands

> [!IMPORTANT]
> These commands are executed via the main Remote Admin command `tme <subcommand>`.

| Subcommand | Aliases | Arguments | Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `convert` | `cv` | <code>&lt;Schematic Name&gt;</code> | `tme.convert` | Converts the PMER schematic with the specified name |
| `coroutines` | `coro, cor` | None | `tme.coroutines` | Lists all the coroutines running or ran |
| `debug` | `deb, draw` | None | `tme.debug` | Generates drawable lines for each object. |
| `destroy` | `de, delete, remove, del` | <code>&lt;Schematic Id&gt;</code> | `tme.destroy` | Destroys the specified schematic |
| `grab` | `gr` | <code>&lt;Schematic ID&gt;</code> | `tme.grab` | Grabs the specified schematic |
| `list` | `li` | None | `tme.list` | Lists all schematics |
| `modify` | `mod` | None | `tme.modify` | Modifies the specified values in the specified schematic |
| `position` | None | <code>&lt;Get&#124;Set&gt;, [X], [Y], [Z]</code> | `tme.modify.position` | Gets or sets the position of a schematic |
| `reload` | `re` | None | `tme.reload` | Reloads all schematics |
| `rotate` | `rot, rotate` | <code>&lt;X&gt;, &lt;Y&gt;, &lt;Z&gt;</code> | `tme.modify.rotate` | Rotates the specified schematic by the specified value. |
| `save` | None | <code>&lt;Map Name&gt;</code> | `tme.save` | Saves the current spawned schematics into a map file |
| `scale` | None | <code>&lt;X&gt;, &lt;Y&gt;, &lt;Z&gt;</code> | `tme.modify.scale` | Scales the specified schematic by the specified values |
| `spawn` | `sp, create, cr` | <code>&lt;Schematic name&gt;, &lt;X&gt;, &lt;Y&gt;, &lt;Z&gt;</code> | `tme.spawn` | Spawns the named Schematic |
| `spawned` | `spd` | None | `tme.spawned` | Gets all spawned Schematics |
| `spawnobject` | `spawnobj, so, obj` | <code>&lt;ObjectType&gt; [X Y Z] [RX RY RZ] [SX SY SZ &#124; S] [key=value ...]</code> | `tme.spawnobject` | Spawns a single object without a schematic |

---

## Console Commands

| Command | Aliases | Description |
| :--- | :--- | :--- |
| `tmelogs` | `tmelogsupload, tmeupload` | Uploads your logs to the TME API. |
| `tmeupdate` | None | Updates the Thaumiel Map Editor plugin to the latest version. |
| `tmeupdatecheck` | None | Checks for updates to the Thaumiel Map Editor plugin. |

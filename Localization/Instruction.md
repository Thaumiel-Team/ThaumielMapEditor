# How to Translate the README and Commands Pages

Thank you for helping translate! You do not need to know how to code.
You only need a GitHub account and to edit text.

There are already two examples you can look at:

- Russian: `Localization/Russian.md` and `Localization/Commands-RU.md`
- Brazilian Portuguese: `Localization/Portuguese-BR.md` and `Localization/Commands-PT-BR.md`

Open one of them next to the English version while you work. Try to make
yours look the same, just in your language.

Please only work on the copies inside the `Localization` folder.
Please do not change the main English `Commands.md` file itself.

---

## 1. Make your own copy to work in

1. Go to the main project page on GitHub and press **Fork** (this makes your
   own personal copy).
2. In your copy, make sure you are working from the **Dev** version of the
   files, since that is the newest text.
3. You will send your finished work back at the end for review.

## 2. Make two new files

Inside the `Localization` folder, make two new files:

1. One for the main page. Name it after your language.
   For example: `German.md`, `Spanish.md`, `French.md`.
2. One for the commands page. Name it `Commands-` plus your language.
   For example: `Commands-DE.md`, `Commands-ES.md`, `Commands-FR.md`.

To start:

- Copy everything from the English `README.md` into your first new file.
- Copy everything from the English `Commands.md` into your second new file.

Then translate the copies. At the very bottom you can add a line about who
translated it, like the Portuguese version does:
`*Translated by [your name](your link).`

## 3. What to translate

Translate all normal reading text:

- The introduction at the top.
- Titles like Features, Supported Object Types, Usage & Commands, Installation.
- The bullet points and numbered steps.
- The table titles in the Commands file, and the explanations of what each
  command does.

Example: translate the title and explanation:

- Before: `| Command | Aliases | Description |`
- After (Russian): `| Команда | Псевдоним | Описание |`

Example: translate the description, but nothing else in that row:

- Before: `` `spawn` `` with `Spawns the specified schematic`
- After: `` `spawn` `` with the translated explanation in your language

## 4. What to leave exactly as it is

Please do not change these things. They have to stay in English or the
commands and links will break:

1. **Command words.** Words like `tme`, `spawn`, `save`, `list`, `tmelogs`,
   and parts like `tme.spawn` must stay exactly the same.
   Only change the explanation next to them.

2. **Shortcuts and settings in the tables.** Things like
   `sp, create, cr` or `tme.list` stay the same.

3. **Links that start with https://.** For example the Discord invite,
   the Unity Hub download link, and the docs link. Leave them alone.

4. **Flag pictures.** Leave the picture addresses like
   `https://flagsapi.com/DE/flat/64.png` alone, except for changing the
   two country letters for your flag (see below).

5. **Special colored boxes** like `> [!WARNING]` or `> [!NOTE]`.
   Translate the sentence after it, but leave the `> [!WARNING]` part itself.

If you are not sure whether to change a word, look at the Russian or
Portuguese versions. If they left it in English, you leave it in English too.

## 5. Add your language button with a flag

At the very top of the main page there is a row of language buttons with
flags (English, Russian, Portuguese, etc.).

You need to add your language there in two places:

1. In your new translated file.
2. In the main English `README.md`.

Find the block for another language and copy it, then change only these
3 things:

1. Where it points to: your file name, for example `Localization/German.md`.
2. The flag letters: for example `DE` for Germany, `ES` for Spain,
   `FR` for France, `IT` for Italy, `PL` for Poland, `JP` for Japan.
3. The language name people will see: for example `Deutsch`.

Here is a block you can copy. This example is for German:

```html
<td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; width: 100px;">
    <a href="Localization/German.md"
       style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
        <img src="https://flagsapi.com/DE/flat/64.png" height=30><br>
        <span style="color: #f0f0f0">Deutsch</span>
    </a>
</td>
```

Just replace `Localization/German.md` with your file,
`DE` with your flag letters (two times), and `Deutsch` with your
language name. If you can, please also add the same button to the other
translated files. If not, the project team will do it for you.

## 6. Link your two files together

In your translated main page there is a section called Usage & Commands with
a link to the Commands page.

Change that link so it opens **your** translated Commands file, which sits in
the same folder. For example, if your Commands file is called
`Commands-DE.md`, the link should end with `(Commands-DE.md)`:

- Before: `**[Commands Documentation](Commands.md)**`
- After: `**[Your translated title](Commands-DE.md)**`

Also, near the bottom is a link to the Dependencies page. Because your file
is inside the `Localization` folder, it needs two dots in front to find it:

- Wrong: `(Dependencies.md)`
- Right: `(../Dependencies.md)`

Leave links that start with `https://` unchanged.

## 7. Check your work and send it in

Before you send it, please check:

- [ ] Both pages look correct when previewed on GitHub.
- [ ] All flag buttons open the correct page when clicked.
- [ ] The link in your main page opens your translated Commands page.
- [ ] You did not change any command words or links by accident.
- [ ] You added your flag button to the main English `README.md`.
- [ ] You did not change the main English `Commands.md`.

To send it in:

1. Save your two new files (plus your small change to `README.md`).
2. On GitHub, press **Contribute > Open pull request** and choose the
   **Dev** branch as the destination.
3. Choose the **Translation** form when GitHub asks you what kind of request
   this is, and fill in your language and files.
4. Write which language you translated in the title, for example
   `Add German translation`.

If the English text changes later, your translation may become a little old.
You are always welcome to come back and update it from a fresh copy. Thank you!


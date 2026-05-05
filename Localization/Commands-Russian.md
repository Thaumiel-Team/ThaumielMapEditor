# Thaumiel Map Editor - Команды

Этот документ являеться списком всех доступных команд, сабкоманд и нужных для использования админ прав для Thaumiel Map Editor.

## Команды Remote Admin

*На данный момент нету команд для Remote Admin.*

---

### Сабкоманды админ-панели

> [!IMPORTANT]
> Данные команды исполняются через основную команду `tme <subcommand>`.

| Сабкоманда | Псевдоним | Аргументы | Права | Описание |
| :--- | :--- | :--- | :--- | :--- |
| `thaumielmapeditor` | `tme` | Нету | Нету | Управление особенностями Thaumiel Map Editor |
| `convert` | `cv` | <code>&lt;Название схематики&gt;</code> | `tme.convert` | Конвертирование указанной схематики ProjectMER в формат Thaumiel Map Editor |
| `coroutines` | `coro, cor` | Нету | `tme.coroutines` | Предоставление списка всех активных или запущенных корутинов |
| `destroy` | `de, delete, remove, del` | <code>&lt;ID схематики&gt;</code> | `tme.destroy` | Удаляет указанную схематику |
| `grab` | `gr` | <code>&lt;ID схематики&gt;</code> | `tme.grab` | Перетаскивание указанной схематики |
| `list` | `li` | Нету | `tme.list` | Предоставление списка схематик |
| `position` | `pos` | <code>[ID схематики], &lt;Get&#124;Set&gt;, [X], [Y], [Z]</code> | `tme.position` | Изменение позиции указанной схематики |
| `reload` | `re` | Нету | `tme.reload` | Перезагрузка всех схематик |
| `rotate` | `rot` | <code>&lt;ID схематики&gt;, &lt;X&gt;, &lt;Y&gt;, &lt;Z&gt;</code> | `tme.rotate` | Изменение ротации указанной схематики |
| `save` | Нету | <code>&lt;Название карты&gt;</code> | `tme.save` | Сохраняет установленные схематики в файл карты |
| `spawn` | `sp, create, cr` | <code>&lt;Название схематики&gt;, &lt;X&gt;, &lt;Y&gt;, &lt;Z&gt;</code> | `tme.spawn` | Спавн указанной схематики |
| `spawned` | `spd` | Нету | `tme.spawned` | Дает список установленных схематик |

---

## Консольные команды

| Команда | Псевдоним | Описание |
| :--- | :--- | :--- |
| `tmelogs` | `tmelogsupload, tmeupload` | Загружает ваши логи в TME API. |
| `tmeupdate` | Нету | Обновляет текущий плагин Thaumiel Map Editor до последней версии. |
| `tmeupdatecheck` | Нету | Проверка на обновления плагина Thaumiel Map Editor. |

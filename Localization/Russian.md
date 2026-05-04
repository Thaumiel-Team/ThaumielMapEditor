# Thaumiel Map Editor (TME)

> [!WARNING]
> Данный проект находится на стадии ранней разработки. Ожидайте баги, отсутствие многих вещей, и изменений ломающий игру.

Thaumiel Map Editor (TME) - это базированный на Unity мап едитор созданный для помощи разработчикам и дизайнерам создавать, редактировать, и управлять картой игры. TME предоставляет интерфейс для расставления и конфигурации широкого спектра объектов, от интерактивных элементов и освещения до пропов и навигационных инструментов.

Наши документы доступны по ссылке: https://thaumiel.gitbook.io/tme-docs/

---

<table align="center" style="width: 100%; max-width: 600px; border-collapse: separate; border-spacing: 15px;">
    <tr>
        <td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; ; width: 100px;">
            <a href="README.md" 
               style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
                <img src="https://flagsapi.com/US/flat/64.png" height=30><br>
                <span style="color: #f0f0f0">English</span>
            </a>
        </td>
        <td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; width: 100px;">
            <a href="Localization/Russian.md" 
               style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
                <img src="https://flagsapi.com/RU/flat/64.png" height=30><br>
                <span style="color: #f0f0f0">Русский</span>
            </a>
        </td>
        <td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; width: 100px;">
            <a href="Localization/Spanish.md" 
               style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
                <img src="https://flagsapi.com/ES/flat/64.png" height=30><br>
                <span style="color: #f0f0f0">Español</span>
            </a>
        </td>
        <td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; width: 100px;">
            <a href="Localization/French.md" 
               style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
                <img src="https://flagsapi.com/FR/flat/64.png" height=30><br>
                <span style="color: #f0f0f0">Français</span>
            </a>
        </td>
        <td align="center" style="background-color: #1d1d1d; border-radius: 10px; padding: 10px; width: 100px;">
            <a href="Localization/Portuguese-BR.md" 
               style="display: block; width: 100%; height: 100%; text-align: center; text-decoration: none; color: #333; cursor: pointer;">
                <img src="https://flagsapi.com/BR/flat/64.png" height=30><br>
                <span style="color: #f0f0f0">Português-BR</span>
            </a>
        </td>
    </tr>
</table>

---

## Особенности

> Новые особенности активно разрабатываются. Список ниже показывает то что на данный момент имплементировано.

- **Базированный на Unity интерфейс для редактирования** — Встроен внутри Unity
- **Система спавна объектов** — Ставьте широкий спектр объектов в свою карту из структурированной спавн-панели.
- **Поддержка объектов на стороне клиента** — Некоторые типы объектов заспавнены только на части клиента и не влияют на состояние сервера.
- **Сохранять и загружать карты** — Облегчите свою работу в формате кастомной карты.

---

## Поддерживаемые типы объектов

> [!NOTE]
> Объекты помеченые как Client заспавнены локально на стороне клиента и не копируются на стороне сервера

TME поддерживает спавн следующий типов объектов. 

| Тип объекта | Сторона спавна |
|-|-|
| Doors | Server |
| Clutter | Server |
| Interactables | Server |
| Pickups | Server |
| Lockers | Server |
| Waypoints | Server |
| Cameras | Server |
| Targets | Server |
| Teleporters | Server |
| Primitives | Client |
| Lights | Client |
| Capybaras | Client |

---
## Использование и команды

Для просмотра полного списка внутриигровой консоли и админ команд, прав и коротких путей, рассмотрите нашу документацию комманд: **[Commands Documentation](Commands.md)**.

## Установка

### Пререквизиты

> [!IMPORTANT]
> До того как вы начнете, убедитесь в том что у вас установлен: [Unity Hub](https://unity.com/download)

### Начало работы

1. Скачайте репозиторий по ссылке: https://www.github.com/Thaumiel-Team/ThaumielMapEditorUnityProject

2. Извлеките скачанный файл.

3. Откройте проект в Unity Hub при помощи кнопки Add и выбора извлеченной папки с файлами.

---

Discord Сервер: https://discord.gg/N8qrNHf4s9

Замечание по зависимостям: [Нажмите сюда](Dependencies.md) - (Dependencies.md)

*Thaumiel Map Editor находится в разработке. Вклад, фидбэк и баг репорты всегда приветствуются.*

(EN)
Smart Defense Protocols - Base Defense & Zone Management Mod
I built a lightweight RimWorld mod with AI assistance to simplify colony defense and safety zone management. I initially made it for my own playthroughs because existing alternatives had UI/UX designs I wasn't happy with.

The mod is open-source - feel free to use, modify, or improve it for your own needs:

GitHub: https://github.com/SeK1ro-12/Smart-Defense-Protocols

Key Features:
Fast Zone Switching
Instant restriction/assignment of safety zones across all groups at once — colonists, animals, mechs, and drones.

Pawn Roles (Combatants & Civilians)
When Red Alert triggers, combatants automatically enter drafted mode regardless of their location (can be disabled in mod settings).

Turret Power Management
Control turret power upon alert status changes (supports both instant toggle and standard flicking tasks for colonists).

Threat Monitoring (DEFCON)
Automatic threat level escalation whenever hostiles spawn on the map.

Auto-Standdown
Automatically lowers the alert level from Red to Yellow once all enemies are eliminated or flee.

Full Manual Override
Toggle option to completely disable automation and manage all alert modes and turrets manually.

Localization:
Currently fully localized in Russian. I plan to add an English translation a bit later when I have time.

Code Architecture & Known Limitations:
Single-Class Architecture
The entire codebase currently sits within a single class (generated with AI help). I'm open to refactoring it into a clean multi-class structure if needed.

Visual/UI Roadblock
I haven't yet managed to replace the default bottom-bar gizmo with a custom texture or get the icon to dynamically change color based on the active alert state.

If you have feature suggestions or know how to properly handle dynamic gizmo textures/coloring in RimWorld's UI — feel free to leave feedback or open a Pull Request on GitHub!# Smart-Defense-Protocols
Всем привет! 









Русская версия (RU)
Smart Defense Protocols — Мод для управления защитой базы и зонами безопасности
Сделал с помощью ИИ небольшой мод для удобного контроля защиты колонии и безопасных зон. Делал в первую очередь для себя — существующие аналоги есть, но их интерфейс меня совсем не устраивал.
Код открыт и выложен на GitHub — забирайте, адаптируйте и улучшайте под свои задачи:

GitHub: https://github.com/SeK1ro-12/Smart-Defense-Protocols

Основные функции:
Быстрое управление зонами
Мгновенная смена зон безопасности сразу для всех групп — поселенцев, животных, мехов и дронов.

Роли пешек (Бойцы и Гражданские)
При включении «Красной зоны» (Red Alert) бойцы автоматически переходят в режим призыва, где бы они ни находились (функцию можно отключить в настройках).

Контроль турелей
Автоматическое или ручное управление питанием турелей при смене уровня тревоги (поддерживается мгновенное переключение или создание стандартных задач на переключение для поселенцев).

Мониторинг угроз (DEFCON)
Автоматическое повышение уровня тревоги при появлении врагов на карте.

Авто-снижение тревоги
Откат уровня с Красного до Жёлтого после уничтожения или бегства всех врагов.
Полный ручной режим
Возможность полностью отключить автоматику и контролировать все режимы и турели вручную.

Локализация:
На данный момент мод полностью на русском языке. Английский перевод планирую добавить чуть позже, когда появится свободное время.

Особенности кода и технические нюансы:
Код в одном классе
Доверился генерации ИИ и не стал дробить проект на множество файлов. Если для удобства чтения сообщества понадобится - позже разделю по классам.

Текущие сложности с визуалкой
Пока не удалось подменить стандартный ярлык на нижней панели на собственную текстуру и сделать так, чтобы иконка динамически меняла цвет в зависимости от текущего режима тревоги.

Если есть идеи по функционалу или вы знаете, как красиво допилить визуальную часть - пишите в комментариях или создавайте Pull Request на GitHub! 

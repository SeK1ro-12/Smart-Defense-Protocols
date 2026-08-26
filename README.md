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

С помощью ИИ создал небольшой мод для управления защитой базы и зонами безопасности. Делал в первую очередь для себя, так как существующие аналоги есть, но их интерфейс мне совсем не нравится.

Код открыт и выложен на GitHub — берите, изменяйте и улучшайте под свои нужды!

Основные функции мода:

Быстрое управление зонами: Мгновенная смена зон безопасности сразу для всех групп — поселенцев, животных, мехов и дронов.

Контроль турелей: Управление питанием турелей при смене режима тревоги (с поддержкой мгновенного переключения или создания задач на переключение для пешек).

Мониторинг угроз (DEFCON): Автоматическое повышение уровня тревоги при появлении врагов на карте.

Авто-снижение тревоги: Возможность автоматического отката режима с Красного до Жёлтого, когда все враги на карте уничтожены или сбежали.

Полный ручной режим: Настройка, позволяющая полностью отключить автоматику и управлять всеми режимами и турелями вручную.

Особенности кода и нюансы:

Весь код написан в одном классе: Доверился ИИ и не стал дробить проект на множество файлов. Если нужно для удобства чтения — в будущем без проблем разделю по классам.

Что пока не удалось реализовать: Не получилось заменить стандартный ярлык снизу на свою картинку и сделать так, чтобы иконка динамически меняла цвет в зависимости от выбранного режима.

Если есть предложения по функционалу или кто-то подскажет, как красиво допилить визуал — пишите, с удовольствием доработаем вместе!

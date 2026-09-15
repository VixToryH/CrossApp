# CrossApp

## Предметна область

Бібліотека.

## Основні сутності

- Book — видання.
- BookCopy — конкретний примірник.
- Reader — читач.
- Loan — видача.

## Призначення застосунку

Система призначена для обліку видач примірників книг читачам і повернень.

## Структура проєкту

```text
CrossApp/
├── src/
│   ├── Core/
│   │   ├── Core.csproj
│   │   ├── EnvironmentInfo.cs
│   │   └── EnvironmentReport.cs
│   │
│   └── Cli/
│       ├── Cli.csproj
│       └── Program.cs
│
├── CrossApp.slnx
├── README.md
└── .gitignore
```

- **`src/Core`** (`classlib`) — бібліотека класів, що містить спільний код для отримання інформації про середовище (`EnvironmentInfo`, `EnvironmentReport`). Підтримує multi-targeting (`net8.0;net10.0`).

- **`src/Cli`** (`console app`) — консольний застосунок (`net10.0`), який залежить від `Core` через `ProjectReference` та відповідає за форматування і відображення даних.

## Запуск

Збірка проєкту:

```powershell
dotnet build
```

Запуск застосунку:

```powershell
dotnet run --project src/Cli
```

Запуск у режимі JSON:

```powershell
dotnet run --project src/Cli -- --json
```

## Публікація

Виконано self-contained публікацію застосунку для двох RID:

win-x64 — 80 381 152 байти (~80,38 MB)
linux-x64 — 82 616 665 байтів (~82,62 MB)

Linux-версія більша на 2 235 513 байтів (~2,24 MB).


Також виконано framework-dependent публікацію для win-x64:

win-x64 — 203 638 байтів (~0,204 MB).

## Порівняння публікацій

| RID | Режим | Розмір | Чи потрібен встановлений runtime |
|---|---|---:|---|
| win-x64 | self-contained | 80 381 152 байти (~80,38 MB) | Ні |
| linux-x64 | self-contained | 82 616 665 байтів (~82,62 MB) | Ні |
| win-x64 | framework-dependent | 203 638 байтів (~0,204 MB) | Так |

Self-contained публікація містить .NET runtime, тому має значно більший розмір. Framework-dependent публікація не містить runtime і потребує встановленої сумісної версії .NET.

## Multi-targeting

Проєкт `Core` підтримує дві цільові версії .NET: `net8.0` та `net10.0`.

У `Core.csproj` використано:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>

Версія .NET SDK: 10.0.400.


## Залежності між проєктами

`Cli` використовує `Core` через `ProjectReference`.

Напрямок залежності:

Cli → Core

`Core` не залежить від `Cli`.

## Запуск опублікованого застосунку

Self-contained версію для Windows було запущено безпосередньо з папки `publish`.

Основний виконуваний файл — `Cli.exe`.


# Веб-сервис для геномной сборки нуклеотидных последовательностей

Веб-приложение для работы с результатами секвенирования в формате `.ab1`.

Приложение позволяет:

- загружать риды секвенирования;
- извлекать нуклеотидные последовательности из `.ab1` файлов;
- извлекать данные хроматограмм и качества;
- собирать консенсусную последовательность методом OLC;
- просматривать результат сборки;
- редактировать консенсусную последовательность;
- сохранять изменения;
- скачивать результат в формате FASTA.

Проект состоит из нескольких частей:

- `API` — серверная часть приложения;
- `Web_prototype` — Web-интерфейс;
- `BusinessLogic` — логика обработки `.ab1` файлов и сборки последовательностей;
- `Database` — работа с базой данных через Entity Framework Core;
- `DataModels` — общие модели данных;
- `Tests` — модульные тесты и демонстрационные `.ab1` файлы.

---

## Стек технологий

- .NET 8
- ASP.NET Core Web API
- ASP.NET Core Razor Pages
- Entity Framework Core 8
- PostgreSQL
- Npgsql
- Docker
- Docker Compose
- BCrypt.Net-Next
- NLog
- MSTest
- Moq
- Bootstrap
- JavaScript

---

## Требования

Для запуска через Docker должны быть установлены:

- Docker;
- Docker Compose.

## Быстрый запуск через Docker

Рекомендуемый способ запуска приложения — через Docker Compose.

```
cd diplom
docker compose up --build
```
После запуска будут доступны:

```text
Web:     http://localhost:5202
API:     http://localhost:5027
Swagger: http://localhost:5027/swagger
```
В состав Docker Compose входят три сервиса:

- `db` — PostgreSQL;
- `api` — ASP.NET Core Web API;
- `web` — веб-интерфейс приложения.

## Конфигурация Docker

Параметры запуска задаются через файл `.env`.

Пример `.env.example`:

```env
POSTGRES_DB=Genom_db
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432
API_PORT=5027
WEB_PORT=5202
```

Внутри Docker Compose API подключается к PostgreSQL по имени сервиса:

```text
Host=db;Port=5432;Database=Genom_db;Username=postgres;Password=postgres
```

Web-приложение внутри Docker обращается к API по адресу:

```text
http://api:8080/
```

Снаружи контейнеров сервисы доступны через проброшенные порты:

- Web — `http://localhost:5202`;
- API — `http://localhost:5027`;
- PostgreSQL — `localhost:5432`.

Для локального запуска без Docker дополнительно нужны:

- .NET SDK 8.0;
- PostgreSQL 14+;
- инструмент Entity Framework Core CLI.

## Демонстрационный пример

В проекте уже есть тестовые `.ab1` файлы:

```text
diplom/Tests/TestData/Ab1/HO6_rbcL_For.ab1
diplom/Tests/TestData/Ab1/HO6_rbcL_Rev.ab1
diplom/Tests/TestData/Ab1/HO6_rbcL_for2.ab1
diplom/Tests/TestData/Ab1/HO6_rbcL_siRev.ab1
```

Порядок демонстрации:

1. Запустите приложение:

   ```bash
   cd diplom
   docker compose up --build
   ```

2. Откройте Web-интерфейс:

   ```text
   http://localhost:5202
   ```

3. Зарегистрируйте нового пользователя.
4. Войдите в систему.
5. Перейдите в раздел «Мои проекты».
6. Создайте новый проект, например `Demo rbcL`.
7. Откройте созданный проект.
8. Загрузите демонстрационные `.ab1` файлы из папки `Tests/TestData/Ab1`.
9. Нажмите «Запустить сборку».
10. Откройте результат сборки.
11. При необходимости отредактируйте консенсусную последовательность.
12. Сохраните изменения.
13. Скачайте результат в формате FASTA через кнопку «Скачать FASTA».
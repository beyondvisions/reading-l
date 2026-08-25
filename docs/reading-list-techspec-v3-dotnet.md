# Technical Specification — Reading List

**.NET / ASP.NET Core implementation**

Version 3.0 · Draft · August 2026

| Field | Value |
| --- | --- |
| Project name | Reading List |
| Document type | Technical specification |
| Companion document | Functional Specification — Reading List, v1.0 |
| Version | 3.0 |
| Status | Draft |
| Date | August 2026 |
| Platform | .NET 10 (LTS) · ASP.NET Core · Entity Framework Core 10 · C# 14 |

---

## 1. Introduction

### 1.1 Purpose

This document describes how the Reading List application is built. Where the functional specification states *what* the system does from the user's point of view, this technical specification defines the implementation: the architecture, the technology stack, the data model, the programming interface (API), and how each functional requirement is realised in code using C# and ASP.NET Core.

### 1.2 Relationship to the functional specification

Every element here traces back to the functional specification. The five functional requirements become five REST endpoints, and the business rules become validation and error handling in the service and API layers. Section 9 provides an explicit traceability matrix.

### 1.3 Audience

Developers who implement or maintain the application, and reviewers who verify that the delivered system matches what was agreed.

### 1.4 Choice of platform version

.NET 10 is the current Long Term Support release, supported until November 2028. .NET 8 (LTS) and .NET 9 (STS) both reach end of support on 10 November 2026, so neither is a viable target for a system entering development now. If a fixed environment constrains the project to .NET 8, every design decision in this document still applies; only the target framework moniker changes, and two minor points noted in section 4.2 require the alternative form.

---

## 2. Architecture overview

The application follows a simple layered design behind a REST API, implemented with ASP.NET Core. A request flows through the layers and a response flows back:

- **API layer** — `[ApiController]` classes receive HTTP requests, validate input, and return HTTP responses with the correct status codes.
- **Domain / service layer** — service classes registered in the dependency-injection container apply the business rules (required fields, valid status, existence checks).
- **Data access layer** — an Entity Framework Core `DbContext` reads and writes `Book` records in the database.

```text
HTTP request
     |
     v
[ BooksController ]      <-- [ApiController], model validation, status codes
     |
     v
[ BookService ]          <-- business rules, existence checks
     |
     v
[ ReadingListDbContext ] <-- EF Core / provider -> database (persistent store)
     |
     v
HTTP response (JSON)
```

The system is stateless between requests; all state lives in the persistent store. There is no authentication, because the functional specification places user accounts out of scope.

Cross-cutting concerns are configured once, in `Program.cs`: dependency registration, JSON serialisation options, the database provider, and the global exception handler. ASP.NET Core has no equivalent of component scanning, so every service is registered explicitly — this makes the composition of the application readable in a single file.

> **Design decision D-1 — no separate repository layer.** EF Core's `DbContext` already implements the repository and unit-of-work patterns; wrapping it in a hand-written `BookRepository` adds a layer that forwards calls without adding behaviour. The service therefore depends on `DbContext` directly. Teams that require a database-free unit test of `BookService` may reintroduce a thin `IBookRepository` abstraction at a cost of roughly two hours; the in-memory or SQLite-in-memory EF provider covers the same need without it.

---

## 3. Technology stack

The application is built on the .NET ecosystem. Each component below maps directly onto one of the architectural layers.

| Concern | Technology | Notes |
| --- | --- | --- |
| Language | C# 14 | Ships with the .NET 10 SDK. |
| Runtime | .NET 10 (LTS) | Long-term-support runtime, supported to Nov 2028. |
| Framework | ASP.NET Core | Kestrel web server hosted in-process. |
| Web / REST | ASP.NET Core MVC controllers | `[ApiController]` endpoints under `/books`. |
| Persistence | Entity Framework Core 10 | `DbContext` + LINQ; migrations for schema. |
| Database | SQLite (dev) / PostgreSQL (prod) | Swappable via configuration and provider package. |
| Validation | DataAnnotations + ModelState | `[Required]`, enum binding; automatic 400 responses. |
| Build | .NET SDK (MSBuild) + NuGet | `dotnet build`, `dotnet publish`. |
| Runtime artifact | Framework-dependent DLL | `dotnet ReadingList.Api.dll`. |
| API documentation | OpenAPI via `Microsoft.AspNetCore.OpenApi` | Served in development only. |
| Tests | xUnit + `WebApplicationFactory` | Unit and in-process integration tests. |

### 3.1 Layer-to-construct mapping

| Layer | .NET construct |
| --- | --- |
| Entry point / composition | `Program.cs` (top-level statements, `WebApplicationBuilder`) |
| API layer | `[ApiController] [Route("books")] class BooksController : ControllerBase` |
| Service layer | `IBookService` / `BookService`, registered `AddScoped` |
| Data access | `ReadingListDbContext : DbContext` exposing `DbSet<Book> Books` |
| Domain model | POCO `Book` class, configured via `OnModelCreating` |
| Input contract | `BookRequest` record with validation attributes |
| Output contract | `BookResponse` record |
| Error handling | `GlobalExceptionHandler : IExceptionHandler` + `AddProblemDetails()` |

### 3.2 NuGet dependencies

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.*" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.*" />
```

Test project additionally references `Microsoft.AspNetCore.Mvc.Testing`, `xunit`, `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk`.

---

## 4. Data model

The system manages a single resource: the `Book`. Each book is stored as one row in the `book` table and mapped to an EF Core entity.

### 4.1 Book fields

| Field | Type | Constraints | Notes |
| --- | --- | --- | --- |
| `Id` | `long` | Primary key, database-generated, immutable | Assigned by the system; never supplied by the client. EF Core infers key and identity from the name `Id`. |
| `Title` | `string` | Required, non-empty, max 255 | The title of the book. |
| `Author` | `string` | Required, non-empty, max 255 | The name of the author. |
| `Status` | `Status` (enum) | Required, one of three values | See 4.2. |

```csharp
public class Book
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public Status Status { get; set; }
}
```

### 4.2 Status values

The status field is modelled as a C# enum. C# enum members are PascalCase by convention, while both the JSON wire format and the stored database code use lower snake_case. The mapping is therefore made explicit in two places: a JSON converter for the API surface, and an EF Core value converter for the database column.

| Meaning (functional spec) | C# enum member | JSON / stored value |
| --- | --- | --- |
| want to read | `Status.WantToRead` | `want_to_read` |
| reading | `Status.Reading` | `reading` |
| finished | `Status.Finished` | `finished` |

**JSON serialisation**

```csharp
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));
```

On .NET 8, `JsonNamingPolicy.SnakeCaseLower` is available and this code is unchanged. An alternative that pins each name individually — and is therefore immune to convention changes — is `[JsonStringEnumMemberName("want_to_read")]` on each member, which requires .NET 9 or later.

**Database persistence**

```csharp
modelBuilder.Entity<Book>(e =>
{
    e.ToTable("book");
    e.Property(b => b.Title).IsRequired().HasMaxLength(255);
    e.Property(b => b.Author).IsRequired().HasMaxLength(255);
    e.Property(b => b.Status)
        .HasMaxLength(20)
        .IsRequired()
        .HasConversion(
            v => v switch
            {
                Status.WantToRead => "want_to_read",
                Status.Reading    => "reading",
                _                 => "finished"
            },
            v => v switch
            {
                "want_to_read" => Status.WantToRead,
                "reading"      => Status.Reading,
                _              => Status.Finished
            });
});
```

### 4.3 Relational schema (illustrative)

The schema is produced by an EF Core migration (`dotnet ef migrations add InitialCreate`) rather than written by hand. The generated definition is equivalent to:

```sql
CREATE TABLE book (
    id     BIGINT PRIMARY KEY,
    title  VARCHAR(255) NOT NULL,
    author VARCHAR(255) NOT NULL,
    status VARCHAR(20)  NOT NULL
           CHECK (status IN ('want_to_read','reading','finished'))
);
```

The `CHECK` constraint is not generated automatically; it is declared in the entity configuration:

```csharp
e.ToTable("book", t => t.HasCheckConstraint(
    "ck_book_status",
    "status IN ('want_to_read','reading','finished')"));
```

It is defence in depth — the enum type and value converter already make an invalid value unreachable through the application — but it protects the data if rows are written by any other means.

### 4.4 JSON representation

```json
{
  "id": 1,
  "title": "The Pragmatic Programmer",
  "author": "Hunt & Thomas",
  "status": "reading"
}
```

Property names are camelCase, which is the ASP.NET Core default and matches the field names in this specification.

### 4.5 Data transfer objects

The entity is never bound directly from the request body. A separate input type guarantees that `id` cannot be supplied by the client, satisfying the immutability rule in the functional specification structurally rather than by validation.

```csharp
public record BookRequest(
    [Required(AllowEmptyStrings = false)] string? Title,
    [Required(AllowEmptyStrings = false)] string? Author,
    [Required] Status? Status);

public record BookResponse(long Id, string Title, string Author, Status Status);
```

The properties are declared nullable so that an omitted field is distinguishable from an empty one and produces a `[Required]` violation rather than binding silently to a default. In particular, a non-nullable `Status` would default to `WantToRead` when absent, which would silently accept an invalid request.

---

## 5. API design

The API is REST over HTTP and exchanges JSON. The base path is `/books`. The five endpoints correspond one-to-one to the five functional requirements.

| # | Requirement | Method | Path | Success | Action result |
| --- | --- | --- | --- | --- | --- |
| 1 | Add a book | POST | `/books` | 201 Created | `CreatedAtAction` |
| 2 | List all books | GET | `/books` | 200 OK | `Ok` |
| 3 | Get a book by id | GET | `/books/{id}` | 200 OK | `Ok` / `NotFound` |
| 4 | Update a book | PUT | `/books/{id}` | 200 OK | `Ok` / `NotFound` |
| 5 | Delete a book | DELETE | `/books/{id}` | 204 No Content | `NoContent` / `NotFound` |

### 5.1 Add a book

`POST /books` — creates a new book. The client supplies `title`, `author` and `status`; the system assigns the `id` and returns the stored book. The response carries a `Location` header pointing at the new resource, produced by `CreatedAtAction`.

```http
Request:  { "title": "Dune", "author": "Frank Herbert", "status": "want_to_read" }
Response 201: { "id": 7, "title": "Dune", "author": "Frank Herbert", "status": "want_to_read" }
Location: /books/7
```

Errors: 400 if a required field is missing or the status is invalid.

### 5.2 List all books

`GET /books` — returns every book as a JSON array, status 200. When the list is empty the response body is `[]`, not 204 and not `null`.

### 5.3 Get a book by identifier

`GET /books/{id}` — returns the single book with the given id. Errors: 404 if no book has that id. A non-numeric `{id}` fails route binding and also yields 400 rather than 404; see 6.3.

### 5.4 Update a book

`PUT /books/{id}` — replaces the title, author and status of an existing book. The id itself cannot be changed: it is taken from the route and any value in the body is ignored, because `BookRequest` has no `id` member. Returns the updated book with status 200.

```http
Request:  { "title": "Dune", "author": "Frank Herbert", "status": "reading" }
Response 200: { "id": 7, "title": "Dune", "author": "Frank Herbert", "status": "reading" }
```

Errors: 404 if the book does not exist; 400 if the supplied data is invalid.

Note that PUT is a full replacement: all three fields are required, and omitting one is a 400 rather than a partial update. The functional specification's wording ("change a book's title, author, or status") is satisfied because the client may resend unchanged values. A partial-update variant would require PATCH and is not in scope.

### 5.5 Delete a book

`DELETE /books/{id}` — removes the book from the list. Returns an empty body with status 204. A subsequent GET for the same id returns 404. Errors: 404 if the book does not exist.

### 5.6 Controller sketch

```csharp
[ApiController]
[Route("books")]
public class BooksController(IBookService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookResponse>> Create(BookRequest request)
    {
        var created = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookResponse>>> GetAll()
        => Ok(await service.GetAllAsync());

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BookResponse>> GetById(long id)
        => Ok(await service.GetAsync(id)); // service throws BookNotFoundException

    [HttpPut("{id:long}")]
    public async Task<ActionResult<BookResponse>> Update(long id, BookRequest request)
        => Ok(await service.UpdateAsync(id, request));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
```

Existence checks live in the service, not the controller, so that the rule is enforced once regardless of caller. The service signals absence by throwing `BookNotFoundException`, which the global handler translates into a 404 (section 6). This keeps controller actions free of repeated null checks.

---

## 6. Error handling

All errors are returned as JSON with a consistent shape so that clients can handle them uniformly. The shape is `ProblemDetails` (RFC 9457), which ASP.NET Core produces natively once `AddProblemDetails()` is registered.

> **Note on the error shape.** An alternative is a bespoke two-field body, `{ "error": ..., "detail": ... }`. This specification adopts `ProblemDetails` instead: it is the platform default, it is a published standard, and validation failures populate it automatically with per-field detail. The functional specification requires only a "clear explanation of what is wrong" and does not constrain the body, so either choice is compliant. If a client requires the two-field shape, it can be produced by implementing `IExceptionHandler` for 404s and overriding `ApiBehaviorOptions.InvalidModelStateResponseFactory` for 400s.

### 6.1 Validation error (400)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Status": [ "The Status field is required." ]
  }
}
```

### 6.2 Not found (404)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "No book exists with id 42."
}
```

### 6.3 Invalid enum value — a platform-specific case

A syntactically valid request carrying an unknown status, such as `"status": "banana"`, fails during JSON deserialisation, before model validation runs. It therefore does not appear as a `[Required]` violation but as a deserialisation error, and the default message names the .NET type rather than the permitted values. To satisfy the acceptance criterion that an invalid status is rejected "with a helpful message", the enum converter is paired with a handler that produces:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "status": [ "Must be one of: want_to_read, reading, finished." ]
  }
}
```

This is implemented by catching `JsonException` in the global handler and rewriting the body. Both paths — missing field and unparseable value — must be covered by tests; see section 9.

### 6.4 Status codes used

| Code | Meaning | When |
| --- | --- | --- |
| 200 OK | Success with a body | List, get, and update succeed. |
| 201 Created | Resource created | A book is added successfully; `Location` header set. |
| 204 No Content | Success, empty body | A book is deleted successfully. |
| 400 Bad Request | Invalid input | Missing required field, invalid status, or unbindable route id. |
| 404 Not Found | Unknown resource | No book exists for the given id. |

### 6.5 Composition in `Program.cs`

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddDbContext<ReadingListDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IBookService, BookService>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapControllers();
app.Run();
```

---

## 7. Project structure

A minimal solution layout following .NET conventions, keeping the layers separate. There is no `Startup.cs` in modern ASP.NET Core: configuration and composition live in `Program.cs`.

```text
ReadingList.sln
src/ReadingList.Api/
    ReadingList.Api.csproj          # target framework, NuGet references
    Program.cs                      # entry point, DI, middleware pipeline
    appsettings.json                # connection strings, logging
    appsettings.Development.json    # SQLite for local development
    Controllers/
        BooksController.cs          # API layer
    Services/
        IBookService.cs
        BookService.cs              # business rules, existence checks
    Data/
        ReadingListDbContext.cs     # DbSet<Book>, OnModelCreating
        Migrations/                 # EF Core generated migrations
    Models/
        Book.cs                     # entity
        Status.cs                   # enum
    Dtos/
        BookRequest.cs              # input contract + validation
        BookResponse.cs             # output contract
    Errors/
        BookNotFoundException.cs
        GlobalExceptionHandler.cs   # IExceptionHandler
tests/ReadingList.Api.Tests/
    ReadingList.Api.Tests.csproj
    BookServiceTests.cs             # unit tests over business rules
    BooksEndpointsTests.cs          # integration tests via WebApplicationFactory
```

### 7.1 Local commands

```bash
dotnet new sln -n ReadingList
dotnet new webapi -n ReadingList.Api -o src/ReadingList.Api --use-controllers
dotnet ef migrations add InitialCreate --project src/ReadingList.Api
dotnet ef database update --project src/ReadingList.Api
dotnet run --project src/ReadingList.Api
dotnet test
```

---

## 8. Work estimation

Indicative effort to build the application from scratch, expressed in hours. Estimates assume one developer familiar with ASP.NET Core.

| # | Task | Description | Estimate (hours) |
| --- | --- | --- | --- |
| 1 | Project setup | `dotnet new` scaffold, solution and test project, NuGet references, `appsettings`, connection string. | 3 |
| 2 | Data model | `Book` entity, `Status` enum, EF configuration, value converter, initial migration. | 4 |
| 3 | Data access layer | `ReadingListDbContext`, `DbSet`, DI registration, provider switch dev/prod. | 2 |
| 4 | Service layer | Business rules and existence checks (CRUD orchestration), DTO mapping. | 8 |
| 5 | API — create & read | `POST /books`, `GET /books`, `GET /books/{id}`. | 8 |
| 6 | API — update & delete | `PUT /books/{id}`, `DELETE /books/{id}`. | 6 |
| 7 | Validation | DataAnnotations on the input DTO, nullable-field handling, invalid-enum path (6.3). | 4 |
| 8 | Error handling | `IExceptionHandler`, `ProblemDetails`, consistent shape across 400 and 404. | 3 |
| 9 | Tests | xUnit unit tests plus `WebApplicationFactory` integration tests covering the acceptance criteria. | 8 |
| 10 | Docs & review | OpenAPI wire-up, README, code review, fixes. | 4 |
| | **Total estimated effort** | | **50 hours** |

Add roughly 15–20% contingency for unknowns and environment/CI setup, giving a planning figure of about 58–60 hours.

Project setup and error handling are estimated lightly because the `dotnet new` templates and the built-in `ProblemDetails` support remove most of that work. The remaining tasks are dominated by the problem rather than the platform.

---

## 9. Traceability and verification

Each row below links a functional-specification element to its implementation and to the test that proves it.

| Functional element (v1.0) | Implementation | Verified by |
| --- | --- | --- |
| FR1 Add a book | `POST /books` → `BookService.CreateAsync` | 201, body echoes input, id assigned |
| FR2 List all books | `GET /books` → `GetAllAsync` | 200 with array; `[]` when empty |
| FR3 Get a book by identifier | `GET /books/{id}` → `GetAsync` | 200 for existing; 404 otherwise |
| FR4 Update a book | `PUT /books/{id}` → `UpdateAsync` | 200, change persisted, id unchanged |
| FR5 Delete a book | `DELETE /books/{id}` → `DeleteAsync` | 204, then 404 on re-fetch |
| BR1 Title, author, status required | `[Required]` on `BookRequest`; `IsRequired()` in EF | 400 per omitted field |
| BR2 Status is one of three values | `Status` enum, JSON converter, CHECK constraint | 400 for unknown value (6.3) |
| BR3 Identifier unique and immutable | Database-generated key; no `id` in `BookRequest` | `id` in body ignored on PUT |
| BR4 Clear "not found" response | `BookNotFoundException` → global handler | 404 + `ProblemDetails` on GET/PUT/DELETE |
| BR5 Clear rejection of invalid input | `ModelState` → `ValidationProblemDetails` | 400 naming the offending field |

---

## 10. Open decisions

| ID | Decision | Chosen | Alternative and cost of change |
| --- | --- | --- | --- |
| D-1 | Repository layer | None — service uses `DbContext` directly | Thin `IBookRepository`; ~2 h, adds a layer without behaviour (see section 2) |
| D-2 | Endpoint style | MVC controllers | Minimal APIs — leaner and idiomatic, but the layered diagram maps less directly; ~2 h to convert |
| D-3 | Error body | `ProblemDetails` (RFC 9457) | Bespoke `{ error, detail }` shape; ~2 h to implement (see section 6) |
| D-4 | Development database | SQLite file | EF in-memory provider — faster, but does not enforce constraints; PostgreSQL via container — highest fidelity |
| D-5 | Target framework | `net10.0` (LTS to Nov 2028) | `net8.0` if the environment is pinned; end of support Nov 2026 |

---

*Technical Specification — Reading List, v3.0 (Draft). Companion document: Functional Specification — Reading List, v1.0.*

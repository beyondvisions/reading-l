using FluentValidation;
using ReadingList.Services;  

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();   
builder.Services.AddOpenApi();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<IBookService, BookService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();                

app.Run();
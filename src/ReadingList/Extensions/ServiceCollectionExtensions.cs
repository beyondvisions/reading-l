using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ReadingList.Data;
using ReadingList.Filters;
using ReadingList.Repositories;
using ReadingList.Services;

namespace ReadingList.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ReadingListDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IBookRepository, BookRepository>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBookService, BookService>();

        return services;
    }

    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        services.AddOpenApi();
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }
}
using System.Data;
using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.LanguageAccounts;
using Application.Users;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.LanguageAccount;
using Infrastructure.Time;
using Infrastructure.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;
using Application.FlashcardCollection;
using Infrastructure.FlashcardCollection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices()
            .AddDatabase(configuration)
            .AddHealthChecks(configuration)
            .AddAuthenticationInternal(configuration)
            .AddAuthorizationInternal();

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<ApplicationDbContext>(
            options => options
                .UseSqlServer(connectionString, sqlServerOptions =>
                    sqlServerOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "dbo")));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IUserWriteRepository, UserWriteRepository>();

        services.AddScoped<IUserReadRepository, UserReadRepository>();

        services.AddScoped<ILanguageAccountRepository, LanguageAccountRepository>();

        services.AddScoped<IFlashcardCollectionRepository, FlashcardCollectionRepository>();

        services.AddScoped<IFlashcardCollectionReadRepository, FlashcardCollectionReadRepository>();

        services.AddScoped<ILanguageAccountReadRepository, LanguageAccountReadRepository>();

        services.AddScoped<IFlashcardRepository, FlashcardRepository>();

        services.AddTransient<IDbConnection>(sp => new SqlConnection(connectionString));

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddSqlServer(configuration.GetConnectionString("Database")!);

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenProvider, TokenProvider>();

        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }
}

using Dourak.Application.Auth;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using Dourak.Infrastructure.Email;
using Dourak.Infrastructure.Identity;
using Dourak.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dourak.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<DourakDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<DourakDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<DourakDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<JwtTokenGenerator>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        // No SMTP host configured -> log emails instead of sending (see LoggingEmailSender);
        // set Email:Host (e.g. via env vars once a provider like Zoho Mail is set up) to switch
        // to real delivery with no code change.
        var emailHost = configuration.GetSection(EmailOptions.SectionName)["Host"];
        if (string.IsNullOrWhiteSpace(emailHost))
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        else
            services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddSingleton<IRandomShuffler, CryptoRandomShuffler>();
        services.AddSingleton<IEvidenceFileStorage, Storage.DiskEvidenceFileStorage>();

        return services;
    }
}

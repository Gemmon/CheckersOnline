using CheckersLone.Data;
using CheckersLone.Hubs;
using CheckersLone.Models;
using CheckersLone.Services;
using Microsoft.EntityFrameworkCore;

internal class Program
{

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();



        builder.Services.AddControllersWithViews();

        builder.Services.AddControllers();

        builder.Services.AddSignalR(options =>
        {
            options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
            options.KeepAliveInterval = TimeSpan.FromSeconds(0.3);

        }).AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = null;
        });



        builder.Logging.AddConsole();

        builder.Services.AddDbContextFactory<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


        builder.Services.AddSingleton<GameService>();

        var app = builder.Build();


        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();
       

        app.UseAuthorization();

        app.MapRazorPages();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<GameHub>("/gameHub");
        });


        app.Run();
    }
}
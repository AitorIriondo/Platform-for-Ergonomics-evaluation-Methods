using Platform_for_Ergonomics_evaluation_Methods;
using Platform_for_Ergonomics_evaluation_Methods.Services;
using Platform_for_Ergonomics_evaluation_Methods.Utils;
using System.Diagnostics;

//Environment.Exit(0);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();  // Register memory cache
builder.Services.AddSingleton<MessageStorageService>();
builder.Services.AddSingleton<JsonDeserializer>();  // Register the JsonDeserializer
builder.Services.AddSingleton<TcpServerService>();  // Register the TCP server

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Start the TCP server
var tcpServer = app.Services.GetRequiredService<TcpServerService>();
tcpServer.StartServer(); // Starts the TCP server in the background

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

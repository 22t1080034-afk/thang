using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels;
using SV22T1080034.Shop.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddMvcOptions(option =>
    {
        option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Register Image Providers
builder.Services.AddScoped<IImageProvider, UnsplashImageProvider>();
builder.Services.AddScoped<IImageProvider, PicsumImageProvider>();
builder.Services.AddScoped<IImageProvider, PexelsImageProvider>();
builder.Services.AddScoped<IImageProvider, LoremFlickrImageProvider>();

builder.Services.AddScoped<IImageFetchService, ImageFetchService>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();

// Add Session (QUAN TRỌNG)
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromHours(2);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(option =>
    {
        option.Cookie.Name = "LiteCommerce.Shop";
        option.LoginPath = "/Account/Login";
        option.AccessDeniedPath = "/Account/Login";
        option.ExpireTimeSpan = TimeSpan.FromDays(7);
        option.SlidingExpiration = true;
        option.Cookie.HttpOnly = true;
        option.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession(); // QUAN TRỌNG: phải trước MapControllerRoute

// Vietnamese culture
var cultureInfo = new System.Globalization.CultureInfo("vi-VN");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Connection string
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString 'LiteCommerceDB' not found.");

SV22T1080034.BusinessLayers.Configuration.Initialize(connectionString);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels;
using SV22T1080034.Shop.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddMvcOptions(option =>
    {
        option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add HttpClientFactory for ImageFetchService
builder.Services.AddHttpClient();

// Register Image Providers (chỉ đăng ký từng provider riêng)
// DI sẽ tự động inject chúng thành IEnumerable<IImageProvider>
builder.Services.AddScoped<UnsplashImageProvider>();
builder.Services.AddScoped<LoremFlickrImageProvider>();
builder.Services.AddScoped<PicsumImageProvider>();

// Add ImageFetchService - sẽ nhận tự động tất cả IImageProvider qua constructor
builder.Services.AddScoped<IImageFetchService, ImageFetchService>();

// Add Newsletter Service
builder.Services.AddScoped<INewsletterService, NewsletterService>();

// Add Session
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromHours(2);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});

// Add Authentication & Authorization
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

// Configure the HTTP request pipeline.
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
app.UseSession();

// Configure Vietnamese culture
var cultureInfo = new System.Globalization.CultureInfo("vi-VN");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Get Connection String
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString 'LiteCommerceDB' not found.");

// Initialize Business Layer Configuration
SV22T1080034.BusinessLayers.Configuration.Initialize(connectionString);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

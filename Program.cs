using Microsoft.EntityFrameworkCore;
using FPSample.Controllers.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVICES SECTION ---

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();


builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- 2. MIDDLEWARE PIPELINE SECTION ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseSession();

app.UseAuthorization();


app.MapControllerRoute(
    name: "default",

    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
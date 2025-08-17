using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Queries;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddRazorPages()
    .AddMvcOptions(opts =>
    {
        var p = opts.ModelBindingMessageProvider;
        p.SetValueMustNotBeNullAccessor(_ => "Please provide a value.");
        p.SetMissingBindRequiredValueAccessor(f => $"Please provide {f}.");
        p.SetMissingKeyOrValueAccessor(() => "This field is required.");
        p.SetAttemptedValueIsInvalidAccessor((v, f) => $"“{v}” isn’t valid for {f}.");
        p.SetUnknownValueIsInvalidAccessor(f => $"The value is invalid for {f}.");
        p.SetValueIsInvalidAccessor(v => $"The value “{v}” is invalid.");
        p.SetValueMustBeANumberAccessor(f => "Enter a valid number.");
        p.SetNonPropertyAttemptedValueIsInvalidAccessor(v => $"“{v}” isn’t valid.");
        p.SetNonPropertyUnknownValueIsInvalidAccessor(() => "The value is invalid.");
        p.SetNonPropertyValueMustBeANumberAccessor(() => "Enter a valid number.");
    });

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseStatusCodePagesWithReExecute("/Error", "?code={0}");
});

app.UseRouting();

// JSON ProblemDetails for missing/invalid API routes
app.Use(async (ctx, next) =>
{
    await next();

    if (!ctx.Response.HasStarted && ctx.Request.Path.StartsWithSegments("/api"))
    {
        if (ctx.Response.StatusCode is StatusCodes.Status404NotFound or StatusCodes.Status405MethodNotAllowed)
        {
            var pd = new ProblemDetails
            {
                Status = ctx.Response.StatusCode,
                Title = ctx.Response.StatusCode == 404 ? "Not Found" : "Method Not Allowed",
                Detail = "The requested API endpoint was not found or the method is not allowed.",
                Instance = ctx.Request.Path
            };
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(pd);
        }
    }
});

// Enable session
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

app.Run();

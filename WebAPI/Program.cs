using EventBookingService.WebAPI.Application;
using EventBookingService.WebAPI.Middleware;
using EventBookingService.WebAPI.Presentation;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddApplication();
builder.Services.AddPresentation();


var app = builder.Build();


app.UseMiddleware<GlobalExceptionHandlingMiddleware>();


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
// Глобальный обработчик ошибок
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
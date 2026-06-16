using EventBookingService.Application;
using EventBookingService.Infrastructure;
using EventBookingService.Infrastructure.Context;
using EventBookingService.Presentation;
using EventBookingService.Presentation.Middleware;

using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();


builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddPresentation();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}


// Глобальный обработчик ошибок
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();


if (app.Environment.IsDevelopment())
{
    //app.UseDeveloperExceptionPage();
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

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

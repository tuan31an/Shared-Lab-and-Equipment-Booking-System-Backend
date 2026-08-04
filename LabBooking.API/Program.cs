using LabBooking.API;
using LabBooking.Application;
using LabBooking.Infrastructure.Sqlserver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPresentation();
builder.Services.AddApplication();
builder.Services.AddInfrastructureSqlServer(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.OpenApi.Models;
using OracleOdbcApi.Transactions;

var builder = WebApplication.CreateBuilder(args);

// TransactionSessionManager を Singleton として登録
builder.Services.AddSingleton<TransactionSessionManager>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OracleOdbcApi",
        Version = "v1"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

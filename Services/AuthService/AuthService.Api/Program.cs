using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<MyAdaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Develop"));
});

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MyAdaDbContext>()
.AddDefaultTokenProviders();

// RSA loading



var rsa = RSA.Create(2048); // generate brand new private key in memory
builder.Services.AddSingleton(rsa);





// Token service
builder.Services.AddScoped<TokenService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.Run();

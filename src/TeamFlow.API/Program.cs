using TeamFlow.API.Extensions;
using TeamFlow.API.Middlewares;
using TeamFlow.Infrastructure;
using TeamFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
 
var builder = WebApplication.CreateBuilder(args);
 
// ─── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
Console.WriteLine(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerWithJwt();
 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
 
// ─── App ──────────────────────────────────────────────────────────────────────
var app = builder.Build();
 
// Auto-migrate on startup (dev only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
 
app.UseMiddleware<ExceptionMiddleware>();
 
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TeamFlow API v1");
    c.RoutePrefix = string.Empty; // Swagger at root
});
 
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
 
app.MapControllers();
 
app.Run();
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;
using WebChatServer.Data;
using WebChatServer.Models;
using WebChatServer.Handlers;
using WebChatServer.Services;
var builder = WebApplication.CreateBuilder(args);

var key = "aVeryStrongSecretKeyThatIsDefinitely32CharactersLong!";
var issuer = "http://localhost";
var audience = "http://localhost";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSession();

builder.Services.AddWebSockets(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(120);
});

builder.Services.AddSingleton<MessageRepository>();
builder.Services.AddSingleton<UserRepository>();

var chatRooms = new List<ChatRoomModel>
{
    new ChatRoomModel { Id = 1, Name = "Room 1", LastActive = DateTime.Now.AddMinutes(-1), LastMessageTimestamp = DateTime.Now.AddMinutes(-1) },
    new ChatRoomModel { Id = 2, Name = "Room 2", LastActive = DateTime.Now.AddMinutes(-5), LastMessageTimestamp = DateTime.Now.AddMinutes(-5) },
};
builder.Services.AddSingleton(chatRooms);

builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddSingleton<ChatFacade>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

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

app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var token = context.Request.Query["access_token"].ToString();
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("aVeryStrongSecretKeyThatIsDefinitely32CharactersLong!");

        try
        {
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "http://localhost",
                ValidAudience = "http://localhost",
                IssuerSigningKey = new SymmetricSecurityKey(key)
            }, out SecurityToken validatedToken);

            var webSocketHandler = context.RequestServices.GetRequiredService<WebSocketHandler>();
            await webSocketHandler.Handle(context);
        }
        catch
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
            }
        }
    }
    else
    {
        await next();
    }
});

app.MapRazorPages();
app.MapDefaultControllerRoute();
app.MapControllers();

app.Run();
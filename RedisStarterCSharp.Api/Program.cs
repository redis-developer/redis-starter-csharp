using RedisStarterCSharp.Api.Components.Todos;
using StackExchange.Redis;
using dotenv.net;

DotEnv.Load();
var env = DotEnv.Read(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 2));
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");

if (env != null && env.ContainsKey("REDIS_URL"))
{
    redisUrl = env["REDIS_URL"];
}

if (redisUrl is null)
{
    redisUrl = "localhost";
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ITodosStore, TodosStore>();
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisUrl));
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

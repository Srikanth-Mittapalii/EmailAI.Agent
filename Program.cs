using MongoDB.Driver;
using EmailAI.Agent.Services;
using EmailAI.Agent.Agents;
using SmartComponents.LocalEmbeddings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Email AI Agent", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
builder.Services.AddMemoryCache();

// JWT Auth
var jwtKey = builder.Configuration["Jwt:Key"] ?? "InitialFallbackKeyForDevelopmentOnly123!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// MongoDB
var mongoConnection = builder.Configuration["MongoDB:ConnectionString"];
var mongoClient = new MongoClient(mongoConnection);
var mongoDatabase = mongoClient.GetDatabase("EmailAI");
builder.Services.AddSingleton(mongoDatabase);
builder.Services.AddSingleton<LocalEmbedder>();


// Intelligence Layer (Multi-Model Fallback)
builder.Services.AddScoped<ILLMService, GroqService>();
builder.Services.AddScoped<ILLMService, SambanovaService>();
builder.Services.AddScoped<ILLMService, DeepSeekService>();
builder.Services.AddScoped<ILLMService, GeminiService>();
builder.Services.AddScoped<ILLMService, HuggingFaceLLMService>();
builder.Services.AddScoped<ILLMService, OpenRouterService>();
builder.Services.AddScoped<ILLMService, StaticRuleService>();
builder.Services.AddScoped<IntelligenceOrchestrator>();

// Services
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<VectorDbService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmailIngestionService>();

// Agents
builder.Services.AddScoped<EmailAgent>();
builder.Services.AddScoped<PlannerAgent>();
builder.Services.AddScoped<AgentOrchestrator>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

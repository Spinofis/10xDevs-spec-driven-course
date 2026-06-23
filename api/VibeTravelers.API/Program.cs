using System.Text.Json;
using System.Text.Json.Serialization;
using VibeTravels.Application;
using VibeTravels.Infrastructure;
using VibeTravelers.API.Endpoints;
using VibeTravelers.API.Middleware;

namespace VibeTravelers.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapAuthEndpoints();
            app.MapTagsEndpoints();
            app.MapTripsEndpoints();
            app.MapGenerationJobsEndpoints();

            app.Run();
        }
    }
}

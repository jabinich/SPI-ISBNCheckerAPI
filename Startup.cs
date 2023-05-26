using Microsoft.AspNetCore.Builder;

namespace SPI_ISBNValidator
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
           
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors();
            app.UseCors(builder =>
            {
                builder.WithOrigins("http://localhost:4200")
                       .AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });

            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            // Default Policy
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins("https://localhost:44351", "http://localhost:4200")
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                    });
            });

        }
    }
}

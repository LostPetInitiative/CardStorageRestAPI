using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using PatCardStorageAPI.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PatCardStorageAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:3000");
                    });
            });

            services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();


            string? cassandra_addrs = Environment.GetEnvironmentVariable("CASSANDRA_ADDRS");
            string? keyspace = Environment.GetEnvironmentVariable("KEYSPACE");
            if (cassandra_addrs == null || keyspace == null)
            {
                Trace.TraceWarning("CASSANDRA_ADDRS or KEYSPACE env var is not found. Thus using test memory storage instead Cassandra");
                var storage = new MemoryTestStorage();
                services.AddSingleton(typeof(ICardStorage), storage);
                services.AddSingleton(typeof(IPhotoStorage), storage);
            }
            else
            {
                var contactPoints = cassandra_addrs.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(addr => !string.IsNullOrEmpty(addr)).ToArray();
                Trace.TraceInformation($"Connecting to Cassandra contact points {cassandra_addrs} and keyspace {keyspace}");
                var storage = new Storage.CassandraStorage(keyspace, contactPoints);
                Trace.TraceInformation("Cassandra Storage adapter created");

                services.AddSingleton(typeof(ICardStorage), storage);
                services.AddSingleton(typeof(IPhotoStorage), storage);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseRouting();

            app.UseCors();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Repository;
using TaskManagement.Data.Migrations.Data;
using TaskManagement.Core.Repository.DAL;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RepositoryStartupExtension
    {
        public static IServiceCollection UseTaskManagementRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IProjectTaskRepository, ProjectTaskRepository>();
            services.AddScoped<IProjectUserRepository, ProjectUserRepository>();
            services.AddScoped<ITaskAttachmentRepository, TaskAttachmentRepository>();
            services.AddScoped<ITaskUserRepository, TaskUserRepository>();
            services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();
            services.AddScoped<ITaskManagementClient, TaskManagementClient>();

            return services;
        }
    }
}

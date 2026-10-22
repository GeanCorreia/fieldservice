using FieldService.Data;
using FieldService.Form.Broker;
using FieldService.Form.Data;
using FieldService.Form.Data.Repositories;
using FieldService.Form.Interfaces;
using FieldService.Form.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Form;

public static class FormModule
{
	public static IServiceCollection AddFormModule(this IServiceCollection services, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.AddScoped<IFormDataDbContextFactory, FormDataDbContextFactory>();
		services.AddScoped<IFormDatabaseService, FormDatabaseService>();
		services.AddScoped<FormRepository>();
		services.AddScoped<IFormService, FormService>();
		services.AddScoped<IFormRepository, FormRepository>();

		services.AddScoped<FormTenantRepository>();
		services.AddScoped<IFormTenantService, FormTenantService>();
		services.AddScoped<IFormTenantRepository>(sp => sp.GetRequiredService<IFormTenantService>());

		services.AddScoped<IFormSubmissionRepositoryFactory, FormSubmissionRepositoryFactory>();
		services.AddScoped<BrokerFormSubmissionBrokerProducer>();
		services.AddScoped<IFormSubmissionService, FormSubmissionService>();
		services.AddScoped<IFormFilterService, FormIoFilterService>();
		
		services.AddSqlModule<FormDbContext>(configuration);

		return services;
	}
}
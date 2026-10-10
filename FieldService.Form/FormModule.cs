using FieldService.Form.Broker;
using FieldService.Form.Data;
using FieldService.Form.Data.Repositories;
using FieldService.Form.Interfaces;
using FieldService.Form.Services;

using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Form;

public static class FormModule
{
	public static IServiceCollection AddFormModule(this IServiceCollection services)
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

		services.AddScoped<FormSubmissionRepository>();
		services.AddScoped<BrokerFormSubmissionBrokerProducer>();
		services.AddScoped<IFormSubmissionService, FormSubmissionService>();
		services.AddScoped<IFormSubmissionRepository>(sp => sp.GetRequiredService<IFormSubmissionService>());
		services.AddScoped<IFormFilterService, FormFilterService>();

		return services;
	}
}
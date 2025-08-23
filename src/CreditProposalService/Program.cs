using Contracts.Messages;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<CreditProposalRequestedConsumer>(cfg =>
    {
        cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
        cfg.UseInMemoryOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RABBIT__HOST"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RABBIT__USER"] ?? "guest");
            h.Password(builder.Configuration["RABBIT__PASS"] ?? "guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();

app.MapGet("/health", () => "ok");

app.Run();

class CreditProposalRequestedConsumer : IConsumer<CreditProposalRequested>
{
    public async Task Consume(ConsumeContext<CreditProposalRequested> ctx)
    {
        // Regras mockadas: como não possuo uma regra de aprovação no desafio, apenas considerei que será sempre aprovada.
        var approved = true;
        if (approved)
            await ctx.Publish(new CreditProposalApproved(ctx.Message.CustomerId, Limit: 3000m));
        else
            await ctx.Publish(new CreditProposalRejected(ctx.Message.CustomerId, "Rejected by rule"));
    }
}
using Contracts.Messages;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<CardsIssuanceRequestedConsumer>(cfg =>
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

class CardsIssuanceRequestedConsumer : IConsumer<CardsIssuanceRequested>
{
    public async Task Consume(ConsumeContext<CardsIssuanceRequested> ctx)
    {
        var qty = Math.Max(1, ctx.Message.Quantity);
        for (int i = 0; i < qty; i++)
        {
            var cardId = Guid.NewGuid();
            var last4 = cardId.ToString("N")[..4];
            await ctx.Publish(new CardIssued(ctx.Message.CustomerId, cardId, last4));
        }
    }
}
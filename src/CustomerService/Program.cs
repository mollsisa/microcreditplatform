using Contracts.Messages;
using CustomerService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<CreditProposalApprovedConsumer>(cfg =>
    {
        cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
        cfg.UseInMemoryOutbox();
    });
    x.AddConsumer<CreditProposalRejectedConsumer>(cfg =>
    {
        cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
        cfg.UseInMemoryOutbox();
    });
    x.AddConsumer<CardIssuedConsumer>(cfg =>
    {
        cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
        cfg.UseInMemoryOutbox();
    });
    x.AddConsumer<CardIssuanceFailedConsumer>(cfg =>
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapPost("/customers", async (CustomerDto dto, AppDbContext db, IPublishEndpoint bus) =>
{
    var id = Guid.NewGuid();
    var customer = new Customer { Id = id, Name = dto.Name, Document = dto.Document, Email = dto.Email, Status = "PENDING" };
    db.Customers.Add(customer);
    await db.SaveChangesAsync();

    await bus.Publish(new CustomerRegistered(id, dto.Name, dto.Document, dto.Email));
    await bus.Publish(new CreditProposalRequested(id));

    return Results.Created($"/customers/{id}", new { customer.Id, customer.Status });
});

app.MapGet("/customers/{id}", async (Guid id, AppDbContext db) =>
{
    var c = await db.Customers.FindAsync(id);
    return c is not null ? Results.Ok(c) : Results.NotFound();
});

app.MapGet("/health", () => Results.Ok("ok"));

app.Run();

record CustomerDto(string Name, string Document, string Email);

class CreditProposalApprovedConsumer(AppDbContext db) : IConsumer<CreditProposalApproved>
{
    public async Task Consume(ConsumeContext<CreditProposalApproved> ctx)
    {
        var c = await db.Customers.FindAsync(ctx.Message.CustomerId);
        if (c is null) return;
        c.Status = "APPROVED";
        await db.SaveChangesAsync();

        await ctx.Publish(new CardsIssuanceRequested(ctx.Message.CustomerId, Quantity: 1));
    }
}

class CreditProposalRejectedConsumer(AppDbContext db) : IConsumer<CreditProposalRejected>
{
    public async Task Consume(ConsumeContext<CreditProposalRejected> ctx)
    {
        var c = await db.Customers.FindAsync(ctx.Message.CustomerId);
        if (c is null) return;
        c.Status = "REJECTED";
        await db.SaveChangesAsync();
    }
}

class CardIssuedConsumer(AppDbContext db) : IConsumer<CardIssued>
{
    public async Task Consume(ConsumeContext<CardIssued> ctx)
    {
        var c = await db.Customers.FindAsync(ctx.Message.CustomerId);
        if (c is null) return;
        c.Status = "READY";
        await db.SaveChangesAsync();
    }
}

class CardIssuanceFailedConsumer(AppDbContext db) : IConsumer<CardIssuanceFailed>
{
    public async Task Consume(ConsumeContext<CardIssuanceFailed> ctx)
    {
        var c = await db.Customers.FindAsync(ctx.Message.CustomerId);
        if (c is null) return;
        c.Status = "CARD_FAILED";
        await db.SaveChangesAsync();
    }
}
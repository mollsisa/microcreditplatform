namespace Contracts.Messages;

public record CustomerRegistered(Guid CustomerId, string Name, string Document, string Email);
public record CreditProposalRequested(Guid CustomerId);
public record CreditProposalApproved(Guid CustomerId, decimal Limit);
public record CreditProposalRejected(Guid CustomerId, string Reason);
public record CardsIssuanceRequested(Guid CustomerId, int Quantity);
public record CardIssued(Guid CustomerId, Guid CardId, string Last4);
public record CardIssuanceFailed(Guid CustomerId, string Reason);
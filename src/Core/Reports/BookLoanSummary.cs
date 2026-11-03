namespace Core.Reports;

public sealed record BookLoanSummary(
    string BookId, 
    string Title, 
    int TotalLoansCount, 
    bool IsCurrentlyIssued
);
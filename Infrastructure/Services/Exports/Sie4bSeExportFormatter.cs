// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data as a Swedish SIE 4B accounting exchange file (tagged plain text,
/// #SIETYP 4 - the transaction import/export variant consumed by Fortnox Bokforing and
/// Visma eEkonomi). Header block: #FLAGGA, #PROGRAM, #FORMAT PC8, #GEN, #SIETYP 4, #FNAMN,
/// followed by #KONTO chart-of-accounts declarations, followed by one #VER verification per
/// WorkExportEntry (and one further #VER per ExpensesExportEntry), each wrapped in braces and
/// containing exactly two #TRANS lines (debit/credit) with an empty object list "{}".
/// </summary>
/// <remarks>
/// Reconstructed from the official English "SIE file format Version 4B - 30 Sept. 2008" PDF
/// (SIE-Gruppen; docs/knowledge/export-samples/se-sie4b-format-english.pdf). Confirmed directly
/// from that spec: tagged-text format, one label per line starting with '#'; #FORMAT PC8 is the
/// only permitted character set (IBM PC-8 / codepage 437); dates are YYYYMMDD; amounts use '.' as
/// the decimal separator with a leading '-' for a credit/negative amount and no leading '+'; item
/// sub-entries (here: the #TRANS rows of a #VER) are wrapped in "{" and "}" standing alone on
/// their own line; a #TRANS object list is written as "{}" when there is no dimension/object
/// breakdown; a #VER's trailing optional fields (regdate, sign) may be omitted entirely, exactly
/// as shown in the spec's own example ("#VER '' '' 20080101 ''Postage''"); verifications within a
/// series must appear in ascending verification-number order, which this formatter guarantees via
/// a single running counter across all orders. Klacks has no real Swedish BAS chart of accounts,
/// so - mirroring the placeholder-account approach already used by MoveinIlExportFormatter and
/// BmdExportFormatter - the accounts below are placeholders only and have not been verified
/// against a real BAS95/BAS96/EUBAS97 chart or an actual Fortnox/Visma test import:
/// PlaceholderWagesAccount "7010" (personnel cost, debited for worked time), PlaceholderPayablesAccount
/// "2890" (other short-term liabilities, credited - the amount owed to the employee),
/// PlaceholderTaxableExpenseAccount "7690" (other taxable staff costs) and
/// PlaceholderNonTaxableExpenseAccount "7389" (non-taxable expense reimbursement), both debited
/// against the same PlaceholderPayablesAccount credit. The optional #KSUMMA control-summation
/// (CRC-32 over the file body, spec section 10) is not implemented. #KPTYP (chart-of-accounts
/// type) is left unspecified, so an importing program is to assume BAS95 per the spec's own
/// default rule. Text fields (#PROGRAM name, #FNAMN, #VER vertext) are always quoted;
/// numeric/identifier fields (account numbers, series, dates, amounts) are left unquoted, which
/// the spec explicitly permits ("quotation marks... are only required when the field contains
/// spaces").
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class Sie4bSeExportFormatter : IExportFormatter
{
    private const string DateFormat = "yyyyMMdd";
    private const string LineEnding = "\n";
    private const int Pc8CodePage = 437;
    private const string ProgramName = "Klacks";
    private const string ProgramVersion = "1.0";
    private const int SieFileType = 4;
    private const string VerificationSeries = "A";
    private const string EmptyObjectList = "{}";
    private const string SubEntryOpen = "{";
    private const string SubEntryClose = "}";
    private const string PlaceholderWagesAccount = "7010";
    private const string PlaceholderWagesAccountName = "Salaries and wages (placeholder)";
    private const string PlaceholderPayablesAccount = "2890";
    private const string PlaceholderPayablesAccountName = "Other short-term liabilities (placeholder)";
    private const string PlaceholderTaxableExpenseAccount = "7690";
    private const string PlaceholderTaxableExpenseAccountName = "Other taxable staff costs (placeholder)";
    private const string PlaceholderNonTaxableExpenseAccount = "7389";
    private const string PlaceholderNonTaxableExpenseAccountName = "Non-taxable expense reimbursement (placeholder)";

    public string FormatKey => ExportConstants.FormatSie4bSe;

    public string ContentType => ExportConstants.ContentTypeOctetStream;

    public string FileExtension => ".se";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();

        AppendLine(sb, "#FLAGGA 0");
        AppendLine(sb, $"#PROGRAM {Quote(ProgramName)} {ProgramVersion}");
        AppendLine(sb, "#FORMAT PC8");
        AppendLine(sb, $"#GEN {data.ExportDate.ToString(DateFormat, CultureInfo.InvariantCulture)}");
        AppendLine(sb, $"#SIETYP {SieFileType}");
        AppendLine(sb, $"#FNAMN {Quote(options.Company.Name)}");

        AppendLine(sb, $"#KONTO {PlaceholderWagesAccount} {Quote(PlaceholderWagesAccountName)}");
        AppendLine(sb, $"#KONTO {PlaceholderPayablesAccount} {Quote(PlaceholderPayablesAccountName)}");
        AppendLine(sb, $"#KONTO {PlaceholderTaxableExpenseAccount} {Quote(PlaceholderTaxableExpenseAccountName)}");
        AppendLine(sb, $"#KONTO {PlaceholderNonTaxableExpenseAccount} {Quote(PlaceholderNonTaxableExpenseAccountName)}");

        var verificationNumber = 1;

        foreach (var order in data.Orders)
        {
            foreach (var work in order.WorkEntries)
            {
                var totalAmount = work.WorkTime + work.Surcharges
                    + work.Changes.Sum(c => c.ChangeTime + c.Surcharges);

                AppendVerification(
                    sb,
                    verificationNumber,
                    work.WorkDate,
                    $"{work.EmployeeName} - {order.OrderName}",
                    PlaceholderWagesAccount,
                    PlaceholderPayablesAccount,
                    totalAmount);
                verificationNumber++;

                foreach (var expense in work.Expenses)
                {
                    var debitAccount = expense.Taxable
                        ? PlaceholderTaxableExpenseAccount
                        : PlaceholderNonTaxableExpenseAccount;

                    AppendVerification(
                        sb,
                        verificationNumber,
                        work.WorkDate,
                        $"{expense.Description} - {work.EmployeeName}",
                        debitAccount,
                        PlaceholderPayablesAccount,
                        expense.Amount);
                    verificationNumber++;
                }
            }
        }

        return Encoding.GetEncoding(Pc8CodePage).GetBytes(sb.ToString());
    }

    private static void AppendVerification(
        StringBuilder sb,
        int verificationNumber,
        DateOnly verificationDate,
        string verificationText,
        string debitAccount,
        string creditAccount,
        decimal amount)
    {
        var date = verificationDate.ToString(DateFormat, CultureInfo.InvariantCulture);

        AppendLine(sb, $"#VER {VerificationSeries} {verificationNumber} {date} {Quote(verificationText)}");
        AppendLine(sb, SubEntryOpen);
        AppendLine(sb, $"#TRANS {debitAccount} {EmptyObjectList} {FormatAmount(amount)}");
        AppendLine(sb, $"#TRANS {creditAccount} {EmptyObjectList} {FormatAmount(-amount)}");
        AppendLine(sb, SubEntryClose);
    }

    private static string FormatAmount(decimal amount)
    {
        var normalized = amount == 0m ? 0m : amount;
        return normalized.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static string Quote(string value)
    {
        var sanitized = value.Replace("\r", string.Empty).Replace("\n", string.Empty);
        return $"\"{sanitized.Replace("\"", "\\\"")}\"";
    }

    private static void AppendLine(StringBuilder sb, string line)
    {
        sb.Append(line);
        sb.Append(LineEnding);
    }
}


using iTextSharp.text;
using iTextSharp.text.pdf;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using System.Text;

namespace Klacks.Api.Infrastructure.Printing;

public static class PrintHelperModule
{
    public static dynamic PrepareImport()
    {

        dynamic values = new System.Dynamic.ExpandoObject();

        values.ClientType = 1;
        values.OwnerDefinedValue = 2;
        values.BindOR = 3;
        values.BindAND = 4;



        return values;

    }
    public static string GetAddressAsHtmlWrapString(Client item, Address address, string currentCountry)
    {
        var tmp = new StringBuilder();

        if (address != null)
        {
            if (address.Type == AddressTypeEnum.Customer)
            {
                if (!string.IsNullOrEmpty(item.Company)) { tmp.Append(item.Company); }
                if (!string.IsNullOrEmpty(tmp.ToString())) { tmp.Append("<br>"); }
                if (!string.IsNullOrEmpty(item.Title)) { tmp.Append(item.Title); }
                if (!string.IsNullOrEmpty(item.Title)) { tmp.Append("<br>"); }

                tmp.Append(item.FirstName);
                if (!string.IsNullOrEmpty(item.SecondName))
                {
                    tmp.Append(" ");
                    tmp.Append(item.SecondName.Substring(0, 1));
                    tmp.Append(". ");
                }
                else
                {
                    tmp.Append(" ");
                }
                tmp.Append(item.Name);


                if (!string.IsNullOrEmpty(address.Street)) { tmp.Append("<br>"); tmp.Append(address.Street); }
                if (!string.IsNullOrEmpty(address.Street2)) { tmp.Append("<br>"); tmp.Append(address.Street2); }
                if (!string.IsNullOrEmpty(address.Street3)) { tmp.Append("<br>"); tmp.Append(address.Street3); }
                tmp.Append("<br>");
                tmp.Append(address.Zip);
                tmp.Append(" ");
                tmp.Append(address.City);
                if (!string.IsNullOrEmpty(currentCountry)) { tmp.Append("<br>"); tmp.Append(currentCountry); }

                return tmp.ToString();
            }
            else
            {


                tmp.Append(address.AddressLine1);
                if (!string.IsNullOrEmpty(address.AddressLine2)) { tmp.Append("<br>"); tmp.Append(address.AddressLine2); }
                if (!string.IsNullOrEmpty(address.Street)) { tmp.Append("<br>"); tmp.Append(address.Street); }
                if (!string.IsNullOrEmpty(address.Street2)) { tmp.Append("<br>"); tmp.Append(address.Street2); }
                if (!string.IsNullOrEmpty(address.Street3)) { tmp.Append("<br>"); tmp.Append(address.Street3); }
                tmp.Append("<br>");
                tmp.Append(address.Zip);
                tmp.Append(" ");
                tmp.Append(address.City);
                if (!string.IsNullOrEmpty(currentCountry)) { tmp.Append("<br>"); tmp.Append(currentCountry); }

                return tmp.ToString();
            }

        }

        return "Keine Adresse";
    }
    public static string GetHtmlWrapString(string str)
    {
        if (!string.IsNullOrEmpty(str))
        {
            var tmp = new StringBuilder();

            str = str.Replace("\r\n", "\n");
            str = str.Replace("\r", "\n");
            var spl = str.Split("\n");

            foreach (var itm in spl)
            {
                if (!string.IsNullOrEmpty(tmp.ToString())) { tmp.Append("<br>"); }
                tmp.Append(itm);
            }


            return tmp.ToString();
        }
        return str;
    }
    public static int FrancAmounts(decimal value)
    {
        return (int)Math.Truncate(100 * value) / 100;
    }
    public static string CentAmounts(decimal value)
    {
        value = Math.Round(value, 2);
        var francs = FrancAmounts(value);
        var result = ((int)(100 * (value - francs))).ToString();
        return result.Length == 1 ? "0" + result : result;
    }
    public static (string, string) ReadGenderDetails(Client client)
    {
        var result = (gender: "", salutation: "Werte Damen und Herren ");
        switch (client.Gender)
        {
            case GenderEnum.Female:
                {
                    result.gender = "Frau";
                    result.salutation = "Sehr geehrte Frau " + client.Name;
                    break;
                }
            case GenderEnum.Male:
                {
                    result.gender = "Herr";
                    result.salutation = "Sehr geehrter Herr " + client.Name;
                    break;
                }

        }

        return result;
    }
    public static string ParseName(string value)
    {
        value = value.Replace(" ", "_");
        value = value.Replace("(", "_");
        value = value.Replace(")", "_");
        value = value.Replace("=", "_");
        value = value.Replace(">", "_");
        value = value.Replace("<", "_");
        value = value.Replace("/", "_");
        value = value.Replace("\\", "_");
        return value;
    }
    public static string GetDocumentDirectory(IConfiguration configuration)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var path = configuration["CurrentPaths:Documents"];
        var docuDirectory = Path.Combine(baseDirectory!, path);

        if (!Directory.Exists(docuDirectory)) { Directory.CreateDirectory(docuDirectory); }

        return docuDirectory;
    }

    public static string GetExportDirectory(IConfiguration configuration)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var path = configuration["CurrentPaths:Exports"];
        var docuDirectory = Path.Combine(baseDirectory!, path);

        if (!Directory.Exists(docuDirectory)) { Directory.CreateDirectory(docuDirectory); }

        return docuDirectory;
    }
    public static void MergeFiles(string destinationFile, string[] sourceFiles)
    {

        try
        {
            int f = 0;

            PdfReader reader = new PdfReader(sourceFiles[f]);
            int n = reader.NumberOfPages;


            Document document = new Document(reader.GetPageSizeWithRotation(1));
            var fileStream = new FileStream(destinationFile, FileMode.Create);
            PdfWriter writer = PdfWriter.GetInstance(document, fileStream);

            document.Open();
            PdfContentByte cb = writer.DirectContent;
            PdfImportedPage page;
            int rotation;

            while (f < sourceFiles.Length)
            {
                int i = 0;
                while (i < n)
                {
                    i++;
                    document.SetPageSize(reader.GetPageSizeWithRotation(i));
                    document.NewPage();
                    page = writer.GetImportedPage(reader, i);
                    rotation = reader.GetPageRotation(i);
                    if (rotation == 90 || rotation == 270)
                    {
                        cb.AddTemplate(page, 0, -1f, 1f, 0, 0, reader.GetPageSizeWithRotation(i).Height);
                    }
                    else
                    {
                        cb.AddTemplate(page, 1f, 0, 0, 1f, 0, 0);
                    }
                }
                f++;
                if (f < sourceFiles.Length)
                {
                    reader = new PdfReader(sourceFiles[f]);

                    n = reader.NumberOfPages;

                }
            }

            document.Close();
            reader.Close();
            writer.Close();
            fileStream.Dispose();


        }
        catch
        {
            // ignored
        }
    }
    public static Address ReadAddressDetails(Guid addressId, ICollection<Address> addresses, DateTime currentDate)
    {

        Address address = null!;
        if (addressId != Guid.Empty)
        {
            address = addresses.FirstOrDefault(x => x.Id == addressId)!;
        }


        if (address != null)
        {
            return address;
        }
        else
        {
            var current = addresses.Where(x => !x.IsDeleted && x.ValidFrom != null && x.ValidFrom.Value.Date <= currentDate.Date && x.Type == AddressTypeEnum.InvoicingAddress).OrderByDescending(x => x.ValidFrom).FirstOrDefault();
            if (current == null)
                current = addresses.Where(x => !x.IsDeleted && x.ValidFrom != null && x.ValidFrom.Value.Date <= currentDate.Date && x.Type == AddressTypeEnum.Customer).OrderByDescending(x => x.ValidFrom).FirstOrDefault();
            if (current == null)
                current = addresses.Where(x => !x.IsDeleted && x.ValidFrom != null && x.ValidFrom.Value.Date <= currentDate.Date).OrderByDescending(x => x.ValidFrom).FirstOrDefault();
            if (current != null)
            {
                return current;

            }

        }

        return null!;
    }
    public static Tuple<string, string> CreateFileName(string currentFolder)
    {
        string tmpFileName = Path.GetRandomFileName();
        tmpFileName = tmpFileName.Replace(tmpFileName.Substring(tmpFileName.Length - 4), ".pdf");
        string fileName = Path.Combine(currentFolder, tmpFileName);


        return new Tuple<string, string>(tmpFileName, fileName);

    }
    public static Tuple<string, string> CreateTmpFileName(IConfiguration configuration)
    {
        ClearEnvironment(configuration);
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var path = configuration["CurrentPaths:Temp"];
        var docuDirectory = Path.Combine(baseDirectory!, path);

        if (!Directory.Exists(docuDirectory)) { Directory.CreateDirectory(docuDirectory); }


        string tmpFileName = Path.GetRandomFileName();
        tmpFileName = tmpFileName.Replace(tmpFileName.Substring(tmpFileName.Length - 4), ".pdf");
        string fileName = Path.Combine(baseDirectory, tmpFileName);


        return new Tuple<string, string>(tmpFileName, fileName);

    }
    public static void ClearEnvironment(IConfiguration configuration)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var path = configuration["CurrentPaths:Temp"];
        var docuDirectory = Path.Combine(baseDirectory!, path);
        if (Directory.Exists(docuDirectory))
        {
            EmptyFolder(new DirectoryInfo(docuDirectory));
        }
    }
    private static void EmptyFolder(DirectoryInfo directoryInfo)
    {
        foreach (FileInfo file in directoryInfo.GetFiles())
        {
            file.Delete();
        }

        foreach (DirectoryInfo subfolder in directoryInfo.GetDirectories())
        {
            EmptyFolder(subfolder);
        }
    }
}
